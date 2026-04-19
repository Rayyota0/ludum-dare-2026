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
            else
                ApplyChecklistPanelLayout(rowParent);

            var canvas = rowParent != null ? rowParent.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                    UiOverlayLayout.ConfigureOverlayScaler(scaler);
            }

            EnsureVerticalLayout();
            BuildRows();
        }

        static void ApplyChecklistPanelLayout(RectTransform panelRt)
        {
            const float marginX = 12f / UiOverlayLayout.ReferenceWidth;
            const float marginY = 12f / UiOverlayLayout.ReferenceHeight;
            const float panelW = 200f / UiOverlayLayout.ReferenceWidth;
            const float panelH = 80f / UiOverlayLayout.ReferenceHeight;
            UiOverlayLayout.SetTopLeftPanelFractions(panelRt, marginX, marginY, panelW, panelH);
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("ChecklistCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            UiOverlayLayout.ConfigureOverlayScaler(canvasGo.AddComponent<CanvasScaler>());
            var cg = canvasGo.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var panelGo = new GameObject("ChecklistPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            ApplyChecklistPanelLayout(panelRt);

            rowParent = panelRt;
        }

        void EnsureVerticalLayout()
        {
            if (rowParent.GetComponent<VerticalLayoutGroup>() != null)
                return;

            var v = rowParent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2f;
            var pad = UiOverlayLayout.PaddingFromRefWidth();
            v.padding = new RectOffset(pad, pad, pad, pad);
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
