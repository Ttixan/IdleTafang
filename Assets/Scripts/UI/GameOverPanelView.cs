using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleTafang.UI
{
    public sealed class GameOverPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private ScrollRect summaryScroll;
        private RectTransform summaryScrollContent;
        private bool layoutApplied;

        public void Initialize(Action onRetry, Action onMainMenu)
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (titleText == null)
            {
                Transform t = transform.Find("GameOverTitle");
                if (t != null)
                {
                    titleText = t.GetComponent<TMP_Text>();
                }
            }

            if (summaryText == null)
            {
                Transform t = transform.Find("GameOverSummary");
                if (t != null)
                {
                    summaryText = t.GetComponent<TMP_Text>();
                }
            }

            if (retryButton == null)
            {
                Transform t = transform.Find("RetryButton");
                if (t != null)
                {
                    retryButton = t.GetComponent<Button>();
                }
            }

            if (mainMenuButton == null)
            {
                Transform t = transform.Find("MainMenuButton");
                if (t != null)
                {
                    mainMenuButton = t.GetComponent<Button>();
                }
            }

            EnsureSettlementLayout();

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                if (onRetry != null)
                {
                    retryButton.onClick.AddListener(() => onRetry());
                }
            }
            else
            {
                Debug.LogWarning("GameOverPanelView: RetryButton not found/bound.");
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                if (onMainMenu != null)
                {
                    mainMenuButton.onClick.AddListener(() => onMainMenu());
                }
            }
            else
            {
                Debug.LogWarning("GameOverPanelView: MainMenuButton not found/bound.");
            }

            Hide();
        }

        public void Show(string title, string summary)
        {
            EnsureSettlementLayout();

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (summaryText != null)
            {
                summaryText.text = summary ?? string.Empty;
            }

            Canvas.ForceUpdateCanvases();
            RefreshSummaryContentSize();

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void EnsureSettlementLayout()
        {
            if (layoutApplied || summaryText == null)
            {
                return;
            }

            RectTransform panelRt = root != null ? (RectTransform)root.transform : (RectTransform)transform;

            ConfigureTitle();
            ConfigureSummaryScroll(panelRt);
            ConfigureButtons(panelRt);

            layoutApplied = true;
        }

        private void ConfigureTitle()
        {
            if (titleText == null)
            {
                return;
            }

            RectTransform rt = titleText.rectTransform;
            rt.anchorMin = new Vector2(0.06f, 1f);
            rt.anchorMax = new Vector2(0.94f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(0f, 56f);
            titleText.enableWordWrapping = false;
            titleText.fontSize = 36f;
            titleText.alignment = TextAlignmentOptions.Center;
        }

        private void ConfigureSummaryScroll(RectTransform panelRt)
        {
            Transform summaryTf = summaryText.transform;

            ScrollRect existing = summaryTf.GetComponentInParent<ScrollRect>();
            if (existing != null && existing.gameObject != gameObject)
            {
                summaryScroll = existing;
                summaryScrollContent = existing.content;
                StyleSummaryText();
                return;
            }

            GameObject scrollGo = new GameObject("SettlementSummaryScroll", typeof(RectTransform), typeof(Image));
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(panelRt, false);

            int insert = 0;
            if (titleText != null)
            {
                insert = titleText.transform.GetSiblingIndex() + 1;
            }

            scrollRt.SetSiblingIndex(insert);

            scrollRt.anchorMin = new Vector2(0.06f, 0.13f);
            scrollRt.anchorMax = new Vector2(0.94f, 0.86f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.03f);
            scrollBg.raycastTarget = true;

            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollRt, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 400f);

            scroll.viewport = viewportRt;
            scroll.content = contentRt;

            summaryTf.SetParent(contentRt, false);
            RectTransform sumRt = summaryTf as RectTransform;
            sumRt.anchorMin = new Vector2(0f, 1f);
            sumRt.anchorMax = new Vector2(1f, 1f);
            sumRt.pivot = new Vector2(0.5f, 1f);
            sumRt.anchoredPosition = Vector2.zero;
            sumRt.offsetMin = new Vector2(12f, sumRt.offsetMin.y);
            sumRt.offsetMax = new Vector2(-12f, sumRt.offsetMax.y);

            summaryScroll = scroll;
            summaryScrollContent = contentRt;

            StyleSummaryText();
        }

        private void StyleSummaryText()
        {
            summaryText.fontSize = 21f;
            summaryText.lineSpacing = 2f;
            summaryText.paragraphSpacing = 6f;
            summaryText.enableWordWrapping = true;
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.overflowMode = TextOverflowModes.Overflow;
        }

        private void ConfigureButtons(RectTransform panelRt)
        {
            if (retryButton == null || mainMenuButton == null)
            {
                return;
            }

            RectTransform retryRt = retryButton.transform as RectTransform;
            RectTransform menuRt = mainMenuButton.transform as RectTransform;

            retryRt.anchorMin = retryRt.anchorMax = new Vector2(0.5f, 0f);
            retryRt.pivot = new Vector2(0.5f, 0f);
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(0.5f, 0f);
            menuRt.pivot = new Vector2(0.5f, 0f);

            float bottomPad = 32f;
            float gap = 16f;
            float retryW = 140f;
            float menuW = 200f;
            float btnH = 44f;
            retryRt.sizeDelta = new Vector2(retryW, btnH);
            menuRt.sizeDelta = new Vector2(menuW, btnH);

            float half = (retryW + gap + menuW) * 0.5f;
            retryRt.anchoredPosition = new Vector2(-half + retryW * 0.5f, bottomPad);
            menuRt.anchoredPosition = new Vector2(half - menuW * 0.5f, bottomPad);

            retryRt.SetAsLastSibling();
            menuRt.SetAsLastSibling();
        }

        private void RefreshSummaryContentSize()
        {
            if (summaryScroll == null || summaryText == null || summaryScrollContent == null)
            {
                return;
            }

            RectTransform scrollRt = summaryScroll.transform as RectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRt);

            summaryText.ForceMeshUpdate();

            float viewportWidth = summaryScroll.viewport.rect.width;
            float viewportHeight = summaryScroll.viewport.rect.height;

            float maxWidth = Mathf.Max(64f, viewportWidth - 28f);
            Vector2 preferred = summaryText.GetPreferredValues(summaryText.text, maxWidth, 0f);
            float textBlockHeight = preferred.y + 24f;
            float contentHeight = Mathf.Max(textBlockHeight, viewportHeight);

            RectTransform sumRt = summaryText.rectTransform;
            sumRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textBlockHeight);
            summaryScrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            summaryScroll.verticalNormalizedPosition = 1f;
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
