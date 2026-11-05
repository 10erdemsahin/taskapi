using System;
using System.Text;
using UnityEngine;

namespace AdventureGame
{
    public enum GamePhase
    {
        /// <summary>Bölüm 1: Köprü kalkık, yoldaş şalteri çekmeli + oyuncu basınç plakasında durmalı.</summary>
        PhysicalObstacle,

        /// <summary>Bölüm 2: Köprü indi, muhafız NPC ikna edilmeli (Yankı Taşı + bilmeceler).</summary>
        SocialObstacle,

        Completed
    }

    /// <summary>
    /// Oyun akışının ARABULUCUSU (Mediator) ve durum makinesi.
    /// Alt sistemler (yoldaş, köprü, NPC, kapı, toplama görevi) birbirini TANIMAZ; yalnızca arayüzler
    /// üzerinden olay (event) yayınlar. GameManager bu olayları dinleyip oyun kurallarını uygular.
    /// Bu sayede sistemler gevşek bağlıdır ve tek tek test edilebilir.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour, INpcContextProvider
    {
        public static GameManager Instance { get; private set; }

        [Header("Sahne Referansları")]
        [SerializeField] private Transform player;

        [Tooltip("IAICompanion uygulayan bileşen (NavMeshCompanionController)")]
        [SerializeField] private MonoBehaviour companionBehaviour;

        [Tooltip("IActivatable uygulayan bileşen (DrawBridge)")]
        [SerializeField] private MonoBehaviour bridgeBehaviour;

        [Tooltip("IConversationalNpc uygulayan bileşen (GuardNPC)")]
        [SerializeField] private MonoBehaviour npcBehaviour;

        [Tooltip("IOpenable uygulayan bileşen (InteractiveDoor)")]
        [SerializeField] private MonoBehaviour doorBehaviour;

        [Tooltip("(Opsiyonel) ICollectionQuest uygulayan bileşen (CollectionQuest). Boşsa toplama kuralı uygulanmaz.")]
        [SerializeField] private MonoBehaviour collectionQuestBehaviour;

        [Header("Ayarlar")]
        [Tooltip("Oyuncu uçurum kenarına gelince yoldaş kendiliğinden göreve başlasın mı?")]
        [SerializeField] private bool autoDispatchCompanionAtObstacle;

        private IAICompanion _companion;
        private IActivatable _bridge;
        private IConversationalNpc _npc;
        private IOpenable _door;
        private ICollectionQuest _quest;

        private bool _obstacleReached;
        private bool _inDialogue;

        public GamePhase Phase { get; private set; } = GamePhase.PhysicalObstacle;
        public bool IsInputLocked { get; private set; }
        public bool IsPaused { get; private set; }
        public string CurrentObjective { get; private set; } = string.Empty;

        /// <summary>Toplama görevi yoksa veya tamamlandıysa true.</summary>
        public bool IsCollectionQuestSatisfied => _quest == null || _quest.IsCompleted;

        public event Action<GamePhase> PhaseChanged;
        public event Action<bool> InputLockChanged;
        public event Action<bool> PausedChanged;
        public event Action<string> ObjectiveChanged;
        public event Action<string> NotificationPosted;

        #region Unity Yaşam Döngüsü

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Sahnede birden fazla GameManager var; fazlası yok ediliyor.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _companion = InterfaceResolver.Resolve<IAICompanion>(companionBehaviour, this, nameof(companionBehaviour));
            _bridge = InterfaceResolver.Resolve<IActivatable>(bridgeBehaviour, this, nameof(bridgeBehaviour));
            _npc = InterfaceResolver.Resolve<IConversationalNpc>(npcBehaviour, this, nameof(npcBehaviour));
            _door = InterfaceResolver.Resolve<IOpenable>(doorBehaviour, this, nameof(doorBehaviour));

            if (collectionQuestBehaviour != null)
            {
                _quest = InterfaceResolver.Resolve<ICollectionQuest>(collectionQuestBehaviour, this, nameof(collectionQuestBehaviour));
            }
        }

        private void OnEnable()
        {
            if (_companion != null) _companion.Reported += HandleCompanionReport;
            if (_bridge != null) _bridge.Activated += HandleBridgeActivated;
            if (_npc != null)
            {
                _npc.ConversationStateChanged += HandleConversationStateChanged;
                _npc.ActionRequested += HandleNpcActionRequested;
            }
            if (_door != null) _door.Opened += HandleDoorOpened;
            SubscribeQuest(_quest);
        }

