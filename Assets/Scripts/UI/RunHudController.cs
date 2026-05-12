using IdleTafang.Gameplay;
using IdleTafang.Gameplay.Typing;
using IdleTafang.Gameplay.Builds;
using IdleTafang.Gameplay.Combat;
using IdleTafang.Gameplay.Resources;
using IdleTafang.Core;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IdleTafang.UI
{
    public sealed class RunHudController : MonoBehaviour
    {
        [SerializeField] private HudView hudView;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private BuildPanelView buildPanelView;
        [SerializeField] private CombatStatusPanelView combatStatusPanel;
        [SerializeField] private GameObject gameOverPanelRoot;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TMP_Text gameOverTitleText;
        [SerializeField] private TMP_Text gameOverSummaryText;

        private TypingSession typingSession;
        private TypingInputRouter inputRouter;
        private ResourceWallet wallet;
        private TypingRewardCalculator rewardCalculator;
        private BuildPrototype buildPrototype;
        private BuildService buildService;
        private CombatWaveManager waveManager;
        private RunSession runSession;
        private bool gameOverShown;
        private int lastGoldReward;

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
            }

            if (buildPanelView != null)
            {
                buildPanelView.BuildChanged += OnBuildChanged;
                buildPanelView.Bind(buildPrototype, wallet, buildService);
            }

            InitializeGameOverUi();

            RefreshDebugText();
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

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }

            // Ensure subsequent scenes are not accidentally paused.
            Time.timeScale = 1f;
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

            debugText.text =
                $"Accuracy: {typingSession.Stats.Accuracy:P0}\nCombo: {typingSession.Stats.Combo}\nBest: {typingSession.Stats.BestCombo}\nEnergy: {wallet.Energy}\nGold: {wallet.Gold}\nTower Lv: {buildPrototype.Level}\n{combatText}\n{sessionText}";
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

        private void InitializeGameOverUi()
        {
            gameOverShown = false;

            // GameOverPanel must be placed in the scene hierarchy and bound via inspector.
            // We keep a best-effort lookup by name for convenience, but we never create UI objects in code.
            if (gameOverPanelRoot == null)
            {
                gameOverPanelRoot = GameObject.Find("GameOverPanel");
            }

            if (gameOverPanelRoot != null)
            {
                if (retryButton == null)
                {
                    retryButton = gameOverPanelRoot.GetComponentInChildren<Button>(true);
                }

                if (mainMenuButton == null)
                {
                    Transform t = gameOverPanelRoot.transform.Find("MainMenuButton");
                    if (t != null)
                    {
                        mainMenuButton = t.GetComponent<Button>();
                    }
                }

                if (gameOverTitleText == null)
                {
                    TMP_Text[] texts = gameOverPanelRoot.GetComponentsInChildren<TMP_Text>(true);
                    for (int i = 0; i < texts.Length; i++)
                    {
                        if (texts[i] != null && texts[i].name == "GameOverTitle")
                        {
                            gameOverTitleText = texts[i];
                            break;
                        }
                    }
                }

                if (gameOverSummaryText == null)
                {
                    TMP_Text[] texts = gameOverPanelRoot.GetComponentsInChildren<TMP_Text>(true);
                    for (int i = 0; i < texts.Length; i++)
                    {
                        if (texts[i] != null && texts[i].name == "GameOverSummary")
                        {
                            gameOverSummaryText = texts[i];
                            break;
                        }
                    }
                }

                gameOverPanelRoot.SetActive(false);
            }
            else
            {
                Debug.LogError("RunHudController requires a GameOverPanel in the scene (assign gameOverPanelRoot or name it 'GameOverPanel').");
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            else
            {
                Debug.LogWarning("RunHudController could not find a Retry Button under GameOverPanel. Please bind retryButton in inspector.");
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void ShowRunEnd(bool victory)
        {
            if (gameOverShown)
            {
                return;
            }

            gameOverShown = true;

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = victory ? "VICTORY" : "GAME OVER";
            }

            if (gameOverSummaryText != null)
            {
                int completed = runSession != null ? runSession.CompletedWaves : 0;
                int max = runSession != null ? runSession.MaxWaves : 0;
                int escaped = waveManager != null ? waveManager.EscapedCount : 0;
                string rewardLine = victory ? $"Gold +{lastGoldReward}" : "Gold +0";
                gameOverSummaryText.text = $"Waves: {completed}/{max}\nEscaped: {escaped}\n{rewardLine}\nTotal Gold: {wallet.Gold}";
            }

            if (gameOverPanelRoot != null)
            {
                gameOverPanelRoot.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        private void OnRetryClicked()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;

            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.LoadMainMenu();
                return;
            }

            SceneManager.LoadScene("MainMenu");
        }
    }
}
