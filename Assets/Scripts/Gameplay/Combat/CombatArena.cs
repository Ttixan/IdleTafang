using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatArena : MonoBehaviour
    {
        [SerializeField] private Transform centerPoint;

        public Transform CenterPoint => centerPoint;

        public CombatArenaLogic Logic => new CombatArenaLogic(new CombatPoint(centerPoint != null ? centerPoint.position.x : transform.position.x, centerPoint != null ? centerPoint.position.z : transform.position.z));
    }
}
