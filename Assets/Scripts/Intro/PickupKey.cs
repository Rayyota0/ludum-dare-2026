using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Audio")]
        [SerializeField] AudioClip pickupClip;
        [SerializeField] float pickupVolume = 1f;

        bool _collected;

        void Update()
        {
            if (_collected) return;
            if (playerTransform == null) return;

            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist < pickupDistance && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                _collected = true;
                if (pickupClip != null)
                    AudioSource.PlayClipAtPoint(pickupClip, transform.position, pickupVolume);
                gameObject.SetActive(false);
            }
        }

        public bool IsCollected => _collected;
    }
}
