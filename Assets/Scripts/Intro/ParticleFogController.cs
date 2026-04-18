using UnityEngine;
using UnityEngine.InputSystem;

namespace LudumDare.Intro
{
    /// <summary>
    /// Zone-based textured fog. Press F to permanently clear fog in the zone
    /// the player is currently standing in.
    /// </summary>
    public class ParticleFogController : MonoBehaviour
    {
        [Header("References")]
        public Transform playerTransform;

        [Header("Clear Settings")]
        public float fadeDuration = 1.5f;
        public float clearRadius = 12f;

        ParticleSystem[] _fogZones;
        bool[] _cleared;

        void Start()
        {
            _fogZones = GetComponentsInChildren<ParticleSystem>();
            _cleared = new bool[_fogZones.Length];

            if (playerTransform == null)
            {
                var player = GameObject.Find("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        void Update()
        {
            if (playerTransform == null) return;
            if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return;

            // Find which zone player is in and clear it
            for (int i = 0; i < _fogZones.Length; i++)
            {
                if (_cleared[i]) continue;

                float dist = Vector3.Distance(playerTransform.position, _fogZones[i].transform.position);
                if (dist < clearRadius)
                {
                    _cleared[i] = true;
                    StartCoroutine(ClearZone(_fogZones[i]));
                }
            }
        }

        System.Collections.IEnumerator ClearZone(ParticleSystem ps)
        {
            var emission = ps.emission;
            float startRate = emission.rateOverTime.constant;

            // Stop emitting new particles
            emission.rateOverTime = 0;

            // Wait for existing particles to fade out naturally
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                // Kill particles faster by reducing lifetime
                var main = ps.main;
                float alpha = Mathf.Lerp(0.12f, 0f, t / fadeDuration);
                main.startColor = new Color(0.5f, 0.5f, 0.55f, alpha);
                yield return null;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
