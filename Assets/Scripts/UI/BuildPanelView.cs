using System;
using IdleTafang.Gameplay.Builds;
using IdleTafang.Gameplay.Resources;
using TMPro;
using UnityEngine;

namespace IdleTafang.UI
{
    public sealed class BuildPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text levelText;

        private BuildPrototype prototype;
        private ResourceWallet wallet;
        private BuildService buildService;

        public event Action BuildChanged;

        public void Bind(BuildPrototype prototype, ResourceWallet wallet, BuildService buildService)
        {
            this.prototype = prototype;
            this.wallet = wallet;
            this.buildService = buildService;
            Refresh();
        }

        public void OnBuildButtonClicked()
        {
            if (buildService == null || prototype == null || wallet == null)
            {
                Debug.LogWarning($"BuildPanelView not bound on {name}");
                return;
            }

            bool built = buildService.TryBuild(prototype, wallet);
            Debug.Log($"Build button clicked on {name}, built={built}, energy={wallet.Energy}, level={prototype.Level}");
            Refresh();
            BuildChanged?.Invoke();
        }

        public void Refresh()
        {
            if (titleText != null)
            {
                titleText.text = prototype != null ? prototype.Name : "Build";
            }

            if (costText != null)
            {
                costText.text = prototype != null ? $"Cost: {prototype.EnergyCost}" : "Cost: -";
            }

            if (levelText != null)
            {
                levelText.text = prototype != null ? $"Level: {prototype.Level}" : "Level: -";
            }
        }
    }
}
