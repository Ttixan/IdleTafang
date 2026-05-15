using IdleTafang.Core;
using IdleTafang.Gameplay.Builds;
using IdleTafang.Gameplay.Combat;
using IdleTafang.Gameplay.Resources;
using IdleTafang.Gameplay;
using IdleTafang.Gameplay.Typing;
using NUnit.Framework;

namespace IdleTafang.Tests.Editor
{
    public sealed class LogicTests
    {
        [Test]
        public void TypingStats_RegisterCorrect_TracksComboAndAccuracy()
        {
            TypingStats stats = new TypingStats();

            stats.RegisterCorrect();
            stats.RegisterCorrect();
            stats.RegisterMiss();

            Assert.That(stats.TotalTyped, Is.EqualTo(3));
            Assert.That(stats.PromptTyped, Is.EqualTo(3));
            Assert.That(stats.CorrectTyped, Is.EqualTo(2));
            Assert.That(stats.Combo, Is.EqualTo(0));
            Assert.That(stats.BestCombo, Is.EqualTo(2));
            Assert.That(stats.Accuracy, Is.EqualTo(2f / 3f).Within(0.0001f));
        }

        [Test]
        public void TypingSession_IgnoresWhitespace_AndCountsVisibleCharacters()
        {
            TypingSession session = new TypingSession();

            session.SubmitCharacter(' ');
            session.SubmitCharacter('\n');
            session.SubmitCharacter('a');
            session.SubmitCharacter('!');

            Assert.That(session.Stats.TotalTyped, Is.EqualTo(2));
            Assert.That(session.Stats.CorrectTyped, Is.EqualTo(2));
            Assert.That(session.Stats.Combo, Is.EqualTo(2));

            session.Reset();

            Assert.That(session.Stats.TotalTyped, Is.EqualTo(0));
            Assert.That(session.Stats.CorrectTyped, Is.EqualTo(0));
            Assert.That(session.Stats.Combo, Is.EqualTo(0));
        }

        [Test]
        public void TypingRewardCalculator_UsesCorrectTypedAndComboBonus()
        {
            TypingStats stats = new TypingStats();
            TypingRewardCalculator calculator = new TypingRewardCalculator();

            stats.RegisterCorrect();
            stats.RegisterCorrect();
            stats.RegisterCorrect();

            Assert.That(calculator.CalculateEnergyReward(stats), Is.EqualTo(4));
            Assert.That(calculator.CalculateEnergyReward(null), Is.EqualTo(0));
        }

        [Test]
        public void ResourceWallet_AddSpendAndReset_WorkAsExpected()
        {
            ResourceWallet wallet = new ResourceWallet();

            wallet.AddEnergy(10);
            wallet.AddGold(7);
            wallet.AddEnergy(-3);
            wallet.AddGold(0);
            wallet.SpendEnergy(4);
            wallet.SpendEnergy(99);

            Assert.That(wallet.Energy, Is.EqualTo(0));
            Assert.That(wallet.Gold, Is.EqualTo(7));

            wallet.Reset();

            Assert.That(wallet.Energy, Is.EqualTo(0));
            Assert.That(wallet.Gold, Is.EqualTo(0));
        }

        [Test]
        public void ResourceWallet_TrySpendEnergy_DoesNotPartialSpend_OnInsufficientBalance()
        {
            ResourceWallet wallet = new ResourceWallet();
            wallet.AddEnergy(5);

            Assert.That(wallet.TrySpendEnergy(6), Is.False);
            Assert.That(wallet.Energy, Is.EqualTo(5));

            Assert.That(wallet.TrySpendEnergy(5), Is.True);
            Assert.That(wallet.Energy, Is.EqualTo(0));
        }

        [Test]
        public void RunBuffState_StacksSectorMultiplierAndDiscounts()
        {
            RunBuffState buff = new RunBuffState();
            Assert.That(buff.SectorProjectileDamageMultiplier, Is.EqualTo(1f).Within(0.001f));

            buff.ApplyOffer(IntermissionBuffCatalog.All[0]);
            Assert.That(buff.SectorProjectileDamageMultiplier, Is.EqualTo(1.1f).Within(0.001f));

            buff.ApplyOffer(IntermissionBuffCatalog.All[1]);
            Assert.That(buff.DiscountSpecialEnergyCost(5), Is.EqualTo(4));

            buff.ApplyOffer(IntermissionBuffCatalog.All[2]);
            Assert.That(buff.LeakDamageMinusStacks, Is.EqualTo(1));
        }

        [Test]
        public void IntermissionBuffCatalog_FillThreeShuffledOffers_IsPermutationOfCatalog()
        {
            IntermissionBuffOffer[] rolled = new IntermissionBuffOffer[3];
            IntermissionBuffCatalog.FillThreeShuffledOffers(new System.Random(42), rolled);

            for (int i = 0; i < IntermissionBuffCatalog.All.Length; i++)
            {
                string id = IntermissionBuffCatalog.All[i].Id;
                int found = 0;
                for (int j = 0; j < rolled.Length; j++)
                {
                    if (rolled[j].Id == id)
                    {
                        found++;
                    }
                }

                Assert.That(found, Is.EqualTo(1));
            }
        }

