using UnityEngine;

namespace LudumDare.Pickup
{
    public readonly struct PickupContext
    {
        public Transform Instigator { get; }

        public PickupContext(Transform instigator)
        {
            Instigator = instigator;
        }
    }

    public interface IPickupable
    {
        bool CanPickup(PickupContext context);
        void OnPickup(PickupContext context);
    }
}
