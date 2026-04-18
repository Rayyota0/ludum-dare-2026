using UnityEngine;

namespace LudumDare.Pickup
{
    /// <summary>
    /// Collectible bound to <see cref="CollectedItemsRegistry"/>; checklist shows a check when picked up.
    /// </summary>
    public sealed class PickupableItem : MonoBehaviour, IPickupable
    {
        [SerializeField] string itemId;
        [SerializeField] string displayNameOverride;

        CollectedItemsRegistry _registry;

        CollectedItemsRegistry Registry =>
            _registry ??= FindFirstObjectByType<CollectedItemsRegistry>();

        public string ItemId => itemId;

        public string DisplayName =>
            string.IsNullOrEmpty(displayNameOverride) ? gameObject.name : displayNameOverride;

        public bool CanPickup(PickupContext context)
        {
            if (Registry == null || string.IsNullOrEmpty(itemId))
                return false;

            if (Registry.IsCollected(itemId))
                return false;

            return true;
        }

        public void OnPickup(PickupContext context)
        {
            if (Registry == null || string.IsNullOrEmpty(itemId))
                return;

            Registry.MarkCollected(itemId);
            gameObject.SetActive(false);
        }
    }
}
