using IdleTafang.Gameplay;
using IdleTafang.Gameplay.Typing;
using IdleTafang.Gameplay.Builds;
using IdleTafang.Gameplay.Combat;
using IdleTafang.Gameplay.Resources;
using UnityEngine;
using TMPro;

namespace IdleTafang.UI
{
    public sealed class RunHudController : MonoBehaviour
    {
        [SerializeField] private HudView hudView;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private BuildPanelView buildPanelView;
        [SerializeField] private CombatStatusPanelView combatStatusPanel;

        private TypingSession typingSession;
        private TypingInputRouter inputRouter;
        private ResourceWallet wallet;
        private TypingRewardCalculator rewardCalculator;
        private BuildPrototype buildPrototype;
        private BuildService buildService;
        private CombatWaveManager waveManager;
        private RunSession runSession;

        private void Awake()
        {
            if (hudView != null)
            {
                hudView.SetVisible(true);
            }

            // Enable debugText display
            // if (debugText != null && debugText.gameObject != null)
            // {
            //     debugText.gameObject.SetActive(true);
            // }

            typingSession = new TypingSession();
            wallet = new ResourceWallet();
            rewardCalculator = new TypingRewardCalculator();
            buildPrototype = new BuildPrototype("Basic Tower", 5);
            buildService = new BuildService();
            runSession = new RunSession();
            runSession.Reset();

            inputRouter = FindObjectOfType<TypingInputRouter>();
            if (inputRouter != null)
            {
                inputRouter.CharacterSubmitted += OnCharacterSubmitted;
            }

            waveManager = FindObjectOfType<CombatWaveManager>();
            if (waveManager != null)
            {
                waveManager.WaveCompleted += OnWaveCompleted;
                waveManager.RunFailed += OnRunFailed;
            }

            if (buildPanelView != null)
            {
                buildPanelView.BuildChanged += OnBuildChanged;
                buildPanelView.Bind(buildPrototype, wallet, buildService);
            }

            RefreshDebugText();
        }

        private void Update()
        {
            SyncRunSessionFromCombat();
            RefreshDebugText();
            UpdateCombatStatusPanel();
        }

        private void OnDestroy()
        {
            if (inputRouter != null)
            {
                inputRouter.CharacterSubmitted -= OnCharacterSubmitted;
            }

            if (waveManager != null)
            {
                waveManager.WaveCompleted -= OnWaveCompleted;
                waveManager.RunFailed -= OnRunFailed;
            }

            if (buildPanelView != null)
            {
                buildPanelView.BuildChanged -= OnBuildChanged;
            }
        }

        private void OnCharacterSubmitted(char typedChar)
        {
            typingSession.SubmitCharacter(typedChar);

            int energyReward = rewardCalculator.CalculateEnergyReward(typingSession.Stats);
            wallet.AddEnergy(energyReward);
            typingSession.Reset();

            RefreshDebugText();
        }

        private void OnBuildChanged()
        {
            RefreshDebugText();
        }

        private void OnWaveCompleted()
        {
            if (runSession == null || runSession.IsFinished)
            {
                return;
            }

            runSession.AdvanceWave();
            runSession.CompleteRun();
            RefreshDebugText();
        }

        private void OnRunFailed()
        {
            if (runSession == null || runSession.IsFinished)
            {
                return;
            }

            runSession.FailRun();
            RefreshDebugText();
        }

        private void SyncRunSessionFromCombat()
        {
            if (runSession == null || runSession.IsFinished || waveManager == null)
            {
                return;
            }

            if (waveManager.IsRunFailed)
            {
                runSession.FailRun();
                return;
            }

            if (waveManager.IsWaveComplete)
            {
                runSession.AdvanceWave();
                runSession.CompleteRun();
            }
        }

        private void RefreshDebugText()
        {
            if (debugText == null || typingSession == null)
            {
                return;
            }

            string combatText;
            if (waveManager == null)
            {
                combatText = "Combat: No WaveManager";
            }
            else
            {
                combatText = $"Base HP: {waveManager.CurrentBaseHealth}\nSpawned: {waveManager.SpawnedCount}/{waveManager.EnemiesPerWave}\nActive: {waveManager.ActiveEnemyCount}\nEscaped: {waveManager.EscapedCount}";
            }

            string sessionText = runSession == null
                ? "Run: Unknown"
                : $"Run: {runSession.Result} (Wave {runSession.WaveIndex})";

            debugText.text =
                $"Accuracy: {typingSession.Stats.Accuracy:P0}\nCombo: {typingSession.Stats.Combo}\nBest: {typingSession.Stats.BestCombo}\nEnergy: {wallet.Energy}\nTower Lv: {buildPrototype.Level}\n{combatText}\n{sessionText}";
        }

        private void UpdateCombatStatusPanel()
        {
            if (combatStatusPanel == null || waveManager == null || runSession == null)
            {
                return;
            }

            combatStatusPanel.UpdateBaseHealth(waveManager.CurrentBaseHealth, waveManager.MaxBaseHealth);
            combatStatusPanel.UpdateWaveProgress(runSession.WaveIndex, runSession.MaxWaves);
            combatStatusPanel.UpdateEscapedCount(waveManager.EscapedCount);
        }
    }
}
