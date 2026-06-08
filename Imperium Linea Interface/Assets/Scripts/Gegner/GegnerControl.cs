using Tutorial;
using UnityEngine;

namespace Gegner
{
    public class GegnerControl : MonoBehaviour
    {
        // The target the enemy will move toward (found at runtime via tag)
        private GameObject _target;

        // Movement speed, set externally by GegnerSpawn after instantiation
        private float _origSpeed;
        public float speed = 1;

        // Controls whether the enemy is actively moving toward its target
        private bool _moving = true;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Find the designated target object in the scene using its tag
            _target = GameObject.FindGameObjectWithTag("EnemyTarget");
        }

        // Update is called once per frame
        void Update()
        {
            if (TutorialSingleton.Instance.GetMoveBlock())
            {
                _origSpeed = _origSpeed != 0 ? _origSpeed : speed;
                speed = 0;
            }
            else
            {
                speed = _origSpeed != 0 ? _origSpeed : speed;
            }
            
            if (_moving)
            {
                // Move the enemy one step closer to the target each frame,
                // scaled by delta time to keep movement framerate-independent
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    _target.transform.position,
                    Time.deltaTime * speed
                );
            }
            else
            {
                // Enemy is blocked (e.g. by a door) — log and wait
                GameLogger.Instance.Log("Enemy blocked, not moving");
            }
        }

        // Called automatically by Unity when another collider enters this object's trigger zone
        private void OnTriggerEnter(Collider other)
        {
            GameLogger.Instance.Log($"{gameObject.name} collided door with {other.gameObject.name}");

            // If the enemy reaches the door, destroy it (counts as passing through / being stopped)
            if (other.CompareTag("Door"))
            {
                GegnerStatsSingleton.IncrementDefeated();
                Destroy(gameObject);
            }
        }
    }
}