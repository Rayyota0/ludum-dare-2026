using UnityEngine;

namespace LudumDare.Player
{
    /// <summary>
    /// Звуки волочимого тела: случайные стоны + шуршание при движении.
    /// Вешать на объект игрока (рядом с FirstPersonController).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class BodyDragSounds : MonoBehaviour
    {
        [Header("Стоны")]
        [SerializeField] AudioClip[] moanClips;
        [SerializeField, Range(0f, 1f)] float moanVolume = 0.6f;
        [SerializeField] float moanIntervalMin = 5f;
        [SerializeField] float moanIntervalMax = 15f;
        [SerializeField] float moanPitchMin = 0.85f;
        [SerializeField] float moanPitchMax = 1.15f;

        [Header("Волочение")]
        [SerializeField] AudioClip[] dragClips;
        [SerializeField, Range(0f, 1f)] float dragVolume = 0.4f;
        [SerializeField] float dragPitchMin = 0.95f;
        [SerializeField] float dragPitchMax = 1.05f;
        [SerializeField] float dragStepInterval = 0.5f;
        [SerializeField] float dragFadeOutTime = 0.5f;

        CharacterController _controller;
        AudioSource _moanSource;
        AudioSource _dragSource;
        float _nextMoanTime;
        float _dragTimer;
        int _lastMoanIndex = -1;
        int _lastDragIndex = -1;
        bool _wasDragging;
        float _fadeOutTimer;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();

            _moanSource = gameObject.AddComponent<AudioSource>();
            _moanSource.playOnAwake = false;
            _moanSource.spatialBlend = 1f;

            _dragSource = gameObject.AddComponent<AudioSource>();
            _dragSource.playOnAwake = false;
            _dragSource.spatialBlend = 1f;

            ScheduleNextMoan();
        }

        void Update()
        {
            UpdateMoans();
            UpdateDrag();
        }

        void UpdateMoans()
        {
            if (moanClips == null || moanClips.Length == 0)
                return;

            if (Time.time >= _nextMoanTime)
            {
                PlayRandomClip(_moanSource, moanClips, moanVolume, moanPitchMin, moanPitchMax, ref _lastMoanIndex);
                ScheduleNextMoan();
            }
        }

        void UpdateDrag()
        {
            if (dragClips == null || dragClips.Length == 0)
                return;

            bool isMoving = _controller.isGrounded && _controller.velocity.sqrMagnitude > 0.1f;

            if (!isMoving)
            {
                _dragTimer = 0f;
                if (_wasDragging && _dragSource.isPlaying)
                {
                    _fadeOutTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(_fadeOutTimer / dragFadeOutTime);
                    _dragSource.volume = Mathf.Lerp(dragVolume, 0f, t);
                    if (t >= 1f)
                    {
                        _dragSource.Stop();
                        _wasDragging = false;
                    }
                }
                return;
            }

            _wasDragging = true;
            _fadeOutTimer = 0f;
            _dragSource.volume = dragVolume;

            _dragTimer += Time.deltaTime;
            if (_dragTimer >= dragStepInterval)
            {
                _dragTimer -= dragStepInterval;
                PlayRandomClip(_dragSource, dragClips, dragVolume, dragPitchMin, dragPitchMax, ref _lastDragIndex);
            }
        }

        void ScheduleNextMoan()
        {
            _nextMoanTime = Time.time + Random.Range(moanIntervalMin, moanIntervalMax);
        }

        void PlayRandomClip(AudioSource source, AudioClip[] clips, float volume, float pitchMin, float pitchMax, ref int lastIndex)
        {
            int index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastIndex)
                index = (index + 1) % clips.Length;
            lastIndex = index;

            source.pitch = Random.Range(pitchMin, pitchMax);
            source.PlayOneShot(clips[index], volume);
        }
    }
}
