using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace AdventureGame
{
    /// <summary>
    /// NavMeshAgent tabanlı otonom yoldaş.
    ///
    /// Akış (hiçbir koordinat sabit kodlanmamıştır):
    ///   Following → [F] → Scanning (Sensor: OverlapSphere + Raycast LOS)
    ///   → aday hedefler için NavMesh.CalculatePath ile ULAŞILABİLİRLİK testi, en kısa yol seçimi
    ///   → MovingToTarget (agent.SetDestination; yol üzerindeki NavMeshLink'ler özel
    ///     parabolik zıplama ile geçilir – oyuncunun yapamadığı hareket)
    ///   → Interacting (Raycast ile son görüş doğrulaması + IInteractable.Interact)
    ///   → Following
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CompanionSensor))]
    public sealed class NavMeshCompanionController : AICompanionBase
    {
        [Header("Takip")]
        [SerializeField, Min(0.5f)] private float followDistance = 3f;
        [SerializeField, Min(0.1f)] private float followStoppingDistance = 2f;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;

        [Header("Görev")]
        [SerializeField, Min(0.05f)] private float taskStoppingDistance = 0.2f;
        [SerializeField, Min(0.1f)] private float arrivalTolerance = 0.5f;
        [SerializeField, Min(0.5f)] private float interactDistance = 2f;
        [SerializeField, Min(0.5f)] private float navSampleRadius = 2f;
        [Tooltip("Ajan bu süre boyunca hiç hareket etmezse görev iptal edilir. Hareketli bloğun (SlidingBlock) bekleme süresinden uzun olmalı.")]
        [SerializeField, Min(1f)] private float stuckTimeout = 10f;

        [Header("Otonomi")]
        [Tooltip("Takip ederken çevreyi kendiliğinden tarar; kullanabileceği bir mekanizma görürse [F] beklemeden gider.")]
        [SerializeField] private bool autoSolveWhenSeen = true;
        [SerializeField, Min(0.25f)] private float autoScanInterval = 1.5f;
        [Tooltip("Bir görev başarısız olunca otomatik taramaya ara verme süresi (rapor spam'ini önler).")]
        [SerializeField, Min(1f)] private float autoScanCooldownAfterFail = 8f;

        [Header("NavMeshLink Geçişi (Zıplama)")]
        [SerializeField, Min(0f)] private float jumpArcHeight = 1.25f;
        [SerializeField, Min(0.5f)] private float linkTraverseSpeed = 4f;

        [Header("Animasyon (Opsiyonel)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";

        private NavMeshAgent _agent;
        private CompanionSensor _sensor;
        private NavMeshPath _pathBuffer;

        private Transform _followTarget;
        private IInteractable _currentTarget;
        private Vector3 _taskDestination;
        private float _nextRepathTime;
        private float _lastMovingTime;
        private bool _isTraversingLink;
        private Coroutine _interactRoutine;
        private float _nextAutoScanTime;

        private int _speedHash;
        private bool _hasSpeedParameter;

        /// <summary>Ajanın NavMesh üzerindeki ayak noktası (baseOffset düşülmüş).</summary>
        private Vector3 FootPosition => transform.position - Vector3.up * _agent.baseOffset;

        #region Unity Yaşam Döngüsü

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _sensor = GetComponent<CompanionSensor>();
            _pathBuffer = new NavMeshPath();

            // NavMeshLink'leri kendimiz (zıplama eğrisiyle) geçeceğiz.
            _agent.autoTraverseOffMeshLink = false;

            if (animator != null && !string.IsNullOrEmpty(speedParameter))
            {
                _speedHash = Animator.StringToHash(speedParameter);
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.nameHash == _speedHash && parameter.type == AnimatorControllerParameterType.Float)
                    {
                        _hasSpeedParameter = true;
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            if (_agent.isOnNavMesh)
            {
                return;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, _agent.areaMask))
            {
                _agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError("Yoldaş NavMesh üzerinde değil. NavMeshSurface 'Bake' edildi mi?", this);
            }
        }

        private void Update()
        {
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                return;
            }

            if (_agent.isOnOffMeshLink && !_isTraversingLink)
            {
                StartCoroutine(TraverseLinkRoutine());
            }

            if (!_isTraversingLink)
            {
                switch (State)
                {
                    case CompanionState.Following:
                        TickAutonomousScan();
                        if (State == CompanionState.Following)
                        {
                            TickFollow();
                        }
                        break;
                    case CompanionState.MovingToTarget:
                        TickMoveToTarget();
                        break;
                }
            }

            if (_hasSpeedParameter)
            {
                animator.SetFloat(_speedHash, _agent.velocity.magnitude);
            }
        }

        #endregion

        #region IAICompanion

        public override void Follow(Transform target)
        {
            _followTarget = target;

            if (!IsBusy)
            {
                BeginFollow();
            }
        }

        public override bool TrySolveObstacle()
        {
            if (IsBusy)
            {
                return false;
            }

            if (!_agent.isOnNavMesh)
            {
                Report("NavMesh üzerinde değilim, hareket edemiyorum!");
                return false;
            }

            if (_isTraversingLink)
            {
                Report("Bir saniye, önce yere ineyim!");
                return false;
            }

            SetState(CompanionState.Scanning);
            Report("Çevreyi tarıyorum...");

            if (!TrySelectReachableTarget(out IInteractable target, out Vector3 destination))
            {
                FailTask("Görebildiğim ve ulaşabildiğim bir mekanizma yok. Beni başka bir noktaya götür.");
                return false;
            }

            return StartTask(target, destination, $"{GetTargetName(target)} görüyorum! Oraya gidiyorum.");
        }

        /// <summary>
        /// Otonom davranış: takip sırasında sessizce tarar. Görüş hattında ve NavMesh ile
        /// ulaşılabilir bir mekanizma bulursa oyuncuya haber verip kendiliğinden göreve başlar.
        /// </summary>
        private void TickAutonomousScan()
        {
            if (!autoSolveWhenSeen || IsBusy || _isTraversingLink || Time.time < _nextAutoScanTime)
            {
                return;
            }

            _nextAutoScanTime = Time.time + autoScanInterval;

            if (!TrySelectReachableTarget(out IInteractable target, out Vector3 destination))
            {
                return;
            }

            SetState(CompanionState.Scanning);
            StartTask(target, destination, $"{GetTargetName(target)} fark ettim! Ben hallederim, sen bekle.");
        }

        private bool StartTask(IInteractable target, Vector3 destination, string announcement)
        {
            _currentTarget = target;
            _taskDestination = destination;
            _agent.stoppingDistance = taskStoppingDistance;

            if (!_agent.SetDestination(destination))
            {
                FailTask("Hedefe rota oluşturamadım.");
                return false;
            }

            _lastMovingTime = Time.time;
            SetState(CompanionState.MovingToTarget);
            Report(announcement);
            return true;
        }

        public override void CancelTask()
        {
            if (_interactRoutine != null)
            {
                StopCoroutine(_interactRoutine);
                _interactRoutine = null;
            }

            // Zıplama sürüyorsa yarıda kesmiyoruz (havada asılı kalmaması için); bitince takibe döner.
            BeginFollow();
        }

        #endregion

        #region Durum Mantığı

        private void BeginFollow()
        {
            _currentTarget = null;
            _agent.stoppingDistance = followStoppingDistance;
            _nextRepathTime = 0f;

            if (_agent.isOnNavMesh && !_isTraversingLink)
            {
                _agent.ResetPath();
            }

            SetState(_followTarget != null ? CompanionState.Following : CompanionState.Idle);
        }

        private void TickFollow()
        {
            if (_followTarget == null)
            {
                SetState(CompanionState.Idle);
                return;
            }

            if (Time.time < _nextRepathTime)
            {
                return;
            }

            _nextRepathTime = Time.time + repathInterval;

            if (Vector3.Distance(transform.position, _followTarget.position) <= followDistance)
            {
                return;
            }

            // Oyuncu NavMesh dışında olabilir (havada, uçurumun karşısında...): en yakın geçerli noktaya git.
            if (NavMesh.SamplePosition(_followTarget.position, out NavMeshHit hit, navSampleRadius, _agent.areaMask))
            {
                _agent.SetDestination(hit.position);
            }
        }

        private void TickMoveToTarget()
        {
            if (!IsTargetStillValid())
            {
                FailTask("Hedef artık kullanılamıyor.");
                return;
            }

            if (_agent.pathPending)
            {
                return;
            }

            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                FailTask("Rota geçersiz hâle geldi.");
                return;
            }

            if (_agent.velocity.sqrMagnitude > 0.01f)
            {
                _lastMovingTime = Time.time;
            }
            else if (Time.time - _lastMovingTime > stuckTimeout)
            {
                FailTask("Takıldım, hedefe ulaşamıyorum.");
                return;
            }

            if (Vector3.Distance(FootPosition, _taskDestination) <= arrivalTolerance)
            {
                _agent.ResetPath();
                SetState(CompanionState.Interacting);
                _interactRoutine = StartCoroutine(InteractRoutine());
            }
        }

        private IEnumerator InteractRoutine()
        {
            IInteractable target = _currentTarget;

            // Hedefe dön.
            Vector3 lookDirection = target.InteractionAnchor.position - transform.position;
            if (target is Component targetComponent)
            {
                lookDirection = targetComponent.transform.position - transform.position;
            }

            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion from = transform.rotation;
                Quaternion to = Quaternion.LookRotation(lookDirection);
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
                {
                    transform.rotation = Quaternion.Slerp(from, to, t);
                    yield return null;
                }
            }

            if (!IsTargetStillValid())
            {
                _interactRoutine = null;
                FailTask("Hedef artık kullanılamıyor.");
                yield break;
            }

            // Son sensör doğrulaması: gerçekten görüyor ve uzanabiliyor muyum?
            if (!_sensor.HasLineOfSight(target) || _sensor.DistanceTo(target) > interactDistance)
            {
                _interactRoutine = null;
                FailTask("Mekanizmaya uzanamıyorum.");
                yield break;
            }

            target.Interact(gameObject, InteractorType.AICompanion);
            Report($"{GetTargetName(target)} çalıştırıldı! Yol açılıyor.");

            yield return new WaitForSeconds(0.75f);

            _interactRoutine = null;
            BeginFollow();
        }

        /// <summary>
        /// Sensörün gördüğü her aday için NavMesh üzerinde TAM yol olup olmadığını test eder
        /// ve en kısa yola sahip olanı seçer.
        /// </summary>
        private bool TrySelectReachableTarget(out IInteractable bestTarget, out Vector3 bestDestination)
        {
            bestTarget = null;
            bestDestination = default;
            float bestLength = float.MaxValue;

            if (!NavMesh.SamplePosition(FootPosition, out NavMeshHit selfHit, navSampleRadius, _agent.areaMask))
            {
                return false;
            }

            foreach (IInteractable candidate in _sensor.Scan())
            {
                Vector3 anchor = candidate.InteractionAnchor.position;

                if (!NavMesh.SamplePosition(anchor, out NavMeshHit targetHit, navSampleRadius, _agent.areaMask))
                {
                    continue;
                }

                if (!NavMesh.CalculatePath(selfHit.position, targetHit.position, _agent.areaMask, _pathBuffer) ||
                    _pathBuffer.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float length = GetPathLength(_pathBuffer);
                if (length < bestLength)
                {
                    bestLength = length;
                    bestTarget = candidate;
                    bestDestination = targetHit.position;
                }
            }

            return bestTarget != null;
        }

        /// <summary>
        /// NavMeshLink geçişi: OffMeshLinkData'dan başlangıç/bitiş noktası alınır ve
        /// parabolik bir eğriyle zıplanır. Ardından CompleteOffMeshLink ile NavMesh'e dönülür.
        /// </summary>
        private IEnumerator TraverseLinkRoutine()
        {
            _isTraversingLink = true;

            OffMeshLinkData linkData = _agent.currentOffMeshLinkData;
            Vector3 start = transform.position;
            Vector3 end = linkData.endPos + Vector3.up * _agent.baseOffset;

            Vector3 flatDirection = end - start;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(flatDirection);
            }

            if (State == CompanionState.MovingToTarget)
            {
                Report(end.y > start.y + 0.5f ? "Platforma zıplıyorum!" : "Aşağı atlıyorum!");
            }

            float duration = Mathf.Max(0.3f, Vector3.Distance(start, end) / linkTraverseSpeed);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float arc = jumpArcHeight * 4f * t * (1f - t);
                transform.position = Vector3.Lerp(start, end, t) + Vector3.up * arc;
                yield return null;
            }

            transform.position = end;
            _agent.CompleteOffMeshLink();
            _lastMovingTime = Time.time;
            _isTraversingLink = false;
        }

        #endregion

        #region Yardımcılar

        private void FailTask(string reason)
        {
            Report(reason);
            _nextAutoScanTime = Time.time + autoScanCooldownAfterFail;
            BeginFollow();
        }

        private bool IsTargetStillValid()
        {
            if (_currentTarget == null)
            {
                return false;
            }

            // Yok edilmiş Unity nesnesi kontrolü (interface üzerinden == null yetmez).
            if (_currentTarget is Object unityObject && unityObject == null)
            {
                return false;
            }

            return _currentTarget.CanInteract(InteractorType.AICompanion) || State == CompanionState.Interacting;
        }

        private static float GetPathLength(NavMeshPath path)
        {
            float length = 0f;
            Vector3[] corners = path.corners;
            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
            }

            return length;
        }

        private static string GetTargetName(IInteractable target)
        {
            return target is Component component && component != null ? component.name : "Hedef";
        }

        private void OnDrawGizmosSelected()
        {
            if (_agent == null || !_agent.hasPath)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Vector3[] corners = _agent.path.corners;
            for (int i = 1; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i - 1], corners[i]);
            }
        }

        #endregion
    }
}
