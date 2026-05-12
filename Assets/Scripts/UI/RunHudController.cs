using IdleTafang.Gameplay;
using IdleTafang.Gameplay.Typing;
using IdleTafang.Gameplay.Builds;
using IdleTafang.Gameplay.Combat;
using IdleTafang.Gameplay.Resources;
using IdleTafang.Core;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace IdleTafang.UI
{
    public sealed class RunHudController : MonoBehaviour
    {
        [SerializeField] private HudView hudView;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private BuildPanelView buildPanelView;
        [SerializeField] private CombatStatusPanelView combatStatusPanel;
        [SerializeField] private GameOverPanelView gameOverPanelView;

        private TypingSession typingSession;
        private TypingInputRouter inputRouter;
        private ResourceWallet wallet;
        private TypingRewardCalculator rewardCalculator;
        private BuildPrototype buildPrototype;
        private BuildService buildService;
        private CombatWaveManager waveManager;
        private ManualTurretController manualTurret;
        private RunSession runSession;
        private bool gameOverShown;
        private int lastGoldReward;
        private int initialMaxBaseHealth;

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
            wallet.AddGold(PersistentGold.Load());
            rewardCalculator = new TypingRewardCalculator();
            buildPrototype = new BuildPrototype("Basic Tower", 5);
            buildService = new BuildService();
            runSession = TryGetRunSession();
            lastGoldReward = 0;

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
                waveManager.StartNewWave();
                initialMaxBaseHealth = waveManager.MaxBaseHealth;
            }

            manualTurret = FindObjectOfType<ManualTurretController>();
            if (manualTurret != null)
            {
                manualTurret.Bind(buildPrototype);
            }

            if (buildPanelView != null)
            {
                buildPanelView.BuildChanged += OnBuildChanged;
                buildPanelView.Bind(buildPrototype, wallet, buildService);
            }

            InitializeGameOverPanel();

            RefreshDebugText();
            RefreshHud();
        }

        private void Update()
        {
            // In case the scene was started directly (no Boot/GameBootstrap),
            // or bootstrap is spawned after this controller.
            if (runSession == null)
            {
                runSession = TryGetRunSession();
            }

            RefreshDebugText();
            UpdateCombatStatusPanel();
            RefreshHud();
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

            FlushPersistentGold();

            // Ensure subsequent scenes are not accidentally paused.
            Time.timeScale = 1f;
        }

        private void FlushPersistentGold()
        {
            if (wallet != null)
            {
                PersistentGold.Save(wallet.Gold);
            }
        }

        private void OnApplicationQuit()
        {
            FlushPersistentGold();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushPersistentGold();
            }
        }

        private void OnCharacterSubmitted(char typedChar)
        {
            typingSession.SubmitCharacter(typedChar);

            int energyReward = rewardCalculator.CalculateEnergyReward(typingSession.Stats);
            wallet.AddEnergy(energyReward);
            typingSession.Reset();

            RefreshDebugText();
            RefreshHud();
            if (buildPanelView != null)
            {
                buildPanelView.Refresh();
            }
        }

        private void OnBuildChanged()
        {
            RefreshDebugText();
            RefreshHud();
            ApplyUpgradeEffects();
        }

        private void OnWaveCompleted()
        {
            if (runSession == null || runSession.IsFinished)
            {
                return;
            }

            runSession.AdvanceWave();
            if (runSession.CompletedWaves >= runSession.MaxWaves)
            {
                runSession.CompleteRun();
                HandleVictory();
            }
            else
            {
                if (waveManager != null && !waveManager.IsRunFailed)
                {
                    waveManager.StartNewWave();
                }
            }
            RefreshDebugText();
        }

        private RunSession TryGetRunSession()
        {
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.RunSession != null)
            {
                return GameBootstrap.Instance.RunSession;
            }

            // Allows playing the Run scene directly in editor without going through Boot.
            GameBootstrap bootstrap = FindObjectOfType<GameBootstrap>();
            if (bootstrap != null && bootstrap.RunSession != null)
            {
                return bootstrap.RunSession;
            }

            Debug.LogWarning("RunHudController: GameBootstrap not found. Creating a local RunSession for this scene run.");
            RunSession local = new RunSession();
            local.Reset();
            return local;
        }

        private void OnRunFailed()
        {
            if (runSession == null || runSession.IsFinished)
            {
                return;
            }

            runSession.FailRun();
            ShowRunEnd(false);
            RefreshDebugText();
        }

        private void HandleVictory()
        {
            if (runSession == null || waveManager == null)
            {
                ShowRunEnd(true);
                return;
            }

            lastGoldReward = CalculateGoldReward(runSession.CompletedWaves, runSession.MaxWaves, waveManager.EscapedCount);
            wallet.AddGold(lastGoldReward);
            ShowRunEnd(true);
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (hudView == null || wallet == null)
            {
                return;
            }

            hudView.SetEnergy(wallet.Energy);
            hudView.SetGold(wallet.Gold);
            if (buildPrototype != null)
            {
                hudView.SetCombatStats(
                    buildPrototype.GetTurretDamage(),
                    buildPrototype.GetTurretFireCooldownSeconds(),
                    buildPrototype.GetBaseHealthBonus());
            }
        }

        private void ApplyUpgradeEffects()
        {
            if (waveManager == null || buildPrototype == null)
            {
                return;
            }

            if (initialMaxBaseHealth <= 0)
            {
                initialMaxBaseHealth = waveManager.MaxBaseHealth;
            }

            int targetMaxBaseHealth = initialMaxBaseHealth + buildPrototype.GetBaseHealthBonus();
            waveManager.ApplyMaxBaseHealth(targetMaxBaseHealth, addDeltaToCurrent: true);
        }

        private int CalculateGoldReward(int completedWaves, int maxWaves, int escapedCount)
        {
            int baseReward = Mathf.Max(0, completedWaves) * 10;
            int perfectBonus = escapedCount <= 0 && completedWaves >= maxWaves ? 20 : 0;
            return Mathf.Max(0, baseReward + perfectBonus);
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
                : $"Run: {runSession.Result} (Completed {runSession.CompletedWaves}/{runSession.MaxWaves})";

            string turretLine = buildPrototype != null
                ? $"Turret: DMG {buildPrototype.GetTurretDamage()} | CD {buildPrototype.GetTurretFireCooldownSeconds():0.00}s | Base bonus +{buildPrototype.GetBaseHealthBonus()}"
                : "Turret: -";

            int towerLv = buildPrototype != null ? buildPrototype.Level : 0;
            debugText.text =
                $"Accuracy: {typingSession.Stats.Accuracy:P0}\nCombo: {typingSession.Stats.Combo}\nBest: {typingSession.Stats.BestCombo}\nEnergy: {wallet.Energy}\nGold: {wallet.Gold}\nTower Lv: {towerLv}\n{turretLine}\n{combatText}\n{sessionText}";
        }

        private void UpdateCombatStatusPanel()
        {
            if (combatStatusPanel == null || waveManager == null || runSession == null)
            {
                return;
            }

            float waveIntraProgress = waveManager.IsWaveComplete
                ? 0f
                : (float)waveManager.SpawnedCount / waveManager.EnemiesPerWave;
            float waveProgressNormalized = Mathf.Clamp01((runSession.CompletedWaves + waveIntraProgress) / runSession.MaxWaves);
            int displayWave = Mathf.Clamp(runSession.CompletedWaves + 1, 1, runSession.MaxWaves);

            combatStatusPanel.UpdateBaseHealth(waveManager.CurrentBaseHealth, waveManager.MaxBaseHealth);
            combatStatusPanel.UpdateWaveProgress(waveProgressNormalized, displayWave, runSession.MaxWaves);
            combatStatusPanel.UpdateEscapedCount(waveManager.EscapedCount);
        }

        private void InitializeGameOverPanel()
        {
            gameOverShown = false;

            if (gameOverPanelView == null)
            {
                GameObject panel = GameObject.Find("GameOverPanel");
                if (panel != null)
                {
                    gameOverPanelView = panel.GetComponent<GameOverPanelView>();
                }
            }

            if (gameOverPanelView == null)
            {
                Debug.LogError("RunHudController requires GameOverPanelView. Add it to the GameOverPanel object and bind it in inspector.");
                return;
            }

            gameOverPanelView.Initialize(OnRetryClicked, OnMainMenuClicked);
        }

        private void ShowRunEnd(bool victory)
        {
            if (gameOverShown)
            {
                return;
            }

            gameOverShown = true;

            int completed = runSession != null ? runSession.CompletedWaves : 0;
            int max = runSession != null ? runSession.MaxWaves : 0;
            int escaped = waveManager != null ? waveManager.EscapedCount : 0;
            string rewardLine = victory ? $"Gold +{lastGoldReward}" : "Gold +0";
            string buildLine = buildPrototype != null
                ? $"Tower Lv {buildPrototype.Level} | Turret DMG {buildPrototype.GetTurretDamage()} | Base+{buildPrototype.GetBaseHealthBonus()}"
                : string.Empty;
            string summary = string.IsNullOrEmpty(buildLine)
                ? $"Waves: {completed}/{max}\nEscaped: {escaped}\n{rewardLine}\nTotal Gold: {wallet.Gold}"
                : $"Waves: {completed}/{max}\nEscaped: {escaped}\n{rewardLine}\n{buildLine}\nTotal Gold: {wallet.Gold}";

            if (gameOverPanelView != null)
            {
                gameOverPanelView.Show(victory ? "VICTORY" : "GAME OVER", summary);
            }

            Time.timeScale = 0f;
        }

        private void OnRetryClicked()
        {
            Time.timeScale = 1f;
            FlushPersistentGold();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            FlushPersistentGold();

            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.LoadMainMenu();
                return;
            }

            SceneManager.LoadScene("MainMenu");
        }
    }
}
