using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using M = AdventureGame.EditorTools.MainMenuBuilder;

namespace AdventureGame.EditorTools
{
    /// <summary>
    /// Duraklatma menüsü prefab'ını tek tıkla üretir: Assets/Art/Prefabs/PauseMenu.prefab.
    /// Sahneye DOKUNMAZ; prefab'ı Main.unity'deki Canvas'ın altına Erdem yerleştirir.
    /// Menü: Tools → AdventureGame → 4) Duraklatma Menüsü Prefab'ını Oluştur
    /// </summary>
    public static class PauseMenuBuilder
    {
        public const string PrefabPath = "Assets/Art/Prefabs/PauseMenu.prefab";

        [MenuItem("Tools/AdventureGame/4) Duraklatma Menüsü Prefab'ını Oluştur")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            // Kök: tam ekran, hep aktif (PauseMenuUI Esc'i dinlemeye devam etsin).
            var root = new GameObject("PauseMenu", typeof(RectTransform)) { layer = M.UiLayer };
            M.Stretch((RectTransform)root.transform);
            PauseMenuUI menu = root.AddComponent<PauseMenuUI>();

            // Görünen kısım: yarı saydam karartma + içerik.
            RectTransform overlay = M.Rect("Overlay", root.transform);
            M.Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            TextMeshProUGUI title = M.Text(overlay, "Title", "Duraklatıldı", 80, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = M.Gold;
            M.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1000, 120));

            // Ana panel
            RectTransform mainPanel = M.Rect("MainPanel", overlay);
            M.Place(mainPanel, M.Center, M.Center, M.Center, new Vector2(0, -60), new Vector2(460, 420));
            VerticalLayoutGroup layout = mainPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            Button resume = M.MakeButton(mainPanel, "ResumeButton", "Devam Et");
            Button settings = M.MakeButton(mainPanel, "SettingsButton", "Ayarlar");
            Button mainMenu = M.MakeButton(mainPanel, "MainMenuButton", "Ana Menü");
            Button quit = M.MakeButton(mainPanel, "QuitButton", "Çıkış");

            // Ayarlar paneli (ana menüdekiyle aynı slider'lar)
            RectTransform settingsPanel = M.MakePanel(overlay, "SettingsPanel", new Vector2(900, 480), "Ayarlar");
            M.SliderRow(settingsPanel, "Sensitivity", "Fare Hassasiyeti", new Vector2(0, 60),
                GameSettings.SensitivityMin, GameSettings.SensitivityMax, GameSettings.SensitivityDefault,
                out Slider sensitivitySlider, out TextMeshProUGUI sensitivityValue);
            M.SliderRow(settingsPanel, "Volume", "Ses Seviyesi", new Vector2(0, -40),
                GameSettings.VolumeMin, GameSettings.VolumeMax, GameSettings.VolumeDefault,
                out Slider volumeSlider, out TextMeshProUGUI volumeValue);
            Button back = M.MakeButton(settingsPanel, "BackButton", "Geri");
            M.Place((RectTransform)back.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(260, 64));

            // Bağlantılar
            M.Set(menu, "panelRoot", overlay.gameObject);
            M.Set(menu, "mainPanel", mainPanel.gameObject);
            M.Set(menu, "settingsPanel", settingsPanel.gameObject);
            M.Set(menu, "sensitivitySlider", sensitivitySlider);
            M.Set(menu, "volumeSlider", volumeSlider);
            M.Set(menu, "sensitivityValue", sensitivityValue);
            M.Set(menu, "volumeValue", volumeValue);
            M.Set(menu, "firstSelected", resume);

            UnityEventTools.AddVoidPersistentListener(resume.onClick, menu.Resume);
            UnityEventTools.AddVoidPersistentListener(settings.onClick, menu.ShowSettings);
            UnityEventTools.AddVoidPersistentListener(mainMenu.onClick, menu.GoToMainMenu);
            UnityEventTools.AddVoidPersistentListener(quit.onClick, menu.Quit);
            UnityEventTools.AddVoidPersistentListener(back.onClick, menu.ShowMain);
            UnityEventTools.AddPersistentListener(sensitivitySlider.onValueChanged, new UnityAction<float>(menu.OnSensitivityChanged));
            UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, new UnityAction<float>(menu.OnVolumeChanged));

            settingsPanel.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            if (success) Debug.Log("[PauseMenuBuilder] Prefab oluşturuldu: " + PrefabPath + " — Main.unity'deki Canvas'ın altına konmalı.");
            else Debug.LogError("[PauseMenuBuilder] Prefab kaydedilemedi: " + PrefabPath);
        }
    }
}
