using System;

namespace IdleTafang.Gameplay.Combat
{
    public sealed class CombatEnemyLogic
    {
        public CombatPoint Position { get; private set; }
        public CombatPoint Target { get; private set; }
        public float MoveSpeed { get; }

        public CombatEnemyLogic(CombatPoint position, float moveSpeed)
        {
            Position = position;
            Target = position;
            MoveSpeed = moveSpeed < 0f ? 0f : moveSpeed;
        }

        public void SetTarget(CombatPoint target)
        {
            Target = target;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float dx = Target.X - Position.X;
            float dz = Target.Z - Position.Z;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);

            if (distance <= 0f)
            {
                return;
            }

            float step = MoveSpeed * deltaTime;
            if (step >= distance)
            {
                Position = Target;
                return;
            }

            float ratio = step / distance;
            Position = new CombatPoint(Position.X + dx * ratio, Position.Z + dz * ratio);
        }
    }
}