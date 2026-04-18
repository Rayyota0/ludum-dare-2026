using LudumDare.Pickup;
using UnityEngine;
using UnityEngine.UI;

namespace LudumDare.UI
{
    /// <summary>
    /// Shows catalog entries with a check mark when the matching id is collected.
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

            if (catalog == null || rowParent == null)
                return;

            EnsureVerticalLayout();
            BuildRows();
        }

        void EnsureVerticalLayout()
        {
            if (rowParent.GetComponent<VerticalLayoutGroup>() != null)
                return;

            var v = rowParent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.padding = new RectOffset(8, 8, 8, 8);
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
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                var le = go.AddComponent<LayoutElement>();
                le.minHeight = 28f;

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
