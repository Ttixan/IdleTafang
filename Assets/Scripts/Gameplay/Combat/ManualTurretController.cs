using IdleTafang.Gameplay;
using IdleTafang.Gameplay.Builds;
using IdleTafang.Gameplay.Resources;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class ManualTurretController : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private int specialAttackBaseEnergyCost = 5;
        [SerializeField] private float specialDamageMultiplier = 2f;

        private float cooldownTimer;
        private BuildPrototype prototype;
        private ResourceWallet wallet;
        private RunBuffState buffState;
        private bool allowFire = true;
        private bool allowEnemyDamage;

        public void Bind(BuildPrototype buildPrototype, ResourceWallet walletRef = null, RunBuffState buffStateRef = null)
        {
            prototype = buildPrototype;
            wallet = walletRef;
            buffState = buffStateRef;
        }

        public void SetInteractionMode(bool fireEnabled, bool enemyDamageEnabled)
        {
            allowFire = fireEnabled;
            allowEnemyDamage = enemyDamageEnabled;
        }

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!allowFire)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f)
            {
                return;
            }

            if (aimCamera == null)
            {
                return;
            }

            Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, hitMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            CombatEnemy enemy = hit.collider != null ? hit.collider.GetComponentInParent<CombatEnemy>() : null;

            if (Input.GetMouseButtonDown(1))
            {
                TrySpecialShot(enemy);
                return;
            }

            if (Input.GetMouseButton(0))
            {
                TryPrimaryShot(enemy);
            }
        }

        private void TrySpecialShot(CombatEnemy enemy)
        {
            if (!allowEnemyDamage || enemy == null)
            {
                return;
            }

            int cost = buffState != null
                ? buffState.DiscountSpecialEnergyCost(specialAttackBaseEnergyCost)
                : specialAttackBaseEnergyCost;

            if (wallet == null || (cost > 0 && !wallet.TrySpendEnergy(cost)))
            {
                return;
            }

            int damage = prototype != null ? prototype.GetTurretDamage() : 1;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Max(1f, specialDamageMultiplier)));
            enemy.TakeDamage(damage);

            float cd = prototype != null ? prototype.GetTurretFireCooldownSeconds() : 0.25f;
            cooldownTimer = Mathf.Max(0.05f, cd);
        }

        private void TryPrimaryShot(CombatEnemy enemy)
        {
            if (enemy == null || !allowEnemyDamage)
            {
                return;
            }

            int damage = prototype != null ? prototype.GetTurretDamage() : 1;
            enemy.TakeDamage(damage);
            float cd = prototype != null ? prototype.GetTurretFireCooldownSeconds() : 0.25f;
            cooldownTimer = Mathf.Max(0.05f, cd);
        }
    }
}
