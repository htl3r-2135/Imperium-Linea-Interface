using Abstract;

namespace Gegner
{
    public class GegnerStatsSingleton : Singleton<GegnerStatsSingleton>
    {
        private int _enemiesDefeated;
        private int _enemiesSpawned;

        public static void Reset()
        {
            Instance._enemiesDefeated = 0;
            Instance._enemiesSpawned = 0;
        }

        public static void Increment()
        {
            Instance._enemiesSpawned++;
        }

        public static void IncrementDefeated()
        {
            Instance._enemiesDefeated++;
        }

        public static int GetSpawnedEnemies()
        {
            return Instance._enemiesSpawned;
        }

        public static int GetDefeatedEnemies()
        {
            return Instance._enemiesDefeated;
        }
    }
}