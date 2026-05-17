using IdleTafang.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IdleTafang.UI
{
    public sealed class HudView : MonoBehaviour
    {
        private const float LayoutTextWidth = 340f;

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text combatStatsText;
        [SerializeField] private TMP_Text runBuffsText;
        [SerializeField] private TMP_Text phaseText;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (energyText == null)
            {
                Transform t = transform.Find("EnergyText");
                if (t != null)
                {
                    energyText = t.GetComponent<TMP_Text>();
                }
            }

            if (goldText == null)
            {
                Transform t = transform.Find("GoldText");
                if (t != null)
                {
                    goldText = t.GetComponent<TMP_Text>();
                }
            }

            if (combatStatsText == null)
            {
                Transform t = transform.Find("CombatStatsText");
                if (t != null)
                {
                    combatStatsText = t.GetComponent<TMP_Text>();
                }
            }

            if (runBuffsText == null)
            {
                Transform t = transform.Find("RunBuffsText");
                if (t != null)
                {
                    runBuffsText = t.GetComponent<TMP_Text>();
                }
            }

            EnsureRunBuffsTextFallback();
            ConfigureRunBuffsTextStyle();

            if (phaseText == null)
            {
                Transform t = transform.Find("PhaseText");
                if (t != null)
                {
                    phaseText = t.GetComponent<TMP_Text>();
                }
            }

            if (energyText == null || goldText == null)
            {
                Debug.LogWarning($"HudView on {name} could not find EnergyText/GoldText. Bind them in inspector or name children 'EnergyText'/'GoldText'.");
            }

            ConfigureHudLayout();
            EnsureLayoutableText(energyText);
            EnsureLayoutableText(goldText);
            EnsureLayoutableText(combatStatsText);
            EnsureLayoutableText(runBuffsText);
            EnsureLayoutableText(phaseText);
        }

        private void EnsureRunBuffsTextFallback()
        {
            if (runBuffsText != null)
            {
                return;
            }

            GameObject go = new GameObject("RunBuffsText", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            runBuffsText = go.AddComponent<TextMeshProUGUI>();
            runBuffsText.alignment = TextAlignmentOptions.TopLeft;
            runBuffsText.enableWordWrapping = true;
            runBuffsText.text = string.Empty;
        }

        private void ConfigureRunBuffsTextStyle()
        {
            if (runBuffsText == null)
            {
                return;
            }

            runBuffsText.fontSize = 20f;
            runBuffsText.color = Color.black;
        }

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        public void SetEnergy(int energy)
        {
            if (energyText != null)
            {
                energyText.text = $"Energy: {energy}";
                RefreshTextLayout(energyText);
            }
        }

        public void SetGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: {gold}";
                RefreshTextLayout(goldText);
            }
        }

        /// <summary>Optional HUD line: turret damage, fire cooldown, base HP bonus from upgrades.</summary>
        public void SetCombatStats(int turretDamage, float turretCooldownSeconds, int baseHealthBonus)
        {
            if (combatStatsText == null)
            {
                return;
            }

            combatStatsText.text =
                $"Auto DMG {turretDamage} | CD {turretCooldownSeconds:0.00}s | Base+{baseHealthBonus}";
            RefreshTextLayout(combatStatsText);
        }

        /// <summary>E7：波间强化叠层与选购记录。</summary>
        public void SetRunBuffsSummary(string multilineOrEmpty)
        {
            if (runBuffsText == null)
            {
                return;
            }

            runBuffsText.text = string.IsNullOrEmpty(multilineOrEmpty) ? string.Empty : multilineOrEmpty;
            RefreshTextLayout(runBuffsText);
        }

        public void SetRunPhase(RunPhase phase, string hint = null)
        {
            if (phaseText == null)
            {
                return;
            }

            string label = phase switch
            {
                RunPhase.Preparation => "Preparation",
                RunPhase.Combat => "Combat",
                RunPhase.Settlement => "Settlement",
                _ => phase.ToString()
            };

            phaseText.text = string.IsNullOrEmpty(hint) ? label : $"{label}\n{hint}";
            RefreshTextLayout(phaseText);
        }

        private void ConfigureHudLayout()
        {
            RectTransform hudRect = transform as RectTransform;
            if (hudRect != null)
            {
                hudRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LayoutTextWidth);
            }

            VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.spacing = 4f;
            }

            ContentSizeFitter hudFitter = GetComponent<ContentSizeFitter>();
            if (hudFitter == null)
            {
                hudFitter = gameObject.AddComponent<ContentSizeFitter>();
            }

            hudFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            hudFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void EnsureLayoutableText(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LayoutTextWidth);

            ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = text.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 24f;
            layoutElement.preferredWidth = LayoutTextWidth;
        }

        private void RefreshTextLayout(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.ForceMeshUpdate();
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }
    }
}
