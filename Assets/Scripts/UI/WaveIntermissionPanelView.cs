using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTafang.UI
{
    /// <summary>E2：波间面板（修基地 + 三选一强化 + 手动继续）。可在场景中绑定，或由运行时创建。</summary>
    public sealed class WaveIntermissionPanelView : MonoBehaviour
    {
        private TMP_Text headlineText;
        private TMP_Text energyText;
        private TMP_Text nextWaveText;
        private TMP_Text buffHintText;
        private Button repairButton;
        private TMP_Text repairButtonLabel;
        private Button continueButton;
        private TMP_Text continueButtonLabel;
        private readonly Button[] buffButtons = new Button[3];
        private readonly TMP_Text[] buffLabels = new TMP_Text[3];

        public event Action RepairClicked;
        public event Action<int> BuffClicked;
        public event Action ContinueClicked;

        public static WaveIntermissionPanelView EnsureUnderCanvas(Canvas canvas, WaveIntermissionPanelView existing)
        {
            if (existing != null)
            {
                return existing;
            }

            if (canvas == null)
            {
                return null;
            }

            GameObject root = new GameObject("WaveIntermissionPanel", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            WaveIntermissionPanelView panel = root.AddComponent<WaveIntermissionPanelView>();
            panel.BuildUi(root.transform);
            root.SetActive(false);
            return panel;
        }

        private void BuildUi(Transform root)
        {
            GameObject card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(root, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.06f, 0.08f);
            cardRt.anchorMax = new Vector2(0.94f, 0.92f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
            cardBg.raycastTarget = true;

            VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 28, 28);
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            headlineText = CreateTmp("Headline", card.transform, 34f, fontStyle: FontStyles.Bold);
            energyText = CreateTmp("Energy", card.transform, 26f);
            nextWaveText = CreateTmp("NextWave", card.transform, 26f);
            buffHintText = CreateTmp("BuffHint", card.transform, 22f);
            buffHintText.enableWordWrapping = true;

            repairButton = CreateButton("RepairBtn", card.transform, 52f, 22f, out repairButtonLabel);
            repairButton.onClick.AddListener(() => RepairClicked?.Invoke());

            GameObject buffRow = new GameObject("BuffRow", typeof(RectTransform));
            buffRow.transform.SetParent(card.transform, false);
            LayoutElement buffRowLe = buffRow.AddComponent<LayoutElement>();
            buffRowLe.flexibleHeight = 1f;
            buffRowLe.minHeight = 220f;

            HorizontalLayoutGroup h = buffRow.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            h.childControlHeight = true;
            h.childControlWidth = true;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                buffButtons[i] = CreateButton($"Buff{i}", buffRow.transform, 140f, 20f, out TMP_Text lbl);
                buffLabels[i] = lbl;
                lbl.enableWordWrapping = true;
                lbl.overflowMode = TextOverflowModes.Ellipsis;
                buffButtons[i].onClick.AddListener(() => BuffClicked?.Invoke(idx));
            }

            continueButton = CreateButton("ContinueBtn", card.transform, 68f, 28f, out continueButtonLabel);
            continueButtonLabel.text = "Continue to next wave";
            continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());

            LayoutElement continueLe = continueButton.gameObject.GetComponent<LayoutElement>();
            if (continueLe != null)
            {
                continueLe.minHeight = 68f;
            }
        }

        private static void ApplyProjectTmpFont(TMP_Text text)
        {
            if (text == null || TMP_Settings.defaultFontAsset == null)
            {
                return;
            }

            text.font = TMP_Settings.defaultFontAsset;
        }

        private static TMP_Text CreateTmp(string name, Transform parent, float fontSize, FontStyles fontStyle = FontStyles.Normal)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = fontSize + 18f;

            TMP_Text t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = fontStyle;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            ApplyProjectTmpFont(t);
            return t;
        }

        private static Button CreateButton(string name, Transform parent, float minHeight, float labelFontSize, out TMP_Text label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            le.flexibleWidth = 1f;

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.48f, 0.82f, 1f);

            Button btn = go.AddComponent<Button>();

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 6f);
            trt.offsetMax = new Vector2(-10f, -6f);

            label = textGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = labelFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = true;
            ApplyProjectTmpFont(label);

            return btn;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(visible);
            }
        }

        /// <summary>标题 / 能量 / 下一波 / 说明（均为 ASCII 文案，避免默认字体缺中日韩字形）。</summary>
        public void SetTexts(string headlineLine, string energyLine, string nextWaveLine, string buffRulesLine)
        {
            if (headlineText != null)
            {
                headlineText.text = headlineLine;
            }

            if (energyText != null)
            {
                energyText.text = energyLine;
            }

            if (nextWaveText != null)
            {
                nextWaveText.text = nextWaveLine;
            }

            if (buffHintText != null)
            {
                buffHintText.text = buffRulesLine;
            }
        }

        public void SetRepairButton(string label, bool interactable)
        {
            if (repairButtonLabel != null)
            {
                repairButtonLabel.text = label;
            }

            if (repairButton != null)
            {
                repairButton.interactable = interactable;
            }
        }

        public void SetBuffSlot(int index, string label, bool interactable)
        {
            if ((uint)index >= 3)
            {
                return;
            }

            if (buffLabels[index] != null)
            {
                buffLabels[index].text = label;
            }

            if (buffButtons[index] != null)
            {
                buffButtons[index].interactable = interactable;
            }
        }

        public void SetContinueButtonInteractable(bool interactable)
        {
            if (continueButton != null)
            {
                continueButton.interactable = interactable;
            }
        }
    }
}
