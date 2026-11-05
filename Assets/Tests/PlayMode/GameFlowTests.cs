using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AdventureGame.Tests
{
    /// <summary>Main sahnesini yükleyip oyunun iki bölümünü uçtan uca test eder.</summary>
    public class GameFlowTests
    {
        private static readonly Vector3 PlateStandPosition = new Vector3(6f, 1f, 8f);

        private GameManager _gm;
        private PlayerController _player;
        private DrawBridge _bridge;
        private AndGate _bridgeLock;
        private InteractiveDoor _door;
        private GuardNPC _guard;
        private DialogueUI _ui;
        private NavMeshCompanionController _companion;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            _gm = GameManager.Instance;
            _player = Object.FindAnyObjectByType<PlayerController>();
            _bridge = Object.FindAnyObjectByType<DrawBridge>();
            _bridgeLock = Object.FindAnyObjectByType<AndGate>();
            _door = Object.FindAnyObjectByType<InteractiveDoor>();
            _guard = Object.FindAnyObjectByType<GuardNPC>();
            _ui = Object.FindAnyObjectByType<DialogueUI>();
            _companion = Object.FindAnyObjectByType<NavMeshCompanionController>();

            Assert.NotNull(_gm, "GameManager yok");
            Assert.NotNull(_player, "Player yok");
            Assert.NotNull(_bridge, "Köprü yok");
            Assert.NotNull(_bridgeLock, "Köprü kilidi (AndGate) yok");
            Assert.NotNull(_door, "Kapı yok");
            Assert.NotNull(_guard, "Muhafız yok");
            Assert.NotNull(_ui, "DialogueUI yok");
            Assert.NotNull(_companion, "Yoldaş yok");
        }

        // ------------------------------------------------------------------ BÖLÜM 1

        [UnityTest]
        public IEnumerator Bolum1_KopruKalkikken_KarsiyaNavMeshYoluYok_AmaPlatformaLinkVar()
        {
            Assert.AreEqual(GamePhase.PhysicalObstacle, _gm.Phase);

            var path = new NavMeshPath();
            NavMesh.CalculatePath(new Vector3(0, 0, 0), new Vector3(0, 0, 25), NavMesh.AllAreas, path);
            Assert.AreNotEqual(NavMeshPathStatus.PathComplete, path.status, "Köprü kalkıkken karşıya yol olmamalı");

            Assert.IsTrue(NavMesh.SamplePosition(new Vector3(-7.5f, 3f, 5f), out NavMeshHit top, 1f, NavMesh.AllAreas),
                "Platform üstünde NavMesh olmalı");
            NavMesh.CalculatePath(new Vector3(0, 0, 0), top.position, NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "NavMeshLink ile platforma tam yol olmalı");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bolum1_GizliSalter_YoldasBaslangictanGoremez()
        {
            var reports = new List<string>();
            _companion.Reported += reports.Add;

            _gm.RequestCompanionHelp();
            yield return null;

            Assert.AreNotEqual(CompanionState.MovingToTarget, _companion.State,
                "Sütun şalteri kapatıyor; yoldaş başlangıçta hedef bulamamalı. Raporlar: " + string.Join(" | ", reports));
            Assert.IsTrue(reports.Exists(r => r.Contains("göremiyorum") || r.Contains("Görebildiğim")),
                "Yoldaş göremediğini söylemeli. Raporlar: " + string.Join(" | ", reports));
        }

        [UnityTest]
        public IEnumerator Bolum1_Yoldas_SalteriCeker_OyuncuPlakada_KopruIner()
        {
            // Bu test [F] akışını ölçer: otonom tarama, yoldaş oyuncuya yetişirken görevi önceden başlatmasın.
            TestUtil.SetField(_companion, "autoSolveWhenSeen", false);

            var states = new List<CompanionState>();
            var reports = new List<string>();
            _companion.StateChanged += states.Add;
            _companion.Reported += reports.Add;

            // Oyuncu basınç plakasına çıkar (ObstacleZone da tetiklenir); yoldaş peşinden gelir.
            _player.Teleport(PlateStandPosition);
            yield return TestUtil.WaitFor(() => _gm.CurrentObjective.Contains("Köprü kalkık"), 3f, "ObstacleZone tetiklenmedi");
            yield return TestUtil.WaitFor(
                () => Vector3.Distance(_companion.transform.position, _player.transform.position) < 5f, 15f,
                "Yoldaş oyuncunun yanına gelmedi");

            _gm.RequestCompanionHelp();

            float maxHeight = _companion.transform.position.y;
            float deadline = Time.realtimeSinceStartup + 60f;
            while (!_bridge.IsActivated)
            {
                maxHeight = Mathf.Max(maxHeight, _companion.transform.position.y);
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Köprü 60 sn içinde inmedi. Raporlar: " + string.Join(" | ", reports));
                }

                yield return null;
            }

            Debug.Log("[TEST] Yoldaş raporları: " + string.Join(" | ", reports));
            Debug.Log($"[TEST] Yoldaşın ulaştığı en yüksek nokta: {maxHeight:0.00}");

            CollectionAssert.Contains(states, CompanionState.Scanning);
            CollectionAssert.Contains(states, CompanionState.MovingToTarget);
            CollectionAssert.Contains(states, CompanionState.Interacting);
            Assert.IsTrue(_bridgeLock.IsLeverPulled, "Şalter yoldaş tarafından çekilmiş olmalı");
            Assert.Greater(maxHeight, 3.5f, "Yoldaş platforma (y=3 + baseOffset) çıkmış olmalı");
            Assert.AreEqual(GamePhase.SocialObstacle, _gm.Phase);

            var path = new NavMeshPath();
            NavMesh.CalculatePath(new Vector3(0, 0, 0), new Vector3(0, 0, 25), NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "Köprü indikten sonra NavMesh yolu olmalı");
        }

        [UnityTest]
        public IEnumerator Bolum1_GuvenlikAgi_BariyerUstundenDusenOyuncuBasaDoner()
        {
            // Bariyerler normalde düşmeyi engeller; bu test KillZone güvenlik ağını sınar.
            _player.Teleport(new Vector3(0, 2, 13.5f));
            yield return TestUtil.WaitFor(() => _player.transform.position.z < -4f, 6f, "Oyuncu yeniden doğmadı");
            Assert.Greater(_player.transform.position.y, 0f);
        }

        [UnityTest]
        public IEnumerator Bolum1_KopruIndiktenSonra_YoldasYineCagrilabilir()
        {
            // Otonom tarama kapalı: yoldaş köprü inerken kendiliğinden göreve başlayıp [F]'yi "meşgul" ile reddetmesin.
            TestUtil.SetField(_companion, "autoSolveWhenSeen", false);
            yield return LowerBridge();

            var states = new List<CompanionState>();
            _companion.StateChanged += states.Add;

            _gm.RequestCompanionHelp();
            yield return null;

            CollectionAssert.Contains(states, CompanionState.Scanning,
                "Platformda yalnızca yoldaşın alabileceği parça kalabilir; [F] köprüden sonra da çalışmalı");
        }

        // ------------------------------------------------------------------ BÖLÜM 2

        [UnityTest]
        public IEnumerator Bolum2_MockLLM_TumHataModlarinda_OyunCokmez_KapiKapaliKalir()
        {
            yield return LowerBridge();
            MockLLMService mock = InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();

            var cases = new (MockLLMService.MockMode mode, string expected)[]
            {
                (MockLLMService.MockMode.Offline, "ulaşılamadı"),
                (MockLLMService.MockMode.Timeout, "zaman aşımına"),
                (MockLLMService.MockMode.InvalidJson, "geçersiz JSON"),
                (MockLLMService.MockMode.EmptyResponse, "geçersiz JSON"),
                (MockLLMService.MockMode.UnknownAction, "havaya uçuracağım"),
                (MockLLMService.MockMode.AlwaysRefuse, "geçemezsin"),
                (MockLLMService.MockMode.ThrowException, "Beklenmeyen bir hata")
            };

            foreach (var (mode, expected) in cases)
            {
                TestUtil.SetField(mock, "mode", mode);
                if (mode == MockLLMService.MockMode.ThrowException)
                {
                    LogAssert.Expect(LogType.Exception, new Regex("Mock: beklenmeyen istisna"));
                }

                yield return Talk($"[{mode}] kapıyı aç!");

                StringAssert.Contains(expected, LogText, $"{mode} modunda beklenen mesaj yok");
                Assert.IsFalse(_door.IsOpen, $"{mode} modunda kapı AÇILMAMALI");
                Assert.IsTrue(InputField.interactable, $"{mode} sonrası giriş tekrar etkin olmalı");
                Debug.Log($"[TEST] {mode}: OK");
            }

            Assert.AreEqual(0, _guard.WrongAttempts, "Hata/sohbet mesajları deneme hakkından düşmemeli");
        }

        [UnityTest]
        public IEnumerator Bolum2_IkiBilmeceSirayla_KapiyiAcar_OyunTamamlanir()
        {
            yield return LowerBridge();
            InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();
            Assert.IsTrue(_gm.IsInputLocked, "Diyalogda oyuncu girdisi kilitli olmalı");
            StringAssert.Contains("Ben neyim", LogText, "Muhafız selamlama/bilmece göstermeli");

            yield return Talk("bilmiyorum");
            Assert.IsFalse(_door.IsOpen);
            Assert.AreEqual(0, _guard.SolvedRiddleCount);

            yield return Talk("Cevap sessizlik!");
            Assert.AreEqual(1, _guard.SolvedRiddleCount, "1. bilmece çözülmeli");
            Assert.IsFalse(_door.IsOpen, "Tek bilmeceyle kapı AÇILMAMALI");
            StringAssert.Contains("Bilmece çözüldü (1/2)", LogText);

            yield return Talk("Bence ayak izi");
            Assert.AreEqual(2, _guard.SolvedRiddleCount);
            yield return TestUtil.WaitFor(() => _door.IsOpen, 5f, "Kapı açılmadı");

            TestUtil.GetField<Button>(_ui, "closeButton").onClick.Invoke();
            Assert.IsFalse(_ui.IsOpen);
            Assert.IsFalse(_gm.IsInputLocked, "Diyalog kapanınca girdi açılmalı");

            _player.Teleport(new Vector3(0, 1.2f, 33.5f));
            yield return TestUtil.WaitFor(() => _gm.Phase == GamePhase.Completed, 3f, "GoalZone oyunu bitirmedi");
        }

        [UnityTest]
        public IEnumerator Bolum2_IkinciCevapOnceVerilirse_SayilmazKapiAcilmaz()
        {
            yield return LowerBridge();
            InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();

            // Mock "çözüldü + open_door" diyor ama sıradaki (1.) bilmecenin cevabı mesajda yok → Unity reddeder.
            yield return Talk("ayak izi");
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(0, _guard.SolvedRiddleCount, "Sıra dışı cevap sayılmamalı");
            Assert.IsFalse(_door.IsOpen);
        }

        [UnityTest]
        public IEnumerator Bolum2_UcYanlisCevap_BeklemeSuresi_MesajLLMeGitmez()
        {
            yield return LowerBridge();
            MockLLMService mock = InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();

            yield return Talk("cevap ayna");
            StringAssert.Contains("Kalan hak: 2", LogText);
            yield return Talk("cevap gölge");
            StringAssert.Contains("Kalan hak: 1", LogText);
            yield return Talk("cevap rüzgar");

            Assert.IsTrue(_guard.IsOnCooldown, "3 yanlıştan sonra bekleme başlamalı");
            int callsBefore = mock.CallCount;

            // Bekleme sırasında mesaj LLM'e gitmez, "düşünüyor" da gösterilmez.
            InputField.text = "cevap sessizlik";
            InputField.onSubmit.Invoke("cevap sessizlik");
            yield return null;

            Assert.AreEqual(callsBefore, mock.CallCount, "Bekleme sırasında LLM çağrılmamalı");
            StringAssert.Contains("Muhafız seni dinlemiyor", LogText);
            Assert.AreEqual(0, _guard.SolvedRiddleCount);
        }

        [UnityTest]
        public IEnumerator Bolum2_CevapSizintisi_OyuncuyaGosterilmez()
        {
            yield return LowerBridge();
            MockLLMService mock = InjectMock(MockLLMService.MockMode.LeakAnswer);
            OpenDialogue();

            yield return Talk("bir ipucu verir misin?");

            Assert.AreEqual(2, mock.CallCount, "Sızıntıda istek bir kez tekrar edilmeli");
            Assert.IsFalse(LogText.ToLowerInvariant().Contains("sessizlik"), "Cevap ekranda görünmemeli: " + LogText);
        }

        [UnityTest]
        public IEnumerator Bolum2_YankiTasiEksikken_KapiAcilmaz_TamamlanincaAcilir()
        {
            var quest = new GameObject("FakeQuest").AddComponent<FakeQuest>();
            _gm.BindCollectionQuest(quest);

            yield return LowerBridge();
            MockLLMService mock = InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();

            yield return Talk("cevap sessizlik");
            StringAssert.Contains("Yankı Taşı parçaları: 0/5 (TAMAMLANMADI", mock.LastRequest.Prompt, "Muhafız parça durumunu bilmeli");

            LogAssert.Expect(LogType.Warning, new Regex("Yankı Taşı parçaları eksik"));
            yield return Talk("ayak izi");
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(2, _guard.SolvedRiddleCount);
            Assert.IsFalse(_door.IsOpen, "Parçalar eksikken kapı AÇILMAMALI (LLM ne derse desin)");

            quest.CompleteAll();
            Assert.IsTrue(_gm.IsCollectionQuestSatisfied);

            yield return Talk("ayak izi");
            StringAssert.Contains("Yankı Taşı parçaları: 5/5 (TAMAMLANDI", mock.LastRequest.Prompt);
            yield return TestUtil.WaitFor(() => _door.IsOpen, 5f, "Parçalar tamamlanınca kapı açılmalı");
        }

        [UnityTest]
        public IEnumerator Bolum2_YanlisAsamadaOpenDoor_GameManagerTarafindanReddedilir()
        {
            // Köprü İNDİRİLMEDEN iki bilmece çözülse bile kapı açılmamalı.
            InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();
            yield return Talk("sessizlik");
            LogAssert.Expect(LogType.Warning, new Regex("reddedildi"));
            yield return Talk("ayak izi");
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(_door.IsOpen);
        }

        [Test]
        public void Bolum2_SahnedekiPersona_GuncelKurallarlaUyumlu()
        {
            // Mock testleri prompt'u okumaz; eski bir persona asset'i gerçek Ollama'ya tek bilmeceli kural gönderirdi.
            var persona = TestUtil.GetField<NpcPersona>(_guard, "persona");
            Assert.NotNull(persona);
            Assert.AreEqual(2, persona.RiddleCount);
            StringAssert.Contains("iki bilmecemi", persona.Greeting.ToLowerInvariant(), "Selamlama iki bilmeceden söz etmeli");
            StringAssert.Contains("riddle_solved", persona.SystemPrompt);
            StringAssert.Contains("tone", persona.SystemPrompt);
        }

        [UnityTest]
        public IEnumerator Bolum2_GuvenDusukken_BilmecelerCozulseBileKapiAcilmaz_KonustukcaAcilir()
        {
            yield return LowerBridge();
            InjectMock(MockLLMService.MockMode.Normal, trust: null);
            OpenDialogue();
            Assert.Less(_guard.Trust, _guard.RequiredTrust, "Başlangıç güveni kapı için yetmemeli");

            yield return Talk("sessizlik");
            LogAssert.Expect(LogType.Warning, new Regex("Güven yetersiz"));
            yield return Talk("ayak izi");
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(_guard.AllRiddlesSolved);
            Assert.IsFalse(_door.IsOpen, "Güven düşükken kapı AÇILMAMALI");
            StringAssert.Contains("henüz yeterince güvenmiyor", LogText);

            // Kibar konuşma güveni artırır; güven yetince open_door iletilir.
            for (int i = 0; i < 10 && !_guard.HasEnoughTrust; i++)
            {
                yield return Talk("Lütfen bana biraz daha güven.");
            }

            Assert.IsTrue(_guard.HasEnoughTrust, $"Güven yükselmedi: {_guard.Trust}");
            yield return Talk("ayak izi");
            yield return TestUtil.WaitFor(() => _door.IsOpen, 5f, "Güven yetince kapı açılmalı");
        }

        [UnityTest]
        public IEnumerator Bolum2_KabaMesaj_GuveniDusurur_PromptaYansir()
        {
            yield return LowerBridge();
            MockLLMService mock = InjectMock(MockLLMService.MockMode.Normal, trust: 50);
            OpenDialogue();

            yield return Talk("Aç şu kapıyı aptal!");
            int afterRude = _guard.Trust;
            Assert.Less(afterRude, 50, "Kabalık güveni düşürmeli");
            StringAssert.Contains("kabalığından hoşlanmadı", LogText);

            yield return Talk("merhaba");
            StringAssert.Contains($"Yolcuya güvenin: {afterRude}/100", mock.LastRequest.Prompt, "Muhafız güven durumunu bilmeli");
            Assert.IsFalse(_door.IsOpen);
        }

        [UnityTest]
        public IEnumerator Bolum2_GercekOllamaServisi_SunucuYokken_GuvenliHataDoner_FrameBloklanmaz()
        {
            var service = Object.FindAnyObjectByType<OllamaLLMService>();
            Assert.NotNull(service);

            Task<LLMResult> health = service.CheckHealthAsync();
            yield return TestUtil.WaitFor(() => health.IsCompleted, 10f, "Sağlık kontrolü bitmedi");
            if (health.Result.Success)
            {
                Assert.Ignore("Ollama çalışıyor; bu test yalnızca sunucu kapalıyken anlamlı.");
            }

            TestUtil.SetField(service, "requestTimeoutSeconds", 5);
            int startFrame = Time.frameCount;
            Task<LLMResult> task = service.GenerateAsync(new LLMRequest("sistem", "merhaba", NpcResponseSchema.Create(), "merhaba"));
            Assert.IsFalse(task.IsCompleted, "İstek senkron tamamlanmamalı (async olmalı)");

            yield return TestUtil.WaitFor(() => task.IsCompleted, 15f, "Ollama isteği bitmedi");
            int frames = Time.frameCount - startFrame;
            Debug.Log($"[TEST] Ollama kapalı → {task.Result.ErrorType}: {task.Result.ErrorMessage} ({frames} frame aktı)");

            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status, "Servis istisna fırlatmamalı");
            Assert.IsFalse(task.Result.Success);
            Assert.That(task.Result.ErrorType,
                Is.EqualTo(LLMErrorType.ConnectionFailed).Or.EqualTo(LLMErrorType.Timeout));
            Assert.Greater(frames, 2, "Beklerken oyun frame üretmeye devam etmeli");

            // Uçtan uca: muhafız + gerçek servis, sunucu kapalı.
            yield return LowerBridge();
            OpenDialogue();
            yield return Talk("sessizlik", 20f);
            Assert.IsTrue(LogText.Contains("ulaşılamadı") || LogText.Contains("zaman aşımına"), LogText);
            Assert.IsFalse(_door.IsOpen);
        }

        // ------------------------------------------------------------------ Yardımcılar

        private TMP_InputField InputField => TestUtil.GetField<TMP_InputField>(_ui, "inputField");
        private string LogText => TestUtil.GetField<TMP_Text>(_ui, "logText").text;

        /// <summary>Köprüyü yoldaşsız indirir: şalter (AndGate.Activate) + oyuncu plakada.</summary>
        private IEnumerator LowerBridge()
        {
            _player.Teleport(PlateStandPosition);
            yield return new WaitForFixedUpdate();
            yield return null;
            _bridgeLock.Activate();
            yield return TestUtil.WaitFor(() => _bridge.IsActivated, 10f, "Köprü inmedi");
            Assert.AreEqual(GamePhase.SocialObstacle, _gm.Phase);
        }

        /// <param name="trust">
        /// Muhafızın başlangıç güveni. Varsayılan 100: güvenle ilgisi olmayan testler güven kuralına takılmasın.
        /// null → persona'daki gerçek başlangıç değeri (güven testleri için).
        /// </param>
        private MockLLMService InjectMock(MockLLMService.MockMode mode, int? trust = 100)
        {
            if (trust.HasValue)
            {
                TestUtil.SetField(_guard, "_trust", trust.Value);
            }

            MockLLMService mock = _guard.gameObject.AddComponent<MockLLMService>();
            TestUtil.SetField(mock, "mode", mode);
            TestUtil.SetField(mock, "simulatedDelaySeconds", 0.6f);
            TestUtil.SetField(mock, "simulatedTimeoutSeconds", 1f);
            TestUtil.SetField(_guard, "_llm", mock);
            return mock;
        }

        private void OpenDialogue()
        {
            _guard.Interact(_player.gameObject, InteractorType.Player);
            Assert.IsTrue(_ui.IsOpen, "Diyalog penceresi açılmalı");
        }

        /// <summary>UI üzerinden mesaj gönderir; "düşünüyor" göstergesini ve frame akışını doğrular.</summary>
        private IEnumerator Talk(string message, float timeout = 10f)
        {
            GameObject thinking = TestUtil.GetField<GameObject>(_ui, "thinkingIndicator");
            TMP_InputField input = InputField;

            input.text = message;
            input.onSubmit.Invoke(message);

            Assert.IsTrue(thinking.activeSelf, "Yanıt beklenirken 'düşünüyor' göstergesi görünmeli");
            Assert.IsFalse(input.interactable, "Yanıt beklenirken giriş kilitli olmalı");

            int startFrame = Time.frameCount;
            yield return TestUtil.WaitFor(() => !TestUtil.GetField<bool>(_guard, "_isAwaitingResponse"), timeout, "NPC yanıtı gelmedi");
            Assert.Greater(Time.frameCount - startFrame, 2, "Bekleme sırasında frame akmalı (ana thread bloklanmamalı)");
            Assert.IsFalse(thinking.activeSelf, "Yanıt sonrası gösterge kapanmalı");
        }
    }
}
