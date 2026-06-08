using Abstract;
using Menues.SettingsMenu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Menues.PauseMenu
{
    /// <summary>
    ///     Singleton that owns the pause/resume lifecycle. Loads and unloads the
    ///     pause menu scene additively so the main scene stays in memory, and
    ///     optionally freezes game time while paused.
    /// </summary>
    public class PauseManager : MonoSingleton<PauseManager>
    {
        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>Name of the additive scene that contains the pause menu UI.</summary>
        [Header("Scene Settings")] private readonly string _pauseMenuSceneName = "PauseMenu";

        /// <summary>
        ///     When true, <see cref="Time.timeScale" /> is set to 0 on pause and
        ///     restored to 1 on resume, freezing all physics and animations.
        /// </summary>
        private readonly bool _pauseTimeOnOpen = true;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>True while the game is currently paused.</summary>
        public bool IsPaused { get; private set; }


        // ── Unity lifecycle ───────────────────────────────────────────────────


        /// <summary>
        ///     Polls the Escape key each frame and toggles pause state when pressed.
        /// </summary>
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) TogglePause();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        ///     Toggles between paused and resumed states. Delegates to
        ///     <see cref="PauseGame" /> or <see cref="ResumeGame" /> based on
        ///     the current <see cref="IsPaused" /> value.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }

        /// <summary>
        ///     Pauses the game: sets <see cref="IsPaused" />, optionally freezes time,
        ///     and loads the pause menu scene additively. No-ops if already paused.
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused || Time.timeScale == 0f) return;

            IsPaused = true;

            SceneManager.LoadSceneAsync(_pauseMenuSceneName, LoadSceneMode.Additive);
            
            if (_pauseTimeOnOpen)
                Time.timeScale = 0f;
        }

        /// <summary>
        ///     Resumes the game by unloading the pause menu scene asynchronously.
        ///     <see cref="IsPaused" /> is cleared and time is restored only after the
        ///     scene finishes unloading, preventing a one-frame window where the game
        ///     runs while the menu is still visible. No-ops if not currently paused.
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused) return;

            // Defer state changes until the unload is complete to avoid a flash
            // where gameplay resumes while the pause UI is still on screen.
            SceneManager.UnloadSceneAsync(_pauseMenuSceneName)!.completed += _ =>
            {
                IsPaused = false;

                if (_pauseTimeOnOpen)
                    Time.timeScale = 1f;
            };
        }
    }
}