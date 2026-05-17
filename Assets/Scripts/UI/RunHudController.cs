using System.Collections;
using System.Text;
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

        [Header("Wave intermission shop (E2–E6)")]
        [Tooltip("留空则在运行时挂到主 Canvas 下自动生成波间面板。")]
        [SerializeField] private WaveIntermissionPanelView waveIntermissionPanel;

        [SerializeField] private int repairBaseEnergyCost = 10;
        [SerializeField] private int repairBaseHealAmount = 1;

        private TypingSession typingSession;
        private TypingInputRouter inputRouter;
        private ResourceWallet wallet;
        private TypingRewardCalculator rewardCalculator;
        private BuildPrototype buildPrototype;
        private BuildService buildService;
        private CombatWaveManager waveManager;
        private SectorFocusCombatAdapter sectorCombat;
        private SectorFocusPresenter sectorPresenter;
        private ManualTurretController manualTurret;
        private RunSession runSession;
        private readonly RunBuffState runBuffState = new RunBuffState();
        private readonly IntermissionBuffOffer[] rolledBuffOffers = new IntermissionBuffOffer[3];
        private System.Random buffRng;
        private bool gameOverShown;
        private int lastGoldReward;
        private int initialMaxBaseHealth;
        private Coroutine waveIntermissionCoroutine;
        private bool waveIntermissionActive;
        private bool intermissionContinueRequested;
        private bool intermissionBuffChosen;
        private bool intermissionPanelEventsWired;

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
            runSession.Reset();
            runBuffState.Reset();
            buffRng = new System.Random();
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
                waveManager.SetCombatActive(false);
                initialMaxBaseHealth = waveManager.MaxBaseHealth;

                sectorCombat = waveManager.GetComponent<SectorFocusCombatAdapter>();
                if (sectorCombat == null)
                {
                    sectorCombat = waveManager.gameObject.AddComponent<SectorFocusCombatAdapter>();
                }

                sectorCombat.Bind(waveManager, buildPrototype);
                SetupSectorPresentation();
            }

            if (runSession != null)
            {
                runSession.Phase.PhaseChanged += OnRunPhaseChanged;
                ApplyRunPhase(runSession.Phase.CurrentPhase);
            }

            manualTurret = FindObjectOfType<ManualTurretController>();
            if (manualTurret != null)
            {
                manualTurret.Bind(buildPrototype, wallet, runBuffState);
            }

            if (buildPanelView != null)
            {
                buildPanelView.BuildChanged += OnBuildChanged;
                buildPanelView.Bind(buildPrototype, wallet, buildService);
            }

            InitializeGameOverPanel();

            RefreshDebugText();
            RefreshHud();
            SyncCombatBuffModifiers();
        }

        private WaveIntermissionPanelView ResolveIntermissionPanel()
        {
            if (waveIntermissionPanel != null)
            {
                return waveIntermissionPanel;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            waveIntermissionPanel = WaveIntermissionPanelView.EnsureUnderCanvas(canvas, null);
            return waveIntermissionPanel;
        }

        private void EnsureIntermissionPanelWired()
        {
            WaveIntermissionPanelView panel = ResolveIntermissionPanel();
            if (panel == null || intermissionPanelEventsWired)
            {
                return;
            }

            panel.RepairClicked += OnIntermissionRepairClicked;
            panel.BuffClicked += OnIntermissionBuffClicked;
            panel.ContinueClicked += OnIntermissionContinueClicked;
            intermissionPanelEventsWired = true;
        }

        private void OnIntermissionContinueClicked()
        {
            if (!waveIntermissionActive)
            {
                return;
            }

            intermissionContinueRequested = true;
        }

        private void OnIntermissionRepairClicked()
        {
            if (!waveIntermissionActive || waveManager == null || wallet == null)
            {
                return;
            }

            waveManager.TryRepairBase(wallet, repairBaseEnergyCost, repairBaseHealAmount);
            RefreshHud();
            RefreshDebugText();
        }

        private void OnIntermissionBuffClicked(int index)
        {
            if (!waveIntermissionActive || intermissionBuffChosen || wallet == null || (uint)index >= 3)
            {
                return;
            }

            IntermissionBuffOffer offer = rolledBuffOffers[index];
            if (!wallet.TrySpendEnergy(offer.EnergyCost))
            {
                return;
            }

            runBuffState.ApplyOffer(offer);
            intermissionBuffChosen = true;
            SyncCombatBuffModifiers();
            RefreshHud();
            RefreshDebugText();
        }

        private void RefreshIntermissionPanelUi()
        {
            WaveIntermissionPanelView panel = ResolveIntermissionPanel();
            if (panel == null || runSession == null || wallet == null || waveManager == null)
            {
                return;
            }

            int nextWave = Mathf.Clamp(runSession.CompletedWaves + 1, 1, runSession.MaxWaves);

            string rule = intermissionBuffChosen
                ? "Buff locked for this break. You can still repair the base. Tap Continue when ready."
                : "Pick ONE buff below (optional). Repair if needed. Tap Continue to start the next wave.";
            string carrying = runBuffState.BuildCarriedFootnote();

            panel.SetTexts(
                "Wave break — spend Energy, then tap Continue.",
                $"Energy: {wallet.Energy}",
                $"Next wave: {nextWave} / {runSession.MaxWaves}",
                $"{rule}\n---\nCarrying:\n{carrying}");

            bool canRepair = wallet.Energy >= repairBaseEnergyCost
                && waveManager.CurrentBaseHealth < waveManager.MaxBaseHealth;
            panel.SetRepairButton($"Repair base ({repairBaseEnergyCost} E → +{repairBaseHealAmount} HP)", canRepair);
            panel.SetContinueButtonInteractable(true);

            for (int i = 0; i < 3; i++)
            {
                IntermissionBuffOffer o = rolledBuffOffers[i];
                string label = $"{o.DisplayName}\n{o.Rarity} · {o.EnergyCost} E";
                bool canPick = !intermissionBuffChosen && wallet.Energy >= o.EnergyCost;
                panel.SetBuffSlot(i, label, canPick);
            }
        }

        private void SyncCombatBuffModifiers()
        {
            if (waveManager != null)
            {
                waveManager.SetLeakDamageReductionStacks(runBuffState.LeakDamageMinusStacks);
            }

            if (sectorCombat != null)
            {
                sectorCombat.SetSectorBuffDamageMultiplier(runBuffState.SectorProjectileDamageMultiplier);
            }

            if (manualTurret != null)
            {
                manualTurret.Bind(buildPrototype, wallet, runBuffState);
            }
        }

        private void Update()
        {
            // In case the scene was started directly (no Boot/GameBootstrap),
            // or bootstrap is spawned after this controller.
            if (runSession == null)
            {
                runSession = TryGetRunSession();
            }

            SyncCombatBuffModifiers();

            if (runSession != null && runSession.Phase.CurrentPhase == RunPhase.Preparation && Input.GetButtonDown("Submit"))
            {
                TryBeginCombat();
            }

            if (waveIntermissionActive && runSession != null && runSession.Phase.CanRunCombat && Input.GetButtonDown("Submit"))
            {
                intermissionContinueRequested = true;
            }

            RefreshDebugText();
            UpdateCombatStatusPanel();
            RefreshHud();
            RefreshPhaseHud();
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

            StopWaveIntermissionCoroutine();

            if (waveIntermissionPanel != null && intermissionPanelEventsWired)
            {
                waveIntermissionPanel.RepairClicked -= OnIntermissionRepairClicked;
                waveIntermissionPanel.BuffClicked -= OnIntermissionBuffClicked;
                waveIntermissionPanel.ContinueClicked -= OnIntermissionContinueClicked;
            }

            intermissionPanelEventsWired = false;

            if (buildPanelView != null)
            {
                buildPanelView.BuildChanged -= OnBuildChanged;
            }

            if (runSession != null)
            {
                runSession.Phase.PhaseChanged -= OnRunPhaseChanged;
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
            if (runSession == null || !runSession.Phase.CanEarnEnergy)
            {
                return;
            }

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
            if (sectorCombat != null)
            {
                sectorCombat.Bind(waveManager, buildPrototype);
            }

            RefreshDebugText();
            RefreshHud();
            ApplyUpgradeEffects();
            SyncCombatBuffModifiers();
        }

        private void OnWaveCompleted()
        {
            if (runSession == null || runSession.IsFinished || !runSession.Phase.CanRunCombat)
            {
                return;
            }

            runSession.AdvanceWave();
            if (runSession.CompletedWaves >= runSession.MaxWaves)
            {
                StopWaveIntermissionCoroutine();
                runSession.CompleteRun();
                HandleVictory();
            }
            else
            {
                if (waveManager != null && !waveManager.IsRunFailed)
                {
                    StopWaveIntermissionCoroutine();
                    waveIntermissionCoroutine = StartCoroutine(WaveIntermissionThenStartNextWave());
                }
            }

            RefreshDebugText();
        }

        private IEnumerator WaveIntermissionThenStartNextWave()
        {
            waveIntermissionActive = true;
            intermissionContinueRequested = false;
            intermissionBuffChosen = false;
            IntermissionBuffCatalog.FillThreeShuffledOffers(buffRng, rolledBuffOffers);

            if (waveManager != null)
            {
                waveManager.SetCombatActive(false);
            }

            EnsureIntermissionPanelWired();
            WaveIntermissionPanelView panel = ResolveIntermissionPanel();
            panel?.SetVisible(true);

            while (!intermissionContinueRequested)
            {
                if (runSession == null || runSession.IsFinished || !runSession.Phase.CanRunCombat)
                {
                    break;
                }

                if (waveManager != null && waveManager.IsRunFailed)
                {
                    break;
                }

                SyncCombatBuffModifiers();
                RefreshIntermissionPanelUi();
                yield return null;
            }

            panel?.SetVisible(false);

            waveIntermissionCoroutine = null;
            waveIntermissionActive = false;
            intermissionContinueRequested = false;

            if (runSession == null || runSession.IsFinished || !runSession.Phase.CanRunCombat)
            {
                yield break;
            }

            if (waveManager == null || waveManager.IsRunFailed)
            {
                yield break;
            }

            waveManager.SetCombatActive(true);
            waveManager.StartNewWave();
            SyncCombatBuffModifiers();
            RefreshDebugText();
        }

        private void StopWaveIntermissionCoroutine()
        {
            if (waveIntermissionCoroutine != null)
            {
                StopCoroutine(waveIntermissionCoroutine);
                waveIntermissionCoroutine = null;
            }

            waveIntermissionActive = false;
            intermissionContinueRequested = false;
            if (waveIntermissionPanel != null)
            {
                waveIntermissionPanel.SetVisible(false);
            }
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
            StopWaveIntermissionCoroutine();

            if (runSession == null || runSession.IsFinished)
            {
                return;
            }

            runSession.FailRun();
            runSession.Phase.EnterSettlement();
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
            runSession.Phase.EnterSettlement();
            ShowRunEnd(true);
            RefreshHud();
        }

        private void TryBeginCombat()
        {
            if (runSession == null || runSession.IsFinished || !runSession.Phase.TryBeginCombat())
            {
                return;
            }

            StopWaveIntermissionCoroutine();

            sectorCombat?.ResetFocus();

            if (waveManager != null)
            {
                waveManager.StartNewWave();
            }
        }

        private void OnRunPhaseChanged(RunPhase phase)
        {
            ApplyRunPhase(phase);
            RefreshPhaseHud();
        }

        private void ApplyRunPhase(RunPhase phase)
        {
            bool preparation = phase == RunPhase.Preparation;
            bool combat = phase == RunPhase.Combat;
            bool settlement = phase == RunPhase.Settlement;

            if (waveManager != null)
            {
                waveManager.SetCombatActive(combat);
            }

            if (manualTurret != null)
            {
                manualTurret.SetInteractionMode(
                    fireEnabled: preparation || combat,
                    enemyDamageEnabled: combat);
            }

            if (sectorCombat != null)
            {
                sectorCombat.SetCombatEnabled(combat);
                if (combat)
                {
                    sectorCombat.System?.SetCombatEnabled(true);
                }
            }

            sectorPresenter?.SetVisible(combat);
        }

        private void SetupSectorPresentation()
        {
            if (sectorCombat == null)
            {
                return;
            }

            SectorFocusHudView sectorHud = FindObjectOfType<SectorFocusHudView>();
            SectorFocusWorldView worldView = FindObjectOfType<SectorFocusWorldView>();
            CombatArena arena = FindObjectOfType<CombatArena>();

            if (sectorHud == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    GameObject hudObject = new GameObject("SectorFocusHud", typeof(RectTransform), typeof(SectorFocusHudView));
                    RectTransform rect = hudObject.GetComponent<RectTransform>();
                    rect.SetParent(canvas.transform, false);
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 120f);
                    rect.sizeDelta = new Vector2(220f, 64f);
                    sectorHud = hudObject.GetComponent<SectorFocusHudView>();
                }
            }

            if (worldView == null)
            {
                Transform parent = arena != null && arena.CenterPoint != null
                    ? arena.CenterPoint
                    : waveManager != null ? waveManager.transform : transform;
                GameObject worldObject = new GameObject("SectorFocusWorld");
                worldObject.transform.SetParent(parent, false);
                worldView = worldObject.AddComponent<SectorFocusWorldView>();
            }

            sectorPresenter = FindObjectOfType<SectorFocusPresenter>();
            if (sectorPresenter == null)
            {
                GameObject presenterObject = new GameObject("SectorFocusPresenter");
                sectorPresenter = presenterObject.AddComponent<SectorFocusPresenter>();
            }

            sectorPresenter.Initialize(sectorCombat, sectorHud, worldView);
            sectorPresenter.SetVisible(false);
        }

        private void RefreshPhaseHud()
        {
            if (hudView == null || runSession == null)
            {
                return;
            }

            RunPhase phase = runSession.Phase.CurrentPhase;
            string hint = phase switch
            {
                RunPhase.Preparation => "Type for Energy. Enter = combat.",
                RunPhase.Combat when waveManager != null => BuildCombatPhaseHint(),
                _ => string.Empty
            };

            hudView.SetRunPhase(phase, hint);
        }

        private string BuildCombatPhaseHint()
        {
            if (waveIntermissionActive)
            {
                int nextWave = Mathf.Clamp(runSession.CompletedWaves + 1, 1, runSession.MaxWaves);
                return $"Wave break — next {nextWave}/{runSession.MaxWaves} | Energy {wallet?.Energy ?? 0} | Tap Continue in panel";
            }

            int wave = Mathf.Clamp(runSession.CompletedWaves + 1, 1, runSession.MaxWaves);
            string line = $"Wave {wave}/{runSession.MaxWaves} | A/D = sector";

            if (sectorCombat == null)
            {
                return line;
            }

            SectorFocusSnapshot status = sectorCombat.Snapshot;
            int displaySector = status.FocusedSector + 1;
            line += $" | Focus {displaySector}/{status.SectorCount}";

            if (!status.IsReady)
            {
                line += $" (arming {status.WarmupRemaining:0.1}s)";
            }
            else if (status.HasTarget)
            {
                line += " | firing";
            }

            return line;
        }

        private void RefreshHud()
        {
            if (gameOverShown)
            {
                return;
            }

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

            hudView.SetRunBuffsSummary(runBuffState.BuildHudMultiline());
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

            string phaseText = runSession == null
                ? "Phase: Unknown"
                : $"Phase: {runSession.Phase.CurrentPhase}";

            string sessionText = runSession == null
                ? "Run: Unknown"
                : $"Run: {runSession.Result} (Completed {runSession.CompletedWaves}/{runSession.MaxWaves})";

            string turretLine = buildPrototype != null
                ? $"Turret: DMG {buildPrototype.GetTurretDamage()} | CD {buildPrototype.GetTurretFireCooldownSeconds():0.00}s | Base bonus +{buildPrototype.GetBaseHealthBonus()}"
                : "Turret: -";

            string buffLine = runBuffState.BuildSummaryLine();

            int towerLv = buildPrototype != null ? buildPrototype.Level : 0;
            debugText.text =
                $"Accuracy: {typingSession.Stats.Accuracy:P0}\nCombo: {typingSession.Stats.Combo}\nBest: {typingSession.Stats.BestCombo}\nEnergy: {wallet.Energy}\nGold: {wallet.Gold}\nTower Lv: {towerLv}\n{turretLine}\n{buffLine}\n{phaseText}\n{combatText}\n{sessionText}";
        }

        private void UpdateCombatStatusPanel()
        {
            if (combatStatusPanel == null || waveManager == null || runSession == null)
            {
                return;
            }

            if (runSession.Phase.CurrentPhase != RunPhase.Combat)
            {
                combatStatusPanel.UpdateBaseHealth(waveManager.CurrentBaseHealth, waveManager.MaxBaseHealth);
                combatStatusPanel.UpdateWaveProgress(0f, 0, runSession.MaxWaves);
                combatStatusPanel.UpdateEscapedCount(waveManager.EscapedCount);
                return;
            }

            if (waveIntermissionActive)
            {
                float betweenWaves = Mathf.Clamp01((float)runSession.CompletedWaves / runSession.MaxWaves);
                int nextWave = Mathf.Clamp(runSession.CompletedWaves + 1, 1, runSession.MaxWaves);
                combatStatusPanel.UpdateBaseHealth(waveManager.CurrentBaseHealth, waveManager.MaxBaseHealth);
                combatStatusPanel.UpdateWaveProgress(betweenWaves, nextWave, runSession.MaxWaves);
                combatStatusPanel.UpdateEscapedCount(waveManager.EscapedCount);
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

            StopWaveIntermissionCoroutine();

            gameOverShown = true;

            if (runSession != null && runSession.Phase.CurrentPhase != RunPhase.Settlement)
            {
                runSession.Phase.EnterSettlement();
            }

            if (waveManager != null)
            {
                waveManager.SetCombatActive(false);
            }

            if (hudView != null)
            {
                hudView.SetVisible(false);
            }

            if (combatStatusPanel != null)
            {
                combatStatusPanel.gameObject.SetActive(false);
            }

            sectorPresenter?.SetVisible(false);

            int completed = runSession != null ? runSession.CompletedWaves : 0;
            int max = runSession != null ? runSession.MaxWaves : 0;
            int escaped = waveManager != null ? waveManager.EscapedCount : 0;
            string rewardLine = victory ? $"Reward: +{lastGoldReward} gold" : "Reward: +0 gold";
            string buffSection = runBuffState.BuildSettlementSection();

            var sb = new StringBuilder();
            sb.AppendLine("--- Run summary ---");
            sb.AppendLine($"Waves: {completed} / {max}");
            sb.AppendLine($"Escaped: {escaped}");
            sb.AppendLine(rewardLine);
            sb.AppendLine();

            if (buildPrototype != null)
            {
                sb.AppendLine("--- Build ---");
                sb.AppendLine($"Tower Lv {buildPrototype.Level}");
                sb.AppendLine($"Turret damage: {buildPrototype.GetTurretDamage()}");
                sb.AppendLine($"Base HP bonus: +{buildPrototype.GetBaseHealthBonus()}");
                sb.AppendLine();
            }

            sb.AppendLine($"Total gold: {wallet.Gold}");
            sb.AppendLine();
            sb.Append(buffSection.TrimEnd());

            string summary = sb.ToString();

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
