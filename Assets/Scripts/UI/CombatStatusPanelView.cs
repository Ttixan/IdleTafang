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

        public void UpdateWaveProgress(int currentWave, int totalWaves)
        {
            if (waveProgressFillImage != null && totalWaves > 0)
            {
                waveProgressFillImage.fillAmount = Mathf.Clamp01((float)currentWave / totalWaves);
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
