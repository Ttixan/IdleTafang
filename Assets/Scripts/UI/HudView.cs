using UnityEngine;
using TMPro;

namespace IdleTafang.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text combatStatsText;

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

            if (energyText == null || goldText == null)
            {
                Debug.LogWarning($"HudView on {name} could not find EnergyText/GoldText. Bind them in inspector or name children 'EnergyText'/'GoldText'.");
            }
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
            }
        }

        public void SetGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: {gold}";
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
                $"Turret: DMG {turretDamage} | CD {turretCooldownSeconds:0.00}s | Base+{baseHealthBonus}";
        }
    }
}
