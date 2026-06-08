using Abstract;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menues.SettingsMenu
{
    /// <summary>
    ///     Utility class to help with some annoying stuff I didn't want to deal with in the actual settings manager.
    /// </summary>
    public class SettingsManager : MonoSingleton<SettingsManager>
    {
        // Internal variables
        [Header("Scene Settings")] private static string _returnSceneName = "MainMenu";

        /// <summary>
        ///     Sets the name of the scene to be shown after exiting the settings menu. <br />
        ///     Setting this to "MainMenu" will result in the main menu scene being loaded alone,
        ///     while every other scene will instead put it atop the other scenes, making it possible
        ///     to preserve older scenes' states (such as the game scene when playing)
        /// </summary>
        /// <param name="sceneName">Name of the scene to be returned to on exit</param>
        public static void SetReturnScene(string sceneName)
        {
            _returnSceneName = sceneName;
        }

        /// <summary>
        ///     Returns to the previous scene by utilizing the _returnSceneName variable
        /// </summary>
        public static void Back()
        {
            // Check if main menu
            if (_returnSceneName == "MainMenu")
            {
                // Unload all scenes, except for main menu
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                // Load scene additively, unload settings menu scene later
                SceneManager.LoadScene(_returnSceneName, LoadSceneMode.Additive);
                SceneManager.UnloadSceneAsync("SettingsMenu");
            }
        }
        
        /// <summary>
        /// Opens the settings menu and sets params to return to the previous scene
        /// </summary>
        /// <param name="sceneName">Scene to return to after exiting the settings menu</param>
        public void OpenSettings(string sceneName)
        {
            SetReturnScene(sceneName);
            SceneManager.LoadScene("SettingsMenu", LoadSceneMode.Additive);
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}