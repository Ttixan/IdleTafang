using UnityEngine;
using UnityEngine.Rendering;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class SectorTurretProjectile : MonoBehaviour
    {
        private CombatEnemy target;
        private int damage;
        private float speed;
        private float hitDistanceSq;

        public void Launch(CombatEnemy enemy, int projectileDamage, float travelSpeed, float hitDistance)
        {
            target = enemy;
            damage = Mathf.Max(0, projectileDamage);
            speed = Mathf.Max(0.5f, travelSpeed);
            hitDistanceSq = hitDistance * hitDistance;
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 destination = target.transform.position + Vector3.up * 0.6f;
            Vector3 delta = destination - transform.position;
            float distSq = delta.sqrMagnitude;
            float step = speed * Time.deltaTime;
            float stepSq = step * step;

            if (distSq <= Mathf.Max(hitDistanceSq, stepSq))
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            Vector3 dir = delta.normalized;
            transform.position += dir * step;

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        public static SectorTurretProjectile Spawn(
            Vector3 worldStart,
            CombatEnemy enemy,
            int projectileDamage,
            float travelSpeed,
            float scale,
            float hitDistance,
            Color tint)
        {
            if (enemy == null || projectileDamage <= 0)
            {
                return null;
            }

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SectorTurretProjectile";
            go.transform.position = worldStart;
            go.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                ApplyBulletMaterial(renderer, tint);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var projectile = go.AddComponent<SectorTurretProjectile>();
            projectile.Launch(enemy, projectileDamage, travelSpeed, hitDistance);
            return projectile;
        }

        private static void ApplyBulletMaterial(Renderer renderer, Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = tint;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            material.renderQueue = 3000;
            renderer.material = material;
        }

        private void OnDestroy()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                Destroy(r.material);
            }
        }
    }
}
