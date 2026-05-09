using UnityEngine;
using UnityEngine.SceneManagement;

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
            SceneManager.LoadScene(runSceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
