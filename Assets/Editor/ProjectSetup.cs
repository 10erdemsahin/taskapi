using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdventureGame.EditorTools
{
    /// <summary>
    /// Proje ayarlarını (katman, TMP kaynakları, HTTP izni) otomatik yapar.
    /// Menü: Tools → AdventureGame → 1) Proje Ayarlarını Yap
    /// Batch: Unity.exe -batchmode -executeMethod AdventureGame.EditorTools.ProjectSetup.Run -quit
    /// </summary>
    public static class ProjectSetup
    {
        public const string InteractableLayerName = "Interactable";
        public const int InteractableLayerIndex = 8;

        [MenuItem("Tools/AdventureGame/1) Proje Ayarlarını Yap")]
        public static void Run()
        {
            EnsureLayer(InteractableLayerIndex, InteractableLayerName);
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            ImportTmpEssentials();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Tamamlandı.");
        }

        private static void EnsureLayer(int index, string layerName)
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);

            if (layer.stringValue != layerName)
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[ProjectSetup] Katman {index} = {layerName}");
            }
        }

        private static void ImportTmpEssentials()
        {
            if (Directory.Exists("Assets/TextMesh Pro"))
            {
                return;
            }

            string[] candidates =
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage"
            };

            foreach (string candidate in candidates)
            {
                string fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    AssetDatabase.ImportPackage(fullPath, false);
                    Debug.Log("[ProjectSetup] TMP Essential Resources içe aktarıldı.");
                    return;
                }
            }

            Debug.LogWarning("[ProjectSetup] TMP Essential Resources paketi bulunamadı.");
        }
    }
}
