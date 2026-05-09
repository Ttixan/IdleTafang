using System;

namespace IdleTafang.Core
{
    public sealed class GameTimer
    {
        public float Elapsed { get; private set; }

        public void Reset()
        {
            Elapsed = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            Elapsed += deltaTime;
        }
    }
}
