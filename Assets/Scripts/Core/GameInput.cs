using UnityEngine;

namespace IdleTafang.Core
{
    public sealed class GameInput : MonoBehaviour
    {
        public float Horizontal => Input.GetAxisRaw("Horizontal");
        public float Vertical => Input.GetAxisRaw("Vertical");
        public bool ConfirmPressed => Input.GetButtonDown("Submit");
        public bool CancelPressed => Input.GetButtonDown("Cancel");
    }
}
