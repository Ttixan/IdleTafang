using System;
using System.Collections.Generic;

namespace IdleTafang.Core
{
    public sealed class GameLoop
    {
        private readonly Dictionary<GameStateId, IGameState> states = new Dictionary<GameStateId, IGameState>();
        private IGameState currentState;

        public GameStateId CurrentStateId => currentState?.Id ?? GameStateId.Boot;

        public void RegisterState(IGameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            states[state.Id] = state;
        }

        public void ChangeState(GameStateId nextStateId)
        {
            if (!states.TryGetValue(nextStateId, out IGameState nextState))
            {
                throw new InvalidOperationException($"State '{nextStateId}' is not registered.");
            }

            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        public void Tick(float deltaTime)
        {
            currentState?.Tick(deltaTime);
        }

        public GameStateId GetCurrentStateId()
        {
            return CurrentStateId;
        }
    }
}
