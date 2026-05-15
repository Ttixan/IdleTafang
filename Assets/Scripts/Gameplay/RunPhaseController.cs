using System;

namespace IdleTafang.Gameplay
{
    public sealed class RunPhaseController
    {
        public event Action<RunPhase> PhaseChanged;

        public RunPhase CurrentPhase { get; private set; } = RunPhase.Preparation;

        public bool CanEarnEnergy => CurrentPhase == RunPhase.Preparation;

        public bool CanRunCombat => CurrentPhase == RunPhase.Combat;

        public bool IsFinished => CurrentPhase == RunPhase.Settlement;

        public void Reset()
        {
            SetPhase(RunPhase.Preparation);
        }

        public bool TryBeginCombat()
        {
            if (CurrentPhase != RunPhase.Preparation)
            {
                return false;
            }

            SetPhase(RunPhase.Combat);
            return true;
        }

        public void EnterSettlement()
        {
            if (CurrentPhase == RunPhase.Settlement)
            {
                return;
            }

            SetPhase(RunPhase.Settlement);
        }

        private void SetPhase(RunPhase next)
        {
            if (CurrentPhase == next)
            {
                return;
            }

            CurrentPhase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
