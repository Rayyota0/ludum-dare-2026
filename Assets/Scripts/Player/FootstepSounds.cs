using UnityEngine;

namespace LudumDare.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class FootstepSounds : MonoBehaviour
    {
        [SerializeField] AudioClip[] footstepClips;
        [SerializeField] float stepInterval = 0.4f;
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;
        [SerializeField] float pitchMin = 0.9f;
        [SerializeField] float pitchMax = 1.1f;

        CharacterController _controller;
        AudioSource _audioSource;
        float _stepTimer;
        int _lastClipIndex = -1;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }

        void Update()
        {
            bool isMoving = _controller.isGrounded && _controller.velocity.sqrMagnitude > 0.1f;

            if (!isMoving)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;
            if (_stepTimer >= stepInterval)
            {
                _stepTimer -= stepInterval;
                PlayFootstep();
            }
        }

        void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0)
                return;

            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == _lastClipIndex)
                index = (index + 1) % footstepClips.Length;
            _lastClipIndex = index;

            _audioSource.pitch = Random.Range(pitchMin, pitchMax);
            _audioSource.PlayOneShot(footstepClips[index], volume);
        }
    }
}
