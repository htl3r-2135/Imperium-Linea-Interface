using Menues.SettingsMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menues.PauseMenu
{
    /// <summary>
    ///     Drives the pause menu UI. Communicates back to <see cref="PauseManager" />
    ///     via a static event so the menu scene does not need a direct reference to
    ///     the manager in the main scene.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        /// <summary>
        ///     Wired to the Resume button in the Inspector.
        /// </summary>
        public void Resume()
        {
            AudioManager.Instance.PlayButton();
            PauseManager.Instance.ResumeGame();
        }

        /// <summary>
        ///     Exits the application. In the Unity Editor this stops Play Mode;
        ///     in a built player it calls <see cref="Application.Quit" />.
        ///     Wired to the Quit button in the Inspector.
        /// </summary>
        public void Quit()
        {
            AudioManager.Instance.PlayButton();
            //Load Main Menu Scene
            SceneManager.LoadScene("MainMenu");

            //Resume Game
            Time.timeScale = 1f;
        }

        /// <summary>
        ///     Opens the in-game settings menu
        /// </summary>
        public void Settings()
        {
            AudioManager.Instance.PlayButton();
            SettingsManager.Instance.OpenSettings("PauseMenu");
        }
    }
}