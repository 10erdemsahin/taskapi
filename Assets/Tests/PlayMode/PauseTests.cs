using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AdventureGame.Tests
{
    /// <summary>GameManager.SetPaused API'si (Ömer'in duraklatma menüsü buna dayanıyor) ve ayar okuma.</summary>
    public class PauseTests
    {
        private GameManager _gm;
        private PlayerController _player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            _gm = GameManager.Instance;
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.NotNull(_gm);
            Assert.NotNull(_player);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator SetPaused_ZamaniDurdurur_GirdiyiKilitler_DevamEdinceAcar()
        {
            bool? lastEvent = null;
            _gm.PausedChanged += paused => lastEvent = paused;

            Assert.IsFalse(_gm.IsPaused);
            Assert.IsFalse(_gm.IsInputLocked);

            _gm.SetPaused(true);
            Assert.IsTrue(_gm.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(_gm.IsInputLocked, "Duraklatınca girdi kilitlenmeli");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Menü için imleç serbest olmalı");
            Assert.AreEqual(true, lastEvent);

            yield return null;

            _gm.SetPaused(false);
            Assert.IsFalse(_gm.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(_gm.IsInputLocked, "Devam edince girdi açılmalı");
            Assert.AreEqual(false, lastEvent);
        }

        [UnityTest]
        public IEnumerator SetPaused_AyniDegerIkiKez_OlayiTekrarTetiklemez()
        {
            int count = 0;
            _gm.PausedChanged += _ => count++;

            _gm.SetPaused(true);
            _gm.SetPaused(true);
            _gm.SetPaused(false);
            _gm.SetPaused(false);

            Assert.AreEqual(2, count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DiyalogSirasindaDuraklatilipDevamEdilirse_GirdiKilitliKalir()
        {
            GuardNPC guard = Object.FindAnyObjectByType<GuardNPC>();
            DialogueUI ui = Object.FindAnyObjectByType<DialogueUI>();

            guard.Interact(_player.gameObject, InteractorType.Player);
            Assert.IsTrue(_gm.IsInputLocked, "Diyalog açıkken girdi kilitli");

            _gm.SetPaused(true);
            _gm.SetPaused(false);
            Assert.IsTrue(_gm.IsInputLocked, "Duraklatmadan çıkınca diyalog hâlâ açık → kilit sürmeli");
            Assert.AreEqual(1f, Time.timeScale);

            TestUtil.GetField<Button>(ui, "closeButton").onClick.Invoke();
            Assert.IsFalse(_gm.IsInputLocked, "Diyalog kapanınca girdi açılmalı");
            yield return null;
        }

        [UnityTest]
        public IEnumerator DuraklatilmisSahneDegisirse_ZamanNormaleDoner()
        {
            _gm.SetPaused(true);
            Assert.AreEqual(0f, Time.timeScale);

            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            Assert.AreEqual(1f, Time.timeScale, "Yeni sahne donuk başlamamalı");
            Assert.IsFalse(GameManager.Instance.IsPaused);
        }

        [UnityTest]
        public IEnumerator KayitliFareHassasiyeti_OyuncuTarafindanOkunur()
        {
            bool hadKey = PlayerPrefs.HasKey(PlayerController.MouseSensitivityKey);
            float previous = PlayerPrefs.GetFloat(PlayerController.MouseSensitivityKey, 0f);

            try
            {
                PlayerPrefs.SetFloat(PlayerController.MouseSensitivityKey, 3.25f);
                SceneManager.LoadScene("Main");
                yield return null;
                yield return null;

                var player = Object.FindAnyObjectByType<PlayerController>();
                Assert.AreEqual(3.25f, TestUtil.GetField<float>(player, "mouseSensitivity"), 0.001f);

                player.SetMouseSensitivity(1.5f);
                Assert.AreEqual(1.5f, TestUtil.GetField<float>(player, "mouseSensitivity"), 0.001f);
            }
            finally
            {
                if (hadKey) PlayerPrefs.SetFloat(PlayerController.MouseSensitivityKey, previous);
                else PlayerPrefs.DeleteKey(PlayerController.MouseSensitivityKey);
            }
        }
    }
}
