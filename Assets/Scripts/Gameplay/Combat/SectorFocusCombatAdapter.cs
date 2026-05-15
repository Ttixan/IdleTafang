using System;
using System.Collections.Generic;
using IdleTafang.Gameplay.Builds;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class SectorFocusCombatAdapter : MonoBehaviour
    {
        [SerializeField] private CombatWaveManager waveManager;
        [SerializeField] private int sectorCount = 3;
        [SerializeField] private float switchWarmupSeconds = 0.5f;
        [SerializeField] private int baseDamage = 1;
        [SerializeField] private float baseFireCooldownSeconds = 0.5f;

        [SerializeField] private bool debugLog = false;
        [SerializeField] private float debugLogInterval = 1f;

        [Header("Sector projectile visual")]
        [SerializeField] private float projectileSpeed = 42f;
        [SerializeField] private float projectileScale = 0.35f;
        [SerializeField] private float projectileHitDistance = 0.65f;
        [SerializeField] private Color projectileColor = new Color(0.35f, 0.95f, 1f, 1f);

        private readonly List<CombatEnemy> enemyScratch = new List<CombatEnemy>();
        private readonly List<SectorCombatEnemyInfo> enemyInfoScratch = new List<SectorCombatEnemyInfo>();

        private SectorFocusSystem system;
        private BuildPrototype buildPrototype;
        private float debugLogTimer;
        private CombatArena cachedArena;
        private float sectorBuffDamageMultiplier = 1f;

        public event Action<SectorFocusSnapshot> SnapshotChanged;

        public SectorFocusSystem System => system;

        public SectorFocusSnapshot Snapshot => system != null ? system.Snapshot : SectorFocusSnapshot.Idle(sectorCount);

        public void Bind(CombatWaveManager manager, BuildPrototype prototype)
        {
            waveManager = manager;
            buildPrototype = prototype;
            ApplyTurretStats();
        }

        public void SetCombatEnabled(bool enabled)
        {
            system?.SetCombatEnabled(enabled);
        }

        public void ResetFocus()
        {
            system?.ResetFocus();
        }

        public void SetSectorBuffDamageMultiplier(float multiplier)
        {
            sectorBuffDamageMultiplier = Mathf.Max(0f, multiplier);
        }

        private void Awake()
        {
            ResolveWaveManager();

            SectorFocusCombatAdapter[] adapters = GetComponents<SectorFocusCombatAdapter>();
            if (adapters.Length > 1)
            {
                Debug.LogWarning(
                    $"Multiple SectorFocusCombatAdapter on '{name}'. Keep only one to avoid duplicate input.",
                    this);
            }

            system = new SectorFocusSystem(sectorCount, switchWarmupSeconds, baseDamage, baseFireCooldownSeconds);
            system.SnapshotChanged += OnSystemSnapshotChanged;
            OnSystemSnapshotChanged(system.Snapshot);
        }

        private void OnDestroy()
        {
            if (system != null)
            {
                system.SnapshotChanged -= OnSystemSnapshotChanged;
            }
        }

        private void Update()
        {
            ResolveWaveManager();
            if (system == null || waveManager == null || !waveManager.IsCombatActive)
            {
                DebugTickStatus("skip:noWaveOrInactive");
                return;
            }

            system.SetCombatEnabled(true);

            bool focusPrevious = Input.GetKeyDown(KeyCode.A);
            bool focusNext = Input.GetKeyDown(KeyCode.D);

            if (!TryResolveBasePosition(out Vector3 basePosition))
            {
                system.Tick(Time.deltaTime, 0f, 0f, enemyInfoScratch, combatActive: false, focusPrevious, focusNext);
                DebugTickStatus("skip:noBase");
                return;
            }

            BuildEnemyInfos();
            SectorAttackCommand attack = system.Tick(
                Time.deltaTime,
                basePosition.x,
                basePosition.z,
                enemyInfoScratch,
                combatActive: true,
                focusPrevious,
                focusNext);

            DebugTickStatus($"tick enemies={enemyInfoScratch.Count} base=({basePosition.x:0.0},{basePosition.z:0.0}) snap={Snapshot.FocusedSector}/{Snapshot.SectorCount} ready={Snapshot.IsReady} target={Snapshot.HasTarget} attack={attack.HasAttack}");

            if (!attack.HasAttack)
            {
                return;
            }

            CombatEnemy target = ResolveEnemy(attack.TargetId);

            if (target != null)
            {
                Vector3 spawn = GetProjectileSpawnWorldPosition();
                int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(attack.Damage * sectorBuffDamageMultiplier));
                SectorTurretProjectile.Spawn(
                    spawn,
                    target,
                    scaledDamage,
                    projectileSpeed,
                    projectileScale,
                    projectileHitDistance,
                    projectileColor);
            }
        }

        private void ResolveWaveManager()
        {
            if (waveManager == null)
            {
                waveManager = GetComponent<CombatWaveManager>();
            }

            if (waveManager == null)
            {
                waveManager = FindObjectOfType<CombatWaveManager>();
            }
        }

        private bool TryResolveBasePosition(out Vector3 basePosition)
        {
            if (waveManager != null && waveManager.TryGetBasePosition(out basePosition))
            {
                return true;
            }

            CombatArena arena = ResolveArena();
            if (arena != null && arena.CenterPoint != null)
            {
                basePosition = arena.CenterPoint.position;
                return true;
            }

            basePosition = default;
            return false;
        }

        private CombatArena ResolveArena()
        {
            if (cachedArena != null)
            {
                return cachedArena;
            }

            cachedArena = FindObjectOfType<CombatArena>();
            return cachedArena;
        }

        private Vector3 GetProjectileSpawnWorldPosition()
        {
            CombatArena arena = ResolveArena();
            if (arena != null)
            {
                return arena.GetProjectileSpawnWorldPosition();
            }

            return TryResolveBasePosition(out Vector3 p) ? p + Vector3.up * 1.5f : Vector3.zero;
        }

        private void BuildEnemyInfos()
        {
            enemyInfoScratch.Clear();
            waveManager.CopyActiveEnemies(enemyScratch);

            for (int i = 0; i < enemyScratch.Count; i++)
            {
                CombatEnemy enemy = enemyScratch[i];
                if (enemy == null)
                {
                    continue;
                }

                Vector3 position = enemy.transform.position;
                enemyInfoScratch.Add(new SectorCombatEnemyInfo(enemy.GetInstanceID(), position.x, position.z));
            }
        }

        private CombatEnemy ResolveEnemy(int targetId)
        {
            for (int i = 0; i < enemyScratch.Count; i++)
            {
                CombatEnemy enemy = enemyScratch[i];
                if (enemy != null && enemy.GetInstanceID() == targetId)
                {
                    return enemy;
                }
            }

            return null;
        }

        private void ApplyTurretStats()
        {
            if (system == null)
            {
                return;
            }

            int damage = buildPrototype != null ? buildPrototype.GetTurretDamage() : baseDamage;
            float cooldown = buildPrototype != null ? buildPrototype.GetTurretFireCooldownSeconds() : baseFireCooldownSeconds;
            system.SetTurretStats(damage, cooldown);
        }

        private void OnSystemSnapshotChanged(SectorFocusSnapshot snapshot)
        {
            SnapshotChanged?.Invoke(snapshot);
        }

        private void DebugTickStatus(string message)
        {
            if (!debugLog)
            {
                return;
            }

            debugLogTimer -= Time.deltaTime;
            if (debugLogTimer > 0f)
            {
                return;
            }

            debugLogTimer = Mathf.Max(0.1f, debugLogInterval);
            Debug.Log($"[SectorAdapter] {message}", this);
        }
    }
}
