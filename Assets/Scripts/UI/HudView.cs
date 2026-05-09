using UnityEngine;

namespace IdleTafang.UI
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        public void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }
    }
}
