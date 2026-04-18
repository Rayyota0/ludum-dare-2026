using System;
using UnityEngine;

namespace LudumDare.Pickup
{
    [Serializable]
    public struct PickupCatalogEntry
    {
        [Tooltip("Stable id; must match PickupableItem.itemId.")]
        public string itemId;

        [Tooltip("Label in the checklist UI.")]
        public string displayName;
    }

    [CreateAssetMenu(fileName = "PickupItemCatalog", menuName = "Ludum Dare/Pickup Item Catalog")]
    public sealed class PickupItemCatalog : ScriptableObject
    {
        public PickupCatalogEntry[] entries = Array.Empty<PickupCatalogEntry>();
    }
}
