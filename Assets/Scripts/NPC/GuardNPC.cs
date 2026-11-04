using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Kilitli kapıyı koruyan LLM destekli NPC.
    /// Sorumluluklar: konuşma geçmişini tutmak, prompt oluşturmak, LLM sonucunu doğrulamak
    /// ve aksiyon İSTEĞİ yayınlamak. Kapıyı kendisi açmaz (GameManager onaylar).
    /// Bağımlılıklar yalnızca arayüzlerdir: ILLMService, IDialogueView.
    /// </summary>
    public sealed class GuardNPC : InteractableBase, IConversationalNpc
    {
        private readonly struct DialogueTurn
        {
            public DialogueTurn(string playerText, string npcText)
            {
                PlayerText = playerText;
                NpcText = npcText;
            }

            public string PlayerText { get; }
            public string NpcText { get; }
        }

        [Header("Kişilik")]
        [SerializeField] private NpcPersona persona;

        [Header("Bağımlılıklar")]
        [Tooltip("ILLMService uygulayan bileşen (OllamaLLMService veya MockLLMService)")]
        [SerializeField] private MonoBehaviour llmServiceBehaviour;

        [Tooltip("IDialogueView uygulayan bileşen (DialogueUI)")]
        [SerializeField] private MonoBehaviour dialogueViewBehaviour;

        [Header("Davranış")]
        [SerializeField] private bool facePlayerWhileTalking = true;
        [SerializeField, Min(0.1f)] private float turnSpeed = 5f;

        private ILLMService _llm;
        private IDialogueView _view;
        private readonly List<DialogueTurn> _history = new List<DialogueTurn>();
        private CancellationTokenSource _lifetimeCts;

        private Transform _interlocutor;
        private bool _inConversation;
        private bool _isAwaitingResponse;
        private bool _hasGreeted;

        public string DisplayName => persona != null ? persona.NpcName : name;
        public override string InteractionPrompt => $"{DisplayName} ile konuş [E]";
        protected override bool IsAvailable => !_inConversation;

        public event Action<bool> ConversationStateChanged;
        public event Action<NpcAction> ActionRequested;

        /// <summary>View bileşeni yok edilmiş olabilir; Unity null kontrolü MonoBehaviour üzerinden yapılır.</summary>
        private bool IsViewAlive => _view != null && dialogueViewBehaviour != null;

        #region Unity Yaşam Döngüsü

        private void Reset()
        {
            allowPlayer = true;
            allowAICompanion = false; // Yoldaş muhafızla konuşmaya çalışmasın.
        }

        private void Awake()
        {
            if (persona == null)
            {
                Debug.LogWarning("GuardNPC: Persona atanmamış, varsayılan kişilik kullanılıyor.", this);
                persona = ScriptableObject.CreateInstance<NpcPersona>();
            }

            _llm = InterfaceResolver.Resolve<ILLMService>(llmServiceBehaviour, this, nameof(llmServiceBehaviour));
            _view = InterfaceResolver.Resolve<IDialogueView>(dialogueViewBehaviour, this, nameof(dialogueViewBehaviour));
            _lifetimeCts = new CancellationTokenSource();
        }

        private void Update()
        {
            if (!_inConversation || !facePlayerWhileTalking || _interlocutor == null)
            {
                return;
            }

            Vector3 direction = _interlocutor.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            _lifetimeCts = null;

            if (_view != null)
            {
                _view.MessageSubmitted -= HandleMessageSubmitted;
                _view.CloseRequested -= EndConversation;
            }
        }

        #endregion

        #region Konuşma Akışı

        protected override void OnInteract(GameObject interactor, InteractorType interactorType)
        {
            if (!IsViewAlive)
            {
                Debug.LogError("GuardNPC: Diyalog arayüzü yok; konuşma başlatılamıyor.", this);
                return;
            }

            BeginConversation(interactor.transform);
        }

        private void BeginConversation(Transform interlocutor)
        {
            _inConversation = true;
            _interlocutor = interlocutor;

            _view.MessageSubmitted += HandleMessageSubmitted;
            _view.CloseRequested += EndConversation;

            _view.Open(DisplayName);
            _view.ClearMessages();

            foreach (DialogueTurn turn in _history)
            {
                if (!string.IsNullOrEmpty(turn.PlayerText))
                {
                    _view.AppendMessage(DialogueSpeaker.Player, "Sen", turn.PlayerText);
                }

                _view.AppendMessage(DialogueSpeaker.Npc, DisplayName, turn.NpcText);
            }

            if (!_hasGreeted)
            {
                _hasGreeted = true;
                _history.Add(new DialogueTurn(null, persona.Greeting));
                _view.AppendMessage(DialogueSpeaker.Npc, DisplayName, persona.Greeting);
            }

            if (_llm == null)
            {
                _view.AppendMessage(DialogueSpeaker.System, "Sistem", "LLM servisi bağlı değil. GuardNPC bileşenini kontrol edin.");
            }

            _view.SetThinking(_isAwaitingResponse, ThinkingLabel);
            _view.SetInputEnabled(!_isAwaitingResponse);

            ConversationStateChanged?.Invoke(true);
        }

        private void EndConversation()
        {
            if (!_inConversation)
            {
                return;
            }

            _inConversation = false;
            _interlocutor = null;

            if (IsViewAlive)
            {
                _view.MessageSubmitted -= HandleMessageSubmitted;
                _view.CloseRequested -= EndConversation;
                _view.Close();
            }

            // Not: Bekleyen LLM isteği iptal edilmez; yanıt gelince geçmişe eklenir ve
            // gerekirse kapı yine açılır (oyuncu pencereyi kapatsa bile mantık tutarlı kalır).
            ConversationStateChanged?.Invoke(false);
        }

        private void HandleMessageSubmitted(string message)
        {
            HandlePlayerMessageAsync(message).Forget(this);
        }

        private string ThinkingLabel => $"{DisplayName} düşünüyor";

        /// <summary>
        /// ASENKRON akış: await sırasında Unity çalışmaya devam eder (frame düşmez),
        /// UI "düşünüyor..." animasyonunu gösterir. Her hata türü yakalanır.
        /// </summary>
        private async Task HandlePlayerMessageAsync(string rawMessage)
        {
            if (_isAwaitingResponse)
            {
                return;
            }

            string message = SanitizePlayerMessage(rawMessage);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _isAwaitingResponse = true;
            AppendToView(DialogueSpeaker.Player, "Sen", message);
            SetBusyUi(true);

            try
            {
                if (_llm == null)
                {
                    HandleFailure(LLMErrorType.Unknown, "LLM servisi bağlı değil.");
                    return;
                }

                var request = new LLMRequest(
                    persona.SystemPrompt,
                    BuildPrompt(message),
                    NpcResponseSchema.Create(),
                    message);

                LLMResult result = await _llm.GenerateAsync(request, _lifetimeCts.Token);

                // await sonrası nesne yok edilmiş olabilir (sahne değişimi vb.).
                if (this == null)
                {
                    return;
                }

                if (result == null || !result.Success)
                {
                    HandleFailure(result?.ErrorType ?? LLMErrorType.Unknown, result?.ErrorMessage);
                    return;
                }

                if (!NpcDecisionParser.TryParse(result.Content, out NpcDecision decision, out string parseError))
                {
                    HandleFailure(LLMErrorType.InvalidResponse, parseError);
                    return;
                }

                AddToHistory(message, decision.Dialogue);
                AppendToView(DialogueSpeaker.Npc, DisplayName, decision.Dialogue);

                if (decision.Action != NpcAction.None)
                {
                    ActionRequested?.Invoke(decision.Action);
                }
            }
            catch (OperationCanceledException)
            {
                // Nesne yok edilirken iptal: sessizce çık.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (this != null)
                {
                    HandleFailure(LLMErrorType.Unknown, exception.Message);
                }
            }
            finally
            {
                _isAwaitingResponse = false;
                if (this != null)
                {
                    SetBusyUi(false);
                }
            }
        }

        #endregion

        #region Prompt ve Geçmiş

        private string BuildPrompt(string playerMessage)
        {
            var builder = new StringBuilder(512);
            builder.AppendLine("Şimdiye kadarki konuşma:");

            foreach (DialogueTurn turn in _history)
            {
                if (!string.IsNullOrEmpty(turn.PlayerText))
                {
                    builder.Append("Oyuncu: ").AppendLine(turn.PlayerText);
                }

                builder.Append(DisplayName).Append(": ").AppendLine(turn.NpcText);
            }

            builder.Append("Oyuncu: ").AppendLine(playerMessage);
            builder.AppendLine();
            builder.AppendLine("Oyuncunun SON mesajına karakterine uygun yanıt ver.");
            builder.AppendLine("Yalnızca şu JSON biçiminde yanıt ver: {\"response\": \"...\", \"action\": \"open_door\" veya \"none\"}");

            return builder.ToString();
        }

        private void AddToHistory(string playerText, string npcText)
        {
            _history.Add(new DialogueTurn(playerText, npcText));

            int overflow = _history.Count - persona.MaxHistoryTurns;
            if (overflow > 0)
            {
                _history.RemoveRange(0, overflow);
            }
        }

        private string SanitizePlayerMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(raw.Length);
            foreach (char character in raw)
            {
                if (!char.IsControl(character))
                {
                    builder.Append(character);
                }
            }

            string cleaned = builder.ToString().Trim();
            return cleaned.Length > persona.MaxPlayerMessageLength
                ? cleaned.Substring(0, persona.MaxPlayerMessageLength)
                : cleaned;
        }

        #endregion

        #region Hata Yönetimi ve UI Yardımcıları

        /// <summary>
        /// Güvenli varsayılan durum: hiçbir hata kapıyı açmaz. NPC karakter içi bir yedek
        /// cümle söyler, oyuncuya anlaşılır bir sistem mesajı gösterilir, oyun devam eder.
        /// Başarısız mesaj geçmişe eklenmez; oyuncu tekrar deneyebilir.
        /// </summary>
        private void HandleFailure(LLMErrorType errorType, string detail)
        {
            Debug.LogWarning($"[GuardNPC] LLM hatası ({errorType}): {detail}", this);

            if (errorType == LLMErrorType.Cancelled)
            {
                return;
            }

            string systemMessage = errorType switch
            {
                LLMErrorType.ConnectionFailed =>
                    "Ollama sunucusuna ulaşılamadı. Terminalde 'ollama serve' çalıştırıp tekrar dene.",
                LLMErrorType.Timeout =>
                    "Yanıt zaman aşımına uğradı (model ilk kez yükleniyor olabilir). Tekrar dene.",
                LLMErrorType.HttpError =>
                    $"Sunucu hatası: {detail}",
                LLMErrorType.InvalidResponse or LLMErrorType.EmptyResponse =>
                    "NPC'nin yanıtı anlaşılamadı (geçersiz JSON). Mesajını tekrar gönder.",
                _ =>
                    "Beklenmeyen bir hata oluştu. Tekrar dene."
            };

            AppendToView(DialogueSpeaker.Npc, DisplayName, persona.GetRandomFallbackLine());
            AppendToView(DialogueSpeaker.System, "Sistem", systemMessage);
        }

        private void AppendToView(DialogueSpeaker speaker, string speakerName, string text)
        {
            if (_inConversation && IsViewAlive)
            {
                _view.AppendMessage(speaker, speakerName, text);
            }
        }

        private void SetBusyUi(bool busy)
        {
            if (_inConversation && IsViewAlive)
            {
                _view.SetThinking(busy, ThinkingLabel);
                _view.SetInputEnabled(!busy);
            }
        }

        #endregion
    }
}
