using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdventureGame.EditorTools
{
    /// <summary>
    /// Ömer'in görsel asset'lerini üretir (tekrar çalıştırılabilir, mevcutların üzerine yazar):
    /// Wall.mat, Railing.mat, EchoShard.mat, StoneRailing/BridgeRailing/EchoShard/ShardPickupFX prefab'ları,
    /// shard_pickup.wav (kodla sentezlenmiş, telifsiz) ve EchoStoneHint.asset. Ayrıca materyal kalite ayarları.
    /// Batch: Unity.exe -batchmode -executeMethod AdventureGame.EditorTools.VisualAssetBuilder.BuildAll -quit
    /// </summary>
    public static class VisualAssetBuilder
    {
        private const string Mat = "Assets/Materials/";
        private const string Prefabs = "Assets/Art/Prefabs/";
        private const string Audio = "Assets/Art/Audio/";
        private const string QuestData = "Assets/Data/Quest/";
        private const string WallTex = "Assets/Art/Textures/Wall/Bricks113B_2K-JPG_";
        private const string StoneTex = "Assets/Art/Textures/Stone/Bricks113A_2K-JPG_";

        public const string ShardPrefabPath = Prefabs + "EchoShard.prefab";
        public const string HintAssetPath = QuestData + "EchoStoneHint.asset";

        [MenuItem("Tools/AdventureGame/Ömer/Görsel Asset'leri Üret (duvar, korkuluk, Yankı Taşı)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory(Audio);
            Directory.CreateDirectory(QuestData);
            AssetDatabase.Refresh();

            TuneExistingMaterials();
            Material wall = TexturedMaterial("Wall", WallTex, new Vector2(1, 1), new Color(0.82f, 0.84f, 0.8f), 0.2f, 1.3f);
            Material railing = TexturedMaterial("Railing", StoneTex, new Vector2(2, 1), new Color(0.92f, 0.9f, 0.86f), 0.2f, 1.2f);
            Material shard = ShardMaterial();
            Material wood = AssetDatabase.LoadAssetAtPath<Material>(Mat + "Wood.mat");

            BuildStoneRailing(railing);
            BuildBridgeRailing(wood);
            AudioClip sound = BuildPickupSound();
            ParticleSystem fx = BuildPickupFx();
            BuildShard(shard, fx, sound);
            BuildHintAsset();

            AssetDatabase.SaveAssets();
            Debug.Log($"[VisualAssetBuilder] Tamam: {wall.name}, {railing.name}, {shard.name}, 4 prefab, ses, ipucu.");
        }

        #region Materyaller

        private static void TuneExistingMaterials()
        {
            Tune("Stone", gloss: 0.2f, bump: 1.25f);
            Tune("Ground", gloss: null, bump: 1.25f);
            Tune("Wood", gloss: 0.25f, bump: 1.15f);
            Tune("Door", gloss: null, bump: 1.2f);
        }

        private static void Tune(string name, float? gloss, float bump)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(Mat + name + ".mat");
            if (m == null) return;
            if (gloss.HasValue) m.SetFloat("_Glossiness", gloss.Value);
            m.SetFloat("_BumpScale", bump);
            EditorUtility.SetDirty(m);
        }

        private static Material LoadOrCreate(string name)
        {
            string path = Mat + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material TexturedMaterial(string name, string texPrefix, Vector2 tiling, Color tint, float gloss, float bump)
        {
            Material m = LoadOrCreate(name);
            m.shader = Shader.Find("Standard");
            m.color = tint;
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPrefix + "Color.jpg");
            m.mainTextureScale = tiling;
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texPrefix + "NormalGL.jpg");
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", bump);
            if (normal != null) m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPrefix + "AmbientOcclusion.jpg"));
            m.SetFloat("_Glossiness", gloss);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material ShardMaterial()
        {
            Material m = LoadOrCreate("EchoShard");
            m.shader = Shader.Find("Standard");
            m.color = new Color(0.35f, 0.85f, 0.95f, 1f);
            m.SetFloat("_Glossiness", 0.85f);
            m.SetFloat("_Metallic", 0.1f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.15f, 0.75f, 0.95f) * 2.2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }

        #endregion

        #region Prefab'lar

        /// <summary>2 m × 1 m × 0.3 m taş korkuluk modülü. Pivot alt-orta, collider yok (çarpışma Erdem'in görünmez duvarlarında).</summary>
        private static void BuildStoneRailing(Material mat)
        {
            var root = new GameObject("StoneRailing");
            Part(root, "Wall", PrimitiveType.Cube, new Vector3(0, 0.425f, 0), new Vector3(2f, 0.85f, 0.3f), mat);
            Part(root, "Cap", PrimitiveType.Cube, new Vector3(0, 0.925f, 0), new Vector3(2f, 0.15f, 0.4f), mat);
            Part(root, "Plinth", PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(2f, 0.12f, 0.38f), mat);
            Save(root, Prefabs + "StoneRailing.prefab");
        }

        /// <summary>1 m × 1 m ahşap köprü korkuluğu: iki dikme + üst ve orta kiriş. Collider yok.</summary>
        private static void BuildBridgeRailing(Material wood)
        {
            var root = new GameObject("BridgeRailing");
            Part(root, "PostL", PrimitiveType.Cube, new Vector3(-0.44f, 0.5f, 0), new Vector3(0.12f, 1f, 0.12f), wood);
            Part(root, "PostR", PrimitiveType.Cube, new Vector3(0.44f, 0.5f, 0), new Vector3(0.12f, 1f, 0.12f), wood);
            Part(root, "TopRail", PrimitiveType.Cube, new Vector3(0, 0.95f, 0), new Vector3(1f, 0.1f, 0.14f), wood);
            Part(root, "MidRail", PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(1f, 0.07f, 0.08f), wood);
            Save(root, Prefabs + "BridgeRailing.prefab");
        }

        private static void BuildShard(Material mat, ParticleSystem fx, AudioClip sound)
        {
            var root = new GameObject("EchoShard");
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) root.layer = layer;

            // Oyuncu Raycast'i trigger'ları yok saydığı için katı ama küçük bir collider.
            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.radius = 0.35f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.8f, 0);
            visual.AddComponent<FloatingSpin>();

            // Kristal: üst üste iki dönük küp → sekiz yüzlü parıltılı taş.
            Part(visual, "CrystalA", PrimitiveType.Cube, Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), mat, new Vector3(45f, 0f, 45f));
            Part(visual, "CrystalB", PrimitiveType.Cube, Vector3.zero, new Vector3(0.18f, 0.42f, 0.18f), mat, new Vector3(0f, 45f, 0f));

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(visual.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.85f, 1f);
            light.range = 3f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;

            var collectible = root.AddComponent<Collectible>();
            var so = new SerializedObject(collectible);
            so.FindProperty("interactionPrompt").stringValue = "Yankı Taşı parçasını al [E]";
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("pickupEffectPrefab").objectReferenceValue = fx;
            so.FindProperty("pickupSound").objectReferenceValue = sound;
            so.FindProperty("shardId").stringValue = "shard";
            so.ApplyModifiedPropertiesWithoutUndo();

            Save(root, ShardPrefabPath);
        }

        private static ParticleSystem BuildPickupFx()
        {
            var go = new GameObject("ShardPickupFX");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.95f, 1f), new Color(0.85f, 1f, 1f));
            main.gravityModifier = -0.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.3f, 0.8f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");

            GameObject saved = Save(go, Prefabs + "ShardPickupFX.prefab");
            return saved.GetComponent<ParticleSystem>();
        }

        private static GameObject Part(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject Save(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        #endregion

        #region Ses ve veri

        /// <summary>Kısa, parlak bir "çın" sesi: üç harmonik + üstel sönüm. Kodla üretildi → lisans sorunu yok.</summary>
        private static AudioClip BuildPickupSound()
        {
            const int rate = 44100;
            const float seconds = 0.7f;
            int n = (int)(rate * seconds);
            var samples = new short[n];
            float[] freqs = { 880f, 1318.5f, 1760f };
            float[] amps = { 0.5f, 0.3f, 0.2f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-5.5f * t) * Mathf.Clamp01(t / 0.005f);
                float s = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    // Hafif yükselen perde: "toplandı" hissi.
                    s += amps[k] * Mathf.Sin(2f * Mathf.PI * freqs[k] * (1f + 0.08f * t) * t);
                }
                samples[i] = (short)(Mathf.Clamp(s * env * 0.8f, -1f, 1f) * short.MaxValue);
            }

            string path = Audio + "shard_pickup.wav";
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + n * 2);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(n * 2);
                foreach (short v in samples) w.Write(v);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void BuildHintAsset()
        {
            QuestHintData hint = AssetDatabase.LoadAssetAtPath<QuestHintData>(HintAssetPath);
            if (hint == null)
            {
                hint = ScriptableObject.CreateInstance<QuestHintData>();
                AssetDatabase.CreateAsset(hint, HintAssetPath);
            }

            hint.questName = "Yankı Taşı";
            hint.hintText =
                "\"Muhafızın ilk sorusunu duyduğunda sus ve dinle: konuştuğun an kaybolan şey cevaptır.\n" +
                "İkincisinde ise geldiğin yola dön ve yere bak: her adımın arkanda ne bırakıyor?\"";
            hint.completionMessage = "Beş parça birleşti; taş sana fısıldıyor.";
            EditorUtility.SetDirty(hint);
        }

        #endregion

        #region Sahneye yerleştirme (Erdem çalıştırır, sahne onda)

        /// <summary>
        /// AÇIK sahneye 5 Yankı Taşı parçası + CollectionQuest ekler ve GameManager'a bağlar.
        /// Sahneyi KAYDETMEZ; Erdem bakıp kaydeder. Bir parça yüksek platformdadır ve yalnızca Robo alabilir.
        /// </summary>
        [MenuItem("Tools/AdventureGame/Ömer/Yankı Taşı Parçalarını Açık Sahneye Yerleştir")]
        public static void PlaceShardsInOpenScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShardPrefabPath);
            QuestHintData hint = AssetDatabase.LoadAssetAtPath<QuestHintData>(HintAssetPath);
            if (prefab == null || hint == null)
            {
                Debug.LogError("Önce 'Görsel Asset'leri Üret' menüsünü çalıştırın.");
                return;
            }

            GameObject old = GameObject.Find("EchoStoneQuest");
            if (old != null) Object.DestroyImmediate(old);

            var questGo = new GameObject("EchoStoneQuest");
            var quest = questGo.AddComponent<CollectionQuest>();
            var list = new System.Collections.Generic.List<Collectible>();

            // (konum, yalnızca yoldaş mı) — Alan A köşeleri, yüksek platform, Alan B.
            (Vector3 pos, bool companionOnly)[] spots =
            {
                (new Vector3(-8.5f, 0f, -8.5f), false),
                (new Vector3(8.5f, 0f, -3f), false),
                (new Vector3(-3f, 0f, 1.5f), false),
                (new Vector3(-7f, 3f, 5f), true),
                (new Vector3(7.5f, 0f, 24f), false),
            };

            for (int i = 0; i < spots.Length; i++)
            {
                var shard = (GameObject)PrefabUtility.InstantiatePrefab(prefab, questGo.transform);
                shard.name = $"EchoShard_{i + 1}";
                shard.transform.position = spots[i].pos;
                var c = shard.GetComponent<Collectible>();
                var so = new SerializedObject(c);
                so.FindProperty("shardId").stringValue = $"shard_{i + 1}";
                if (spots[i].companionOnly)
                {
                    so.FindProperty("allowPlayer").boolValue = false;
                    so.FindProperty("allowAICompanion").boolValue = true;
                    so.FindProperty("interactionPrompt").stringValue = "Bu parçaya ancak Robo uzanabilir";
                }
                else
                {
                    so.FindProperty("allowAICompanion").boolValue = false;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                list.Add(c);
            }

            var qso = new SerializedObject(quest);
            SerializedProperty arr = qso.FindProperty("collectibles");
            arr.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            qso.FindProperty("hintData").objectReferenceValue = hint;
            qso.ApplyModifiedPropertiesWithoutUndo();

            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                var gso = new SerializedObject(gm);
                gso.FindProperty("collectionQuestBehaviour").objectReferenceValue = quest;
                gso.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(questGo.scene);
            Debug.Log("[VisualAssetBuilder] 5 parça + CollectionQuest eklendi, GameManager'a bağlandı. Sahneyi kontrol edip kaydedin.");
        }

        #endregion
    }
}
