using Menues.SettingsMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Menues.MainMenu
{
    /// <summary>
    ///     Drives the main menu UI. Handles game mode selection, scene loading,
    ///     and application exit via buttons wired up in the Unity Inspector.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        /// <summary>Dropdown used to select the game mode before starting.</summary>
        public TMP_Dropdown dropdown;

        /// <summary>Label used to display validation messages (e.g. "please select a mode").</summary>
        public TMP_Text label;

        /// <summary>
        ///     The currently selected game mode value, mirroring the dropdown index.
        ///     0 = no selection, 1 = valid game mode that allows play.
        /// </summary>
        private int _gameMode;

        /// <summary>
        ///     Attempts to start the game. Loads the game scene if a valid game mode
        ///     has been selected, otherwise prompts the user to make a selection.
        ///     Wired to the Play button in the Inspector.
        /// </summary>
        public void Play()
        {
            AudioManager.Instance.PlayButton();
            if (_gameMode == 1)
                SceneManager.LoadScene("Tutorial");
            else if (_gameMode == 2)
                SceneManager.LoadScene("GameEnvironment");
            else
                label.text = "Please select a Game Mode";
        }

        /// <summary>
        ///     Reads the current dropdown value and stores it as the selected game mode.
        ///     Clears any validation message that was previously shown.
        ///     Wired to the dropdown's OnValueChanged event in the Inspector.
        /// </summary>
        public void GameMode()
        {
            AudioManager.Instance.PlayButton();

            _gameMode = dropdown.value;
            label.text = "";
        }

        /// <summary>
        ///     Navigates to the Settings scene.
        ///     Wired to the Settings button in the Inspector.
        /// </summary>
        public void Settings()
        {
            AudioManager.Instance.PlayButton();
            SettingsManager.Instance.OpenSettings("MainMenu");
        }

        /// <summary>
        ///     Exits the application. In the Unity Editor this stops Play Mode;
        ///     in a built player it calls <see cref="Application.Quit" />.
        ///     Wired to the Quit button in the Inspector.
        /// </summary>
        public void Quit()
        {
            AudioManager.Instance.PlayButton();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}