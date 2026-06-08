using System.Collections;
using Abstract.Console;
using Console;
using Tutorial;
using UnityEngine;
using Random = System.Random;

namespace Gegner
{
    public class GegnerSpawn : MonoBehaviour
    {
        // Interval in seconds between each enemy spawn; decreases over time to ramp up difficulty
        public float timeToSpawn = 10;

        // Movement speed assigned to each spawned enemy; increases over time
        public float speed = 1;

        // The enemy prefab to instantiate when spawning
        public GameObject enemyPrefab;

        // Array of possible spawn point GameObjects to choose from
        public GameObject[] spawnLocations;

        // Reference to the command handler used to check if the game session has started
        private readonly ACommandHandler<SimulatedHandler> _handler = ACommandHandler<SimulatedHandler>.Instance;

        // Parent transform used to keep spawned enemies organized in the hierarchy
        private Transform _enemyParent;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Create an empty GameObject to serve as the parent for all spawned enemies
            var parentObject = new GameObject("Enemies");
            _enemyParent = parentObject.transform;

            GegnerStatsSingleton.Reset();
            
            // Begin the repeating spawn loop
            StartCoroutine(SpawnControl());
        }

        // Update is called once per frame
        private void Update()
        {
            // Only adjust difficulty if the game session has started
            if (_handler.StartedUp && !TutorialSingleton.Instance.GetSpawnBlock())
            {
                // Gradually reduce spawn interval (floored at 1s) to increase enemy frequency
                if (timeToSpawn > 1) timeToSpawn *= 0.99995f;

                // Gradually increase enemy speed (capped at 10) to increase difficulty
                if (speed < 10) speed *= 1.00005f;
            }
        }

        // Coroutine that waits for the current spawn interval, then triggers a spawn — loops forever
        private IEnumerator SpawnControl()
        {
            while (true)
            {
                Spawn();
                yield return new WaitForSeconds(timeToSpawn);
            }
        }

        // Instantiates an enemy at a randomly selected spawn location if the game has started
        private void Spawn()
        {
            if (_handler.StartedUp && !TutorialSingleton.Instance.GetSpawnBlock())
            {
                if (TutorialSingleton.Instance.IsTutorial())
                {
                    TutorialSingleton.Instance.SetSpawnBlock(true);
                }
                else
                {
                    TutorialSingleton.Instance.SetSpawnBlock(false);
                }
                var random = new Random();

                // Pick a random index into the spawnLocations array
                var ind = random.Next(spawnLocations.Length);

                GameLogger.Instance.LogDebug("Spawning enemy at: " + spawnLocations[ind].transform.position);

                // Instantiate the enemy prefab at the chosen spawn point, under the shared parent
                var spawned = Instantiate(
                    enemyPrefab,
                    spawnLocations[ind].transform.position,
                    spawnLocations[ind].transform.rotation,
                    _enemyParent
                );

                // Flip the index to the opposite spawn point (assumes exactly 2 spawn locations)
                // This is used to determine which direction the enemy should face
                if (ind == 0)
                    ind = 1;
                else
                    ind = 0;

                Debug.Log(spawned.transform.position.ToString());

                // Rotate the enemy 0° or 180° on the Y-axis depending on which end it spawned at
                spawned.transform.rotation = Quaternion.Euler(0, (ind * 180) + 90, 0);

                // Pass the current speed value to the enemy's controller
                spawned.GetComponent<GegnerControl>().speed = speed;

                GegnerStatsSingleton.Increment();
            }
        }
    }
}