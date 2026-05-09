using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatArena : MonoBehaviour
    {
        [SerializeField] private Transform centerPoint;

        public Transform CenterPoint => centerPoint;
    }
}
