using System;
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
using Object = UnityEngine.Object;

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

        // Ömer'in dosyaları: VARSA sahneye eklenir, yoksa sessizce atlanır.
        private const string WallMaterialPath = "Assets/Materials/Wall.mat";
        private const string SkyboxPath = "Assets/Art/Skybox/TempleSky.mat";
        private const string StoneRailingPrefabPath = "Assets/Art/Prefabs/StoneRailing.prefab";
        private const string BridgeRailingPrefabPath = "Assets/Art/Prefabs/BridgeRailing.prefab";
        private const string EchoShardPrefabPath = "Assets/Art/Prefabs/EchoShard.prefab";
        private const string PauseMenuPrefabPath = "Assets/Art/Resources/PauseMenu.prefab"; // Ömer Resources'a taşıdı (GameHUD yedek olarak buradan yükler)
        private const string QuestHintAssetPath = "Assets/Data/Quest/EchoStoneHint.asset";
        private const bool PlaceEchoShardsInScene = false;

        // Texture yoğunluğu: bir desen tekrarının kapladığı metre (WorldSpaceUV).
        private const float GroundMetersPerTile = 2.5f;
        private const float StoneMetersPerTile = 1.5f;
        private const float WoodMetersPerTile = 1.2f;
        private const float DoorMetersPerTile = 3f;

        /// <summary>Yankı Taşı parçalarının konumları ve kimin alabileceği.</summary>
        private static readonly (string name, Vector3 position, bool companionOnly)[] ShardSpots =
        {
            ("EchoShard_1", new Vector3(-9.3f, 0.5f, 8.5f), false),  // Alan A, platformun arkası, uçurum kenarı (bariyerin önünde)
            ("EchoShard_2", new Vector3(8.5f, 0.5f, -8.5f), false),  // Alan A, sağ arka köşe, kutunun arkası
            ("EchoShard_3", new Vector3(-8f, 3.5f, 6f), true),       // Yüksek platformun üstü — sadece yoldaş
            ("EchoShard_4", new Vector3(6f, 0.5f, 29f), false),      // Alan B, muhafızın arkası
            ("EchoShard_5", new Vector3(-9f, 1.4f, 24f), false)      // Alan B, 0.9 m'lik kayanın üstü
        };

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
            ConfigureLighting();

            Tile(Cube("Ground_A", new Vector3(0, -0.5f, 0), new Vector3(20, 1, 20), groundMat), GroundMetersPerTile);
            Tile(Cube("Ground_B", new Vector3(0, -0.5f, 27), new Vector3(20, 1, 20), groundMat), GroundMetersPerTile);
            Tile(Cube("HighPlatform", new Vector3(-7, 1.5f, 5), new Vector3(4, 3, 4), stoneMat), StoneMetersPerTile);

            // ---------- Köprü ----------
            var bridgePivot = new GameObject("BridgePivot");
            bridgePivot.transform.position = new Vector3(0, 0, 17);
            bridgePivot.transform.rotation = Quaternion.Euler(90, 0, 0); // KALKIK
            GameObject deck = Cube("Deck", Vector3.zero, new Vector3(3, 0.3f, 7.4f), woodMat, bridgePivot.transform);
            deck.transform.localPosition = new Vector3(0, -0.15f, -3.6f);
            Tile(deck, WoodMetersPerTile);
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
            Tile(Cube("Wall_L", new Vector3(-6, 2, 30), new Vector3(8, 4, 1), stoneMat), StoneMetersPerTile);
            Tile(Cube("Wall_R", new Vector3(6, 2, 30), new Vector3(8, 4, 1), stoneMat), StoneMetersPerTile);
            Tile(Cube("Lintel", new Vector3(0, 3.5f, 30), new Vector3(4, 1, 1), stoneMat), StoneMetersPerTile);
            var doorHinge = new GameObject("DoorHinge");
            doorHinge.transform.position = new Vector3(-2, 0, 30);
            GameObject doorPanel = Cube("DoorPanel", Vector3.zero, new Vector3(4, 3, 0.3f), doorMat, doorHinge.transform);
            doorPanel.transform.localPosition = new Vector3(2, 1.5f, 0);
            Tile(doorPanel, DoorMetersPerTile);
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

            // ---------- Harita duvarları (uçurum tarafı AÇIK) ----------
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath) ?? stoneMat; // Ömer'in Wall.mat'ı, yoksa taş
            var walls = new GameObject("Walls");
            Tile(Cube("Wall_A_Left", new Vector3(-10.5f, 1.5f, 0), new Vector3(1, 3, 21), wallMat, walls.transform), StoneMetersPerTile);
            Tile(Cube("Wall_A_Right", new Vector3(10.5f, 1.5f, 0), new Vector3(1, 3, 21), wallMat, walls.transform), StoneMetersPerTile);
            Tile(Cube("Wall_A_Back", new Vector3(0, 1.5f, -10.5f), new Vector3(22, 3, 1), wallMat, walls.transform), StoneMetersPerTile);
            Tile(Cube("Wall_B_Left", new Vector3(-10.5f, 1.5f, 27), new Vector3(1, 3, 21), wallMat, walls.transform), StoneMetersPerTile);
            Tile(Cube("Wall_B_Right", new Vector3(10.5f, 1.5f, 27), new Vector3(1, 3, 21), wallMat, walls.transform), StoneMetersPerTile);
            Tile(Cube("Wall_B_Back", new Vector3(0, 1.5f, 37.5f), new Vector3(22, 3, 1), wallMat, walls.transform), StoneMetersPerTile);

            // ---------- Düşme engelleri (görünmez, Ignore Raycast → NavMesh'e ve yoldaşın görüşüne karışmaz) ----------
            var barriers = new GameObject("FallBarriers");
            InvisibleWall("ChasmBarrier_A_Left", new Vector3(-5.75f, 1.5f, 9.8f), new Vector3(8.5f, 3, 0.3f), barriers.transform);
            InvisibleWall("ChasmBarrier_A_Right", new Vector3(5.75f, 1.5f, 9.8f), new Vector3(8.5f, 3, 0.3f), barriers.transform);
            InvisibleWall("ChasmBarrier_B_Left", new Vector3(-5.75f, 1.5f, 17.2f), new Vector3(8.5f, 3, 0.3f), barriers.transform);
            InvisibleWall("ChasmBarrier_B_Right", new Vector3(5.75f, 1.5f, 17.2f), new Vector3(8.5f, 3, 0.3f), barriers.transform);
            GameObject bridgeGate = InvisibleWall("BridgeGate_A", new Vector3(0, 1.5f, 9.8f), new Vector3(3, 3, 0.3f), barriers.transform);
            DisableOnActivated gateOpener = bridgeGate.AddComponent<DisableOnActivated>();

            // Köprü korkulukları: köprüyle birlikte döner (BridgePivot'un çocuğu).
            GameObject railLeft = InvisibleWall("BridgeRail_Left", Vector3.zero, new Vector3(0.2f, 2.6f, 7.4f), bridgePivot.transform);
            railLeft.transform.localPosition = new Vector3(-1.6f, 1.3f, -3.6f);
            railLeft.transform.localRotation = Quaternion.identity;
            GameObject railRight = InvisibleWall("BridgeRail_Right", Vector3.zero, new Vector3(0.2f, 2.6f, 7.4f), bridgePivot.transform);
            railRight.transform.localPosition = new Vector3(1.6f, 1.3f, -3.6f);
            railRight.transform.localRotation = Quaternion.identity;

            // Görsel korkuluklar (Ömer'in prefab'ları varsa).
            PlaceRailingVisuals(bridgePivot.transform);

            // ---------- Bölüm 1 zorlukları ----------
            // a) Çift kilit: şalter + basınç plakası → köprü.
            var plate = new GameObject("PressurePlate") { layer = IgnoreRaycastLayer };
            plate.transform.position = new Vector3(6f, 0f, 8f);
            BoxCollider plateTrigger = plate.AddComponent<BoxCollider>();
            plateTrigger.isTrigger = true;
            plateTrigger.center = new Vector3(0, 0.5f, 0);
            plateTrigger.size = new Vector3(2f, 1f, 2f);
            GameObject plateVisual = Cube("PlateVisual", Vector3.zero, new Vector3(1.8f, 0.1f, 1.8f), leverMat, plate.transform, false);
            plateVisual.transform.localPosition = new Vector3(0, 0.05f, 0);
            PressurePlate pressurePlate = plate.AddComponent<PressurePlate>();
            var bridgeLock = new GameObject("BridgeLock");
            AndGate andGate = bridgeLock.AddComponent<AndGate>();

            // b) Hareketli engel: yoldaşın platform yolunu kesen taş blok.
            var slideA = new GameObject("SlideA");
            slideA.transform.position = new Vector3(-2.3f, 0.6f, 6.8f);
            var slideB = new GameObject("SlideB");
            slideB.transform.position = new Vector3(-2.3f, 0.6f, 3.6f);
            GameObject slidingBlock = Cube("SlidingBlock", slideA.transform.position, new Vector3(1.2f, 1.2f, 1.2f), stoneMat);
            Tile(slidingBlock, StoneMetersPerTile);
            Rigidbody blockBody = slidingBlock.AddComponent<Rigidbody>();
            blockBody.isKinematic = true;
            NavMeshObstacle blockObstacle = slidingBlock.AddComponent<NavMeshObstacle>();
            blockObstacle.carving = true;
            blockObstacle.carveOnlyStationary = false;
            slidingBlock.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            SlidingBlock slider = slidingBlock.AddComponent<SlidingBlock>();

            // c) Gizli şalter: başlangıç noktasından şaltere görüşü kapatan tapınak sütunu (kaide + gövde + başlık).
            var pillar = new GameObject("SightPillar");
            pillar.transform.position = new Vector3(-3.5f, 0f, 2.2f);
            GameObject pillarBase = Cube("Base", new Vector3(0, 0.15f, 0), new Vector3(1.6f, 0.3f, 1.6f), stoneMat, pillar.transform);
            GameObject pillarShaft = Cube("Shaft", new Vector3(0, 2f, 0), new Vector3(1.2f, 3.4f, 1.2f), stoneMat, pillar.transform);
            GameObject pillarCap = Cube("Capital", new Vector3(0, 3.85f, 0), new Vector3(1.6f, 0.3f, 1.6f), stoneMat, pillar.transform);
            Tile(pillarBase, StoneMetersPerTile);
            Tile(pillarShaft, StoneMetersPerTile);
            Tile(pillarCap, StoneMetersPerTile);

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
            else if (persona.Version < NpcPersona.CurrentVersion)
            {
                // Eski persona (tek bilmeceli) → yeni varsayılanlarla güncelle (iki bilmece, deneme hakkı).
                NpcPersona fresh = ScriptableObject.CreateInstance<NpcPersona>();
                EditorUtility.CopySerialized(fresh, persona);
                Object.DestroyImmediate(fresh);
                EditorUtility.SetDirty(persona);
                Debug.Log("[SceneBuilder] GuardPersona yeni sürüme güncellendi.");
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
            Set(leverSwitch, "targetBehaviour", andGate); // şalter → çift kilit → köprü
            Set(leverSwitch, "handlePivot", handlePivot.transform);
            Set(leverSwitch, "interactionAnchor", leverAnchor.transform);
            SetBool(leverSwitch, "allowPlayer", false);
            SetBool(leverSwitch, "allowAICompanion", true);
            SetString(leverSwitch, "interactionPrompt", "Şalteri çek [E]");

            Set(bridge, "pivot", bridgePivot.transform);
            Set(bridge, "navMeshSurface", surface);
            Set(door, "hinge", doorHinge.transform);

            Set(andGate, "signalBehaviour", pressurePlate);
            Set(andGate, "targetBehaviour", bridge);
            Set(pressurePlate, "visual", plateVisual.transform);

            Set(gateOpener, "activatableBehaviour", bridge);
            SetObjectArray(gateOpener, "collidersToDisable", bridgeGate.GetComponent<Collider>());

            Set(slider, "pointA", slideA.transform);
            Set(slider, "pointB", slideB.transform);

            Set(guardNpc, "contextProviderBehaviour", manager);
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

            // ---------- Ömer'den gelenler (dosyalar varsa) ----------
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
            }

            // Yankı Taşı parçaları teslim sürümünde KAPALI: platformdaki parça Robo'nun şalter görevini
            // bozuyor (AgentSuccessTests 12/15 başarısız) ve kapı kuralı parçaları bekliyor. Kod/prefab hazır;
            // açmak için true yap ve testleri yeniden çalıştır.
            if (PlaceEchoShardsInScene)
            {
                PlaceEchoShards(interactableLayer, manager, hud);
            }
            PlacePauseMenu();

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
            Place((RectTransform)restart.transform, Center, Center, Center, new Vector2(-290, -100), new Vector2(260, 70));
            Button mainMenu = MakeButton(victory, "MainMenuButton", "Ana Menü");
            Place((RectTransform)mainMenu.transform, Center, Center, Center, new Vector2(0, -100), new Vector2(260, 70));
            Button quit = MakeButton(victory, "QuitButton", "Çıkış");
            Place((RectTransform)quit.transform, Center, Center, Center, new Vector2(290, -100), new Vector2(260, 70));

            // Yankı Taşı sayacı ve ipucu paneli (Ömer'in GameHUD alanları gelince bağlanır)
            TextMeshProUGUI questCounter = Text(hudRoot, "QuestCounterText", "", 26, TextAlignmentOptions.TopRight);
            Place(questCounter.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -80), new Vector2(500, 50));
            questCounter.color = new Color(0.55f, 0.9f, 1f);
            RectTransform hintPanel = Rect("HintPanel", hudRoot);
            Place(hintPanel, Center, Center, Center, new Vector2(0, 160), new Vector2(1100, 180));
            hintPanel.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.12f, 0.85f);
            TextMeshProUGUI hintText = Text(hintPanel, "HintText", "", 30, TextAlignmentOptions.Center);
            hintText.fontStyle = FontStyles.Italic;
            hintText.color = new Color(0.65f, 0.95f, 1f);
            Place(hintText.rectTransform, Vector2.zero, Vector2.one, Center, Vector2.zero, new Vector2(-60, -30));
            hintPanel.gameObject.SetActive(false);

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
            SetIfExists(hud, "mainMenuButton", mainMenu);
            SetIfExists(hud, "questCounterText", questCounter);
            SetIfExists(hud, "hintPanel", hintPanel.gameObject);
            SetIfExists(hud, "hintText", hintText);

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

        #region Ömer'in Asset'leri (varsa)

        private static void PlaceRailingVisuals(Transform bridgePivot)
        {
            GameObject stoneRailing = AssetDatabase.LoadAssetAtPath<GameObject>(StoneRailingPrefabPath);
            if (stoneRailing != null)
            {
                var root = new GameObject("RailingVisuals");
                float[] xs = { -9f, -7f, -5f, -3f, 3f, 5f, 7f, 9f };
                foreach (float z in new[] { 9.8f, 17.2f })
                {
                    foreach (float x in xs)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(stoneRailing);
                        instance.transform.SetParent(root.transform, false);
                        instance.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.identity);
                    }
                }
            }
            else
            {
                Debug.Log("[SceneBuilder] StoneRailing prefab'ı yok; uçurum kenarında sadece görünmez bariyer var.");
            }

            GameObject bridgeRailing = AssetDatabase.LoadAssetAtPath<GameObject>(BridgeRailingPrefabPath);
            if (bridgeRailing != null)
            {
                foreach (float side in new[] { -1.6f, 1.6f })
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(bridgeRailing);
                        instance.transform.SetParent(bridgePivot, false);
                        instance.transform.localPosition = new Vector3(side, 0f, -6.8f + i);
                        instance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    }
                }
            }
        }

        private static void PlaceEchoShards(int interactableLayer, GameManager manager, GameHUD hud)
        {
            GameObject shardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoShardPrefabPath);
            if (shardPrefab == null)
            {
                Debug.Log("[SceneBuilder] EchoShard prefab'ı yok; Yankı Taşı parçaları atlandı (kapı kuralı devre dışı).");
                return;
            }

            // Parçaların saklandığı ortam objeleri — sadece parçalar varken anlamlı.
            Material crateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wood.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stone.mat");
            Tile(Cube("ShardCrate", new Vector3(8.5f, 0.5f, -7f), new Vector3(1f, 1f, 1f), crateMat), WoodMetersPerTile);
            Tile(Cube("ShardRock", new Vector3(-9f, 0.45f, 24f), new Vector3(1.5f, 0.9f, 1.5f), rockMat), StoneMetersPerTile);

            Type collectibleType = FindRuntimeType("AdventureGame.Collectible");
            Type questType = FindRuntimeType("AdventureGame.CollectionQuest");

            var root = new GameObject("EchoShards");
            var collectibles = new List<Object>();

            foreach (var spot in ShardSpots)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(shardPrefab);
                instance.name = spot.name;
                instance.layer = interactableLayer;
                instance.transform.SetParent(root.transform, false);
                instance.transform.position = spot.position;

                Component collectible = collectibleType != null ? instance.GetComponent(collectibleType) : null;
                if (collectible == null)
                {
                    Debug.LogWarning($"[SceneBuilder] {spot.name}: Collectible bileşeni bulunamadı.");
                    continue;
                }

                if (spot.companionOnly)
                {
                    SetBool(collectible, "allowPlayer", false);
                    SetBool(collectible, "allowAICompanion", true);
                }

                collectibles.Add(collectible);
            }

            if (questType == null)
            {
                Debug.LogWarning("[SceneBuilder] CollectionQuest sınıfı yok; parçalar yerleşti ama görev bağlanmadı.");
                return;
            }

            var questGo = new GameObject("EchoStoneQuest");
            Component quest = questGo.AddComponent(questType);
            SetObjectArray(quest, "collectibles", collectibles.ToArray());
            SetIfExists(quest, "hintData", AssetDatabase.LoadAssetAtPath<ScriptableObject>(QuestHintAssetPath));
            Set(manager, "collectionQuestBehaviour", quest);
            SetIfExists(hud, "questBehaviour", quest);
        }

        private static void PlacePauseMenu()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPrefabPath);
            GameObject canvas = GameObject.Find("Canvas");
            if (prefab == null || canvas == null)
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(canvas.transform, false);
        }

        private static Type FindRuntimeType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }

            return null;
        }

        #endregion

        #region Yardımcılar

        /// <summary>Texture'ı objenin gerçek boyutuna göre döşer (uzamayı önler).</summary>
        private static GameObject Tile(GameObject go, float metersPerTile)
        {
            if (!go.TryGetComponent(out WorldSpaceUV tiler))
            {
                tiler = go.AddComponent<WorldSpaceUV>();
            }
            tiler.MetersPerTile = metersPerTile;
            return go;
        }

        /// <summary>Gölgeli güneş ışığı, gökyüzünden ortam ışığı ve hafif sis → daha derinlikli görüntü.</summary>
        private static void ConfigureLighting()
        {
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                sun.color = new Color(1f, 0.94f, 0.84f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.85f;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.62f, 0.66f, 0.70f);
        }

        /// <summary>Görünmez, trigger olmayan çarpışma duvarı (Ignore Raycast katmanı).</summary>
        private static GameObject InvisibleWall(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            var go = new GameObject(name) { layer = IgnoreRaycastLayer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.AddComponent<BoxCollider>();
            return go;
        }

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

        /// <summary>Alan varsa bağlar, yoksa sessizce geçer (Ömer'in henüz eklemediği alanlar için).</summary>
        private static bool SetIfExists(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return false;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void SetObjectArray(Object target, string field, params Object[] values)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null || !p.isArray) return;

            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

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
