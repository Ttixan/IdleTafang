using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class SectorFocusWorldView : MonoBehaviour
    {
        [SerializeField] private CombatArena arena;
        [SerializeField] private float innerRadius = 1.5f;
        [SerializeField] private float outerRadius = 14f;
        [SerializeField] private float yOffset = 0.08f;
        [SerializeField] private Color inactiveColor = new Color(0.35f, 0.45f, 0.55f, 0.18f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.95f, 0.45f, 0.42f);
        [SerializeField] private Color armingColor = new Color(1f, 0.75f, 0.15f, 0.38f);

        private Renderer[] sectorRenderers;
        private Material[] sectorMaterials;
        private bool visible;

        public void SetVisible(bool isVisible)
        {
            visible = isVisible;
            if (sectorRenderers == null)
            {
                return;
            }

            for (int i = 0; i < sectorRenderers.Length; i++)
            {
                if (sectorRenderers[i] != null)
                {
                    sectorRenderers[i].enabled = isVisible;
                }
            }
        }

        public void Render(SectorFocusSnapshot snapshot)
        {
            if (!visible || sectorMaterials == null)
            {
                return;
            }

            for (int i = 0; i < sectorMaterials.Length; i++)
            {
                if (sectorMaterials[i] == null)
                {
                    continue;
                }

                Color color = inactiveColor;
                if (i == snapshot.FocusedSector)
                {
                    color = snapshot.IsReady ? activeColor : armingColor;
                }

                sectorMaterials[i].color = color;
            }
        }

        private void Awake()
        {
            if (arena == null)
            {
                arena = FindObjectOfType<CombatArena>();
            }

            BuildSectorMeshes();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (!visible)
            {
                return;
            }

            Transform anchor = GetAnchor();
            if (anchor != null)
            {
                transform.position = anchor.position + Vector3.up * yOffset;
                transform.rotation = Quaternion.identity;
            }
        }

        private Transform GetAnchor()
        {
            if (arena != null && arena.CenterPoint != null)
            {
                return arena.CenterPoint;
            }

            return transform.parent;
        }

        private void BuildSectorMeshes()
        {
            int count = 3;
            sectorRenderers = new Renderer[count];
            sectorMaterials = new Material[count];
            float sectorWidth = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float start = i * sectorWidth;
                float end = (i + 1) * sectorWidth;
                GameObject wedge = new GameObject($"SectorWedge_{i + 1}");
                wedge.transform.SetParent(transform, false);

                var filter = wedge.AddComponent<MeshFilter>();
                filter.sharedMesh = SectorWedgeMesh.Create(innerRadius, outerRadius, start, end);

                var renderer = wedge.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Material material = CreateSectorMaterial(inactiveColor);
                renderer.sharedMaterial = material;

                sectorRenderers[i] = renderer;
                sectorMaterials[i] = material;
            }
        }

        private static Material CreateSectorMaterial(Color color)
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
            material.color = color;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            material.renderQueue = 3000;
            return material;
        }

        private void OnDestroy()
        {
            if (sectorMaterials == null)
            {
                return;
            }

            for (int i = 0; i < sectorMaterials.Length; i++)
            {
                if (sectorMaterials[i] != null)
                {
                    Destroy(sectorMaterials[i]);
                }
            }
        }
    }
}