        [Test]
        public void BuildPrototype_NormalizesInput_AndUpgrades()
        {
            BuildPrototype prototype = new BuildPrototype(null, -5);

            Assert.That(prototype.Name, Is.EqualTo("Build"));
            Assert.That(prototype.EnergyCost, Is.EqualTo(0));
            Assert.That(prototype.Level, Is.EqualTo(1));

            prototype.Upgrade();

            Assert.That(prototype.Level, Is.EqualTo(2));
        }

        [Test]
        public void BuildService_BuildsOnlyWhenEnoughEnergy()
        {
            BuildPrototype prototype = new BuildPrototype("Tower", 5);
            ResourceWallet wallet = new ResourceWallet();
            BuildService service = new BuildService();

            Assert.That(service.TryBuild(prototype, wallet), Is.False);

            wallet.AddEnergy(5);

            Assert.That(service.TryBuild(prototype, wallet), Is.True);
            Assert.That(wallet.Energy, Is.EqualTo(0));
            Assert.That(prototype.Level, Is.EqualTo(2));
        }

        [Test]
        public void GameLoop_ChangesState_AndTicksCurrentState()
        {
            GameLoop loop = new GameLoop();
            TestState boot = new TestState(GameStateId.Boot);
            TestState run = new TestState(GameStateId.Run);

            loop.RegisterState(boot);
            loop.RegisterState(run);

            loop.ChangeState(GameStateId.Boot);
            loop.Tick(0.25f);
            loop.ChangeState(GameStateId.Run);
            loop.Tick(0.5f);

            Assert.That(boot.EnterCount, Is.EqualTo(1));
            Assert.That(boot.ExitCount, Is.EqualTo(1));
            Assert.That(boot.TickCount, Is.EqualTo(1));
            Assert.That(boot.LastDeltaTime, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(run.EnterCount, Is.EqualTo(1));
            Assert.That(run.ExitCount, Is.EqualTo(0));
            Assert.That(run.TickCount, Is.EqualTo(1));
            Assert.That(run.LastDeltaTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(loop.CurrentStateId, Is.EqualTo(GameStateId.Run));
        }

        [Test]
        public void GameLoop_ThrowsWhenStateIsMissing()
        {
            GameLoop loop = new GameLoop();

            Assert.That(() => loop.ChangeState(GameStateId.Run), Throws.InvalidOperationException);
        }

        [Test]
        public void RunSession_TracksWaveProgress_AndOutcome()
        {
            RunSession session = new RunSession();

            session.AdvanceWave();
            session.AdvanceWave();
            session.CompleteRun();
            session.AdvanceWave();

            Assert.That(session.WaveIndex, Is.EqualTo(2));
            Assert.That(session.CompletedWaves, Is.EqualTo(2));
            Assert.That(session.Result, Is.EqualTo(RunResult.Victory));
            Assert.That(session.IsFinished, Is.True);

            session.Reset();

            Assert.That(session.WaveIndex, Is.EqualTo(0));
            Assert.That(session.CompletedWaves, Is.EqualTo(0));
            Assert.That(session.Result, Is.EqualTo(RunResult.InProgress));
            Assert.That(session.IsFinished, Is.False);
            Assert.That(session.Phase.CurrentPhase, Is.EqualTo(RunPhase.Preparation));
        }

        [Test]
        public void RunPhaseController_GatesEnergyAndCombatTransitions()
        {
            RunPhaseController phase = new RunPhaseController();
            int changeCount = 0;
            phase.PhaseChanged += _ => changeCount += 1;

            Assert.That(phase.CurrentPhase, Is.EqualTo(RunPhase.Preparation));
            Assert.That(phase.CanEarnEnergy, Is.True);
            Assert.That(phase.CanRunCombat, Is.False);

            Assert.That(phase.TryBeginCombat(), Is.True);
            Assert.That(phase.CurrentPhase, Is.EqualTo(RunPhase.Combat));
            Assert.That(phase.CanEarnEnergy, Is.False);
            Assert.That(phase.CanRunCombat, Is.True);
            Assert.That(phase.TryBeginCombat(), Is.False);

            phase.EnterSettlement();
            Assert.That(phase.IsFinished, Is.True);
            Assert.That(phase.CanRunCombat, Is.False);

            phase.Reset();
            Assert.That(phase.CurrentPhase, Is.EqualTo(RunPhase.Preparation));
            Assert.That(changeCount, Is.EqualTo(3));
        }

        [Test]
        public void SectorMath_MapsAnglesToThreeSectors()
        {
            Assert.That(SectorMath.GetSectorIndex(0f, 0f, 0f, 10f, 3), Is.EqualTo(0));
            Assert.That(SectorMath.GetSectorIndex(0f, 0f, 0f, -10f, 3), Is.EqualTo(1));
            Assert.That(SectorMath.GetSectorIndex(0f, 0f, -10f, 0f, 3), Is.EqualTo(2));
        }

        [Test]
        public void SectorFocusLogic_SwitchAppliesWarmup()
        {
            SectorFocusLogic focus = new SectorFocusLogic(3, 0.5f, 0);

            focus.FocusNext();
            Assert.That(focus.FocusedSector, Is.EqualTo(1));
            Assert.That(focus.IsReady, Is.False);
            Assert.That(focus.WarmupRemaining, Is.EqualTo(0.5f).Within(0.0001f));

            focus.Tick(0.5f);
            Assert.That(focus.IsReady, Is.True);

            focus.FocusPrevious();
            Assert.That(focus.FocusedSector, Is.EqualTo(0));
            Assert.That(focus.IsReady, Is.False);
        }

        [Test]
        public void SectorTargetSelector_PicksNearestEnemyInSector()
        {
            var enemies = new[]
            {
                new SectorCombatEnemyInfo(1, 0f, 8f),
                new SectorCombatEnemyInfo(2, 0f, 4f),
                new SectorCombatEnemyInfo(3, 0f, -8f)
            };

            bool found = SectorTargetSelector.TrySelectNearestInSector(enemies, 0f, 0f, 0, 3, out int selectedId);

            Assert.That(found, Is.True);
            Assert.That(selectedId, Is.EqualTo(2));
        }

        [Test]
        public void SectorTargetSelector_AcceptsNegativeIds()
        {
            var enemies = new[]
            {
                new SectorCombatEnemyInfo(-42, 0f, 5f)
            };

            bool found = SectorTargetSelector.TrySelectNearestInSector(enemies, 0f, 0f, 0, 3, out int selectedId);

            Assert.That(found, Is.True);
            Assert.That(selectedId, Is.EqualTo(-42));
        }

        [Test]
        public void SectorFocusSystem_ReturnsAttackWhenReadyAndTargetExists()
        {
            SectorFocusSystem system = new SectorFocusSystem(3, 0f, 2, 0.5f);
            system.SetCombatEnabled(true);
            var enemies = new[] { new SectorCombatEnemyInfo(10, 0f, 5f) };

            SectorAttackCommand attack = system.Tick(0f, 0f, 0f, enemies, combatActive: true);

            Assert.That(attack.HasAttack, Is.True);
            Assert.That(attack.TargetId, Is.EqualTo(10));
            Assert.That(attack.Damage, Is.EqualTo(2));
        }

        [Test]
        public void SectorFocusSystem_AttacksWhenCombatActiveEvenIfEnableFlagWasMissed()
        {
            SectorFocusSystem system = new SectorFocusSystem(3, 0f, 2, 0.5f);
            var enemies = new[] { new SectorCombatEnemyInfo(10, 0f, 5f) };

            SectorAttackCommand attack = system.Tick(0f, 0f, 0f, enemies, combatActive: true);

            Assert.That(attack.HasAttack, Is.True);
        }

        [Test]
        public void CombatWaveManagerLogic_CyclesSpawnPointsAtInterval()
        {
            CombatWaveManagerLogic logic = new CombatWaveManagerLogic(1f);

            Assert.That(logic.Tick(0.5f, 3, out int spawnIndex), Is.False);
            Assert.That(spawnIndex, Is.EqualTo(-1));

            Assert.That(logic.Tick(0.5f, 3, out spawnIndex), Is.True);
            Assert.That(spawnIndex, Is.EqualTo(0));

            Assert.That(logic.Tick(1f, 3, out spawnIndex), Is.True);
            Assert.That(spawnIndex, Is.EqualTo(1));

            Assert.That(logic.Tick(1f, 3, out spawnIndex), Is.True);
            Assert.That(spawnIndex, Is.EqualTo(2));
        }

        [Test]
        public void CombatEnemyLogic_MovesTowardTarget_AndStopsOnTarget()
        {
            CombatEnemyLogic logic = new CombatEnemyLogic(new CombatPoint(0f, 0f), 2f);

            logic.SetTarget(new CombatPoint(10f, 0f));
            logic.Tick(1f);

            Assert.That(logic.Position.X, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(logic.Position.Z, Is.EqualTo(0f).Within(0.0001f));

            logic.Tick(5f);

            Assert.That(logic.Position.X, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(logic.Position.Z, Is.EqualTo(0f).Within(0.0001f));
        }

        private sealed class TestState : IGameState
        {
            public TestState(GameStateId id)
            {
                Id = id;
            }

            public GameStateId Id { get; }
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public int TickCount { get; private set; }
            public float LastDeltaTime { get; private set; }

            public void Enter()
            {
                EnterCount += 1;
            }

            public void Tick(float deltaTime)
            {
                TickCount += 1;
                LastDeltaTime = deltaTime;
            }

            public void Exit()
            {
                ExitCount += 1;
            }
        }
    }
}