using UnityEngine;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatSpawnPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        public CombatPoint LogicPosition
        {
            get { return new CombatPoint(transform.position.x, transform.position.z); }
        }
    }
}
