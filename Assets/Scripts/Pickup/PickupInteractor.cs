using System;
using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Pickup
{
    /// <summary>
    /// Raycasts from the camera forward axis to try picking up an <see cref="IPickupable"/>.
    /// Uses all hits along the ray (sorted by distance) so the first collider can be the player's
    /// <see cref="CharacterController"/> while a pickup still counts if it is farther along the look direction.
    /// </summary>
    public sealed class PickupInteractor : MonoBehaviour
    {
        const int MaxRayHits = 32;

        [SerializeField] Transform cameraTransform;
        [SerializeField] float maxDistance = 3f;
        [SerializeField] LayerMask pickupLayers = ~0;
        [SerializeField] AudioClip pickupSound;

        readonly RaycastHit[] _rayHits = new RaycastHit[MaxRayHits];

        Transform InstigatorRoot => cameraTransform != null ? cameraTransform.root : transform;

        void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        public bool TryPickup()
        {
            if (cameraTransform == null)
                return false;

            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!TryGetFirstPickupAlongRay(ray, out var pickupable))
                return false;

            var ctx = new PickupContext(InstigatorRoot);
            if (!pickupable.CanPickup(ctx))
                return false;

            pickupable.OnPickup(ctx);
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, cameraTransform.position);
            return true;
        }

        /// <summary>Optional: only react to voice when already aiming at a pickup (reduces false triggers).</summary>
        public bool HasPickupableInView()
        {
            if (cameraTransform == null)
                return false;

            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            return TryGetFirstPickupAlongRay(ray, out _);
        }

        bool TryGetFirstPickupAlongRay(Ray ray, out IPickupable pickupable)
        {
            pickupable = null;
            var n = Physics.RaycastNonAlloc(ray, _rayHits, maxDistance, pickupLayers,
                QueryTriggerInteraction.Ignore);
            if (n <= 0)
                return false;

            Array.Sort(_rayHits, 0, n, RaycastHitByDistanceComparer.Instance);

            for (var i = 0; i < n; i++)
            {
                var col = _rayHits[i].collider;
                if (IsUnderInstigator(col.transform))
                    continue;

                var p = col.GetComponentInParent<IPickupable>();
                if (p != null)
                {
                    pickupable = p;
                    return true;
                }

                return false;
            }

            return false;
        }

        bool IsUnderInstigator(Transform t)
        {
            var root = InstigatorRoot;
            while (t != null)
            {
                if (t == root)
                    return true;
                t = t.parent;
            }

            return false;
        }

        sealed class RaycastHitByDistanceComparer : IComparer<RaycastHit>
        {
            internal static readonly RaycastHitByDistanceComparer Instance = new RaycastHitByDistanceComparer();

            public int Compare(RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance);
        }
    }
}
