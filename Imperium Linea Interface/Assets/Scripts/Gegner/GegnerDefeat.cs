using Abstract;
using Timer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gegner
{
    // Singleton MonoBehaviour that handles the player's defeat condition.
    // Attach this to the player or a trigger zone that should end the game on enemy contact.
    public class GegnerDefeat : MonoSingleton<GegnerDefeat>
    {
        // Called automatically by Unity when another collider enters this object's trigger zone
        private void OnTriggerEnter(Collider other)
        {
            GameLogger.Instance.Log($"{gameObject.name} collided with {other.gameObject.name}");

            // Only trigger defeat if the colliding object is tagged as an enemy
            if (other.CompareTag("Enemy"))
            {
                Debug.Log("Enemy collided...");

                // Freeze the game by stopping time before loading the death screen
                Time.timeScale = 0f;

                TimerManager.Instance.GameOver();

                // Load the death/game-over menu scene
                SceneManager.LoadScene("DeathMenu");
            }
        }
    }
}