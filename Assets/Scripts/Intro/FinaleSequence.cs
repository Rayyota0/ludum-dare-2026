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
        [Tooltip("IntroBodyPickup or ShoulderCarryPickup on the body bag.")]
        [SerializeField] MonoBehaviour bodyPickup;
        [SerializeField] Transform playerTransform;
        [SerializeField] Transform cameraTransform;

        [Header("Trigger")]
        [SerializeField] float triggerRadius = 4f;

        [Header("Scene Objects")]
        [Tooltip("Existing pit object on the scene. Will be deepened during dig phase.")]
        [SerializeField] Transform pitObject;

        [Header("Tool Models")]
        [Tooltip("Axe model prefab/asset to show during chop phase.")]
        [SerializeField] GameObject axePrefab;
        [Tooltip("Shovel model prefab/asset to show during dig phase.")]
        [SerializeField] GameObject shovelPrefab;

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
        [SerializeField] AudioClip lastTheme;
        [SerializeField] float sfxVolume = 0.8f;

        Canvas _canvas;
        CanvasGroup _fadeGroup;
        Image _fadeImage;
        Image _flashImage;
        Text _promptText;
        Text _endText;

        bool _triggered;
        bool _waitingForE;
        bool _ePressed;
        AudioSource _loopSource;
        float _debugTimer;

        GameObject _bodyOnGround;
        GameObject _headObj;
        GameObject _holeObj;
        Vector3 _holeCenter;

        // Tool visuals
        GameObject _axeObj;
        GameObject _shovelObj;

        // Ground plug that covers the pit area
        GameObject _groundPlug;

        IBodyFinaleCarry _bodyFinale;

        void Awake()
        {
            Debug.Log($"[Finale] Awake called on '{gameObject.name}', active={gameObject.activeInHierarchy}, enabled={enabled}");
            _bodyFinale = bodyPickup as IBodyFinaleCarry;
            BuildUI();
        }

        void Update()
        {
            if (_waitingForE && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                _ePressed = true;

            _debugTimer -= Time.deltaTime;
            if (_debugTimer <= 0f)
            {
                _debugTimer = 1f;
                float d = playerTransform != null
                    ? Vector3.Distance(playerTransform.position, transform.position) : -1f;
                Debug.Log($"[Finale] triggered={_triggered}, player={playerTransform != null}, " +
                          $"dist={d:F1}, radius={triggerRadius}, enabled={enabled}, " +
                          $"finalePos={transform.position}");
            }

            if (_triggered) return;
            if (playerTransform == null) { Debug.Log("[Finale] playerTransform is null"); return; }

            float dist = Vector3.Distance(playerTransform.position, transform.position);
            if (dist > triggerRadius) return;

            Debug.Log($"[Finale] In range (dist={dist:F1}). " +
                      $"body={_bodyFinale != null}, carried={_bodyFinale?.IsCarriedForFinale}, " +
                      $"registry={CollectedItemsRegistry.Instance != null}, " +
                      $"axe={CollectedItemsRegistry.Instance?.IsCollected("axe")}, " +
                      $"shovel={CollectedItemsRegistry.Instance?.IsCollected("shovel")}");

            if (_bodyFinale == null || !_bodyFinale.IsCarriedForFinale) return;

            var reg = CollectedItemsRegistry.Instance;
            if (reg == null) return;
            bool axe = reg.IsCollected("axe"), shovel = reg.IsCollected("shovel");
            if (!axe || !shovel)
            {
                _triggered = true;
                StartCoroutine(ShowMissing(axe, shovel));
                return;
            }

            Debug.Log("[Finale] All conditions met — starting finale!");
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
            _triggered = false;
        }

        // ════════════════════════════
        //  MAIN
        // ════════════════════════════
        IEnumerator Run()
        {
            var fpc = playerTransform.GetComponent<FirstPersonController>();
            if (fpc != null) fpc.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var interactionUI = FindFirstObjectByType<UI.InteractionPromptUI>();
            if (interactionUI != null) interactionUI.enabled = false;
            var pickupInput = playerTransform.GetComponent<PickupInteractInput>();
            if (pickupInput != null) pickupInput.enabled = false;

            MuteAllAudio();

            if (breathingLoop != null)
            {
                var breathSource = gameObject.AddComponent<AudioSource>();
                breathSource.clip = breathingLoop;
                breathSource.loop = true;
                breathSource.volume = 0.4f;
                breathSource.Play();
            }

            BuildTools();

            // Start last theme at the beginning of the finale
            if (lastTheme != null)
            {
                _loopSource = gameObject.AddComponent<AudioSource>();
                _loopSource.clip = lastTheme;
                _loopSource.loop = true;
                _loopSource.volume = 0.3f;
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
            foreach (var src in playerTransform.GetComponentsInChildren<AudioSource>())
                src.Stop();

            var fogDmg = FindFirstObjectByType<VoiceFog.FogDamageController>();
            if (fogDmg != null) fogDmg.enabled = false;

            var footsteps = playerTransform.GetComponent<FootstepSounds>();
            if (footsteps != null) footsteps.enabled = false;
            var drag = playerTransform.GetComponent<BodyDragSounds>();
            if (drag != null) drag.enabled = false;

            var music = FindFirstObjectByType<Audio.MusicLoop>();
            if (music != null) music.enabled = false;
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (src != _loopSource && src.isPlaying)
                    src.Stop();
            }
        }

        // ════════════════════════════
        //  GROUND SPLIT
        // ════════════════════════════

        /// <summary>
        /// Replaces Burial_Ground with pieces around the pit + a removable plug over the pit.
        /// </summary>
        void SplitGroundAroundPit()
        {
            var burialGround = GameObject.Find("Burial_Ground");
            if (burialGround == null) return;

            var groundRenderer = burialGround.GetComponent<Renderer>();
            Material groundMat = groundRenderer != null ? groundRenderer.material : null;

            // Get Burial_Ground bounds
            var bgPos = burialGround.transform.position;   // (0, 0.1, 53)
            var bgScale = burialGround.transform.localScale; // (14, 0.1, 22)
            float groundY = bgPos.y;
            float groundH = bgScale.y;

            float minX = bgPos.x - bgScale.x / 2f;
            float maxX = bgPos.x + bgScale.x / 2f;
            float minZ = bgPos.z - bgScale.z / 2f;
            float maxZ = bgPos.z + bgScale.z / 2f;

            // Hole extents (slightly bigger than the pit scale 1.5 x 2.2)
            float holeHalfX = 0.9f;
            float holeHalfZ = 1.3f;
            float hMinX = _holeCenter.x - holeHalfX;
            float hMaxX = _holeCenter.x + holeHalfX;
            float hMinZ = _holeCenter.z - holeHalfZ;
            float hMaxZ = _holeCenter.z + holeHalfZ;

            // Hide original
            burialGround.SetActive(false);

            var parent = new GameObject("SplitGround");

            // South piece: full width, from minZ to hMinZ
            CreateGroundPiece(parent.transform, "Ground_S", groundMat, groundY, groundH,
                minX, maxX, minZ, hMinZ);

            // North piece: full width, from hMaxZ to maxZ
            CreateGroundPiece(parent.transform, "Ground_N", groundMat, groundY, groundH,
                minX, maxX, hMaxZ, maxZ);

            // West piece: between hole Z range, from minX to hMinX
            CreateGroundPiece(parent.transform, "Ground_W", groundMat, groundY, groundH,
                minX, hMinX, hMinZ, hMaxZ);

            // East piece: between hole Z range, from hMaxX to maxX
            CreateGroundPiece(parent.transform, "Ground_E", groundMat, groundY, groundH,
                hMaxX, maxX, hMinZ, hMaxZ);

            // Plug: covers the hole, will be hidden when digging
            _groundPlug = CreateGroundPiece(parent.transform, "Ground_Plug", groundMat, groundY, groundH,
                hMinX, hMaxX, hMinZ, hMaxZ);
        }

        GameObject CreateGroundPiece(Transform parent, string name, Material mat,
            float y, float height, float x0, float x1, float z0, float z1)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, true);

            float cx = (x0 + x1) / 2f;
            float cz = (z0 + z1) / 2f;
            float sx = x1 - x0;
            float sz = z1 - z0;

            piece.transform.position = new Vector3(cx, y, cz);
            piece.transform.localScale = new Vector3(sx, height, sz);

            if (mat != null)
            {
                var r = piece.GetComponent<Renderer>();
                if (r != null) r.material = mat;
            }

            return piece;
        }

        // ════════════════════════════
        //  TOOL VISUALS
        // ════════════════════════════
        void BuildTools()
        {
            if (cameraTransform == null) return;

            if (axePrefab != null)
            {
                _axeObj = Instantiate(axePrefab, cameraTransform);
                _axeObj.name = "FinaleAxe";
                _axeObj.transform.localPosition = Vector3.zero;
                _axeObj.transform.localRotation = Quaternion.identity;
                _axeObj.transform.localScale = Vector3.one;
                foreach (var col in _axeObj.GetComponentsInChildren<Collider>()) Destroy(col);
                _axeObj.SetActive(false);
            }

            if (shovelPrefab != null)
            {
                _shovelObj = Instantiate(shovelPrefab, cameraTransform);
                _shovelObj.name = "FinaleShovel";
                _shovelObj.transform.localPosition = Vector3.zero;
                _shovelObj.transform.localRotation = Quaternion.identity;
                _shovelObj.transform.localScale = Vector3.one;
                foreach (var col in _shovelObj.GetComponentsInChildren<Collider>()) Destroy(col);
                _shovelObj.SetActive(false);
            }
        }

        static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        // ── DROP BODY ──────────────
        IEnumerator DropBody()
        {
            _bodyFinale.DetachForFinale();
            var body = _bodyFinale.BodyTransform;

            var breathing = body.GetComponent<BodyBagBreathing>();
            if (breathing != null) breathing.enabled = false;

            // Setup pit first so _holeCenter is valid
            if (pitObject != null)
            {
                _holeObj = pitObject.gameObject;
                _holeCenter = pitObject.position;
                _holeCenter.y = 0f;
                SetColor(_holeObj, new Color(0.04f, 0.02f, 0.01f));
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

            SplitGroundAroundPit();

            // Drop body next to pit (between player and pit)
            var toHole = (_holeCenter - playerTransform.position).normalized;
            var dropTarget = _holeCenter - toHole * 1.2f;
            dropTarget.y = 0.25f;

            // Drop body — rotated 90° so it lies across
            var startPos = body.position;
            var startRot = body.rotation;
            var endRot = Quaternion.LookRotation(playerTransform.forward) * Quaternion.Euler(0, 90f, 5f);

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

                yield return AxeSwing();

                PlaySfx(chopSound, bodyPos);
                yield return RedFlash(0.15f);

                var orig = _bodyOnGround.transform.position;
                _bodyOnGround.transform.position += new Vector3(
                    Random.Range(-0.05f, 0.05f), 0, Random.Range(-0.05f, 0.05f));
                yield return new WaitForSeconds(0.06f);
                _bodyOnGround.transform.position = orig;

                yield return new WaitForSeconds(0.35f);
            }

            var headPos = bodyPos + _bodyOnGround.transform.forward * 0.5f + Vector3.up * 0.1f;
            _headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _headObj.name = "Head";
            _headObj.transform.position = headPos;
            _headObj.transform.localScale = Vector3.one * 0.2f;
            SetColor(_headObj, new Color(0.2f, 0.1f, 0.08f));
            DestroyCollider(_headObj);

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
            // Remove the ground plug to reveal the pit
            if (_groundPlug != null)
                _groundPlug.SetActive(false);

            yield return LookAt(_holeCenter, 0.5f);
            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < digCount; i++)
            {
                ShowPrompt("E");
                yield return WaitE();
                HidePrompt();

                yield return ShovelDigAnim();

                PlaySfx(digSound, _holeCenter);

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
                float pitDepth = _holeObj.transform.localScale.y;
                var end = _holeCenter + Vector3.down * (pitDepth * 0.6f);
                yield return Animate(_bodyOnGround.transform, start, end, 1f);
                PlaySfx(bodyDropSound, end);
                yield return Shake(0.08f, 0.015f);
            }

            if (_headObj != null)
            {
                var hs = _headObj.transform.position;
                var he = _holeCenter + Vector3.down * (_holeObj.transform.localScale.y * 0.4f);
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
            // Dirt fill that rises from pit bottom to ground level
            float pitDepth = _holeObj.transform.localScale.y; // current pit depth
            float pitBottom = _holeCenter.y - pitDepth;

            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = "Dirt";
            cover.transform.rotation = _holeObj.transform.rotation;
            cover.transform.localScale = new Vector3(1.4f, 0.01f, 2.1f);
            cover.transform.position = new Vector3(_holeCenter.x, pitBottom, _holeCenter.z);
            SetColor(cover, new Color(0.12f, 0.08f, 0.04f));
            DestroyCollider(cover);

            for (int i = 0; i < coverCount; i++)
            {
                ShowPrompt("E");
                yield return WaitE();
                HidePrompt();

                yield return ShovelDigAnim();
                PlaySfx(coverSound != null ? coverSound : digSound, _holeCenter);

                // Fill rises from pit bottom up to ground level (y~0.1)
                float progress = (float)(i + 1) / coverCount;
                float fillH = Mathf.Lerp(0.01f, pitDepth, progress);
                cover.transform.localScale = new Vector3(1.4f, fillH, 2.1f);
                cover.transform.position = new Vector3(_holeCenter.x, pitBottom + fillH / 2f, _holeCenter.z);

                yield return new WaitForSeconds(0.15f);
            }

            if (_bodyOnGround != null) _bodyOnGround.SetActive(false);
            if (_headObj != null) _headObj.SetActive(false);

            // Restore ground plug over the filled hole
            if (_groundPlug != null)
                _groundPlug.SetActive(true);

            yield return new WaitForSeconds(1.5f);
        }

        // ── FINALE ─────────────────
        IEnumerator Finale()
        {
            HidePrompt();
            if (_shovelObj != null) _shovelObj.SetActive(false);

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

            for (float f = 0; f < 1f; f += Time.deltaTime * 0.25f)
            {
                _fadeGroup.alpha = f;
                yield return null;
            }
            _fadeGroup.alpha = 1f;

            yield return new WaitForSeconds(2.5f);

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
        //  TOOL ANIMATIONS
        // ════════════════════════════

        IEnumerator AxeSwing()
        {
            if (_axeObj == null) yield break;
            _axeObj.SetActive(true);

            var tr = _axeObj.transform;

            var restPos = new Vector3(0.35f, 0.15f, 0.5f);
            var raisedPos = new Vector3(0.3f, 0.45f, 0.4f);
            var raisedRot = Quaternion.Euler(-30f, 0f, -20f);
            var impactPos = new Vector3(0.15f, -0.15f, 0.55f);
            var impactRot = Quaternion.Euler(40f, 0f, 10f);

            tr.localPosition = restPos;
            tr.localRotation = Quaternion.identity;

            // Raise up
            for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
            {
                float e = t * t;
                tr.localPosition = Vector3.Lerp(restPos, raisedPos, e);
                tr.localRotation = Quaternion.Slerp(Quaternion.identity, raisedRot, e);
                yield return null;
            }

            // Swing down fast
            for (float t = 0; t < 1f; t += Time.deltaTime * 12f)
            {
                float e = t * t;
                tr.localPosition = Vector3.Lerp(raisedPos, impactPos, e);
                tr.localRotation = Quaternion.Slerp(raisedRot, impactRot, e);
                yield return null;
            }

            yield return Shake(0.08f, 0.025f);

            // Return and hide
            for (float t = 0; t < 1f; t += Time.deltaTime * 5f)
            {
                tr.localPosition = Vector3.Lerp(impactPos, restPos, t);
                tr.localRotation = Quaternion.Slerp(impactRot, Quaternion.identity, t);
                yield return null;
            }

            _axeObj.SetActive(false);
        }

        IEnumerator ShovelDigAnim()
        {
            Debug.Log($"[Finale] ShovelDigAnim called, shovelObj={_shovelObj != null}");
            if (_shovelObj == null) yield break;
            _shovelObj.SetActive(true);

            var tr = _shovelObj.transform;
            Debug.Log($"[Finale] Shovel pos={tr.localPosition}, rot={tr.localEulerAngles}, scale={tr.localScale}");

            var restPos = new Vector3(0.4f, -0.2f, 1.0f);
            var restRot = Quaternion.Euler(59f, -152f, -255f);
            var raisePos = new Vector3(0.35f, 0.15f, 0.9f);
            var raiseRot = Quaternion.Euler(20f, -152f, -255f);
            var digPos = new Vector3(0.3f, -0.5f, 1.1f);
            var digRot = Quaternion.Euler(90f, -152f, -255f);

            tr.localPosition = restPos;
            tr.localRotation = restRot;

            // Raise shovel
            for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
            {
                float e = t * t * (3f - 2f * t);
                tr.localPosition = Vector3.Lerp(restPos, raisePos, e);
                tr.localRotation = Quaternion.Slerp(restRot, raiseRot, e);
                yield return null;
            }

            // Plunge down
            for (float t = 0; t < 1f; t += Time.deltaTime * 6f)
            {
                float e = t * t;
                tr.localPosition = Vector3.Lerp(raisePos, digPos, e);
                tr.localRotation = Quaternion.Slerp(raiseRot, digRot, e);
                yield return null;
            }

            yield return Shake(0.04f, 0.01f);

            // Lift back
            for (float t = 0; t < 1f; t += Time.deltaTime * 3f)
            {
                float e = t * t * (3f - 2f * t);
                tr.localPosition = Vector3.Lerp(digPos, restPos, e);
                tr.localRotation = Quaternion.Slerp(digRot, restRot, e);
                yield return null;
            }
        }

        // ════════════════════════════
        //  UI
        // ═��══════════════════════════
        void BuildUI()
        {
            var go = new GameObject("FinaleCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            UiOverlayLayout.ConfigureOverlayScaler(go.AddComponent<CanvasScaler>());

            var fadeHolder = new GameObject("FadeHolder");
            fadeHolder.transform.SetParent(go.transform, false);
            var fadeHolderRT = fadeHolder.AddComponent<RectTransform>();
            UiOverlayLayout.StretchToParent(fadeHolderRT);
            _fadeGroup = fadeHolder.AddComponent<CanvasGroup>();
            _fadeGroup.alpha = 0;
            _fadeGroup.blocksRaycasts = false;

            _fadeImage = new GameObject("Fade").AddComponent<Image>();
            _fadeImage.transform.SetParent(fadeHolder.transform, false);
            _fadeImage.color = Color.black;
            _fadeImage.raycastTarget = false;
            UiOverlayLayout.StretchToParent(_fadeImage.rectTransform);

            _flashImage = new GameObject("Flash").AddComponent<Image>();
            _flashImage.transform.SetParent(go.transform, false);
            _flashImage.color = new Color(0.5f, 0, 0, 0);
            _flashImage.raycastTarget = false;
            UiOverlayLayout.StretchToParent(_flashImage.rectTransform);

            _promptText = new GameObject("Prompt").AddComponent<Text>();
            _promptText.transform.SetParent(go.transform, false);
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 28;
            _promptText.color = new Color(0.8f, 0.8f, 0.75f, 0.7f);
            _promptText.alignment = TextAnchor.MiddleCenter;
            var ol = _promptText.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.4f);
            ol.effectDistance = new Vector2(1, -1);
            var prt = _promptText.rectTransform;
            UiOverlayLayout.SetNormalizedBand(prt, 0.15f, 0.85f, 0.12f, 0.22f);
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
            UiOverlayLayout.SetNormalizedBand(ert, 0.15f, 0.85f, 0.38f, 0.62f);
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
            _flashImage.color = new Color(0.6f, 0, 0, 0.4f);
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                var c = _flashImage.color;
                c.a = Mathf.Lerp(0.4f, 0, t / dur);
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
            {
                cameraTransform.rotation = Quaternion.Slerp(from, to, t * t * (3f - 2f * t));
                yield return null;
            }
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

    }
}
