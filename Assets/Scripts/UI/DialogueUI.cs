using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdventureGame
{
    /// <summary>
    /// uGUI + TextMeshPro tabanlı sohbet penceresi (IDialogueView uygulaması).
    /// Sadece görüntüleme ve girdi toplama yapar; NPC/LLM mantığını bilmez (SRP).
    /// ÖNEMLİ: Bu bileşeni HER ZAMAN AKTİF bir objeye (örn. Canvas) ekleyin;
    /// açılıp kapanan kısım "panelRoot" alanına atanan alt objedir.
    /// </summary>
    public sealed class DialogueUI : MonoBehaviour, IDialogueView
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;

        [Header("Mesaj Geçmişi")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text logText;
        [SerializeField, Range(10, 200)] private int maxMessages = 80;

        [Header("Girdi")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("\"Düşünüyor...\" Göstergesi")]
        [SerializeField] private GameObject thinkingIndicator;
        [SerializeField] private TMP_Text thinkingLabel;
        [SerializeField, Min(0.05f)] private float dotInterval = 0.35f;

        [Header("Renkler")]
        [SerializeField] private Color playerColor = new Color(0.55f, 0.85f, 1f);
        [SerializeField] private Color npcColor = new Color(1f, 0.82f, 0.45f);
        [SerializeField] private Color systemColor = new Color(1f, 0.45f, 0.45f);

        private static readonly Regex NoParseTagRegex = new Regex("</?noparse>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<string> _entries = new List<string>();
        private Coroutine _thinkingRoutine;
        private bool _inputEnabled = true;

        public event Action<string> MessageSubmitted;
        public event Action CloseRequested;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>Açılıp kapanan panel (güven çubuğu gibi ek göstergeler buraya eklenir).</summary>
        public GameObject PanelRoot => panelRoot;

        #region Unity Yaşam Döngüsü

        private void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (thinkingIndicator != null) thinkingIndicator.SetActive(false);

            // Güven çubuğu: sahnede ayrıca obje kurmaya gerek kalmasın diye kendini ekler.
            if (GetComponent<TrustMeterUI>() == null) gameObject.AddComponent<TrustMeterUI>();

            if (sendButton != null) sendButton.onClick.AddListener(Submit);
            if (closeButton != null) closeButton.onClick.AddListener(RaiseCloseRequested);
            if (inputField != null) inputField.onSubmit.AddListener(_ => Submit());
        }

        private void OnDestroy()
        {
            if (sendButton != null) sendButton.onClick.RemoveListener(Submit);
            if (closeButton != null) closeButton.onClick.RemoveListener(RaiseCloseRequested);
            if (inputField != null) inputField.onSubmit.RemoveAllListeners();
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(closeKey))
            {
                RaiseCloseRequested();
            }
        }

        #endregion

        #region IDialogueView

        public void Open(string title)
        {
            if (panelRoot == null)
            {
                Debug.LogError("DialogueUI: panelRoot atanmamış.", this);
                return;
            }

            panelRoot.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (inputField != null) inputField.text = string.Empty;

            // [E] tuşunun karakter olarak giriş kutusuna yazılmaması için bir kare bekle.
            StartCoroutine(FocusInputNextFrame());
        }

        public void Close()
        {
            SetThinking(false);
            if (panelRoot != null) panelRoot.SetActive(false);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void ClearMessages()
        {
            _entries.Clear();
            RefreshLog();
        }

        public void AppendMessage(DialogueSpeaker speaker, string speakerName, string text)
        {
            string color = ColorUtility.ToHtmlStringRGB(GetColor(speaker));
            string entry = speaker == DialogueSpeaker.System
                ? $"<color=#{color}><i>[{Escape(speakerName)}] {Escape(text)}</i></color>"
                : $"<color=#{color}><b>{Escape(speakerName)}:</b></color> {Escape(text)}";

            _entries.Add(entry);
            if (_entries.Count > maxMessages)
            {
                _entries.RemoveRange(0, _entries.Count - maxMessages);
            }

            RefreshLog();
        }

        public void SetThinking(bool isThinking, string label = null)
        {
            if (_thinkingRoutine != null)
            {
                StopCoroutine(_thinkingRoutine);
                _thinkingRoutine = null;
            }

            if (thinkingIndicator != null)
            {
                thinkingIndicator.SetActive(isThinking);
            }

            if (isThinking && isActiveAndEnabled)
            {
                _thinkingRoutine = StartCoroutine(AnimateThinking(string.IsNullOrEmpty(label) ? "Düşünüyor" : label));
            }
        }

        public void SetInputEnabled(bool isEnabled)
        {
            _inputEnabled = isEnabled;
            if (inputField != null) inputField.interactable = isEnabled;
            if (sendButton != null) sendButton.interactable = isEnabled;

            if (isEnabled && IsOpen && isActiveAndEnabled)
            {
                StartCoroutine(FocusInputNextFrame());
            }
        }

        #endregion

        #region İç Mantık

        private void Submit()
        {
            if (!IsOpen || !_inputEnabled || inputField == null)
            {
                return;
            }

            string text = inputField.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            inputField.text = string.Empty;
            MessageSubmitted?.Invoke(text);
        }

        private void RaiseCloseRequested() => CloseRequested?.Invoke();

        private void RefreshLog()
        {
            if (logText == null)
            {
                return;
            }

            logText.text = string.Join("\n\n", _entries);

            if (isActiveAndEnabled && IsOpen)
            {
                StartCoroutine(ScrollToBottomNextFrame());
            }
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null; // Layout'un yeniden hesaplanmasını bekle.
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private IEnumerator FocusInputNextFrame()
        {
            yield return null;
            if (inputField != null && inputField.interactable && IsOpen)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        /// <summary>Gerçek zamanlı (timeScale'den bağımsız) nokta animasyonu: "Düşünüyor." → ".." → "..."</summary>
        private IEnumerator AnimateThinking(string label)
        {
            var wait = new WaitForSecondsRealtime(dotInterval);
            int dots = 0;

            while (true)
            {
                dots = (dots % 3) + 1;
                if (thinkingLabel != null)
                {
                    thinkingLabel.text = label + new string('.', dots);
                }

                yield return wait;
            }
        }

        private Color GetColor(DialogueSpeaker speaker) => speaker switch
        {
            DialogueSpeaker.Player => playerColor,
            DialogueSpeaker.Npc => npcColor,
            _ => systemColor
        };

        /// <summary>Oyuncu veya LLM metnindeki rich-text etiketlerinin UI'ı bozmasını engeller.</summary>
        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return $"<noparse>{NoParseTagRegex.Replace(text, string.Empty)}</noparse>";
        }

        #endregion
    }
}
