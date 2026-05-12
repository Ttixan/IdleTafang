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
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (summaryText != null)
            {
                summaryText.text = summary ?? string.Empty;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
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

