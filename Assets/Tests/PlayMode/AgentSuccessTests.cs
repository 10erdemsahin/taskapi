using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AdventureGame.Tests
{
    /// <summary>
    /// Ders dokümanı 4.4: otonom yoldaşın farklı başlangıç konumlarında tekrarlanabilir başarı testi.
    /// Her konum <see cref="TrialsPerStart"/> kez denenir. Oyuncu basınç plakasında bekler; [F] ile yardım
    /// istenir. Başarı = yoldaş şalteri sensörle bulur, NavMesh (+ link zıplaması) ile gider, çeker ve
    /// köprü iner. Süre: [F]'den köprünün inmesine kadar (2.5 sn iniş animasyonu dahil).
    /// Sonuç tablosu Docs/Tests/ajan_basari_sonuclari.md dosyasına yazılır (README 9.2).
    /// </summary>
    public class AgentSuccessTests
    {
        private const int TrialsPerStart = 3;
        private const float TrialTimeoutSeconds = 45f;
        private static readonly Vector3 PlateStandPosition = new Vector3(6f, 1f, 8f);

        /// <summary>NavMeshCompanionController.FailTask mesajlarından parçalar: görev başarısız bitti.</summary>
        private static readonly string[] FailureMarkers =
        {
            "Beni başka bir noktaya götür", "rota oluşturamadım", "Takıldım", "geçersiz hâle geldi",
            "artık kullanılamıyor", "uzanamıyorum"
        };

        public sealed class StartCase
        {
            public StartCase(string name, Vector3 position, bool expectSuccess)
            {
                Name = name;
                Position = position;
                ExpectSuccess = expectSuccess;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public bool ExpectSuccess { get; }
            public override string ToString() => Name;
        }

        private static readonly StartCase[] Starts =
        {
            new StartCase("Basınç plakası yanı (5, 0, 7)", new Vector3(5f, 0f, 7f), true),
            new StartCase("Alan ortası (0, 0, 4)", new Vector3(0f, 0f, 4f), true),
            new StartCase("Sağ kenar (8, 0, 2)", new Vector3(8f, 0f, 2f), true),
            new StartCase("Platform önü (-3, 0, 8)", new Vector3(-3f, 0f, 8f), true),
            new StartCase("Oyun başlangıcı, sütun arkası (2, 0, -5)", new Vector3(2f, 0f, -5f), false),
        };

        private sealed class TrialResult
        {
            public StartCase Start;
            public bool Success;
            public float Seconds;
            public string Note;
        }

        private static readonly List<TrialResult> Results = new List<TrialResult>();

        [UnityTest]
        public IEnumerator Yoldas_FarkliBaslangicKonumlarinda_SalteriBulupKopruyuIndirir(
            [ValueSource(nameof(Starts))] StartCase start,
            [Values(1, 2, 3)] int trial)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            var gm = GameManager.Instance;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var bridge = Object.FindAnyObjectByType<DrawBridge>();
            var companion = Object.FindAnyObjectByType<NavMeshCompanionController>();
            Assert.NotNull(gm);
            Assert.NotNull(player);
            Assert.NotNull(bridge);
            Assert.NotNull(companion);

            // [F] akışını ölçüyoruz: otonom tarama görevi kendiliğinden başlatmasın.
            TestUtil.SetField(companion, "autoSolveWhenSeen", false);

            player.Teleport(PlateStandPosition);
            var agent = companion.GetComponent<NavMeshAgent>();
            Assert.IsTrue(NavMesh.SamplePosition(start.Position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas),
                $"Başlangıç konumu NavMesh üzerinde değil: {start.Position}");
            Assert.IsTrue(agent.Warp(hit.position), "Yoldaş başlangıç konumuna konamadı");
            yield return new WaitForFixedUpdate();
            yield return null;

            var reports = new List<string>();
            companion.Reported += reports.Add;

            float startTime = Time.time;
            float deadline = Time.realtimeSinceStartup + TrialTimeoutSeconds;
            gm.RequestCompanionHelp();

            string note = null;
            while (!bridge.IsActivated)
            {
                if (reports.Any(r => FailureMarkers.Any(r.Contains)))
                {
                    note = reports.Last();
                    break;
                }

                if (Time.realtimeSinceStartup > deadline)
                {
                    note = "Zaman aşımı";
                    break;
                }

                yield return null;
            }

            var result = new TrialResult
            {
                Start = start,
                Success = bridge.IsActivated,
                Seconds = Time.time - startTime,
                Note = note ?? string.Empty
            };
            Results.Add(result);

            Debug.Log($"[AJAN] {start.Name} #{trial}: {(result.Success ? "BAŞARILI" : "BAŞARISIZ")} " +
                      $"{result.Seconds:0.00} sn | Raporlar: {string.Join(" | ", reports)}");

            Assert.AreEqual(start.ExpectSuccess, result.Success,
                $"{start.Name}: beklenen {(start.ExpectSuccess ? "başarı" : "başarısızlık")}. Raporlar: {string.Join(" | ", reports)}");
        }

        [OneTimeTearDown]
        public void WriteReport()
        {
            if (Results.Count == 0)
            {
                return;
            }

            CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
            var md = new StringBuilder();
            md.AppendLine("# Otonom Yoldaş Başarı Testi (ders dokümanı 4.4)");
            md.AppendLine();
            md.AppendLine($"Otomatik üretildi: `AgentSuccessTests` (PlayMode), {System.DateTime.Now.ToString("d MMMM yyyy HH:mm", tr)}.");
            md.AppendLine("Oyuncu basınç plakasında bekler, [F] ile yardım istenir. Başarı: yoldaş şalteri sensörle bulur,");
            md.AppendLine("NavMesh ve platform zıplamasıyla gider, çeker ve köprü iner. Süre [F]'den köprünün inmesine kadardır");
            md.AppendLine("(2,5 sn iniş animasyonu dahil). Başarısız olması beklenen konumda yoldaş şalteri sütun yüzünden göremez.");
            md.AppendLine();
            md.AppendLine("| Başlangıç konumu | Deneme | Başarı | Ort. süre (sn) | Beklenen |");
            md.AppendLine("|---|---|---|---|---|");

            foreach (var group in Results.GroupBy(r => r.Start))
            {
                int trials = group.Count();
                int ok = group.Count(r => r.Success);
                string avg = ok > 0
                    ? group.Where(r => r.Success).Average(r => r.Seconds).ToString("0.0", tr)
                    : "—";
                bool asExpected = group.All(r => r.Success == group.Key.ExpectSuccess);
                md.AppendLine($"| {group.Key.Name} | {trials} | {ok}/{trials} | {avg} | " +
                              $"{(group.Key.ExpectSuccess ? "Başarı" : "Başarısızlık")} {(asExpected ? "✅" : "❌")} |");
            }

            var successes = Results.Where(r => r.Start.ExpectSuccess).ToList();
            if (successes.Count > 0)
            {
                int ok = successes.Count(r => r.Success);
                string avg = ok > 0 ? successes.Where(r => r.Success).Average(r => r.Seconds).ToString("0.0", tr) : "—";
                md.AppendLine();
                md.AppendLine($"**Ulaşılabilir konumlarda toplam:** {ok}/{successes.Count} başarılı " +
                              $"(%{100f * ok / successes.Count:0}), ortalama {avg} sn.");
            }

            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Tests"));
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "ajan_basari_sonuclari.md"), md.ToString(), new UTF8Encoding(false));
            }
            catch (IOException exception)
            {
                Debug.LogWarning("Ajan başarı raporu yazılamadı: " + exception.Message);
            }

            Results.Clear();
        }
    }
}
