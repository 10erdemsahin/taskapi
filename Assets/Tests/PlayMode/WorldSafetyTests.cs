using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AdventureGame.Tests
{
    /// <summary>Duvarlar, düşme engelleri, köprü kapısı ve çift kilit (basınç plakası + şalter).</summary>
    public class WorldSafetyTests
    {
        private GameManager _gm;
        private PlayerController _player;
        private CharacterController _controller;
        private DrawBridge _bridge;
        private AndGate _bridgeLock;
        private PressurePlate _plate;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            _gm = GameManager.Instance;
            _player = Object.FindAnyObjectByType<PlayerController>();
            _controller = _player.GetComponent<CharacterController>();
            _bridge = Object.FindAnyObjectByType<DrawBridge>();
            _bridgeLock = Object.FindAnyObjectByType<AndGate>();
            _plate = Object.FindAnyObjectByType<PressurePlate>();

            Assert.NotNull(_gm);
            Assert.NotNull(_bridge);
            Assert.NotNull(_bridgeLock, "BridgeLock (AndGate) sahnede olmalı");
            Assert.NotNull(_plate, "PressurePlate sahnede olmalı");
        }

        // ---------------------------------------------------------------- Düşme engeli

        [UnityTest]
        public IEnumerator UcurumKenari_KopruKalkikken_OyuncuGecemezVeDusmez()
        {
            yield return PlaceAndPush(new Vector3(0, 1, 9f), Vector3.forward, 2f);

            Assert.Less(_player.transform.position.z, 9.7f, "Bariyer oyuncuyu uçurumun önünde durdurmalı");
            Assert.Greater(_player.transform.position.y, 0.5f, "Oyuncu düşmemeli");
            Assert.AreEqual(GamePhase.PhysicalObstacle, _gm.Phase);
        }

        [UnityTest]
        public IEnumerator UcurumKenari_KopruYanindan_DeGecilemez()
        {
            yield return PlaceAndPush(new Vector3(-5f, 1, 9f), Vector3.forward, 2f);
            Assert.Less(_player.transform.position.z, 9.7f);

            yield return PlaceAndPush(new Vector3(5f, 1, 9f), Vector3.forward, 2f);
            Assert.Less(_player.transform.position.z, 9.7f);
        }

        [UnityTest]
        public IEnumerator KopruIndikten_SonraKapiAcilir_OyuncuKopruyeGecebilir()
        {
            yield return LowerBridge();

            yield return PlaceAndPush(new Vector3(0, 1, 9f), Vector3.forward, 2.5f);
            Assert.Greater(_player.transform.position.z, 10.5f, "Köprü inince kapı açılmalı, oyuncu köprüye çıkabilmeli");
            Assert.Greater(_player.transform.position.y, 0.5f, "Oyuncu köprünün üstünde olmalı");
        }

        [UnityTest]
        public IEnumerator KoprudenYana_Dusulmez()
        {
            yield return LowerBridge();

            yield return PlaceAndPush(new Vector3(0, 1.1f, 13.5f), Vector3.right, 2f);
            Assert.Less(_player.transform.position.x, 1.5f, "Sağ korkuluk durdurmalı");
            Assert.Greater(_player.transform.position.y, 0.5f, "Köprüden düşmemeli");

            yield return PlaceAndPush(new Vector3(0, 1.1f, 13.5f), Vector3.left, 2f);
            Assert.Greater(_player.transform.position.x, -1.5f, "Sol korkuluk durdurmalı");
            Assert.Greater(_player.transform.position.y, 0.5f, "Köprüden düşmemeli");
        }

        [UnityTest]
        public IEnumerator HaritaKenarlari_DuvarlarDisariCikarmaz()
        {
            yield return PlaceAndPush(new Vector3(8f, 1, 0f), Vector3.right, 2f);
            Assert.Less(_player.transform.position.x, 10f, "Sağ duvar");

            yield return PlaceAndPush(new Vector3(-8f, 1, -2f), Vector3.left, 2f);
            Assert.Greater(_player.transform.position.x, -10f, "Sol duvar");

            yield return PlaceAndPush(new Vector3(0f, 1, -8f), Vector3.back, 2f);
            Assert.Greater(_player.transform.position.z, -10f, "Arka duvar");
            Assert.Greater(_player.transform.position.y, 0.5f);
        }

        // ---------------------------------------------------------------- Çift kilit

        [UnityTest]
        public IEnumerator CiftKilit_PlakaBosken_SalterKopruyuIndirmez_SonraPlakayaBasilincaIner()
        {
            Assert.IsFalse(_plate.IsOn);

            _bridgeLock.Activate(); // yoldaşın şalteri çekmesini taklit eder
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(_bridgeLock.IsLeverPulled);
            Assert.IsFalse(_bridge.IsActivated, "Plaka boşken köprü İNMEMELİ");

            _player.Teleport(new Vector3(6f, 1f, 8f));
            yield return TestUtil.WaitFor(() => _plate.IsOn, 3f, "Oyuncu plakaya basmadı");
            yield return TestUtil.WaitFor(() => _bridge.IsActivated, 8f, "Plakaya sonradan basılınca köprü inmeli");
            Assert.AreEqual(GamePhase.SocialObstacle, _gm.Phase);
        }

        [UnityTest]
        public IEnumerator CiftKilit_SadecePlaka_KopruyuIndirmez()
        {
            _player.Teleport(new Vector3(6f, 1f, 8f));
            yield return TestUtil.WaitFor(() => _plate.IsOn, 3f, "Oyuncu plakaya basmadı");
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(_bridge.IsActivated, "Şalter çekilmeden köprü İNMEMELİ");
        }

        // ---------------------------------------------------------------- Yardımcılar

        private IEnumerator LowerBridge()
        {
            _player.Teleport(new Vector3(6f, 1f, 8f));
            yield return TestUtil.WaitFor(() => _plate.IsOn, 3f, "Oyuncu plakaya basmadı");
            _bridgeLock.Activate();
            yield return TestUtil.WaitFor(() => _bridge.IsActivated, 10f, "Köprü inmedi");
        }

        /// <summary>Oyuncuyu bir noktaya koyup belirli süre bir yöne "yürütür" (girdi yerine CharacterController.Move).</summary>
        private IEnumerator PlaceAndPush(Vector3 start, Vector3 direction, float seconds)
        {
            _player.Teleport(start);
            yield return null;

            float end = Time.time + seconds;
            while (Time.time < end)
            {
                _controller.Move(direction.normalized * (6f * Time.deltaTime));
                yield return null;
            }

            yield return null;
        }
    }
}
