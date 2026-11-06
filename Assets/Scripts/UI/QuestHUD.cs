using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureGame
{
    /// <summary>
    /// Yankı Taşı görevinin HUD'u: sağ üstte "Yankı Taşı: 2/5" sayacı ve tamamlanınca açılan ipucu paneli.
    /// [H] tuşu ipucunu tekrar gösterir (görev bittiyse, girdi kilitli/duraklatılmış değilse).
    /// Sadece ICollectionQuest olaylarını dinler. Sahnede hazır obje gerekmez: GameHUD kendine ekler,
    /// arayüzü Canvas altında kendisi kurar. Sahnede görev yoksa hiçbir şey göstermez.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestHUD : MonoBehaviour
    {
        [Tooltip("ICollectionQuest uygulayan bileşen (boşsa sahnedeki CollectionQuest aranır).")]
        [SerializeField] private MonoBehaviour questBehaviour;
        [SerializeField] private KeyCode hintKey = KeyCode.H;
        [SerializeField, Min(1f)] private float hintDuration = 8f;
        [SerializeField] private Color progressColor = new Color(0.55f, 0.9f, 1f);
        [SerializeField] private Color completedColor = new Color(0.45f, 0.95f, 0.5f);

        private ICollectionQuest _quest;
        private string _questName = "Yankı Taşı";
        private TMP_Text _counter;
        private GameObject _hintPanel;
        private TMP_Text _hintText;
        private Coroutine _hintRoutine;

        public bool HasQuest => _quest != null;
        public string CounterText => _counter != null ? _counter.text : string.Empty;
        public bool IsHintVisible => _hintPanel != null && _hintPanel.activeSelf;

        private void Start()
        {
            if (questBehaviour != null)
            {
                _quest = InterfaceResolver.Resolve<ICollectionQuest>(questBehaviour, this, nameof(questBehaviour));
            }
            else
            {
                CollectionQuest found = FindAnyObjectByType<CollectionQuest>();
                if (found != null)
                {
                    _quest = found;
                    _questName = found.QuestName;
                }
            }

            if (_quest == null) return; // Görev sahneye bağlı değil: HUD sessiz kalır.

            ICollectionQuest quest = _quest;
            _quest = null;
            Bind(quest);
        }

        /// <summary>Görevi sonradan bağlar (sahneye çalışma anında eklenen görevler ve testler için).</summary>
        public void Bind(ICollectionQuest quest)
        {
            if (ReferenceEquals(quest, _quest)) return;
            Unsubscribe();
            _quest = quest;
            if (_quest == null) return;

            if (_quest is CollectionQuest concrete) _questName = concrete.QuestName;
            if (_counter == null) Build();
            _quest.ProgressChanged += HandleProgress;
            _quest.Completed += HandleCompleted;
            HandleProgress(_quest.CollectedCount, _quest.RequiredCount);
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_quest == null) return;
            _quest.ProgressChanged -= HandleProgress;
            _quest.Completed -= HandleCompleted;
        }

        private void Update()
        {
            if (_quest == null || !_quest.IsCompleted || !Input.GetKeyDown(hintKey)) return;

            GameManager gm = GameManager.Instance;
            if (gm != null && (gm.IsInputLocked || gm.IsPaused)) return;

            ShowHint(_quest.HintText);
        }

        private void HandleProgress(int collected, int required)
        {
            if (_counter == null) return;
            bool done = required > 0 && collected >= required;
            _counter.text = done ? $"{_questName}: {collected}/{required} (tamam)" : $"{_questName}: {collected}/{required}";
            _counter.color = done ? completedColor : progressColor;
        }

        private void HandleCompleted(string hint) => ShowHint(hint);

        /// <summary>İpucu panelini gösterir; süre gerçek zamanla sayılır.</summary>
        public void ShowHint(string hint)
        {
            if (_hintPanel == null || string.IsNullOrWhiteSpace(hint)) return;

            _hintText.text = hint;
            _hintPanel.SetActive(true);
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = StartCoroutine(HideAfter(hintDuration));
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _hintPanel.SetActive(false);
            _hintRoutine = null;
        }

        #region Arayüz kurulumu

        private void Build()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("QuestHUD: Canvas bulunamadı.", this);
                return;
            }

            Transform root = canvas.transform;

            // Sayaç: LLM durum yazısının altında, sağ üst.
            RectTransform counterRt = NewRect("QuestCounterText", root);
            counterRt.anchorMin = counterRt.anchorMax = counterRt.pivot = new Vector2(1f, 1f);
            counterRt.anchoredPosition = new Vector2(-30f, -85f);
            counterRt.sizeDelta = new Vector2(500f, 40f);
            _counter = counterRt.gameObject.AddComponent<TextMeshProUGUI>();
            _counter.fontSize = 26;
            _counter.fontStyle = FontStyles.Bold;
            _counter.alignment = TextAlignmentOptions.TopRight;
            _counter.raycastTarget = false;

            // İpucu paneli: ekran ortası, yarı saydam, italik.
            RectTransform panelRt = NewRect("HintPanel", root);
            panelRt.anchorMin = panelRt.anchorMax = panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = new Vector2(0f, 120f);
            panelRt.sizeDelta = new Vector2(900f, 200f);
            Image bg = panelRt.gameObject.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.08f, 0.12f, 0.82f);
            bg.raycastTarget = false;

            RectTransform titleRt = NewRect("HintTitle", panelRt);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -14f);
            titleRt.sizeDelta = new Vector2(-40f, 40f);
            TMP_Text title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            title.text = $"{_questName} fısıldıyor...";
            title.fontSize = 26;
            title.fontStyle = FontStyles.Bold;
            title.color = progressColor;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;

            RectTransform textRt = NewRect("HintText", panelRt);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(30f, 36f);
            textRt.offsetMax = new Vector2(-30f, -56f);
            _hintText = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            _hintText.fontSize = 26;
            _hintText.fontStyle = FontStyles.Italic;
            _hintText.alignment = TextAlignmentOptions.Center;
            _hintText.textWrappingMode = TextWrappingModes.Normal;
            _hintText.raycastTarget = false;

            RectTransform footRt = NewRect("HintFooter", panelRt);
            footRt.anchorMin = new Vector2(0f, 0f);
            footRt.anchorMax = new Vector2(1f, 0f);
            footRt.pivot = new Vector2(0.5f, 0f);
            footRt.anchoredPosition = new Vector2(0f, 8f);
            footRt.sizeDelta = new Vector2(-40f, 28f);
            TMP_Text foot = footRt.gameObject.AddComponent<TextMeshProUGUI>();
            foot.text = $"[{hintKey}] ipucunu tekrar göster";
            foot.fontSize = 18;
            foot.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            foot.alignment = TextAlignmentOptions.Center;
            foot.raycastTarget = false;

            _hintPanel = panelRt.gameObject;
            _hintPanel.SetActive(false);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        #endregion
    }
}
