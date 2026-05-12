using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace IdleTafang.UI
{
    public sealed class CombatStatusPanelView : MonoBehaviour
    {
        [SerializeField] private Image baseHpFillImage;
        [SerializeField] private TextMeshProUGUI baseHpText;
        [SerializeField] private Image waveProgressFillImage;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI escapedText;
        [SerializeField] private float waveSmoothSpeed = 1.5f;

        private float displayedWaveProgress;

        public void UpdateBaseHealth(int currentHealth, int maxHealth)
        {
            if (baseHpFillImage != null && maxHealth > 0)
            {
                baseHpFillImage.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);
            }

            if (baseHpText != null)
            {
                baseHpText.text = $"{currentHealth}/{maxHealth}";
            }
        }

        public void UpdateWaveProgress(float normalizedProgress, int currentWave, int totalWaves)
        {
            float targetProgress = Mathf.Clamp01(normalizedProgress);
            displayedWaveProgress = Mathf.MoveTowards(displayedWaveProgress, targetProgress, Mathf.Max(0.01f, waveSmoothSpeed) * Time.deltaTime);

            if (waveProgressFillImage != null && totalWaves > 0)
            {
                waveProgressFillImage.fillAmount = displayedWaveProgress;
            }

            if (waveText != null)
            {
                waveText.text = $"{currentWave}/{totalWaves}";
            }
        }

        public void UpdateEscapedCount(int escapedCount)
        {
            if (escapedText != null)
            {
                escapedText.text = $"Escaped: {escapedCount}";
            }
        }
    }
}
