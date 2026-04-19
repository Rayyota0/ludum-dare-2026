using LudumDare.Intro;
using LudumDare.Pickup;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.UI
{
    /// <summary>
    /// Raycasts from camera and shows contextual interaction prompt text.
    /// Attach to the Player GameObject.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] float maxDistance = 4f;
        [SerializeField] LayerMask layers = ~0;

        Canvas _canvas;
        Text _text;
        CanvasGroup _group;
        float _alpha;

        readonly RaycastHit[] _hits = new RaycastHit[16];

        void Awake()
        {
            BuildUI();
        }

        void BuildUI()
        {
            var go = new GameObject("InteractionPromptCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            CanvasScalerSetup.ApplyScreenSpaceScale(scaler);

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(go.transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 22;
            _text.color = Color.white;
            _text.alignment = TextAnchor.MiddleCenter;

            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.3f);
            rt.anchorMax = new Vector2(0.5f, 0.3f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 50);
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null) { Hide(); return; }

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            var n = Physics.RaycastNonAlloc(ray, _hits, maxDistance, layers, QueryTriggerInteraction.Ignore);

            string prompt = null;

            if (n > 0)
            {
                System.Array.Sort(_hits, 0, n, HitComparer.Instance);

                for (int i = 0; i < n; i++)
                {
                    var col = _hits[i].collider;

                    // Skip player's own collider
                    if (col.transform.root == transform.root) continue;

                    prompt = GetPrompt(col.gameObject);
                    if (prompt != null) break;

                    // Also check parent (for prefab models with child meshes)
                    if (col.transform.parent != null)
                        prompt = GetPrompt(col.transform.parent.gameObject);
                    if (prompt != null) break;

                    // Check root
                    prompt = GetPrompt(col.transform.root.gameObject);
                    if (prompt != null) break;

                    // No interactable on this collider, stop (something blocks the view)
                    break;
                }
            }

            if (prompt != null)
                Show(prompt);
            else
                Hide();
        }

        string GetPrompt(GameObject go)
        {
            // Body bag pickup
            var body = go.GetComponentInParent<IntroBodyPickup>();
            if (body != null && !body.IsPickedUp)
                return "[E] Поднять тело";

            // Key pickup
            var key = go.GetComponentInParent<PickupKey>();
            if (key != null && !key.IsCollected)
                return "[E] Подобрать ключ";

            // Gate
            var gate = go.GetComponentInParent<KeyGate>();
            if (gate != null && !gate.IsOpened)
                return gate.HasKey ? "[E] Открыть ворота" : "Нужен ключ";

            // Generic pickup item
            var pickup = go.GetComponentInParent<IPickupable>();
            if (pickup != null)
            {
                var item = pickup as PickupableItem;
                if (item != null)
                {
                    var ctx = new PickupContext(transform.root);
                    if (pickup.CanPickup(ctx))
                        return $"[E] Подобрать {item.DisplayName}";
                }
            }

            return null;
        }

        void Show(string prompt)
        {
            _text.text = prompt;
            _alpha = Mathf.MoveTowards(_alpha, 1f, Time.deltaTime * 8f);
            _group.alpha = _alpha;
        }

        void Hide()
        {
            _alpha = Mathf.MoveTowards(_alpha, 0f, Time.deltaTime * 6f);
            _group.alpha = _alpha;
        }

        sealed class HitComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            internal static readonly HitComparer Instance = new();
            public int Compare(RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance);
        }
    }
}
