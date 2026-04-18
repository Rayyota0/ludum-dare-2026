using System;
using System.Collections.Generic;
using UnityEngine;

namespace LudumDare.Pickup
{
    /// <summary>
    /// Tracks which catalog item ids have been picked up; drives checklist UI updates.
    /// </summary>
    public sealed class CollectedItemsRegistry : MonoBehaviour
    {
        static CollectedItemsRegistry _instance;

        readonly HashSet<string> _collectedIds = new HashSet<string>();

        public static CollectedItemsRegistry Instance => _instance;

        public event Action Changed;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public bool IsCollected(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _collectedIds.Contains(itemId);
        }

        /// <summary>Returns true if this id was newly added.</summary>
        public bool MarkCollected(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            if (!_collectedIds.Add(itemId))
                return false;

            Changed?.Invoke();
            return true;
        }

        public IReadOnlyCollection<string> CollectedIds => _collectedIds;
    }
}
