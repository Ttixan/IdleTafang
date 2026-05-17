using System;
using IdleTafang.Gameplay;
using IdleTafang.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleTafang.UI;

namespace IdleTafang.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [SerializeField] private string bootSceneName = "Boot";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string runSceneName = "Run";
        [SerializeField] private RunConfig runConfig;

        private GameLoop gameLoop;
        private SceneFlowState bootState;
        private SceneFlowState mainMenuState;
        private SceneFlowState runState;
        private GameInput gameInput;
        private GameTimer gameTimer;
        private RunSession runSession;

        public RunSession RunSession => runSession;

        public RunConfig RunBalanceConfig => runConfig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            gameLoop = new GameLoop();
            gameInput = gameObject.AddComponent<GameInput>();
            gameTimer = new GameTimer();
            runSession = new RunSession();
            bootState = new SceneFlowState(GameStateId.Boot, bootSceneName, false);
            mainMenuState = new SceneFlowState(GameStateId.MainMenu, mainMenuSceneName, true);
            runState = new SceneFlowState(GameStateId.Run, runSceneName, true, OnRunSceneEntered);

            gameLoop.RegisterState(bootState);
            gameLoop.RegisterState(mainMenuState);
            gameLoop.RegisterState(runState);
            gameLoop.RegisterState(new EmptyState(GameStateId.Loading));
            gameLoop.RegisterState(new EmptyState(GameStateId.Paused));
            gameLoop.RegisterState(new EmptyState(GameStateId.Exit));

            gameLoop.ChangeState(GameStateId.Boot);
        }

        private void Start()
        {
            gameLoop.ChangeState(GameStateId.MainMenu);
        }

        private void Update()
        {
            gameTimer.Tick(Time.deltaTime);
            gameLoop?.Tick(Time.deltaTime);

            if (gameInput != null && gameInput.ConfirmPressed && gameLoop.CurrentStateId == GameStateId.MainMenu)
            {
                LoadRunScene();
            }

            if (gameInput != null && gameInput.CancelPressed && gameLoop.CurrentStateId == GameStateId.Run)
            {
                runSession.FailRun();
                LoadMainMenu();
            }
        }

        public void LoadMainMenu()
        {
            gameLoop.ChangeState(GameStateId.MainMenu);
        }

        public void LoadRunScene()
        {
            gameLoop.ChangeState(GameStateId.Run);
        }

        private void OnRunSceneEntered()
        {
            if (runSession != null)
            {
                int mw = runConfig != null ? runConfig.MaxWaves : 5;
                runSession.ConfigureMaxWaves(mw);
                runSession.Reset();
            }
        }

        private sealed class SceneFlowState : IGameState
        {
            private readonly string sceneName;
            private readonly bool loadSceneOnEnter;
            private readonly Action onEnter;

            public SceneFlowState(GameStateId id, string sceneName, bool loadSceneOnEnter, Action onEnter = null)
            {
                Id = id;
                this.sceneName = sceneName;
                this.loadSceneOnEnter = loadSceneOnEnter;
                this.onEnter = onEnter;
            }

            public GameStateId Id { get; }

            public void Enter()
            {
                onEnter?.Invoke();

                if (loadSceneOnEnter && !string.IsNullOrWhiteSpace(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                }
            }

            public void Tick(float deltaTime)
            {
            }

            public void Exit()
            {
            }
        }

        private sealed class EmptyState : IGameState
        {
            public EmptyState(GameStateId id)
            {
                Id = id;
            }

            public GameStateId Id { get; }

            public void Enter()
            {
            }

            public void Tick(float deltaTime)
            {
            }

            public void Exit()
            {
            }
        }
    }
}
