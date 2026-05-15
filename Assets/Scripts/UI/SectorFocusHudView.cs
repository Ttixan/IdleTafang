using IdleTafang.Gameplay.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTafang.UI
{
    public sealed class SectorFocusHudView : MonoBehaviour
    {
        [SerializeField] private Image[] sectorSlots;
        [SerializeField] private TMP_Text[] sectorLabels;
        [SerializeField] private Color inactiveColor = new Color(0.25f, 0.28f, 0.32f, 0.85f);
        [SerializeField] private Color activeColor = new Color(0.15f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color armingColor = new Color(1f, 0.72f, 0.1f, 1f);
        [SerializeField] private float slotSize = 52f;

        private void Awake()
        {
            EnsureBuilt();
            gameObject.SetActive(false);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void Render(SectorFocusSnapshot snapshot)
        {
            EnsureBuilt();

            for (int i = 0; i < sectorSlots.Length; i++)
            {
                if (sectorSlots[i] == null)
                {
                    continue;
                }

                Color color = inactiveColor;
                if (i == snapshot.FocusedSector)
                {
                    color = snapshot.IsReady ? activeColor : armingColor;
                }

                sectorSlots[i].color = color;

                if (sectorLabels != null && i < sectorLabels.Length && sectorLabels[i] != null)
                {
                    string suffix = i == snapshot.FocusedSector && !snapshot.IsReady ? "*" : string.Empty;
                    sectorLabels[i].text = $"{i + 1}{suffix}";
                }
            }
        }

        private void EnsureBuilt()
        {
            if (sectorSlots != null && sectorSlots.Length >= 3)
            {
                return;
            }

            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            sectorSlots = new Image[3];
            sectorLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject slot = new GameObject($"SectorSlot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                slot.transform.SetParent(transform, false);

                var image = slot.GetComponent<Image>();
                image.color = inactiveColor;

                var layoutElement = slot.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = slotSize;
                layoutElement.preferredHeight = slotSize;

                RectTransform rect = slot.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(slotSize, slotSize);

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(slot.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 28f;
                label.color = Color.white;
                label.text = $"{i + 1}";
                TMP_Text fontSource = FindObjectOfType<TMP_Text>();
                if (fontSource != null)
                {
                    label.font = fontSource.font;
                }

                sectorSlots[i] = image;
                sectorLabels[i] = label;
            }
        }
    }
}
