using LudumDare.Pickup;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.UI
{
    /// <summary>
    /// Shows catalog entries with a check mark when the matching id is collected.
    /// Auto-creates Canvas if rowParent is not assigned. Auto-loads catalog from Resources.
    /// </summary>
    public sealed class PickupChecklistUI : MonoBehaviour
    {
        const string UncheckedPrefix = "☐ ";
        const string CheckedPrefix = "☑ ";

        [SerializeField] PickupItemCatalog catalog;
        [SerializeField] CollectedItemsRegistry registry;
        [SerializeField] RectTransform rowParent;
        [SerializeField] Font rowFont;

        Text[] _rows;

        void Awake()
        {
            if (registry == null)
                registry = FindFirstObjectByType<CollectedItemsRegistry>();

            if (catalog == null)
                catalog = Resources.Load<PickupItemCatalog>("PickupItemCatalog_Default");

            if (catalog == null)
                return;

            if (rowParent == null)
                BuildCanvas();

            EnsureVerticalLayout();
            BuildRows();
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("ChecklistCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            CanvasScalerSetup.ApplyScreenSpaceScale(scaler);
            var cg = canvasGo.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var panelGo = new GameObject("ChecklistPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(12f, -12f);
            panelRt.sizeDelta = new Vector2(200f, 80f);

            rowParent = panelRt;
        }

        void EnsureVerticalLayout()
        {
            if (rowParent.GetComponent<VerticalLayoutGroup>() != null)
                return;

            var v = rowParent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
        }

        void OnEnable()
        {
            if (registry != null)
                registry.Changed += RefreshAll;

            RefreshAll();
        }

        void OnDisable()
        {
            if (registry != null)
                registry.Changed -= RefreshAll;
        }

        void BuildRows()
        {
            foreach (Transform c in rowParent)
                Destroy(c.gameObject);

            var entries = catalog.entries;
            _rows = new Text[entries.Length];

            for (var i = 0; i < entries.Length; i++)
            {
                var go = new GameObject($"Row_{entries[i].itemId}", typeof(Text));
                go.transform.SetParent(rowParent, false);

                var text = go.GetComponent<Text>();
                text.font = rowFont != null ? rowFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 13;
                text.color = new Color(0.7f, 0.7f, 0.65f, 0.6f);
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                var le = go.AddComponent<LayoutElement>();
                le.minHeight = 18f;

                _rows[i] = text;
            }
        }

        void RefreshAll()
        {
            if (_rows == null || catalog == null || registry == null)
                return;

            var entries = catalog.entries;
            for (var i = 0; i < entries.Length && i < _rows.Length; i++)
            {
                var label = string.IsNullOrEmpty(entries[i].displayName)
                    ? entries[i].itemId
                    : entries[i].displayName;

                var prefix = registry.IsCollected(entries[i].itemId) ? CheckedPrefix : UncheckedPrefix;
                _rows[i].text = prefix + label;
            }
        }
    }
}
