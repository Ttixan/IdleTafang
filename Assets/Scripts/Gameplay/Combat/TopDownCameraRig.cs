using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class TopDownCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float height = 18f;
        [SerializeField] private float distance = 0f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + new Vector3(0f, height, -distance);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
