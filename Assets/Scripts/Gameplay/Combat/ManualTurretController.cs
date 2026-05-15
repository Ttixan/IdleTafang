using IdleTafang.Gameplay.Builds;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class ManualTurretController : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private LayerMask hitMask = ~0;

        private float cooldownTimer;
        private BuildPrototype prototype;
        private bool allowFire = true;
        private bool allowEnemyDamage;

        public void Bind(BuildPrototype buildPrototype)
        {
            prototype = buildPrototype;
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

            if (!Input.GetMouseButton(0))
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