        private void OnDisable()
        {
            if (_companion != null) _companion.Reported -= HandleCompanionReport;
            if (_bridge != null) _bridge.Activated -= HandleBridgeActivated;
            if (_npc != null)
            {
                _npc.ConversationStateChanged -= HandleConversationStateChanged;
                _npc.ActionRequested -= HandleNpcActionRequested;
            }
            if (_door != null) _door.Opened -= HandleDoorOpened;
            UnsubscribeQuest(_quest);
        }

        private void Start()
        {
            SetPhase(GamePhase.PhysicalObstacle);
            SetObjective("Etrafı keşfet ve karşı tarafa geçmenin bir yolunu bul." + QuestObjectiveSuffix());
            SetInputLocked(false);

            if (_companion != null && player != null)
            {
                _companion.Follow(player);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                // Duraklatılmışken sahne değişirse yeni sahne donuk başlamasın.
                Time.timeScale = 1f;
                Instance = null;
            }
        }

        #endregion

        #region Dış Bileşenlerin Çağırdığı API (Trigger Zone'lar, Oyuncu Girdisi)

        /// <summary>ObstacleZone (PlayerTriggerZone UnityEvent) tarafından çağrılır.</summary>
        public void NotifyObstacleReached()
        {
            if (Phase != GamePhase.PhysicalObstacle || _obstacleReached)
            {
                return;
            }

            _obstacleReached = true;
            SetObjective("Köprü kalkık! Şalter yüksek platformda: yoldaşından yardım iste [F] ve köprü kilidi için basınç plakasında dur.");
            PostNotification("Bu uçurumu tek başına geçemezsin...");

            if (autoDispatchCompanionAtObstacle)
            {
                RequestCompanionHelp();
            }
        }

        /// <summary>
        /// Oyuncu [F] tuşuna bastığında PlayerInteractor tarafından çağrılır.
        /// Köprü indikten sonra da çalışır: yüksek platformdaki Yankı Taşı parçası gibi
        /// yalnızca yoldaşın alabileceği hedefler kalmış olabilir (yoksa oyun kilitlenirdi).
        /// </summary>
        public void RequestCompanionHelp()
        {
            if (_companion == null)
            {
                PostNotification("Sahnede bir yoldaş bulunamadı.");
                return;
            }

            if (Phase == GamePhase.Completed)
            {
                PostNotification($"{_companion.DisplayName}: Başardık! Yapılacak bir şey kalmadı.");
                return;
            }

            if (_companion.IsBusy)
            {
                PostNotification($"{_companion.DisplayName}: Zaten üzerinde çalışıyorum!");
                return;
            }

            // Başarısızlık durumunda yoldaş nedenini kendisi Report eder.
            _companion.TrySolveObstacle();
        }

        /// <summary>GoalZone (PlayerTriggerZone UnityEvent) tarafından çağrılır.</summary>
        public void CompleteGame()
        {
            if (Phase == GamePhase.Completed)
            {
                return;
            }

            if (_door != null && !_door.IsOpen)
            {
                Debug.LogWarning("Kapı açılmadan bitiş bölgesine girildi; yok sayılıyor.", this);
                return;
            }

            if (IsPaused)
            {
                SetPaused(false);
            }

            SetPhase(GamePhase.Completed);
            SetObjective("Tebrikler! Macerayı tamamladın.");
            SetInputLocked(true);
        }

