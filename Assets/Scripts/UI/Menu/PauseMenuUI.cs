using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdventureGame
{
    /// <summary>
    /// Oyun içi duraklatma menüsü (Esc): Devam Et / Ayarlar / Ana Menü / Çıkış.
    /// Duraklatmanın kendisi GameManager'dadır (SetPaused / IsPaused / PausedChanged);
    /// bu sınıf yalnızca girdiyi iletir ve paneli gösterir. Prefab: Assets/Art/Prefabs/PauseMenu.prefab
    /// (Canvas altına konur). Kök obje hep aktif kalır, görünen kısım panelRoot'tur.
    /// </summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [Header("Paneller")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Ayarlar")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text sensitivityValue;
        [SerializeField] private TMP_Text volumeValue;

        [Header("Klavye gezinmesi (opsiyonel)")]
        [SerializeField] private Selectable firstSelected;

        private GameManager _gameManager;
        private PlayerController _player;
        private bool _wasLockedLastFrame;

        private void Awake()
        {
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

            UpdateSensitivityLabel(GameSettings.MouseSensitivity);
            UpdateVolumeLabel(GameSettings.Volume);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null)
            {
                Debug.LogError("PauseMenuUI: Sahnede GameManager yok.", this);
                return;
            }

            _gameManager.PausedChanged += HandlePausedChanged;
            HandlePausedChanged(_gameManager.IsPaused);

            _player = FindAnyObjectByType<PlayerController>();
            if (_player != null)
            {
                _player.SetMouseSensitivity(GameSettings.MouseSensitivity);
            }
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.PausedChanged -= HandlePausedChanged;
            }
        }

        private void Update()
        {
            GameManager gm = _gameManager;
            if (gm == null || gm.Phase == GamePhase.Completed || !Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (gm.IsPaused)
            {
                // Ayarlar açıksa önce ana duraklatma paneline dön, sonra oyuna.
                if (settingsPanel != null && settingsPanel.activeSelf) ShowMain();
                else Resume();
            }
            else if (!gm.IsInputLocked && !_wasLockedLastFrame)
            {
                // Diyalog açıkken (veya bu karede kapanırken) Esc sadece diyaloğu kapatır.
                gm.SetPaused(true);
            }
        }

        private void LateUpdate()
        {
            _wasLockedLastFrame = _gameManager != null && _gameManager.IsInputLocked && !_gameManager.IsPaused;
        }

        private void HandlePausedChanged(bool paused)
        {
            if (panelRoot != null) panelRoot.SetActive(paused);

            if (paused)
            {
                ShowMain();
            }
            else
            {
                GameSettings.Save();
            }
        }

        #region Butonlar (Inspector → OnClick)

        public void Resume()
        {
            if (_gameManager != null && _gameManager.IsPaused)
            {
                _gameManager.SetPaused(false);
            }
        }

        public void ShowSettings() => ShowOnly(settingsPanel);

        public void ShowMain()
        {
            bool leavingSettings = settingsPanel != null && settingsPanel.activeSelf;
            ShowOnly(mainPanel);
            if (leavingSettings) GameSettings.Save();

            if (firstSelected != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }

        public void GoToMainMenu()
        {
            GameSettings.Save();
            if (_gameManager != null && _gameManager.IsPaused)
            {
                _gameManager.SetPaused(false);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuUI.MenuSceneName);
        }

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
            if (_player != null) _player.SetMouseSensitivity(GameSettings.MouseSensitivity);
            UpdateSensitivityLabel(value);
        }

        public void OnVolumeChanged(float value)
        {
            GameSettings.Volume = value;
            UpdateVolumeLabel(value);
        }

        #endregion

        private void ShowOnly(GameObject panel)
        {
            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        }

        private void UpdateSensitivityLabel(float value)
        {
            if (sensitivityValue != null) sensitivityValue.text = value.ToString("0.0");
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValue != null) volumeValue.text = $"%{Mathf.RoundToInt(value * 100f)}";
        }
    }
}
