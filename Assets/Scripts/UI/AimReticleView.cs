using UnityEngine;
using UnityEngine.UI;

namespace IdleTafang.UI
{
    public sealed class AimReticleView : MonoBehaviour
    {
        [SerializeField] private RectTransform reticle;
        [SerializeField] private Image reticleImage;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color targetColor = new Color(1f, 0.25f, 0.25f, 0.95f);
        [SerializeField] private Camera aimCamera;
        [SerializeField] private LayerMask enemyMask;

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (reticle == null)
            {
                reticle = transform as RectTransform;
            }

            if (reticleImage == null)
            {
                reticleImage = GetComponent<Image>();
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            // UI准星（推荐挂在 Screen Space - Overlay 的 Canvas 下）
            if (reticle != null)
            {
                reticle.position = Input.mousePosition;
            }

            if (reticleImage == null || aimCamera == null)
            {
                return;
            }

            bool aimingEnemy = false;
            Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, enemyMask, QueryTriggerInteraction.Ignore))
            {
                aimingEnemy = true;
            }

            reticleImage.color = aimingEnemy ? targetColor : idleColor;
        }
    }
}

