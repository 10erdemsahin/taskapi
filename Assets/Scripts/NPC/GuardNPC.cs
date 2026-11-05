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
    /// Bağımlılıklar yalnızca arayüzlerdir: ILLMService, IDialogueView, INpcContextProvider.
    ///
    /// Zorluk kuralları (hepsi Unity tarafında doğrulanır, LLM kandırılsa bile geçerlidir):
    ///  - İki bilmece sırayla çözülmeli. Bir bilmece, LLM "çözüldü" dese bile oyuncunun mesajında
    ///    o bilmecenin kabul edilen cevabı geçmiyorsa çözülmüş SAYILMAZ.
    ///  - open_door yalnızca tüm bilmeceler çözülünce GameManager'a iletilir.
    ///  - Art arda yanlış cevap denemesi sınırı → bekleme süresi (bu sırada mesaj LLM'e gitmez).
    ///  - Cevap sızıntısı filtresi: NPC sıradaki bilmecenin cevabını söylerse yanıt gösterilmez.
    ///  - Güven puanı: LLM yalnızca mesajın TONUNU söyler (polite/neutral/rude), puanı Unity hesaplar.
    ///    Güven persona.RequiredTrust'ın altındayken open_door iletilmez. Kaba olmayan her mesaj güveni
    ///    artırır (bilerek: "güven zamanla artar"); bu bir açık değildir çünkü kapı ayrıca bilmece
    ///    cevaplarının oyuncunun mesajında geçmesini şart koşar.
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

        [Tooltip("(Opsiyonel) INpcContextProvider uygulayan bileşen (GameManager) — prompt'a oyun durumu ekler.")]
        [SerializeField] private MonoBehaviour contextProviderBehaviour;

        [Header("Davranış")]
        [SerializeField] private bool facePlayerWhileTalking = true;
        [SerializeField, Min(0.1f)] private float turnSpeed = 5f;
        [Tooltip("NPC cevabı sızdırırsa isteğin kaç kez tekrar gönderileceği.")]
        [SerializeField, Range(0, 3)] private int leakRetryCount = 1;

        private ILLMService _llm;
        private IDialogueView _view;
        private INpcContextProvider _contextProvider;
        private readonly List<DialogueTurn> _history = new List<DialogueTurn>();
        private CancellationTokenSource _lifetimeCts;

        private Transform _interlocutor;
        private bool _inConversation;
        private bool _isAwaitingResponse;
        private bool _hasGreeted;

        private int _solvedRiddles;
        private int _wrongAttempts;
        private float _cooldownUntil = -1f;
        private int _trust = -1; // -1: henüz persona'dan okunmadı (Awake'te ayarlanır)

        public string DisplayName => persona != null ? persona.NpcName : name;
        public override string InteractionPrompt => $"{DisplayName} ile konuş [E]";
        protected override bool IsAvailable => !_inConversation;

        public int SolvedRiddleCount => _solvedRiddles;
        public int RequiredRiddleCount => persona != null ? persona.RiddleCount : 0;
        public bool AllRiddlesSolved => _solvedRiddles >= RequiredRiddleCount;
        public int WrongAttempts => _wrongAttempts;
        public bool IsOnCooldown => Time.time < _cooldownUntil;
        public int Trust => _trust;
        public int RequiredTrust => persona != null ? persona.RequiredTrust : 0;
        public bool HasEnoughTrust => _trust >= RequiredTrust;

        /// <summary>Güven puanı değişince (yeni değer, gereken değer).</summary>
        public event Action<int, int> TrustChanged;

        public event Action<bool> ConversationStateChanged;
        public event Action<NpcAction> ActionRequested;

        /// <summary>View bileşeni yok edilmiş olabilir; Unity null kontrolü MonoBehaviour üzerinden yapılır.</summary>
        private bool IsViewAlive => _view != null && dialogueViewBehaviour != null;

        private Riddle CurrentRiddle => persona != null ? persona.GetRiddle(_solvedRiddles) : null;

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

            if (contextProviderBehaviour != null)
            {
                _contextProvider = InterfaceResolver.Resolve<INpcContextProvider>(contextProviderBehaviour, this, nameof(contextProviderBehaviour));
            }

            _trust = persona.InitialTrust;
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

            if (IsOnCooldown)
            {
                _view.AppendMessage(DialogueSpeaker.System, "Sistem", CooldownMessage());
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

            AppendToView(DialogueSpeaker.Player, "Sen", message);

            // Bekleme süresi: mesaj LLM'e GİTMEZ.
            if (IsOnCooldown)
            {
                AppendToView(DialogueSpeaker.Npc, DisplayName, "(Muhafız sırtını dönmüş, seni duymazdan geliyor.)");
                AppendToView(DialogueSpeaker.System, "Sistem", CooldownMessage());
                return;
            }

            _isAwaitingResponse = true;
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

                NpcDecision decision = null;
                bool leaked = false;

                for (int attempt = 0; attempt <= leakRetryCount; attempt++)
                {
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

                    if (!NpcDecisionParser.TryParse(result.Content, out decision, out string parseError))
                    {
                        HandleFailure(LLMErrorType.InvalidResponse, parseError);
                        return;
                    }

                    leaked = LeaksCurrentAnswer(decision, message);
                    if (!leaked)
                    {
                        break;
                    }

                    Debug.LogWarning($"[GuardNPC] Cevap sızıntısı tespit edildi (deneme {attempt + 1}): {decision.Dialogue}", this);
                }

                ApplyDecision(message, decision, leaked);
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

        /// <summary>LLM kararını oyun kurallarıyla birleştirir: bilmece sayacı, deneme hakkı, kapı isteği.</summary>
        private void ApplyDecision(string playerMessage, NpcDecision decision, bool leaked)
        {
            Riddle riddle = CurrentRiddle;

            // Unity doğrulaması: LLM "çözüldü" dese bile mesajda sıradaki bilmecenin cevabı olmalı.
            bool solved = riddle != null && decision.RiddleSolved && riddle.IsMentionedIn(playerMessage);
            if (riddle != null && decision.RiddleSolved && !solved)
            {
                Debug.LogWarning("[GuardNPC] LLM bilmeceyi çözülmüş saydı ama oyuncunun mesajında sıradaki cevap yok; yok sayıldı.", this);
            }

            string dialogue = leaked ? persona.GetRandomFallbackLine() : decision.Dialogue;
            if (solved && leaked)
            {
                // Çözülmüşken sızıntı filtresi zaten devreye girmez; güvenlik için.
                dialogue = decision.Dialogue;
            }

            AddToHistory(playerMessage, dialogue);
            AppendToView(DialogueSpeaker.Npc, DisplayName, dialogue);

            ApplyTrust(decision.Tone);

            if (solved)
            {
                _solvedRiddles++;
                _wrongAttempts = 0;

                if (!AllRiddlesSolved)
                {
                    AppendToView(DialogueSpeaker.System, "Sistem", $"Bilmece çözüldü ({_solvedRiddles}/{RequiredRiddleCount}).");
                }
            }
            else if (decision.IsAnswerAttempt && !AllRiddlesSolved)
            {
                RegisterWrongAttempt();
            }

            if (decision.Action == NpcAction.OpenDoor)
            {
                if (AllRiddlesSolved && !HasEnoughTrust)
                {
                    Debug.LogWarning($"[GuardNPC] Güven yetersiz ({_trust}/{RequiredTrust}); open_door yok sayıldı.", this);
                    AppendToView(DialogueSpeaker.System, "Sistem",
                        $"Muhafız sana henüz yeterince güvenmiyor (güven {_trust}/{RequiredTrust}). Onunla biraz daha konuş.");
                }
                else if (AllRiddlesSolved)
                {
                    ActionRequested?.Invoke(NpcAction.OpenDoor);
                }
                else
                {
                    Debug.LogWarning($"[GuardNPC] Bilmeceler tamamlanmadı ({_solvedRiddles}/{RequiredRiddleCount}); open_door yok sayıldı.", this);
                }
            }
            else if (decision.Action != NpcAction.None)
            {
                ActionRequested?.Invoke(decision.Action);
            }
        }

        /// <summary>Güveni tona göre günceller (0-100 arası). Kabalık oyuncuya sistem mesajıyla bildirilir.</summary>
        private void ApplyTrust(PlayerTone tone)
        {
            int previous = _trust;
            _trust = Mathf.Clamp(_trust + persona.GetTrustDelta(tone), 0, 100);
            if (_trust == previous)
            {
                return;
            }

            if (tone == PlayerTone.Rude)
            {
                AppendToView(DialogueSpeaker.System, "Sistem", $"Muhafız kabalığından hoşlanmadı. Güven: {_trust}/100");
            }

            TrustChanged?.Invoke(_trust, RequiredTrust);
        }

        private void RegisterWrongAttempt()
        {
            _wrongAttempts++;
            int remaining = persona.MaxWrongAttempts - _wrongAttempts;

            if (remaining > 0)
            {
                AppendToView(DialogueSpeaker.System, "Sistem", $"Yanlış cevap. Kalan hak: {remaining}");
                return;
            }

            _wrongAttempts = 0;
            _cooldownUntil = Time.time + persona.CooldownSeconds;
            AppendToView(DialogueSpeaker.System, "Sistem",
                $"Muhafız sabrını yitirdi ve sırtını döndü. {Mathf.CeilToInt(persona.CooldownSeconds)} sn sonra tekrar dene.");
        }

        /// <summary>NPC, henüz çözülmemiş sıradaki bilmecenin cevabını söyledi mi?</summary>
        private bool LeaksCurrentAnswer(NpcDecision decision, string playerMessage)
        {
            Riddle riddle = CurrentRiddle;
            if (riddle == null) return false;

            // Bu mesajla bilmece gerçekten çözüldüyse NPC'nin cevabı tekrar etmesi sızıntı değildir.
            if (decision.RiddleSolved && riddle.IsMentionedIn(playerMessage)) return false;

            return riddle.IsMentionedIn(decision.Dialogue);
        }

        private string CooldownMessage()
        {
            int seconds = Mathf.Max(1, Mathf.CeilToInt(_cooldownUntil - Time.time));
            return $"Muhafız seni dinlemiyor. {seconds} sn bekle.";
        }

        #endregion

        #region Prompt ve Geçmiş

        private string BuildPrompt(string playerMessage)
        {
            var builder = new StringBuilder(768);

            builder.Append("[Oyun durumu] Çözülen bilmece: ").Append(_solvedRiddles).Append('/').Append(RequiredRiddleCount);
            Riddle riddle = CurrentRiddle;
            if (riddle != null)
            {
                builder.Append(" — SIRADAKİ bilmece (").Append(_solvedRiddles + 1).Append(". bilmece): \"").Append(riddle.question).Append('"');
            }

            builder.AppendLine();
            builder.Append("[Oyun durumu] Yolcuya güvenin: ").Append(_trust).Append("/100 (kapı için en az ")
                   .Append(RequiredTrust).Append(HasEnoughTrust ? ", yeterli)" : ", YETERSİZ)").AppendLine();

            string context = _contextProvider?.GetNpcContext();
            if (!string.IsNullOrWhiteSpace(context))
            {
                builder.Append("[Oyun durumu] ").AppendLine(context);
            }

            builder.AppendLine();
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
            builder.AppendLine("Yalnızca şu JSON biçiminde yanıt ver: {\"response\": \"...\", \"action\": \"open_door\" veya \"none\", \"riddle_solved\": true/false, \"is_answer_attempt\": true/false, \"tone\": \"polite\" veya \"neutral\" veya \"rude\"}");

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
        /// Başarısız mesaj geçmişe eklenmez ve deneme hakkından düşülmez; oyuncu tekrar deneyebilir.
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
