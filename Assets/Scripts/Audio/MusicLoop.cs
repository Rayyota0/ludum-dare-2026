using UnityEngine;

namespace LudumDare.Audio
{
    /// <summary>
    /// Плавный зацикленный трек с кроссфейдом: два AudioSource
    /// перекрывают друг друга чтобы не было слышно стыка.
    /// </summary>
    public sealed class MusicLoop : MonoBehaviour
    {
        [SerializeField] AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] float maxVolume = 0.15f;
        [SerializeField] float fadeTime = 2f;

        AudioSource _sourceA;
        AudioSource _sourceB;
        AudioSource _current;
        bool _fading;
        float _fadeTimer;

        void Awake()
        {
            _sourceA = CreateSource();
            _sourceB = CreateSource();
        }

        AudioSource CreateSource()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = musicClip;
            src.loop = false;
            src.playOnAwake = false;
            src.volume = 0f;
            src.spatialBlend = 0f;
            return src;
        }

        void Start()
        {
            _current = _sourceA;
            _current.volume = maxVolume;
            _current.Play();
        }

        void Update()
        {
            if (musicClip == null) return;

            float timeLeft = _current.clip.length - _current.time;

            if (!_fading && timeLeft <= fadeTime)
            {
                StartCrossfade();
            }

            if (_fading)
            {
                _fadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_fadeTimer / fadeTime);

                var old = _current == _sourceA ? _sourceB : _sourceA;
                // Новый нарастает, старый затухает (но старый уже не _current)
                // _current — это новый источник после StartCrossfade
                _current.volume = Mathf.Lerp(0f, maxVolume, t);
                old.volume = Mathf.Lerp(maxVolume, 0f, t);

                if (t >= 1f)
                {
                    old.Stop();
                    _fading = false;
                }
            }
        }

        void StartCrossfade()
        {
            _fading = true;
            _fadeTimer = 0f;

            var next = _current == _sourceA ? _sourceB : _sourceA;
            next.volume = 0f;
            next.Play();
            _current = next;
        }
    }
}
