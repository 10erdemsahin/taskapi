using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdventureGame.EditorTools
{
    /// <summary>
    /// README'deki sahneyi tek tıkla kurar: geometri, şalter, köprü, NavMeshLink, kapı, NPC,
    /// oyuncu, yoldaş, UI ve tüm Inspector bağlantıları; ardından NavMesh'i bake eder.
    /// Menü: Tools → AdventureGame → 2) Sahneyi Oluştur
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        private const string NavMeshAssetPath = "Assets/Scenes/Main_NavMesh.asset";
        private const string PersonaPath = "Assets/Data/GuardPersona.asset";
        private const int IgnoreRaycastLayer = 2;
        private const int UiLayer = 5;

        [MenuItem("Tools/AdventureGame/2) Sahneyi Oluştur")]
        public static void Build()
        {
            ProjectSetup.Run();
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Materials");
            Directory.CreateDirectory("Assets/Data");

            int interactableLayer = LayerMask.NameToLayer(ProjectSetup.InteractableLayerName);
            if (interactableLayer < 0)
            {
                interactableLayer = ProjectSetup.InteractableLayerIndex;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null) Object.DestroyImmediate(defaultCamera);

            Material groundMat = Mat("Ground", new Color(0.42f, 0.45f, 0.40f));
            Material stoneMat = Mat("Stone", new Color(0.55f, 0.52f, 0.48f));
            Material woodMat = Mat("Wood", new Color(0.50f, 0.33f, 0.18f));
            Material leverMat = Mat("Lever", new Color(0.95f, 0.75f, 0.10f));
            Material playerMat = Mat("Player", new Color(0.25f, 0.55f, 0.95f));
            Material companionMat = Mat("Companion", new Color(0.30f, 0.90f, 0.55f));
            Material guardMat = Mat("Guard", new Color(0.85f, 0.25f, 0.25f));
            Material doorMat = Mat("Door", new Color(0.30f, 0.28f, 0.26f));

            // ---------- Geometri ----------
            Cube("Ground_A", new Vector3(0, -0.5f, 0), new Vector3(20, 1, 20), groundMat);
            Cube("Ground_B", new Vector3(0, -0.5f, 27), new Vector3(20, 1, 20), groundMat);
            Cube("HighPlatform", new Vector3(-7, 1.5f, 5), new Vector3(4, 3, 4), stoneMat);

            // ---------- Köprü ----------
            var bridgePivot = new GameObject("BridgePivot");
            bridgePivot.transform.position = new Vector3(0, 0, 17);
            bridgePivot.transform.rotation = Quaternion.Euler(90, 0, 0); // KALKIK
            GameObject deck = Cube("Deck", Vector3.zero, new Vector3(3, 0.3f, 7.4f), woodMat, bridgePivot.transform);
            deck.transform.localPosition = new Vector3(0, -0.15f, -3.6f);
            DrawBridge bridge = bridgePivot.AddComponent<DrawBridge>();

            // ---------- Şalter ----------
            var lever = new GameObject("Lever") { layer = interactableLayer };
            lever.transform.position = new Vector3(-5.6f, 3f, 5f);
            BoxCollider leverCollider = lever.AddComponent<BoxCollider>();
            leverCollider.center = new Vector3(0, 0.6f, 0);
            leverCollider.size = new Vector3(0.6f, 1.2f, 0.6f);
            GameObject leverBase = Cube("Base", Vector3.zero, new Vector3(0.5f, 0.2f, 0.5f), stoneMat, lever.transform, false);
            leverBase.transform.localPosition = new Vector3(0, 0.1f, 0);
            var handlePivot = new GameObject("HandlePivot");
            handlePivot.transform.SetParent(lever.transform, false);
            handlePivot.transform.localPosition = new Vector3(0, 0.2f, 0);
            GameObject handle = Cube("Handle", Vector3.zero, new Vector3(0.12f, 1f, 0.12f), leverMat, handlePivot.transform, false);
            handle.transform.localPosition = new Vector3(0, 0.5f, 0);
            var leverAnchor = new GameObject("LeverAnchor");
            leverAnchor.transform.SetParent(lever.transform, false);
            leverAnchor.transform.localPosition = new Vector3(-1f, 0, 0);
            LeverSwitch leverSwitch = lever.AddComponent<LeverSwitch>();

            // ---------- NavMeshLink (yoldaşın zıplaması) ----------
            var jumpLink = new GameObject("PlatformJumpLink");
            NavMeshLink link = jumpLink.AddComponent<NavMeshLink>();
            link.startPoint = new Vector3(-3.5f, 0f, 5f);
            link.endPoint = new Vector3(-7.2f, 3f, 5f);
            link.width = 1.5f;
            link.bidirectional = true;

            // ---------- Duvar, kapı ----------
            Cube("Wall_L", new Vector3(-6, 2, 30), new Vector3(8, 4, 1), stoneMat);
            Cube("Wall_R", new Vector3(6, 2, 30), new Vector3(8, 4, 1), stoneMat);
            Cube("Lintel", new Vector3(0, 3.5f, 30), new Vector3(4, 1, 1), stoneMat);
            var doorHinge = new GameObject("DoorHinge");
            doorHinge.transform.position = new Vector3(-2, 0, 30);
            GameObject doorPanel = Cube("DoorPanel", Vector3.zero, new Vector3(4, 3, 0.3f), doorMat, doorHinge.transform);
            doorPanel.transform.localPosition = new Vector3(2, 1.5f, 0);
            NavMeshObstacle doorObstacle = doorPanel.AddComponent<NavMeshObstacle>();
            doorObstacle.carving = true;
            doorHinge.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            InteractiveDoor door = doorHinge.AddComponent<InteractiveDoor>();

            // ---------- Muhafız ----------
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guard.name = "GuardNPC";
            guard.layer = interactableLayer;
            guard.transform.SetPositionAndRotation(new Vector3(3.5f, 1f, 28.5f), Quaternion.Euler(0, 180, 0));
            guard.GetComponent<Renderer>().sharedMaterial = guardMat;
            GuardNPC guardNpc = guard.AddComponent<GuardNPC>();

            // ---------- Oyuncu ----------
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1, -6);
            player.GetComponent<Renderer>().sharedMaterial = playerMat;
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = Vector3.zero;
            player.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.SetParent(player.transform, false);
            cameraGo.transform.localPosition = new Vector3(0, 0.7f, 0);
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            cameraGo.AddComponent<AudioListener>();
            PlayerController playerController = player.AddComponent<PlayerController>();
            PlayerInteractor playerInteractor = player.AddComponent<PlayerInteractor>();

            // ---------- Yoldaş ----------
            GameObject companion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            companion.name = "Companion";
            companion.transform.position = new Vector3(2, 1, -5);
            companion.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            companion.GetComponent<Renderer>().sharedMaterial = companionMat;
            companion.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            NavMeshAgent agent = companion.AddComponent<NavMeshAgent>();
            agent.baseOffset = 1f;
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 4.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            var eye = new GameObject("Eye");
            eye.transform.SetParent(companion.transform, false);
            eye.transform.localPosition = new Vector3(0, 0.6f, 0.35f);
            CompanionSensor sensor = companion.AddComponent<CompanionSensor>();
            NavMeshCompanionController companionController = companion.AddComponent<NavMeshCompanionController>();

            // ---------- Tetik bölgeleri ----------
            var respawnPoint = new GameObject("RespawnPoint");
            respawnPoint.transform.position = new Vector3(0, 1, -6);
            GameObject obstacleZone = TriggerBox("ObstacleZone", new Vector3(0, 1, 8), new Vector3(20, 2, 2.5f));
            GameObject goalZone = TriggerBox("GoalZone", new Vector3(0, 1.5f, 33.5f), new Vector3(4, 3, 3));
            GameObject killZone = TriggerBox("KillZone", new Vector3(0, -8, 13), new Vector3(200, 2, 200));
            PlayerTriggerZone obstacleTrigger = obstacleZone.AddComponent<PlayerTriggerZone>();
            PlayerTriggerZone goalTrigger = goalZone.AddComponent<PlayerTriggerZone>();
            RespawnZone respawnZone = killZone.AddComponent<RespawnZone>();

            // ---------- Servisler & Persona ----------
            var services = new GameObject("Services");
            OllamaLLMService ollama = services.AddComponent<OllamaLLMService>();
            services.AddComponent<MockLLMService>();
            NpcPersona persona = AssetDatabase.LoadAssetAtPath<NpcPersona>(PersonaPath);
            if (persona == null)
            {
                persona = ScriptableObject.CreateInstance<NpcPersona>();
                AssetDatabase.CreateAsset(persona, PersonaPath);
            }

            // ---------- GameManager ----------
            var managerGo = new GameObject("GameManager");
            GameManager manager = managerGo.AddComponent<GameManager>();

            // ---------- NavMesh ----------
            var navMeshGo = new GameObject("NavMesh");
            NavMeshSurface surface = navMeshGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << IgnoreRaycastLayer);

            // ---------- UI ----------
            BuildUi(out DialogueUI dialogueUi, out GameHUD hud);

            // ---------- Bağlantılar (Inspector alanları) ----------
            Set(leverSwitch, "targetBehaviour", bridge);
            Set(leverSwitch, "handlePivot", handlePivot.transform);
            Set(leverSwitch, "interactionAnchor", leverAnchor.transform);
            SetBool(leverSwitch, "allowPlayer", false);
            SetBool(leverSwitch, "allowAICompanion", true);
            SetString(leverSwitch, "interactionPrompt", "Şalteri çek [E]");

            Set(bridge, "pivot", bridgePivot.transform);
            Set(bridge, "navMeshSurface", surface);
            Set(door, "hinge", doorHinge.transform);

            Set(guardNpc, "persona", persona);
            Set(guardNpc, "llmServiceBehaviour", ollama);
            Set(guardNpc, "dialogueViewBehaviour", dialogueUi);
            SetBool(guardNpc, "allowPlayer", true);
            SetBool(guardNpc, "allowAICompanion", false);

            Set(playerController, "cameraPivot", cameraGo.transform);
            Set(playerInteractor, "viewCamera", camera);
            SetInt(playerInteractor, "interactableMask", 1 << interactableLayer);

            Set(sensor, "eye", eye.transform);
            SetInt(sensor, "interactableMask", 1 << interactableLayer);
            SetInt(sensor, "occluderMask", 1 << 0);

            Set(respawnZone, "respawnPoint", respawnPoint.transform);

            Set(manager, "player", player.transform);
            Set(manager, "companionBehaviour", companionController);
            Set(manager, "bridgeBehaviour", bridge);
            Set(manager, "npcBehaviour", guardNpc);
            Set(manager, "doorBehaviour", door);

            Set(hud, "playerInteractor", playerInteractor);
            Set(hud, "llmServiceBehaviour", ollama);

            AddListener(obstacleTrigger, manager.NotifyObstacleReached);
            AddListener(goalTrigger, manager.CompleteGame);

            // ---------- Bake & Kaydet ----------
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, ScenePath);

            if (surface.navMeshData != null)
            {
                AssetDatabase.DeleteAsset(NavMeshAssetPath);
                AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);
                EditorUtility.SetDirty(surface);
            }
            else
            {
                Debug.LogError("[SceneBuilder] NavMesh bake başarısız.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Sahne oluşturuldu: " + ScenePath);
        }

        #region UI

        private static void BuildUi(out DialogueUI dialogueUi, out GameHUD hud)
        {
            var canvasGo = new GameObject("Canvas") { layer = UiLayer };
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            // HUD
            RectTransform hudRoot = Rect("HUD", canvasGo.transform);
            Stretch(hudRoot);
            TextMeshProUGUI objective = Text(hudRoot, "ObjectiveText", "Görev:", 30, TextAlignmentOptions.TopLeft);
            Place(objective.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -25), new Vector2(1100, 120));
            TextMeshProUGUI llmStatus = Text(hudRoot, "LlmStatusText", "LLM:", 22, TextAlignmentOptions.TopRight);
            Place(llmStatus.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -25), new Vector2(700, 60));
            TextMeshProUGUI crosshair = Text(hudRoot, "Crosshair", "+", 40, TextAlignmentOptions.Center);
            Place(crosshair.rectTransform, Center, Center, Center, Vector2.zero, new Vector2(60, 60));
            TextMeshProUGUI prompt = Text(hudRoot, "PromptText", "", 30, TextAlignmentOptions.Center);
            Place(prompt.rectTransform, Center, Center, Center, new Vector2(0, -80), new Vector2(900, 60));
            RectTransform notification = Rect("Notification", hudRoot);
            Place(notification, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1200, 80));
            CanvasGroup notificationGroup = notification.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI notificationText = Text(notification, "NotificationText", "", 30, TextAlignmentOptions.Center);
            Stretch(notificationText.rectTransform);
            notificationText.color = new Color(1f, 0.92f, 0.6f);

            // Diyalog paneli
            RectTransform panel = Rect("DialoguePanel", canvasGo.transform);
            Place(panel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1100, 620));
            panel.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.88f);

            TextMeshProUGUI title = Text(panel, "TitleText", "NPC", 34, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(-60, 50));

            GameObject scrollGo = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            scrollGo.name = "ScrollView";
            scrollGo.transform.SetParent(panel, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(30, 150);
            scrollRt.offsetMax = new Vector2(-30, -75);
            scrollGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.04f);
            ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            if (scrollRect.horizontalScrollbar != null)
            {
                Object.DestroyImmediate(scrollRect.horizontalScrollbar.gameObject);
                scrollRect.horizontalScrollbar = null;
            }

            RectTransform content = scrollRect.content;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI logText = Text(content, "LogText", "", 26, TextAlignmentOptions.TopLeft);
            logText.textWrappingMode = TextWrappingModes.Normal;
            logText.richText = true;

            RectTransform thinking = Rect("ThinkingIndicator", panel);
            Place(thinking, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(-60, 40));
            TextMeshProUGUI thinkingLabel = Text(thinking, "ThinkingLabel", "Düşünüyor...", 24, TextAlignmentOptions.Left);
            thinkingLabel.fontStyle = FontStyles.Italic;
            thinkingLabel.color = new Color(0.8f, 0.8f, 0.8f);
            Stretch(thinkingLabel.rectTransform);

            GameObject inputGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            inputGo.name = "InputField";
            inputGo.transform.SetParent(panel, false);
            Place((RectTransform)inputGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(30, 25), new Vector2(-250, 60));
            TMP_InputField inputField = inputGo.GetComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.pointSize = 26;
            if (inputField.placeholder is TMP_Text placeholder) placeholder.text = "Muhafıza bir şey yaz ve Enter'a bas...";

            Button send = MakeButton(panel, "SendButton", "Gönder");
            Place((RectTransform)send.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 25), new Vector2(190, 60));
            Button close = MakeButton(panel, "CloseButton", "X");
            Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-15, -15), new Vector2(50, 50));

            // Zafer paneli
            RectTransform victory = Rect("VictoryPanel", canvasGo.transform);
            Stretch(victory);
            victory.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            TextMeshProUGUI victoryTitle = Text(victory, "Title", "Tebrikler!\n<size=60%>Yankı Tapınağı'nın kapısından geçtin.</size>", 80, TextAlignmentOptions.Center);
            Place(victoryTitle.rectTransform, Center, Center, Center, new Vector2(0, 120), new Vector2(1400, 300));
            Button restart = MakeButton(victory, "RestartButton", "Yeniden Başla");
            Place((RectTransform)restart.transform, Center, Center, Center, new Vector2(-150, -100), new Vector2(260, 70));
            Button quit = MakeButton(victory, "QuitButton", "Çıkış");
            Place((RectTransform)quit.transform, Center, Center, Center, new Vector2(150, -100), new Vector2(260, 70));

            dialogueUi = canvasGo.AddComponent<DialogueUI>();
            Set(dialogueUi, "panelRoot", panel.gameObject);
            Set(dialogueUi, "titleText", title);
            Set(dialogueUi, "scrollRect", scrollRect);
            Set(dialogueUi, "logText", logText);
            Set(dialogueUi, "inputField", inputField);
            Set(dialogueUi, "sendButton", send);
            Set(dialogueUi, "closeButton", close);
            Set(dialogueUi, "thinkingIndicator", thinking.gameObject);
            Set(dialogueUi, "thinkingLabel", thinkingLabel);

            hud = canvasGo.AddComponent<GameHUD>();
            Set(hud, "objectiveText", objective);
            Set(hud, "notificationText", notificationText);
            Set(hud, "notificationGroup", notificationGroup);
            Set(hud, "interactionPromptText", prompt);
            Set(hud, "crosshair", crosshair.gameObject);
            Set(hud, "llmStatusText", llmStatus);
            Set(hud, "victoryPanel", victory.gameObject);
            Set(hud, "restartButton", restart);
            Set(hud, "quitButton", quit);

            panel.gameObject.SetActive(false);
            victory.gameObject.SetActive(false);
        }

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

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
            return tmp;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            RectTransform rt = Rect(name, parent);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.45f, 0.75f, 1f);
            Button button = rt.gameObject.AddComponent<Button>();
            TextMeshProUGUI text = Text(rt, "Text", label, 26, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            return button;
        }

        #endregion

        #region Yardımcılar

        /// <summary>
        /// Build Settings sahne listesini KORUR (Ömer'in MainMenu'sü 0. sırada kalır);
        /// sahne listede yoksa sona ekler, devre dışıysa etkinleştirir.
        /// </summary>
        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => s.path == scenePath);

            if (index < 0)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            else if (!scenes[index].enabled)
            {
                scenes[index] = new EditorBuildSettingsScene(scenePath, true);
            }
            else
            {
                return;
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Material Mat(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                // Materyaller Ömer'in alanında (texture atanmış olabilir): mevcut olana DOKUNMA.
                return material;
            }

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent = null, bool keepCollider = true)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            if (parent != null) cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (parent != null) cube.layer = parent.gameObject.layer;
            if (!keepCollider) Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        private static GameObject TriggerBox(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name) { layer = IgnoreRaycastLayer };
            go.transform.position = position;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        private static SerializedProperty Prop(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[SceneBuilder] {target.GetType().Name}.{field} bulunamadı.");
            }

            return property;
        }

        private static void Set(Object target, string field, Object value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null) return;
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null) return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddListener(PlayerTriggerZone zone, UnityAction action)
        {
            FieldInfo field = typeof(PlayerTriggerZone).GetField("onPlayerEnter", BindingFlags.NonPublic | BindingFlags.Instance);
            var unityEvent = (UnityEvent)field.GetValue(zone);
            UnityEventTools.AddVoidPersistentListener(unityEvent, action);
            EditorUtility.SetDirty(zone);
        }

        #endregion
    }
}
