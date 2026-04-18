using UnityEngine;

namespace LudumDare.Intro
{
    /// <summary>
    /// Realistic struggling animation for the body bag.
    /// Alternates between calm breathing and violent bursts of struggling.
    /// </summary>
    public class BodyBagBreathing : MonoBehaviour
    {
        [Header("Breathing (calm phase)")]
        public float breatheAmount = 0.01f;
        public float breatheSpeed = 0.7f;

        [Header("Struggle Bursts")]
        public float struggleIntensity = 0.05f;
        public float struggleRotation = 3f;
        public float minCalmTime = 2f;
        public float maxCalmTime = 5f;
        public float minBurstTime = 0.8f;
        public float maxBurstTime = 2.5f;

        Vector3 _basePos;
        Quaternion _baseRot;

        // State
        bool _struggling;
        float _stateTimer;
        float _burstSpeed;
        float _burstIntensity;
        int _burstKickCount;
        float _kickTimer;
        float _kickLerp;
        Vector3 _kickDir;

        void Start()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _stateTimer = Random.Range(0.5f, 2f); // short calm before first burst
        }

        void Update()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0f)
            {
                // Switch state
                _struggling = !_struggling;
                if (_struggling)
                {
                    // Start a burst
                    _stateTimer = Random.Range(minBurstTime, maxBurstTime);
                    _burstSpeed = Random.Range(8f, 15f);
                    _burstIntensity = Random.Range(0.6f, 1f);
                    _burstKickCount = Random.Range(1, 4);
                    _kickTimer = _stateTimer / (_burstKickCount + 1);
                    _kickLerp = 0f;
                    _kickDir = new Vector3(
                        Random.Range(-1f, 1f),
                        0f,
                        Random.Range(-0.3f, 0.3f)
                    ).normalized;
                }
                else
                {
                    // Go calm
                    _stateTimer = Random.Range(minCalmTime, maxCalmTime);
                }
            }

            Vector3 offset = Vector3.zero;
            float rotZ = 0f;
            float rotX = 0f;

            if (_struggling)
            {
                float t = Time.time;
                float intensity = _burstIntensity * struggleIntensity;

                // Frantic side-to-side
                float sway = Mathf.Sin(t * _burstSpeed) * intensity;
                sway += Mathf.Sin(t * _burstSpeed * 2.3f + 1f) * intensity * 0.4f;

                // No vertical movement
                float heave = 0f;

                // Forward/back thrash
                float thrash = Mathf.Sin(t * _burstSpeed * 1.5f + 2f) * intensity * 0.3f;

                // Sudden kicks
                _kickTimer -= Time.deltaTime;
                if (_kickTimer <= 0f && _burstKickCount > 0)
                {
                    _burstKickCount--;
                    _kickTimer = Random.Range(0.15f, 0.4f);
                    _kickLerp = 1f;
                    _kickDir = new Vector3(
                        Random.Range(-1f, 1f) > 0 ? 1f : -1f,
                        Random.Range(0f, 0.3f),
                        Random.Range(-0.5f, 0.5f)
                    ).normalized;
                }
                _kickLerp = Mathf.MoveTowards(_kickLerp, 0f, Time.deltaTime * 5f);
                Vector3 kick = _kickDir * _kickLerp * intensity * 2f;

                offset = new Vector3(sway, heave, thrash) + kick;
                rotZ = Mathf.Sin(t * _burstSpeed + 0.5f) * struggleRotation * _burstIntensity;
                rotX = Mathf.Sin(t * _burstSpeed * 0.8f) * struggleRotation * _burstIntensity * 0.3f;
            }
            else
            {
                // Calm — no movement
                offset = Vector3.zero;
            }

            transform.localPosition = _basePos + offset;
            transform.localRotation = _baseRot * Quaternion.Euler(rotX, 0, rotZ);
        }
    }
}
