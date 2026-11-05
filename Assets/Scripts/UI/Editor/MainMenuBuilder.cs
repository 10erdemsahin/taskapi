using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AdventureGame.EditorTools
{
    /// <summary>
    /// Ana menü sahnesini tek tıkla kurar: dönen kameralı tapınak arka planı, Canvas,
    /// Başla / Nasıl Oynanır / Ayarlar / Çıkış, paneller, slider'lar ve tüm Inspector
    /// bağlantıları. Ardından Build Settings'i MainMenu (0), Main (1) olarak ayarlar.
    /// Menü: Tools → AdventureGame → 3) Ana Menü Sahnesini Oluştur
    /// Bu araç Main.unity'ye DOKUNMAZ.
    /// </summary>
    public static class MainMenuBuilder
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/Main.unity";
        private const string SkyboxPath = "Assets/Art/Skybox/TempleSky.mat";
        private const int UiLayer = 5;

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Color ButtonColor = new Color(0.36f, 0.27f, 0.16f, 0.92f);
        private static readonly Color ButtonHighlight = new Color(0.55f, 0.42f, 0.24f, 1f);
        private static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.04f, 0.82f);
        private static readonly Color Gold = new Color(0.96f, 0.84f, 0.55f);

        private const string HowToText =
            "<b>Hikâye</b>\n" +
            "Ormanın derinliklerindeki Yankı Tapınağı yüzyıllardır kapalı. Kapıya ulaşmak için önce " +
            "uçurumu aşmalı, sonra kapının bekçisi Muhafız Kaan'ı ikna etmelisin. Yanında yardımcı " +
            "robotun Robo var: senin ulaşamadığın yerlere o ulaşabilir.\n\n" +
            "<b>Kontroller</b>\n" +
            "<mspace=0.6em>WASD </mspace>  Yürü\n" +
            "<mspace=0.6em>Shift</mspace>  Koş\n" +
            "<mspace=0.6em>Space</mspace>  Zıpla\n" +
            "<mspace=0.6em>Fare </mspace>  Etrafa bak\n" +
            "<mspace=0.6em>F    </mspace>  Robo'dan yardım iste\n" +
            "<mspace=0.6em>E    </mspace>  Etkileşim / konuş\n" +
            "<mspace=0.6em>Esc  </mspace>  Diyaloğu kapat / duraklat\n\n" +
            "<b>İpucu</b>\n" +
            "Robo etrafını görebildiği kadarını bilir. Muhafız ise kaba sözlere değil, akla ve kanıta ikna olur.";

        [MenuItem("Tools/AdventureGame/3) Ana Menü Sahnesini Oluştur")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Ana Menü", $"{ScenePath} zaten var. Baştan oluşturulsun mu?", "Evet, oluştur", "Vazgeç"))
            {
                return;
            }

            // Açık sahnede kaydedilmemiş değişiklik varsa kullanıcıya sor.
            // Main.unity açıksa "Don't Save" seçin (Main.unity Erdem'in).
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildBackdrop();
            BuildUi();

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[MainMenuBuilder] Ana menü oluşturuldu: " + ScenePath + " — Build Settings: MainMenu (0), Main (1)");
        }

        #region Arka plan (3B tapınak + dönen kamera)

        private static void BuildBackdrop()
        {
            Material ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ground.mat");
            Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stone.mat");
            Material door = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Door.mat");
            Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wood.mat");

            var root = new GameObject("Backdrop").transform;
            Cube("Ground", root, new Vector3(0, -0.5f, 0), new Vector3(40, 1, 40), ground);
            Cube("TempleBase", root, new Vector3(0, 0.25f, -3), new Vector3(10, 0.5f, 6), stone);
            Cube("TempleWall", root, new Vector3(0, 3f, -5.5f), new Vector3(10, 5, 1), stone);
            Cube("TempleDoor", root, new Vector3(0, 2.25f, -4.95f), new Vector3(2.4f, 3.5f, 0.2f), door);
            Cube("Lintel", root, new Vector3(0, 5.75f, -5.2f), new Vector3(11, 0.6f, 1.6f), stone);

            for (int i = 0; i < 4; i++)
            {
                float x = -4f + i * (8f / 3f);
                Cube($"Pillar_{i}", root, new Vector3(x, 2.75f, -1f), new Vector3(0.8f, 5f, 0.8f), stone);
            }

            Cube("Bridge", root, new Vector3(0, 0.05f, 5f), new Vector3(2.5f, 0.1f, 6f), wood);
            Cube("RubbleA", root, new Vector3(-6f, 0.4f, 3f), new Vector3(1.2f, 0.8f, 1f), stone).transform.rotation = Quaternion.Euler(0, 25, 8);
            Cube("RubbleB", root, new Vector3(5.5f, 0.3f, 2f), new Vector3(0.9f, 0.6f, 1.3f), stone).transform.rotation = Quaternion.Euler(4, -30, 0);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 55f;
                cam.gameObject.AddComponent<MenuCameraOrbit>();
                cam.transform.position = new Vector3(0, 4.5f, 14f);
                cam.transform.LookAt(new Vector3(0, 1.5f, 0));
            }

            GameObject sunGo = GameObject.Find("Directional Light");
            Light sun = sunGo != null ? sunGo.GetComponent<Light>() : null;
            if (sun != null)
            {
                sun.color = new Color(1f, 0.88f, 0.72f);
                sun.intensity = 1.1f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(32f, -40f, 0f);
            }

            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Skybox;
            }

            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.55f, 0.5f, 0.43f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 60f;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            if (material != null) cube.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(cube.GetComponent<Collider>()); // menüde fizik yok
            cube.isStatic = true;
            return cube;
        }

        #endregion

        #region UI

        private static void BuildUi()
        {
            var canvasGo = new GameObject("Canvas") { layer = UiLayer };
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Proje eski Input Manager kullanıyor → Standalone Input Module (Input System modülüne çevirmeyin).
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            // Arka planı hafif karart (yazılar okunsun).
            RectTransform background = Rect("Background", canvasGo.transform);
            Stretch(background);
            Image bg = background.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);
            bg.raycastTarget = false;

            TextMeshProUGUI title = Text(canvasGo.transform, "Title", "Yankı Tapınağı", 110, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = Gold;
            title.characterSpacing = 4f;
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1400, 150));

            TextMeshProUGUI subtitle = Text(canvasGo.transform, "Subtitle", "Bir yoldaş, bir muhafız, tek bir kapı.", 34, TextAlignmentOptions.Center);
            subtitle.fontStyle = FontStyles.Italic;
            subtitle.color = new Color(1f, 1f, 1f, 0.8f);
            Place(subtitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -235), new Vector2(1200, 60));

            // ---------- Ana panel ----------
            RectTransform mainPanel = Rect("MainPanel", canvasGo.transform);
            Place(mainPanel, Center, Center, Center, new Vector2(0, -90), new Vector2(460, 420));
            VerticalLayoutGroup layout = mainPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            Button start = MakeButton(mainPanel, "StartButton", "Başla");
            Button howTo = MakeButton(mainPanel, "HowToButton", "Nasıl Oynanır");
            Button settings = MakeButton(mainPanel, "SettingsButton", "Ayarlar");
            Button quit = MakeButton(mainPanel, "QuitButton", "Çıkış");

            // ---------- Nasıl Oynanır ----------
            RectTransform howToPanel = MakePanel(canvasGo.transform, "HowToPanel", new Vector2(1100, 700), "Nasıl Oynanır");
            TextMeshProUGUI howToBody = Text(howToPanel, "Body", HowToText, 28, TextAlignmentOptions.TopLeft);
            howToBody.textWrappingMode = TextWrappingModes.Normal;
            howToBody.richText = true;
            howToBody.rectTransform.anchorMin = new Vector2(0, 0);
            howToBody.rectTransform.anchorMax = new Vector2(1, 1);
            howToBody.rectTransform.offsetMin = new Vector2(50, 110);
            howToBody.rectTransform.offsetMax = new Vector2(-50, -100);
            Button howToBack = MakeButton(howToPanel, "BackButton", "Geri");
            Place((RectTransform)howToBack.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(260, 64));

            // ---------- Ayarlar ----------
            RectTransform settingsPanel = MakePanel(canvasGo.transform, "SettingsPanel", new Vector2(900, 480), "Ayarlar");
            SliderRow(settingsPanel, "Sensitivity", "Fare Hassasiyeti", new Vector2(0, 60),
                GameSettings.SensitivityMin, GameSettings.SensitivityMax, GameSettings.SensitivityDefault,
                out Slider sensitivitySlider, out TextMeshProUGUI sensitivityValue);
            SliderRow(settingsPanel, "Volume", "Ses Seviyesi", new Vector2(0, -40),
                GameSettings.VolumeMin, GameSettings.VolumeMax, GameSettings.VolumeDefault,
                out Slider volumeSlider, out TextMeshProUGUI volumeValue);
            Button settingsBack = MakeButton(settingsPanel, "BackButton", "Geri");
            Place((RectTransform)settingsBack.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(260, 64));

            // ---------- Script + bağlantılar ----------
            var menuGo = new GameObject("MainMenu");
            MainMenuUI menu = menuGo.AddComponent<MainMenuUI>();
            Set(menu, "mainPanel", mainPanel.gameObject);
            Set(menu, "howToPanel", howToPanel.gameObject);
            Set(menu, "settingsPanel", settingsPanel.gameObject);
            Set(menu, "sensitivitySlider", sensitivitySlider);
            Set(menu, "volumeSlider", volumeSlider);
            Set(menu, "sensitivityValue", sensitivityValue);
            Set(menu, "volumeValue", volumeValue);
            Set(menu, "firstSelected", start);

            UnityEventTools.AddVoidPersistentListener(start.onClick, menu.StartGame);
            UnityEventTools.AddObjectPersistentListener<GameObject>(howTo.onClick, menu.ShowPanel, howToPanel.gameObject);
            UnityEventTools.AddObjectPersistentListener<GameObject>(settings.onClick, menu.ShowPanel, settingsPanel.gameObject);
            UnityEventTools.AddVoidPersistentListener(quit.onClick, menu.Quit);
            UnityEventTools.AddVoidPersistentListener(howToBack.onClick, menu.ShowMain);
            UnityEventTools.AddVoidPersistentListener(settingsBack.onClick, menu.ShowMain);
            UnityEventTools.AddPersistentListener(sensitivitySlider.onValueChanged, new UnityAction<float>(menu.OnSensitivityChanged));
            UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, new UnityAction<float>(menu.OnVolumeChanged));

            foreach (Object o in new Object[] { start, howTo, settings, quit, howToBack, settingsBack, sensitivitySlider, volumeSlider })
            {
                EditorUtility.SetDirty(o);
            }

            howToPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
        }

        private static RectTransform MakePanel(Transform parent, string name, Vector2 size, string heading)
        {
            RectTransform panel = Rect(name, parent);
            Place(panel, Center, Center, Center, new Vector2(0, -60), size);
            panel.gameObject.AddComponent<Image>().color = PanelColor;

            TextMeshProUGUI head = Text(panel, "Heading", heading, 48, TextAlignmentOptions.Center);
            head.fontStyle = FontStyles.Bold;
            head.color = Gold;
            Place(head.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(0, 70));
            return panel;
        }

        private static void SliderRow(RectTransform parent, string name, string label, Vector2 position,
            float min, float max, float value, out Slider slider, out TextMeshProUGUI valueText)
        {
            RectTransform row = Rect(name + "Row", parent);
            Place(row, Center, Center, Center, position, new Vector2(780, 70));

            TextMeshProUGUI labelText = Text(row, "Label", label, 30, TextAlignmentOptions.Left);
            Place(labelText.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(280, 0));

            GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = name + "Slider";
            SetLayerRecursive(sliderGo, UiLayer);
            sliderGo.transform.SetParent(row, false);
            Place((RectTransform)sliderGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(360, 28));
            slider = sliderGo.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            TintSlider(sliderGo);

            valueText = Text(row, "Value", "", 30, TextAlignmentOptions.Right);
            Place(valueText.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(100, 0));
            valueText.color = Gold;
        }

        private static void TintSlider(GameObject sliderGo)
        {
            foreach (Image image in sliderGo.GetComponentsInChildren<Image>(true))
            {
                switch (image.gameObject.name)
                {
                    case "Background": image.color = new Color(1f, 1f, 1f, 0.15f); break;
                    case "Fill": image.color = ButtonHighlight; break;
                    case "Handle": image.color = Gold; break;
                }
            }
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            RectTransform rt = Rect(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white;
            Button button = rt.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHighlight;
            colors.selectedColor = ButtonHighlight;
            colors.pressedColor = new Color(0.25f, 0.19f, 0.11f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TextMeshProUGUI text = Text(rt, "Text", label, 34, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            RectTransform rt = Rect(name, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[MainMenuBuilder] {target.GetType().Name}.{field} bulunamadı.");
                return;
            }

            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Build Settings

        /// <summary>MainMenu 0., Main 1. sırada; listedeki diğer sahneler korunur.</summary>
        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path != ScenePath && existing.path != GameScenePath)
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        #endregion
    }
}
