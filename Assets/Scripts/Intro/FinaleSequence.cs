using System.Collections;
using LudumDare.Pickup;
using LudumDare.Player;
using LudumDare.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LudumDare.Intro
{
    public sealed class FinaleSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] IntroBodyPickup bodyPickup;
        [SerializeField] Transform playerTransform;
        [SerializeField] Transform cameraTransform;

        [Header("Trigger")]
        [SerializeField] float triggerRadius = 4f;

        [Header("Scene Objects")]
        [Tooltip("Existing pit object on the scene. Will be deepened during dig phase.")]
        [SerializeField] Transform pitObject;

        [Header("Counts")]
        [SerializeField] int chopCount = 4;
        [SerializeField] int digCount = 5;
        [SerializeField] int coverCount = 3;

        [Header("Audio")]
        [SerializeField] AudioClip chopSound;
        [SerializeField] AudioClip digSound;
        [SerializeField] AudioClip bodyDropSound;
        [SerializeField] AudioClip coverSound;
        [SerializeField] AudioClip breathingLoop;
        [SerializeField] AudioClip sirenSound;
        [SerializeField] float sfxVolume = 0.8f;

        Canvas _canvas;
        CanvasGroup _fadeGroup;
        Image _flashImage;
        Text _promptText;
        Text _endText;

        bool _triggered;
        bool _waitingForE;
        bool _ePressed;
        AudioSource _loopSource;

        GameObject _bodyOnGround;
        GameObject _headObj;
        GameObject _holeObj;
        Vector3 _holeCenter;

        void Awake()
        {
            BuildUI();
        }

        void Update()
        {
            if (_waitingForE && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                _ePressed = true;

            if (_triggered) return;
            if (playerTransform == null) return;

            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist > triggerRadius) return;

            // Check body
            if (bodyPickup == null || !bodyPickup.IsPickedUp) return;

            // Check items
            var reg = CollectedItemsRegistry.Instance;
            if (reg == null) return;
            bool axe = reg.IsCollected("axe"), shovel = reg.IsCollected("shovel");
            if (!axe || !shovel)
            {
                _triggered = true; // prevent spam
                StartCoroutine(ShowMissing(axe, shovel));
                return;
            }

            _triggered = true;
            StartCoroutine(Run());
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.12f);
            Gizmos.DrawSphere(transform.position, triggerRadius);
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }

        IEnumerator ShowMissing(bool axe, bool shovel)
        {
            string msg = !axe && !shovel ? "Нужны топор и лопата"
                       : !axe ? "Нужен топор" : "Нужна лопата";
            ShowPrompt(msg);
            yield return new WaitForSeconds(3f);
            HidePrompt();
            _triggered = false; // allow re-entry
        }

        // ════════════════════════════
        //  MAIN
        // ════════════════════════════
        IEnumerator Run()
        {
            // Lock player
            var fpc = playerTransform.GetComponent<FirstPersonController>();
            if (fpc != null) fpc.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Kill fog noise, footsteps, music
            MuteAllAudio();

            // Breathing
            if (breathingLoop != null)
            {
                _loopSource = gameObject.AddComponent<AudioSource>();
                _loopSource.clip = breathingLoop;
                _loopSource.loop = true;
                _loopSource.volume = 0.4f;
                _loopSource.Play();
            }

            yield return DropBody();
            yield return new WaitForSeconds(0.8f);
            yield return ChopPhase();
            yield return new WaitForSeconds(1f);
            yield return DigPhase();
            yield return new WaitForSeconds(0.6f);
            yield return PushBodyIn();
            yield return new WaitForSeconds(0.6f);
            yield return CoverPhase();
            yield return Finale();
        }

        void MuteAllAudio()
        {
            // Stop all audio sources on player (footsteps, drag, fog hiss, etc.)
            foreach (var src in playerTransform.GetComponentsInChildren<AudioSource>())
                src.Stop();

            // Stop fog damage controller effects
            var fogDmg = FindFirstObjectByType<VoiceFog.FogDamageController>();
            if (fogDmg != null) fogDmg.enabled = false;

            // Stop footsteps and body drag
            var footsteps = playerTransform.GetComponent<FootstepSounds>();
            if (footsteps != null) footsteps.enabled = false;
            var drag = playerTransform.GetComponent<BodyDragSounds>();
            if (drag != null) drag.enabled = false;

            // Stop music
            var music = FindFirstObjectByType<Audio.MusicLoop>();
            if (music != null) music.enabled = false;
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (src != _loopSource && src.isPlaying)
                    src.Stop();
            }
        }

        // ── DROP BODY ──────────────
        IEnumerator DropBody()
        {
            bodyPickup.Detach();
            var body = bodyPickup.transform;

            var dropTarget = playerTransform.position + playerTransform.forward * 2f;
            dropTarget.y = 0.25f;

            // Use existing pit from scene, or create one
            if (pitObject != null)
            {
                _holeObj = pitObject.gameObject;
                _holeCenter = pitObject.position;
                _holeCenter.y = 0f;
            }
            else
            {
                _holeCenter = playerTransform.position + playerTransform.forward * 3.5f;
                _holeCenter.y = 0f;

                _holeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _holeObj.name = "Pit";
                _holeObj.transform.position = _holeCenter + Vector3.down * 0.02f;
                _holeObj.transform.localScale = new Vector3(2f, 0.06f, 3f);
                _holeObj.transform.rotation = Quaternion.LookRotation(playerTransform.forward);
                SetColor(_holeObj, new Color(0.04f, 0.02f, 0.01f));
            }
            var holeCol = _holeObj.GetComponent<Collider>();
            if (holeCol != null) Destroy(holeCol);

            // Drop body
            var startPos = body.position;
            var startRot = body.rotation;
            var endRot = Quaternion.LookRotation(playerTransform.forward) * Quaternion.Euler(0, 0, 5f);

            yield return Animate(body, startPos, dropTarget, startRot, endRot, 0.8f);

            PlaySfx(bodyDropSound, dropTarget);
            yield return Shake(0.1f, 0.02f);

            _bodyOnGround = body.gameObject;

            yield return LookAt(dropTarget, 0.8f);
        }

        // ── CHOP ───────────────────
        IEnumerator ChopPhase()
        {
            var bodyPos = _bodyOnGround.transform.position;

            for (int i = 0; i < chopCount; i++)
            {
                ShowPrompt("E");
                yield return WaitE();
                HidePrompt();

                // Axe swing animation
                yield return AxeSwing();

                PlaySfx(chopSound, bodyPos);
                yield return RedFlash(0.1f);

                // Body twitches
                var orig = _bodyOnGround.transform.position;
                _bodyOnGround.transform.position += new Vector3(
                    Random.Range(-0.03f, 0.03f), 0, Random.Range(-0.03f, 0.03f));
                yield return new WaitForSeconds(0.06f);
                _bodyOnGround.transform.position = orig;

                yield return new WaitForSeconds(0.35f);
            }

            // Head detaches
            var headPos = bodyPos + _bodyOnGround.transform.forward * 0.5f + Vector3.up * 0.1f;
            _headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _headObj.name = "Head";
            _headObj.transform.position = headPos;
            _headObj.transform.localScale = Vector3.one * 0.2f;
            SetColor(_headObj, new Color(0.2f, 0.1f, 0.08f));
            var headCol = _headObj.GetComponent<Collider>();
            if (headCol != null) Destroy(headCol);

            // Roll
            var rollEnd = headPos + _bodyOnGround.transform.right * 0.7f;
            rollEnd.y = 0.1f;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                _headObj.transform.position = Vector3.Lerp(headPos, rollEnd, t);
                _headObj.transform.Rotate(Vector3.forward, 300f * Time.deltaTime);
                yield return null;
            }
        }

        // ── DIG ────────────────────
        IEnumerator DigPhase()
        {
            yield return LookAt(_holeCenter, 0.5f);
            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < digCount; i++)
            {
                ShowPrompt("E");
                yield return WaitE();
                HidePrompt();

                yield return ShovelDig();

                PlaySfx(digSound, _holeCenter);

                // Pit deepens
                float progress = (float)(i + 1) / digCount;
                float depth = Mathf.Lerp(0.06f, 1.5f, progress);
                _holeObj.transform.localScale = new Vector3(1.5f, depth, 2.2f);
                _holeObj.transform.position = _holeCenter + Vector3.down * (depth / 2f);

                yield return new WaitForSeconds(0.15f);
            }
        }

        // ── PUSH IN ────────────────
        IEnumerator PushBodyIn()
        {
            ShowPrompt("E");
            yield return WaitE();
            HidePrompt();

            if (_bodyOnGround != null)
            {
                var start = _bodyOnGround.transform.position;
                var end = _holeCenter + Vector3.down * 0.5f;
                yield return Animate(_bodyOnGround.transform, start, end, 1f);
                PlaySfx(bodyDropSound, end);
                yield return Shake(0.08f, 0.015f);
            }

            if (_headObj != null)
            {
                var hs = _headObj.transform.position;
                var he = _holeCenter + Vector3.down * 0.3f;
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * 2f;
                    var p = Vector3.Lerp(hs, he, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * 0.3f;
                    _headObj.transform.position = p;
                    yield return null;
                }
            }
        }

        // ── COVER ──────────────────
        IEnumerator CoverPhase()
        {
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = "Dirt";
            cover.transform.rotation = _holeObj.transform.rotation;
            cover.transform.localScale = new Vector3(1.5f, 0.01f, 2.2f);
            cover.transform.position = _holeCenter + Vector3.down * 0.7f;
            SetColor(cover, new Color(0.12f, 0.08f, 0.04f));
            var coverCol = cover.GetComponent<Collider>();
            if (coverCol != null) Destroy(coverCol);

            for (int i = 0; i < coverCount; i++)
            {
                ShowPrompt("E");
                yield return WaitE();
                HidePrompt();

                yield return ShovelDig();
                PlaySfx(coverSound != null ? coverSound : digSound, _holeCenter);

                float progress = (float)(i + 1) / coverCount;
                float h = Mathf.Lerp(0.01f, 1.5f, progress);
                cover.transform.localScale = new Vector3(1.5f, h, 2.2f);
                cover.transform.position = _holeCenter + Vector3.down * (0.7f - h / 2f);

                yield return new WaitForSeconds(0.15f);
            }

            if (_bodyOnGround != null) _bodyOnGround.SetActive(false);
            if (_headObj != null) _headObj.SetActive(false);

            yield return new WaitForSeconds(1.5f);
        }

        // ── FINALE ─────────────────
        IEnumerator Finale()
        {
            HidePrompt();

            if (_loopSource != null)
            {
                for (float t = 0; t < 1f; t += Time.deltaTime * 0.8f)
                {
                    _loopSource.volume = Mathf.Lerp(0.4f, 0, t);
                    yield return null;
                }
                _loopSource.Stop();
            }

            yield return new WaitForSeconds(1.5f);

            if (sirenSound != null)
                PlaySfx(sirenSound, playerTransform.position + Vector3.forward * 40f, 0.25f);

            yield return new WaitForSeconds(2f);

            // Fade to black
            for (float f = 0; f < 1f; f += Time.deltaTime * 0.25f)
            {
                _fadeGroup.alpha = f;
                yield return null;
            }
            _fadeGroup.alpha = 1f;

            yield return new WaitForSeconds(2.5f);

            // End text
            _endText.gameObject.SetActive(true);
            var c = _endText.color;
            for (float f = 0; f < 1f; f += Time.deltaTime * 0.4f)
            {
                c.a = f;
                _endText.color = c;
                yield return null;
            }

            yield return new WaitForSeconds(5f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ════════════════════════════
        //  ANIMATIONS
        // ════════════════════════════

        IEnumerator AxeSwing()
        {
            if (cameraTransform == null) yield break;
            var origRot = cameraTransform.localRotation;
            var origPos = cameraTransform.localPosition;

            // Wind up
            for (float t = 0; t < 1f; t += Time.deltaTime * 6f)
            {
                cameraTransform.localRotation = origRot * Quaternion.Euler(-3f * t, 0, 0);
                yield return null;
            }
            // Swing down
            for (float t = 0; t < 1f; t += Time.deltaTime * 12f)
            {
                float angle = Mathf.Lerp(-3f, 10f, t * t);
                cameraTransform.localRotation = origRot * Quaternion.Euler(angle, 0, Random.Range(-0.3f, 0.3f));
                yield return null;
            }
            // Impact
            yield return Shake(0.06f, 0.015f);
            // Return
            for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
            {
                cameraTransform.localRotation = Quaternion.Slerp(
                    origRot * Quaternion.Euler(10f, 0, 0), origRot, t);
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, origPos, t);
                yield return null;
            }
            cameraTransform.localRotation = origRot;
            cameraTransform.localPosition = origPos;
        }

        IEnumerator ShovelDig()
        {
            if (cameraTransform == null) yield break;
            var origRot = cameraTransform.localRotation;
            var origPos = cameraTransform.localPosition;

            for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
            {
                float pitch = Mathf.Sin(t * Mathf.PI) * 6f;
                float sway = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.5f;
                cameraTransform.localRotation = origRot * Quaternion.Euler(pitch, sway, 0);
                cameraTransform.localPosition = origPos + Vector3.down * (Mathf.Sin(t * Mathf.PI) * 0.03f);
                yield return null;
            }
            cameraTransform.localRotation = origRot;
            cameraTransform.localPosition = origPos;
        }

        // ════════════════════════════
        //  UI
        // ════════════════════════════
        void BuildUI()
        {
            var go = new GameObject("FinaleCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = go.AddComponent<CanvasScaler>();
            CanvasScalerSetup.ApplyScreenSpaceScale(scaler);

            _fadeGroup = go.AddComponent<CanvasGroup>();
            _fadeGroup.alpha = 0;
            _fadeGroup.blocksRaycasts = false;

            var fade = new GameObject("Fade").AddComponent<Image>();
            fade.transform.SetParent(go.transform, false);
            fade.color = Color.black;
            fade.raycastTarget = false;
            Stretch(fade.rectTransform);

            _flashImage = new GameObject("Flash").AddComponent<Image>();
            _flashImage.transform.SetParent(go.transform, false);
            _flashImage.color = new Color(0.5f, 0, 0, 0);
            _flashImage.raycastTarget = false;
            Stretch(_flashImage.rectTransform);

            _promptText = new GameObject("Prompt").AddComponent<Text>();
            _promptText.transform.SetParent(go.transform, false);
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 18;
            _promptText.color = new Color(0.7f, 0.7f, 0.65f, 0.45f);
            _promptText.alignment = TextAnchor.MiddleCenter;
            var ol = _promptText.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.4f);
            ol.effectDistance = new Vector2(1, -1);
            var prt = _promptText.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.18f);
            prt.anchorMax = new Vector2(0.5f, 0.18f);
            prt.sizeDelta = new Vector2(150, 30);
            _promptText.gameObject.SetActive(false);

            _endText = new GameObject("End").AddComponent<Text>();
            _endText.transform.SetParent(go.transform, false);
            _endText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _endText.fontSize = 48;
            _endText.color = new Color(0.85f, 0.85f, 0.8f, 0f);
            _endText.alignment = TextAnchor.MiddleCenter;
            _endText.text = "КОНЕЦ";
            var eol = _endText.gameObject.AddComponent<Outline>();
            eol.effectColor = new Color(0, 0, 0, 0.8f);
            eol.effectDistance = new Vector2(2, -2);
            var ert = _endText.rectTransform;
            ert.anchorMin = new Vector2(0.5f, 0.5f);
            ert.anchorMax = new Vector2(0.5f, 0.5f);
            ert.sizeDelta = new Vector2(400, 80);
            _endText.gameObject.SetActive(false);
        }

        void ShowPrompt(string t) { _promptText.text = t; _promptText.gameObject.SetActive(true); }
        void HidePrompt() { _promptText.gameObject.SetActive(false); }

        // ════════════════════════════
        //  HELPERS
        // ════════════════════════════
        IEnumerator WaitE()
        {
            _ePressed = false;
            _waitingForE = true;
            while (!_ePressed) yield return null;
            _waitingForE = false;
        }

        IEnumerator Shake(float dur, float mag)
        {
            if (cameraTransform == null) yield break;
            var orig = cameraTransform.localPosition;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                cameraTransform.localPosition = orig + Random.insideUnitSphere * mag;
                yield return null;
            }
            cameraTransform.localPosition = orig;
        }

        IEnumerator RedFlash(float dur)
        {
            _flashImage.color = new Color(0.5f, 0, 0, 0.2f);
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                var c = _flashImage.color;
                c.a = Mathf.Lerp(0.2f, 0, t / dur);
                _flashImage.color = c;
                yield return null;
            }
            _flashImage.color = new Color(0.5f, 0, 0, 0);
        }

        IEnumerator LookAt(Vector3 target, float dur)
        {
            if (cameraTransform == null) yield break;
            var from = cameraTransform.rotation;
            var to = Quaternion.LookRotation((target - cameraTransform.position).normalized);
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
                cameraTransform.rotation = Quaternion.Slerp(from, to, t * t * (3f - 2f * t));
            cameraTransform.rotation = to;
        }

        IEnumerator Animate(Transform tr, Vector3 from, Vector3 to, float dur)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                tr.position = Vector3.Lerp(from, to, t * t * (3f - 2f * t));
                yield return null;
            }
            tr.position = to;
        }

        IEnumerator Animate(Transform tr, Vector3 fp, Vector3 tp, Quaternion fr, Quaternion tr2, float dur)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / dur)
            {
                float e = t * t * (3f - 2f * t);
                tr.position = Vector3.Lerp(fp, tp, e);
                tr.rotation = Quaternion.Slerp(fr, tr2, e);
                yield return null;
            }
            tr.position = tp;
            tr.rotation = tr2;
        }

        void PlaySfx(AudioClip clip, Vector3 pos, float vol = -1f)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, vol < 0 ? sfxVolume : vol);
        }

        void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(s);
            m.SetColor("_BaseColor", color);
            r.material = m;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
