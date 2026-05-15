using System;
using System.Collections.Generic;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class SectorFocusSystem
    {
        private readonly int sectorCount;
        private readonly float switchWarmupSeconds;
        private readonly int fallbackDamage;
        private readonly float fallbackCooldownSeconds;

        private SectorFocusLogic focusLogic;
        private float fireCooldownTimer;
        private int turretDamage;
        private float turretCooldownSeconds;
        private bool combatEnabled;
        private bool hasTargetInSector;
        private SectorFocusSnapshot snapshot;

        public SectorFocusSystem(
            int sectorCount = 3,
            float switchWarmupSeconds = 0.5f,
            int fallbackDamage = 1,
            float fallbackCooldownSeconds = 0.5f)
        {
            this.sectorCount = sectorCount < 1 ? 1 : sectorCount;
            this.switchWarmupSeconds = switchWarmupSeconds < 0f ? 0f : switchWarmupSeconds;
            this.fallbackDamage = fallbackDamage < 1 ? 1 : fallbackDamage;
            this.fallbackCooldownSeconds = fallbackCooldownSeconds < 0.05f ? 0.05f : fallbackCooldownSeconds;
            turretDamage = this.fallbackDamage;
            turretCooldownSeconds = this.fallbackCooldownSeconds;
            focusLogic = new SectorFocusLogic(this.sectorCount, this.switchWarmupSeconds);
            snapshot = BuildSnapshot();
        }

        public event Action<SectorFocusSnapshot> SnapshotChanged;

        public SectorFocusSnapshot Snapshot => snapshot;

        public void SetTurretStats(int damage, float cooldownSeconds)
        {
            turretDamage = damage < 1 ? 1 : damage;
            turretCooldownSeconds = cooldownSeconds < 0.05f ? 0.05f : cooldownSeconds;
        }

        public void SetCombatEnabled(bool enabled)
        {
            combatEnabled = enabled;
            if (!combatEnabled)
            {
                ResetFocus();
                return;
            }

            PublishSnapshotIfChanged();
        }

        public void ResetFocus()
        {
            focusLogic.Reset();
            fireCooldownTimer = 0f;
            hasTargetInSector = false;
            PublishSnapshotIfChanged();
        }

        public void FocusPrevious()
        {
            if (!combatEnabled)
            {
                return;
            }

            TrySwitchSector(focusLogic.FocusPrevious);
        }

        public void FocusNext()
        {
            if (!combatEnabled)
            {
                return;
            }

            TrySwitchSector(focusLogic.FocusNext);
        }

        public SectorAttackCommand Tick(
            float deltaTime,
            float baseX,
            float baseZ,
            IReadOnlyList<SectorCombatEnemyInfo> enemies,
            bool combatActive,
            bool focusPrevious = false,
            bool focusNext = false)
        {
            if (!combatActive)
            {
                hasTargetInSector = false;
                PublishSnapshotIfChanged();
                return SectorAttackCommand.None;
            }

            if (!combatEnabled)
            {
                combatEnabled = true;
            }

            if (focusPrevious)
            {
                TrySwitchSector(focusLogic.FocusPrevious);
            }

            if (focusNext)
            {
                TrySwitchSector(focusLogic.FocusNext);
            }

            focusLogic.Tick(deltaTime);
            RefreshTargetState(baseX, baseZ, enemies);

            fireCooldownTimer -= deltaTime;
            if (!focusLogic.IsReady || fireCooldownTimer > 0f || !hasTargetInSector)
            {
                return SectorAttackCommand.None;
            }

            if (!SectorTargetSelector.TrySelectNearestInSector(
                    enemies,
                    baseX,
                    baseZ,
                    focusLogic.FocusedSector,
                    focusLogic.SectorCount,
                    out int targetId))
            {
                return SectorAttackCommand.None;
            }

            fireCooldownTimer = turretCooldownSeconds;
            return new SectorAttackCommand(targetId, turretDamage);
        }

        private void TrySwitchSector(System.Action switchSector)
        {
            int previousSector = focusLogic.FocusedSector;
            switchSector();
            if (focusLogic.FocusedSector == previousSector)
            {
                return;
            }

            fireCooldownTimer = 0f;
            PublishSnapshotIfChanged();
        }

        private void RefreshTargetState(float baseX, float baseZ, IReadOnlyList<SectorCombatEnemyInfo> enemies)
        {
            hasTargetInSector = SectorTargetSelector.TrySelectNearestInSector(
                enemies,
                baseX,
                baseZ,
                focusLogic.FocusedSector,
                focusLogic.SectorCount,
                out _);

            PublishSnapshotIfChanged();
        }

        private SectorFocusSnapshot BuildSnapshot()
        {
            return new SectorFocusSnapshot(
                focusLogic.FocusedSector,
                focusLogic.SectorCount,
                focusLogic.IsReady,
                focusLogic.WarmupRemaining,
                hasTargetInSector);
        }

        private void PublishSnapshotIfChanged()
        {
            SectorFocusSnapshot next = BuildSnapshot();
            if (next.FocusedSector == snapshot.FocusedSector
                && next.SectorCount == snapshot.SectorCount
                && next.IsReady == snapshot.IsReady
                && Math.Abs(next.WarmupRemaining - snapshot.WarmupRemaining) < 0.0001f
                && next.HasTarget == snapshot.HasTarget)
            {
                return;
            }

            snapshot = next;
            SnapshotChanged?.Invoke(snapshot);
        }
    }
}
