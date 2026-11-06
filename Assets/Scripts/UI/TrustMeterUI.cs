using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureGame
{
    /// <summary>
    /// Sohbet penceresinin sağ üstündeki güven çubuğu (0-100).
    /// Sadece gösterir: güveni GuardNPC hesaplar, bu bileşen TrustChanged olayını dinler (SRP).
    /// Sahnede hazır obje gerekmez; DialogueUI kendine ekler ve çubuk panelin içinde kurulur.
    /// Renk: kırmızı (düşük) → sarı → yeşil (eşik ve üstü). Eşik, çubuk üzerinde dikey çizgiyle gösterilir.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrustMeterUI : MonoBehaviour
    {
        [Tooltip("Çubuğun ekleneceği panel (boşsa DialogueUI'nin paneli).")]
        [SerializeField] private RectTransform host;
        [SerializeField] private Vector2 size = new Vector2(380, 30);
        [SerializeField] private Vector2 offset = new Vector2(-30, -22);
        [SerializeField, Min(1f)] private float fillSpeed = 120f; // puan / sn (gerçek zaman)

        [Header("Renkler")]
        [SerializeField] private Color lowColor = new Color(0.86f, 0.27f, 0.22f);
        [SerializeField] private Color midColor = new Color(0.95f, 0.76f, 0.2f);
        [SerializeField] private Color highColor = new Color(0.3f, 0.8f, 0.38f);

        private readonly Dictionary<GuardNPC, Action<bool>> _conversationHandlers = new Dictionary<GuardNPC, Action<bool>>();
        private readonly Dictionary<GuardNPC, Action<int, int>> _trustHandlers = new Dictionary<GuardNPC, Action<int, int>>();
        private GuardNPC _active;
        private RectTransform _root;
        private RectTransform _fill;
        private Image _fillImage;
        private RectTransform _threshold;
        private TMP_Text _label;
        private float _shown;
        private int _target;
        private int _required;

        /// <summary>Gösterilen hedef güven değeri (testler için).</summary>
        public int DisplayedTrust => _target;
        /// <summary>Çubuk şu an görünür mü (testler için).</summary>
        public bool IsVisible => _root != null && _root.gameObject.activeSelf;
        /// <summary>Çubukta yazan metin (testler için).</summary>
        public string LabelText => _label != null ? _label.text : string.Empty;

        private void Start()
        {
            Build();
            foreach (GuardNPC guard in FindObjectsByType<GuardNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Watch(guard);
            }
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<GuardNPC, Action<bool>> pair in _conversationHandlers)
            {
                if (pair.Key == null) continue;
                pair.Key.ConversationStateChanged -= pair.Value;
                pair.Key.TrustChanged -= _trustHandlers[pair.Key];
            }

            _conversationHandlers.Clear();
            _trustHandlers.Clear();
        }

        private void Update()
        {
            if (_root == null || !_root.gameObject.activeInHierarchy) return;

            // Duraklatılmışken de akıcı olsun diye gerçek zaman kullanılır.
            _shown = Mathf.MoveTowards(_shown, _target, fillSpeed * Time.unscaledDeltaTime);
            ApplyVisuals(_shown);
        }

        /// <summary>Bir muhafızı izlemeye başlar (sahneye sonradan eklenenler için de çağrılabilir).</summary>
        public void Watch(GuardNPC guard)
        {
            if (guard == null || _conversationHandlers.ContainsKey(guard)) return;

            Action<bool> onConversation = open => HandleConversation(guard, open);
            Action<int, int> onTrust = (trust, required) => HandleTrustChanged(guard, trust, required);
            _conversationHandlers[guard] = onConversation;
            _trustHandlers[guard] = onTrust;
            guard.ConversationStateChanged += onConversation;
            guard.TrustChanged += onTrust;
        }

        #region Olaylar

        private void HandleConversation(GuardNPC guard, bool open)
        {
            if (open)
            {
                _active = guard;
                _required = Mathf.Clamp(guard.RequiredTrust, 0, 100);
                _target = Mathf.Clamp(guard.Trust, 0, 100);
                _shown = _target; // Pencere açılırken animasyonsuz başla.
                SetVisible(true);
                ApplyVisuals(_shown);
            }
            else if (_active == guard)
            {
                _active = null;
                SetVisible(false);
            }
        }

        private void HandleTrustChanged(GuardNPC guard, int trust, int required)
        {
            if (guard != _active) return; // Sadece konuşulan muhafız gösterilir.
            _target = Mathf.Clamp(trust, 0, 100);
            _required = Mathf.Clamp(required, 0, 100);
        }

        #endregion

        #region Görsel

        private void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        private void ApplyVisuals(float value)
        {
            if (_fill == null) return;

            _fill.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            _fillImage.color = ColorFor(value);

            float req = Mathf.Clamp01(_required / 100f);
            _threshold.anchorMin = new Vector2(req, 0f);
            _threshold.anchorMax = new Vector2(req, 1f);
            _threshold.gameObject.SetActive(_required > 0);

            int shown = Mathf.RoundToInt(value);
            string status = _required <= 0 ? string.Empty : shown >= _required ? "  (yeterli)" : $"  (gereken {_required})";
            _label.text = $"Güven: {shown}/100{status}";
        }

        /// <summary>Eşiğin altında kırmızı→sarı, eşik ve üstünde yeşil.</summary>
        private Color ColorFor(float value)
        {
            float req = _required > 0 ? _required : 60f;
            if (value >= req) return highColor;
            float t = Mathf.Clamp01(value / req);
            return t < 0.6f ? Color.Lerp(lowColor, midColor, t / 0.6f) : Color.Lerp(midColor, Color.Lerp(midColor, highColor, 0.5f), (t - 0.6f) / 0.4f);
        }

        private void Build()
        {
            if (host == null)
            {
                DialogueUI dialogue = GetComponent<DialogueUI>();
                if (dialogue == null) dialogue = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
                if (dialogue != null && dialogue.PanelRoot != null) host = dialogue.PanelRoot.transform as RectTransform;
            }

            if (host == null)
            {
                Debug.LogWarning("TrustMeterUI: Çubuğun ekleneceği panel bulunamadı; güven gösterilmeyecek.", this);
                return;
            }

            _root = NewRect("TrustMeter", host);
            _root.anchorMin = _root.anchorMax = new Vector2(1f, 1f);
            _root.pivot = new Vector2(1f, 1f);
            _root.anchoredPosition = offset;
            _root.sizeDelta = size;
            Image background = _root.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            RectTransform fillArea = NewRect("FillArea", _root);
            Stretch(fillArea, 3f);

            _fill = NewRect("Fill", fillArea);
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(0f, 1f);
            _fill.offsetMin = _fill.offsetMax = Vector2.zero;
            _fillImage = _fill.gameObject.AddComponent<Image>();
            _fillImage.raycastTarget = false;

            _threshold = NewRect("Threshold", fillArea);
            _threshold.pivot = new Vector2(0.5f, 0.5f);
            _threshold.sizeDelta = new Vector2(3f, 8f);
            _threshold.anchoredPosition = Vector2.zero;
            Image tick = _threshold.gameObject.AddComponent<Image>();
            tick.color = new Color(1f, 1f, 1f, 0.9f);
            tick.raycastTarget = false;

            RectTransform labelRt = NewRect("Label", _root);
            Stretch(labelRt, 0f);
            _label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 20;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;
            _label.raycastTarget = false;

            SetVisible(false);
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        #endregion
    }
}
