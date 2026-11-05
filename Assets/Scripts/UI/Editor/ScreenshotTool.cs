using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdventureGame.EditorTools
{
    /// <summary>
    /// README için ekran görüntüsü: Play modunda F12 → Game penceresinin o anki görüntüsü
    /// (UI dahil) Docs/Images/ekran_NN.png olarak kaydedilir.
    /// Game penceresini önce 1920x1080 yapın (Game sekmesi → çözünürlük menüsü).
    /// </summary>
    public static class ScreenshotTool
    {
        private const string Folder = "Docs/Images";

        [MenuItem("Tools/AdventureGame/Ekran Görüntüsü Al _F12")]
        public static void Capture()
        {
            Directory.CreateDirectory(Folder);
            int n = 1;
            string path;
            do { path = Path.Combine(Folder, $"ekran_{n:00}.png"); n++; } while (File.Exists(path));

            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Ekran görüntüsü] {path} kaydedildi ({Screen.width}x{Screen.height}).");
        }
    }
}
