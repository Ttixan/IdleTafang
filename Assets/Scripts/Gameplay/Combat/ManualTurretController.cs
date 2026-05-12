using IdleTafang.Gameplay.Builds;
using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class ManualTurretController : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float fireCooldownSeconds = 0.25f;
        [SerializeField] private int baseDamage = 2;
        [SerializeField] private int damagePerLevel = 1;

        private float cooldownTimer;
        private BuildPrototype prototype;

        public void Bind(BuildPrototype buildPrototype)
        {
            prototype = buildPrototype;
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
            if (enemy == null)
            {
                return;
            }

            int level = prototype != null ? prototype.Level : 1;
            int damage = Mathf.Max(1, baseDamage + Mathf.Max(0, level - 1) * damagePerLevel);
            enemy.TakeDamage(damage);
            cooldownTimer = Mathf.Max(0.05f, fireCooldownSeconds);
        }
    }
}

