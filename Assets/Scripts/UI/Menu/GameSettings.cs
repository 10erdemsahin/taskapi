using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Menülerin (ana menü, duraklatma) paylaştığı oyuncu ayarları: PlayerPrefs anahtarları,
    /// varsayılanlar ve sınırlar tek yerde. Anahtar adlarını başka yerde tekrar yazmayın.
    /// </summary>
    public static class GameSettings
    {
        /// <summary>PlayerController bu anahtarı okur: PlayerPrefs.GetFloat("MouseSensitivity", ...).</summary>
        public const string SensitivityKey = "MouseSensitivity";
        public const string VolumeKey = "MasterVolume";

        public const float SensitivityMin = 0.5f;
        public const float SensitivityMax = 5f;
        public const float SensitivityDefault = 2f;

        public const float VolumeMin = 0f;
        public const float VolumeMax = 1f;
        public const float VolumeDefault = 1f;

        public static float MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, SensitivityDefault), SensitivityMin, SensitivityMax);
            set => PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, SensitivityMin, SensitivityMax));
        }

        public static float Volume
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(VolumeKey, VolumeDefault), VolumeMin, VolumeMax);
            set
            {
                float v = Mathf.Clamp(value, VolumeMin, VolumeMax);
                PlayerPrefs.SetFloat(VolumeKey, v);
                AudioListener.volume = v;
            }
        }

        public static void Save() => PlayerPrefs.Save();

        /// <summary>
        /// Oyun hangi sahneden başlarsa başlasın (Editor'de doğrudan Main.unity dahil)
        /// kayıtlı ses seviyesini uygular. Sahneye bileşen eklemek gerekmez.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedVolume()
        {
            AudioListener.volume = Volume;
        }
    }
}