        /// <summary>
        /// Duraklatma menüsü (PauseMenuUI) tarafından çağrılır. Oyun zamanını durdurur,
        /// oyuncu girdisini kilitler ve imleci serbest bırakır. Oyun bittiyse duraklatılamaz.
        /// UI'de çalışacak animasyonlar Time.unscaledDeltaTime / WaitForSecondsRealtime kullanmalıdır.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused == IsPaused || (paused && Phase == GamePhase.Completed))
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            SetInputLocked(IsPaused || _inDialogue || Phase == GamePhase.Completed);
            PausedChanged?.Invoke(paused);
        }

        /// <summary>
        /// Toplama görevini çalışma anında bağlar (Inspector yerine kodla bağlamak veya test için).
        /// Önceki görevin aboneliği bırakılır.
        /// </summary>
        public void BindCollectionQuest(ICollectionQuest quest)
        {
            if (ReferenceEquals(quest, _quest)) return;

            if (isActiveAndEnabled) UnsubscribeQuest(_quest);
            _quest = quest;
            if (isActiveAndEnabled) SubscribeQuest(_quest);
        }

        public void PostNotification(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                NotificationPosted?.Invoke(message);
            }
        }

        /// <summary>INpcContextProvider: muhafızın prompt'una eklenen güncel oyun durumu.</summary>
        public string GetNpcContext()
        {
            var builder = new StringBuilder();

            if (_quest != null)
            {
                builder.Append("Yankı Taşı parçaları: ")
                       .Append(_quest.CollectedCount).Append('/').Append(_quest.RequiredCount)
                       .Append(_quest.IsCompleted
                           ? " (TAMAMLANDI — yolcu taşı birleştirdi)"
                           : " (TAMAMLANMADI — bilmeceyi bilse bile kapıyı açma, önce taşın parçalarını iste)");
            }

            return builder.ToString();
        }

        #endregion

        #region Olay İşleyicileri (Mediator Kuralları)

        private void HandleCompanionReport(string message)
        {
            PostNotification($"{_companion.DisplayName}: {message}");
        }

        private void HandleBridgeActivated()
        {
            if (Phase != GamePhase.PhysicalObstacle)
            {
                return;
            }

            SetPhase(GamePhase.SocialObstacle);
            SetObjective("Köprü indi! Karşıya geç ve kapıyı koruyan muhafızla konuş [E]." + QuestObjectiveSuffix());

            if (_companion != null && player != null)
            {
                _companion.Follow(player);
            }
        }

        private void HandleConversationStateChanged(bool isActive)
        {
            _inDialogue = isActive;
            SetInputLocked(_inDialogue || IsPaused || Phase == GamePhase.Completed);
        }

        /// <summary>
        /// LLM'den gelen aksiyon burada OYUN KURALLARINA göre doğrulanır.
        /// LLM yanlışlıkla (veya kandırılarak) "open_door" üretse bile:
        /// yanlış aşamada veya Yankı Taşı tamamlanmadan kapı açılmaz.
        /// </summary>
        private void HandleNpcActionRequested(NpcAction action)
        {
            switch (action)
            {
                case NpcAction.OpenDoor:
                    if (Phase != GamePhase.SocialObstacle)
                    {
                        Debug.LogWarning($"'{action}' aksiyonu {Phase} aşamasında geçersiz; reddedildi.", this);
                        return;
                    }

                    if (!IsCollectionQuestSatisfied)
                    {
                        Debug.LogWarning("Yankı Taşı parçaları eksik; open_door reddedildi.", this);
                        PostNotification($"Muhafız: Önce Yankı Taşı'nı tamamla ({_quest.CollectedCount}/{_quest.RequiredCount}).");
                        return;
                    }

                    if (_door == null || _door.IsOpen)
                    {
                        return;
                    }

                    _door.Open();
                    break;

                case NpcAction.None:
                default:
                    break;
            }
        }

        private void HandleDoorOpened()
        {
            PostNotification("Taş kapı gıcırdayarak açıldı!");
            SetObjective("Kapı açıldı! Geçitten geçerek macerayı tamamla.");
        }

        private void HandleQuestProgress(int collected, int required)
        {
            if (collected < required)
            {
                PostNotification($"Yankı Taşı parçası bulundu ({collected}/{required}).");
            }
        }

        private void HandleQuestCompleted(string hint)
        {
            PostNotification("Yankı Taşı birleşti! Taş sana bir şey fısıldıyor... [H]");

            if (Phase == GamePhase.SocialObstacle && (_door == null || !_door.IsOpen))
            {
                SetObjective("Yankı Taşı tamam! İpucuyla muhafızın bilmecelerini çöz [E].");
            }
        }

        #endregion

        #region Durum Yardımcıları

        private void SubscribeQuest(ICollectionQuest quest)
        {
            if (quest == null) return;
            quest.ProgressChanged += HandleQuestProgress;
            quest.Completed += HandleQuestCompleted;
        }

        private void UnsubscribeQuest(ICollectionQuest quest)
        {
            if (quest == null) return;
            quest.ProgressChanged -= HandleQuestProgress;
            quest.Completed -= HandleQuestCompleted;
        }

        private string QuestObjectiveSuffix()
        {
            if (_quest == null || _quest.IsCompleted) return string.Empty;
            return $" Yol boyunca Yankı Taşı parçalarını topla ({_quest.CollectedCount}/{_quest.RequiredCount}).";
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void SetObjective(string objective)
        {
            CurrentObjective = objective ?? string.Empty;
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        private void SetInputLocked(bool locked)
        {
            IsInputLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
            InputLockChanged?.Invoke(locked);
        }

        #endregion
    }
}
