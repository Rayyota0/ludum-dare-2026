using UnityEngine;

namespace LudumDare.Pickup
{
    /// <summary>
    /// Raycasts from the camera forward axis to try picking up an <see cref="IPickupable"/>.
    /// </summary>
    public sealed class PickupInteractor : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField] float maxDistance = 3f;
        [SerializeField] LayerMask pickupLayers = ~0;

        Transform _cachedInstigator;

        void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            _cachedInstigator = cameraTransform != null ? cameraTransform.root : transform;
        }

        public bool TryPickup()
        {
            if (cameraTransform == null)
                return false;

            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.Raycast(ray, out var hit, maxDistance, pickupLayers,
                    QueryTriggerInteraction.Ignore))
                return false;

            var pickupable = hit.collider.GetComponentInParent<IPickupable>();
            if (pickupable == null)
                return false;

            var ctx = new PickupContext(_cachedInstigator != null ? _cachedInstigator : transform);
            if (!pickupable.CanPickup(ctx))
                return false;

            pickupable.OnPickup(ctx);
            return true;
        }

        /// <summary>Optional: only react to voice when already aiming at a pickup (reduces false triggers).</summary>
        public bool HasPickupableInView()
        {
            if (cameraTransform == null)
                return false;

            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.Raycast(ray, out var hit, maxDistance, pickupLayers,
                    QueryTriggerInteraction.Ignore))
                return false;

            return hit.collider.GetComponentInParent<IPickupable>() != null;
        }
    }
}
