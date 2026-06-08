using Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menues.DeathMenu
{
    /// <summary>
    ///     Callbacks for the Death Menu Scene
    /// </summary>
    public class Callbacks : MonoBehaviour
    {
        /// <summary>
        ///     Quits and heads back to the main menu
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
        ///     Reloads the Game Environment and restarts the game
        /// </summary>
        public void Restart()
        {
            AudioManager.Instance.PlayButton();
            
            //Load Game Environment Scene
            SceneManager.LoadScene(TutorialSingleton.Instance.IsTutorial() ? "Tutorial" : "GameEnvironment");

            //Resume Game
            Time.timeScale = 1f;
        }
    }
}