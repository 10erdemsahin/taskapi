using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdventureGame
{
    /// <summary>
    /// Ana menü: Başla / Nasıl Oynanır / Ayarlar / Çıkış.
    /// Butonların ve slider'ların OnClick / On Value Changed bağlantıları Inspector'da
    /// (MainMenuBuilder bunları otomatik kurar). Oyun mantığı içermez.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        public const string SensitivityKey = GameSettings.SensitivityKey;
        public const string VolumeKey = GameSettings.VolumeKey;
        public const string GameSceneName = "Main";
        public const string MenuSceneName = "MainMenu";

        [Header("Paneller")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject howToPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Ayarlar")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private TMP_Text volumeValue;

        [Header("İlk seçilecek buton (klavye/gamepad gezinmesi için, opsiyonel)")]
        [SerializeField] private Selectable firstSelected;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = GameSettings.SensitivityMin;
                sensitivitySlider.maxValue = GameSettings.SensitivityMax;
                sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            }

            if (volumeSlider != null)
            {
                volumeSlider.minValue = GameSettings.VolumeMin;
                volumeSlider.maxValue = GameSettings.VolumeMax;
                volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            }

            AudioListener.volume = GameSettings.Volume;
            UpdateSensitivityLabel(GameSettings.MouseSensitivity);
            UpdateVolumeLabel(GameSettings.Volume);
            ShowPanel(mainPanel);
        }

        private void Update()
        {
            // Alt panellerden Esc ile geri dön.
            if (Input.GetKeyDown(KeyCode.Escape) && mainPanel != null && !mainPanel.activeSelf)
            {
                ShowPanel(mainPanel);
            }
        }

        private void OnDisable()
        {
            GameSettings.Save();
        }

        #region Butonlar (Inspector → OnClick)

        public void StartGame()
        {
            GameSettings.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameSceneName);
        }

        public void ShowPanel(GameObject panel)
        {
            bool leavingSettings = settingsPanel != null && settingsPanel.activeSelf && panel != settingsPanel;

            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (howToPanel != null) howToPanel.SetActive(panel == howToPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);

            if (leavingSettings)
            {
                GameSettings.Save();
            }

            if (panel == mainPanel && firstSelected != null && UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }

        public void ShowMain() => ShowPanel(mainPanel);

        public void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Slider'lar (Inspector → On Value Changed → Dynamic float)

        public void OnSensitivityChanged(float value)
        {
            GameSettings.MouseSensitivity = value;
            UpdateSensitivityLabel(value);
        }

        public void OnVolumeChanged(float value)
        {
            GameSettings.Volume = value;
            UpdateVolumeLabel(value);
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (sensitivityValue != null) sensitivityValue.text = value.ToString("0.0");
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValue != null) volumeValue.text = $"%{Mathf.RoundToInt(value * 100f)}";
        }

        #endregion
    }
}
