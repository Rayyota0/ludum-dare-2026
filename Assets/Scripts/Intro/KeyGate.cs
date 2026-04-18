using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Intro
{
    /// <summary>
    /// A gate that opens when the player has picked up the key and presses E.
    /// Attach to the gate GameObject. Assign the key GameObject.
    /// </summary>
    public class KeyGate : MonoBehaviour
    {
        [Tooltip("The key GameObject that must be picked up first")]
        public GameObject keyObject;

        [Tooltip("Player transform to check distance")]
        public Transform playerTransform;

        [Tooltip("Distance to interact")]
        public float interactDistance = 3f;

        bool _opened;
        bool _keyCollected;

        void Update()
        {
            if (_opened) return;

            // Check if key was collected (key object disabled or destroyed)
            if (!_keyCollected && keyObject != null)
            {
                if (!keyObject.activeInHierarchy)
                    _keyCollected = true;
            }

            // Also check if key is gone
            if (keyObject == null)
                _keyCollected = true;

            // Check player distance and E press
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, transform.position);
                if (dist < interactDistance && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (_keyCollected)
                        Open();
                }
            }
        }

        void Open()
        {
            _opened = true;
            // Slide gate up to open
            StartCoroutine(OpenAnimation());
        }

        System.Collections.IEnumerator OpenAnimation()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 3f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            // Disable after opening
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Call this from the pickup system when key is collected.
        /// </summary>
        public void OnKeyCollected()
        {
            _keyCollected = true;
        }

        public bool IsOpened => _opened;
        public bool HasKey => _keyCollected;
    }
}
