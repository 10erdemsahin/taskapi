using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdventureGame
{
    /// <summary>
    /// Oyun içi bilgi ekranı: görev metni, bildirimler, etkileşim ipucu, LLM bağlantı
    /// durumu ve zafer paneli. Sadece olayları DİNLER; oyun mantığı içermez.
    /// </summary>
    public sealed class GameHUD : MonoBehaviour
    {
        [Header("Görev ve Bildirim")]
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private CanvasGroup notificationGroup;
        [SerializeField, Min(0.5f)] private float notificationDuration = 4f;

        [Header("Etkileşim")]
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private TMP_Text interactionPromptText;
        [SerializeField] private GameObject crosshair;

        [Header("LLM Durumu")]
        [Tooltip("ILLMService uygulayan bileşen (opsiyonel)")]
        [SerializeField] private MonoBehaviour llmServiceBehaviour;
        [SerializeField] private TMP_Text llmStatusText;
        [SerializeField, Min(3f)] private float healthCheckInterval = 15f;

        [Header("Zafer Paneli")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        private ILLMService _llm;
        private GameManager _gameManager;
        private Coroutine _notificationRoutine;
        private CancellationTokenSource _lifetimeCts;
        private bool _isCheckingHealth;

        private void Awake()
        {
            _lifetimeCts = new CancellationTokenSource();

            if (llmServiceBehaviour != null)
            {
                _llm = InterfaceResolver.Resolve<ILLMService>(llmServiceBehaviour, this, nameof(llmServiceBehaviour));
            }

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (notificationGroup != null) notificationGroup.alpha = 0f;
            SetPrompt(null);

            if (restartButton != null) restartButton.onClick.AddListener(RestartScene);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager != null)
            {
                _gameManager.ObjectiveChanged += SetObjective;
                _gameManager.NotificationPosted += ShowNotification;
                _gameManager.PhaseChanged += HandlePhaseChanged;
                _gameManager.InputLockChanged += HandleInputLockChanged;

                // Abonelikten önce yayınlanmış durumu da yansıt.
                SetObjective(_gameManager.CurrentObjective);
                HandlePhaseChanged(_gameManager.Phase);
            }
            else
            {
                Debug.LogError("GameHUD: Sahnede GameManager yok.", this);
            }

            if (playerInteractor != null)
            {
                playerInteractor.FocusChanged += SetPrompt;
            }

            if (_llm != null)
            {
                StartCoroutine(HealthCheckLoop());
            }
            else if (llmStatusText != null)
            {
                llmStatusText.text = string.Empty;
            }
        }

        private void OnDestroy()
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;

            if (_gameManager != null)
            {
                _gameManager.ObjectiveChanged -= SetObjective;
                _gameManager.NotificationPosted -= ShowNotification;
                _gameManager.PhaseChanged -= HandlePhaseChanged;
                _gameManager.InputLockChanged -= HandleInputLockChanged;
            }

            if (playerInteractor != null)
            {
                playerInteractor.FocusChanged -= SetPrompt;
            }
        }

        #region Olay İşleyicileri

        private void SetObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = string.IsNullOrEmpty(objective) ? string.Empty : $"Görev: {objective}";
            }
        }

        private void ShowNotification(string message)
        {
            if (notificationText == null)
            {
                return;
            }

            notificationText.text = message;

            if (_notificationRoutine != null)
            {
                StopCoroutine(_notificationRoutine);
            }

            _notificationRoutine = StartCoroutine(NotificationRoutine());
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(phase == GamePhase.Completed);
            }
        }

        private void HandleInputLockChanged(bool locked)
        {
            if (crosshair != null)
            {
                crosshair.SetActive(!locked);
            }
        }

        private void SetPrompt(string prompt)
        {
            if (interactionPromptText == null)
            {
                return;
            }

            interactionPromptText.text = prompt ?? string.Empty;
            interactionPromptText.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
        }

        #endregion

        #region Coroutine'ler / Async

        private IEnumerator NotificationRoutine()
        {
            if (notificationGroup == null)
            {
                yield break;
            }

            notificationGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(notificationDuration);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                notificationGroup.alpha = 1f - t;
                yield return null;
            }

            notificationGroup.alpha = 0f;
            _notificationRoutine = null;
        }

        private IEnumerator HealthCheckLoop()
        {
            var wait = new WaitForSecondsRealtime(healthCheckInterval);
            SetLlmStatus("LLM: kontrol ediliyor...", Color.yellow);

            while (true)
            {
                if (!_isCheckingHealth)
                {
                    CheckHealthAsync().Forget(this);
                }

                yield return wait;
            }
        }

        private async Task CheckHealthAsync()
        {
            _isCheckingHealth = true;
            try
            {
                LLMResult result = await _llm.CheckHealthAsync(_lifetimeCts.Token);
                if (this == null)
                {
                    return;
                }

                if (result.Success)
                {
                    SetLlmStatus($"LLM: {result.Content}", new Color(0.4f, 1f, 0.4f));
                }
                else
                {
                    SetLlmStatus($"LLM: {result.ErrorMessage}", new Color(1f, 0.45f, 0.45f));
                }
            }
            finally
            {
                _isCheckingHealth = false;
            }
        }

        private void SetLlmStatus(string text, Color color)
        {
            if (llmStatusText == null)
            {
                return;
            }

            llmStatusText.text = text;
            llmStatusText.color = color;
        }

        #endregion

        #region Butonlar

        private static void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
