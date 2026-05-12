using UnityEngine;
using UnityEngine.SceneManagement;
using IdleTafang.Core;

namespace IdleTafang.UI
{
    public sealed class SceneMenuView : MonoBehaviour
    {
        [SerializeField] private string runSceneName = "Run";
        [SerializeField] private UnityEngine.UI.Text tipText;

        private void Awake()
        {
            if (tipText != null)
            {
                tipText.text = "Main Menu\nPress Enter to Start";
            }
        }

        public void StartGame()
        {
            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.LoadRunScene();
                return;
            }

            // Allows starting MainMenu scene directly without going through Boot.
            SceneManager.LoadScene(runSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
