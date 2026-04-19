using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Intro
{
    /// <summary>
    /// Handles the intro sequence: player sees a body bag on the ground,
    /// presses E to pick it up onto their shoulder.
    /// Attach to the BodyBag GameObject. Assign the player transform.
    /// </summary>
    public class IntroBodyPickup : MonoBehaviour, IBodyFinaleCarry
    {
        [Header("References")]
        [Tooltip("Player's Transform (FirstPersonController root)")]
        public Transform playerTransform;

        [Tooltip("Player's Camera transform")]
        public Transform playerCamera;

        [Header("Pickup Settings")]
        [Tooltip("Offset from camera when carried on shoulder")]
        public Vector3 shoulderOffset = new Vector3(0.4f, -0.35f, 0.3f);

        [Header("UI")]
        [Tooltip("Optional: UI prompt text object to show 'Press E'")]
        public GameObject promptUI;

        bool _pickedUp;
        bool _wasPickedUp;
        bool _inRange;
        float _pickupLerpT;
        Vector3 _startPos;
        Quaternion _startRot;

        [Header("Intro Camera")]
        [Tooltip("Initial camera pitch (degrees down) to look at the body")]
        public float startLookDownAngle = 60f;

        [Header("Auto Pickup")]
        [Tooltip("If true, body starts already picked up and attached to camera")]
        public bool startPickedUp;

        void Start()
        {
            _startPos = transform.position;
            _startRot = transform.rotation;

            if (promptUI != null)
                promptUI.SetActive(false);

            if (startPickedUp && playerCamera != null)
            {
                _pickedUp = true;
                _wasPickedUp = true;
                _pickupLerpT = 1f;
                foreach (var col in GetComponentsInChildren<Collider>())
                    col.enabled = false;
                return;
            }

            // Set initial pitch on FirstPersonController so it looks down at the body
            if (playerTransform != null)
            {
                var fpc = playerTransform.GetComponent<LudumDare.Player.FirstPersonController>();
                if (fpc != null)
                {
                    var pitchField = fpc.GetType().GetField("_pitch",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (pitchField != null)
                        pitchField.SetValue(fpc, startLookDownAngle);
                }
            }
        }

        void Update()
        {
            if (_pickedUp)
            {
                // Smoothly lerp body to shoulder position
                _pickupLerpT = Mathf.MoveTowards(_pickupLerpT, 1f, Time.deltaTime * 2f);

                Vector3 targetPos = playerCamera.TransformPoint(shoulderOffset);
                Quaternion targetRot = playerCamera.rotation * Quaternion.Euler(0, 0, 25);

                if (_pickupLerpT < 1f)
                {
                    transform.position = Vector3.Lerp(_startPos, targetPos, EaseOutCubic(_pickupLerpT));
                    transform.rotation = Quaternion.Slerp(_startRot, targetRot, EaseOutCubic(_pickupLerpT));
                }
                else
                {
                    // Follow camera
                    transform.position = targetPos;
                    transform.rotation = targetRot;
                }
                return;
            }

            // Check distance to player
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, transform.position);
                _inRange = dist < 3f;

                if (promptUI != null)
                    promptUI.SetActive(_inRange);

                if (_inRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    PickUp();
                }
            }
        }

        void PickUp()
        {
            _pickedUp = true;
            _wasPickedUp = true;
            _pickupLerpT = 0f;
            _startPos = transform.position;
            _startRot = transform.rotation;

            // Disable colliders so body doesn't block player
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            if (promptUI != null)
                promptUI.SetActive(false);
        }

        public bool IsPickedUp => _pickedUp || _wasPickedUp;

        public bool IsCarriedForFinale => IsPickedUp;

        public Transform BodyTransform => transform;

        /// <summary>Detach body from camera so it can be placed elsewhere.</summary>
        public void Detach()
        {
            _pickedUp = false;
            transform.SetParent(null);
            enabled = false; // prevent re-pickup during finale
        }

        public void DetachForFinale() => Detach();

        static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
