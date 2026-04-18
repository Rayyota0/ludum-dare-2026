using UnityEngine;

namespace LudumDare.Intro
{
    /// <summary>
    /// Simple key pickup ??? press E near it to collect.
    /// Disables the key object when picked up.
    /// </summary>
    public class PickupKey : MonoBehaviour
    {
        public Transform playerTransform;
        public float pickupDistance = 3f;

        bool _collected;

        void Update()
        {
            if (_collected) return;
            if (playerTransform == null) return;

            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist < pickupDistance && Input.GetKeyDown(KeyCode.E))
            {
                _collected = true;
                gameObject.SetActive(false);
            }
        }

        public bool IsCollected => _collected;
    }
}
