using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Pickup
{
    /// <summary>
    /// Pick up with keyboard E (same raycast rules as <see cref="PickupInteractor"/>).
    /// </summary>
    public sealed class PickupInteractInput : MonoBehaviour
    {
        [SerializeField] PickupInteractor interactor;

        [Tooltip("If true, E does nothing unless the camera ray already hits an IPickupable.")]
        [SerializeField] bool requirePickupTargetInView = true;

        void Update()
        {
            if (interactor == null || Keyboard.current == null)
                return;

            if (!Keyboard.current.eKey.wasPressedThisFrame)
                return;

            if (requirePickupTargetInView && !interactor.HasPickupableInView())
                return;

            interactor.TryPickup();
        }
    }
}
