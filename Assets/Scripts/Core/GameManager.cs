using System;
using UnityEngine;

namespace AdventureGame
{
    public enum GamePhase
    {
        /// <summary>Bölüm 1: Köprü kalkık, yoldaşın şalteri çekmesi gerekiyor.</summary>
        PhysicalObstacle,

        /// <summary>Bölüm 2: Köprü indi, muhafız NPC ikna edilmeli.</summary>
        SocialObstacle,

        Completed
    }

    /// <summary>
    /// Oyun akışının ARABULUCUSU (Mediator) ve durum makinesi.
    /// Alt sistemler (yoldaş, köprü, NPC, kapı) birbirini TANIMAZ; yalnızca arayüzler
    /// üzerinden olay (event) yayınlar. GameManager bu olayları dinleyip oyun kurallarını uygular.
    /// Bu sayede sistemler gevşek bağlıdır ve tek tek test edilebilir.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
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

        [Header("Ayarlar")]
        [Tooltip("Oyuncu uçurum kenarına gelince yoldaş kendiliğinden göreve başlasın mı?")]
        [SerializeField] private bool autoDispatchCompanionAtObstacle;

        private IAICompanion _companion;
        private IActivatable _bridge;
        private IConversationalNpc _npc;
        private IOpenable _door;

        private bool _obstacleReached;
        private bool _inDialogue;

        public GamePhase Phase { get; private set; } = GamePhase.PhysicalObstacle;
        public bool IsInputLocked { get; private set; }
        public string CurrentObjective { get; private set; } = string.Empty;

        public event Action<GamePhase> PhaseChanged;
        public event Action<bool> InputLockChanged;
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
        }

        private void Start()
        {
            SetPhase(GamePhase.PhysicalObstacle);
            SetObjective("Etrafı keşfet ve karşı tarafa geçmenin bir yolunu bul.");
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
            SetObjective("Köprü kalkık! Şalter yüksek platformda. Yoldaşından yardım iste [F].");
            PostNotification("Bu uçurumu tek başına geçemezsin...");

            if (autoDispatchCompanionAtObstacle)
            {
                RequestCompanionHelp();
            }
        }

        /// <summary>Oyuncu [F] tuşuna bastığında PlayerInteractor tarafından çağrılır.</summary>
        public void RequestCompanionHelp()
        {
            if (_companion == null)
            {
                PostNotification("Sahnede bir yoldaş bulunamadı.");
                return;
            }

            if (Phase != GamePhase.PhysicalObstacle)
            {
                PostNotification($"{_companion.DisplayName}: Şu an yapılacak bir iş görmüyorum.");
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

            SetPhase(GamePhase.Completed);
            SetObjective("Tebrikler! Macerayı tamamladın.");
            SetInputLocked(true);
        }

        public void PostNotification(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                NotificationPosted?.Invoke(message);
            }
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
            SetObjective("Köprü indi! Karşıya geç ve kapıyı koruyan muhafızla konuş [E].");

            if (_companion != null && player != null)
            {
                _companion.Follow(player);
            }
        }

        private void HandleConversationStateChanged(bool isActive)
        {
            _inDialogue = isActive;
            SetInputLocked(_inDialogue || Phase == GamePhase.Completed);
        }

        /// <summary>
        /// LLM'den gelen aksiyon burada OYUN KURALLARINA göre doğrulanır.
        /// LLM yanlışlıkla "open_door" üretse bile yanlış aşamada kapı açılmaz.
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

        #endregion

        #region Durum Yardımcıları

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
