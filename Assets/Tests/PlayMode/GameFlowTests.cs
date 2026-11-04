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

namespace AdventureGame.Tests
{
    /// <summary>Main sahnesini yükleyip oyunun iki bölümünü uçtan uca test eder.</summary>
    public class GameFlowTests
    {
        private GameManager _gm;
        private PlayerController _player;
        private DrawBridge _bridge;
        private InteractiveDoor _door;
        private GuardNPC _guard;
        private DialogueUI _ui;
        private NavMeshCompanionController _companion;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            _gm = GameManager.Instance;
            _player = Object.FindAnyObjectByType<PlayerController>();
            _bridge = Object.FindAnyObjectByType<DrawBridge>();
            _door = Object.FindAnyObjectByType<InteractiveDoor>();
            _guard = Object.FindAnyObjectByType<GuardNPC>();
            _ui = Object.FindAnyObjectByType<DialogueUI>();
            _companion = Object.FindAnyObjectByType<NavMeshCompanionController>();

            Assert.NotNull(_gm, "GameManager yok");
            Assert.NotNull(_player, "Player yok");
            Assert.NotNull(_bridge, "Köprü yok");
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
        public IEnumerator Bolum1_Yoldas_SensorleSalteriBulur_NavMeshIleTirmanir_KopruyuIndirir()
        {
            var states = new List<CompanionState>();
            var reports = new List<string>();
            _companion.StateChanged += states.Add;
            _companion.Reported += reports.Add;

            // Oyuncu uçurum kenarına gelir → ObstacleZone tetiklenir.
            _player.Teleport(new Vector3(0, 1, 7.5f));
            yield return TestUtil.WaitFor(() => _gm.CurrentObjective.Contains("Köprü kalkık"), 3f, "ObstacleZone tetiklenmedi");

            _gm.RequestCompanionHelp();

            float maxHeight = _companion.transform.position.y;
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!_bridge.IsActivated)
            {
                maxHeight = Mathf.Max(maxHeight, _companion.transform.position.y);
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Köprü 45 sn içinde inmedi. Raporlar: " + string.Join(" | ", reports));
                }

                yield return null;
            }

            Debug.Log("[TEST] Yoldaş raporları: " + string.Join(" | ", reports));
            Debug.Log($"[TEST] Yoldaşın ulaştığı en yüksek nokta: {maxHeight:0.00}");

            CollectionAssert.Contains(states, CompanionState.Scanning);
            CollectionAssert.Contains(states, CompanionState.MovingToTarget);
            CollectionAssert.Contains(states, CompanionState.Interacting);
            Assert.Greater(maxHeight, 3.5f, "Yoldaş platforma (y=3 + baseOffset) çıkmış olmalı");
            Assert.AreEqual(GamePhase.SocialObstacle, _gm.Phase);

            var path = new NavMeshPath();
            NavMesh.CalculatePath(new Vector3(0, 0, 0), new Vector3(0, 0, 25), NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "Köprü indikten sonra NavMesh yolu olmalı");
        }

        [UnityTest]
        public IEnumerator Bolum1_UcurumaDusenOyuncu_BaslangicaIsinlanir()
        {
            _player.Teleport(new Vector3(0, 2, 13.5f));
            yield return TestUtil.WaitFor(() => _player.transform.position.z < -4f, 6f, "Oyuncu yeniden doğmadı");
            Assert.Greater(_player.transform.position.y, 0f);
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
        }

        [UnityTest]
        public IEnumerator Bolum2_MockLLM_DogruCevap_KapiyiAcar_OyunTamamlanir()
        {
            yield return LowerBridge();
            InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();
            Assert.IsTrue(_gm.IsInputLocked, "Diyalogda oyuncu girdisi kilitli olmalı");
            StringAssert.Contains("Ben neyim", LogText, "Muhafız selamlama/bilmece göstermeli");

            yield return Talk("bilmiyorum");
            Assert.IsFalse(_door.IsOpen);

            yield return Talk("Cevap sessizlik!");
            yield return TestUtil.WaitFor(() => _door.IsOpen, 5f, "Kapı açılmadı");

            TestUtil.GetField<Button>(_ui, "closeButton").onClick.Invoke();
            Assert.IsFalse(_ui.IsOpen);
            Assert.IsFalse(_gm.IsInputLocked, "Diyalog kapanınca girdi açılmalı");

            _player.Teleport(new Vector3(0, 1.2f, 33.5f));
            yield return TestUtil.WaitFor(() => _gm.Phase == GamePhase.Completed, 3f, "GoalZone oyunu bitirmedi");
        }

        [UnityTest]
        public IEnumerator Bolum2_YanlisAsamadaOpenDoor_GameManagerTarafindanReddedilir()
        {
            // Köprü İNDİRİLMEDEN NPC open_door üretirse kapı açılmamalı.
            InjectMock(MockLLMService.MockMode.Normal);
            OpenDialogue();
            LogAssert.Expect(LogType.Warning, new Regex("reddedildi"));
            yield return Talk("sessizlik");
            yield return new WaitForSeconds(2f);
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

        private IEnumerator LowerBridge()
        {
            _bridge.Activate();
            yield return TestUtil.WaitFor(() => _bridge.IsActivated, 10f, "Köprü inmedi");
            Assert.AreEqual(GamePhase.SocialObstacle, _gm.Phase);
        }

        private MockLLMService InjectMock(MockLLMService.MockMode mode)
        {
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
