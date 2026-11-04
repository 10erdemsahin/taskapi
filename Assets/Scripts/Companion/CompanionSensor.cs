using System.Collections.Generic;
using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Yoldaşın "duyu organı". İki aşamalı algılama yapar:
    ///   1) Physics.OverlapSphere ile yakın çevredeki etkileşimli nesne adaylarını bulur.
    ///   2) Her aday için gözden Raycast atarak görüş hattı (line of sight) kontrol eder;
    ///      duvar/platform arkasında kalan nesneler "görülmez".
    /// SRP: Sadece algılama yapar; karar vermez, hareket ettirmez.
    /// </summary>
    public sealed class CompanionSensor : MonoBehaviour
    {
        [Tooltip("Işınların çıkış noktası. Boşsa transform + 1.5 m.")]
        [SerializeField] private Transform eye;
        [SerializeField, Min(1f)] private float scanRadius = 25f;
        [SerializeField] private LayerMask interactableMask;
        [Tooltip("Görüşü engelleyen katmanlar (duvar, zemin, platform).")]
        [SerializeField] private LayerMask occluderMask = ~0;
        [SerializeField] private bool requireLineOfSight = true;
        [SerializeField, Range(4, 128)] private int maxColliders = 32;
        [SerializeField] private bool drawDebugRays = true;

        private Collider[] _overlapBuffer;
        private readonly List<IInteractable> _results = new List<IInteractable>();
        private readonly HashSet<IInteractable> _visited = new HashSet<IInteractable>();
        private readonly List<Vector3> _sensePoints = new List<Vector3>(2);

        public Vector3 EyePosition => eye != null ? eye.position : transform.position + Vector3.up * 1.5f;

        private void Awake()
        {
            _overlapBuffer = new Collider[maxColliders];
        }

        /// <summary>Algılanan ve yoldaşın kullanabileceği nesneleri döndürür.</summary>
        public IReadOnlyList<IInteractable> Scan()
        {
            _results.Clear();
            _visited.Clear();

            int count = Physics.OverlapSphereNonAlloc(
                EyePosition, scanRadius, _overlapBuffer, interactableMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = _overlapBuffer[i].GetComponentInParent<IInteractable>();

                if (candidate == null || !_visited.Add(candidate))
                {
                    continue;
                }

                if (!candidate.CanInteract(InteractorType.AICompanion))
                {
                    continue;
                }

                if (requireLineOfSight && !HasLineOfSight(candidate))
                {
                    continue;
                }

                _results.Add(candidate);
            }

            return _results;
        }

        /// <summary>Hedefin collider merkezine veya üst noktasına engelsiz ışın atılabiliyor mu?</summary>
        public bool HasLineOfSight(IInteractable target)
        {
            if (target == null)
            {
                return false;
            }

            CollectSensePoints(target);
            foreach (Vector3 point in _sensePoints)
            {
                if (CastTo(target, point))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Hedefin algılama noktasına (collider merkezi) olan mesafe.</summary>
        public float DistanceTo(IInteractable target)
        {
            CollectSensePoints(target);
            return Vector3.Distance(EyePosition, _sensePoints[0]);
        }

        private bool CastTo(IInteractable target, Vector3 point)
        {
            Vector3 origin = EyePosition;
            Vector3 direction = point - origin;
            float distance = direction.magnitude;

            if (distance < 0.01f)
            {
                return true;
            }

            int mask = occluderMask | interactableMask;
            bool blocked = Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance, mask,
                QueryTriggerInteraction.Ignore);

            bool visible = !blocked || ReferenceEquals(hit.collider.GetComponentInParent<IInteractable>(), target);

            if (drawDebugRays)
            {
                Debug.DrawLine(origin, blocked ? hit.point : point, visible ? Color.green : Color.red, 1.5f);
            }

            return visible;
        }

        private void CollectSensePoints(IInteractable target)
        {
            _sensePoints.Clear();

            if (target is Component component)
            {
                Collider targetCollider = component.GetComponentInChildren<Collider>();
                if (targetCollider != null)
                {
                    Bounds bounds = targetCollider.bounds;
                    _sensePoints.Add(bounds.center);
                    _sensePoints.Add(new Vector3(bounds.center.x, bounds.max.y - 0.05f, bounds.center.z));
                    return;
                }
            }

            _sensePoints.Add(target.InteractionAnchor.position);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(EyePosition, scanRadius);
        }
    }
}
