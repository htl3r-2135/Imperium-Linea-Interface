using UnityEngine;

namespace Abstract
{
    /// <summary>
    ///     Thread-safe (single-threaded Unity context) lazy singleton for plain
    ///     C# classes. The instance is created on first access and cached for the
    ///     lifetime of the application. Subclasses must have a public parameterless
    ///     constructor.
    /// </summary>
    /// <typeparam name="T">The concrete singleton type.</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;

        /// <summary>
        ///     Returns the singleton instance, creating it if it does not exist yet.
        ///     Uses the null-coalescing assignment operator for a concise lazy-init pattern.
        /// </summary>
        public static T Instance
        {
            get
            {
                _instance ??= new T();
                return _instance;
            }
        }
    }

    /// <summary>
    ///     Singleton base class for MonoBehaviour components. Ensures only one
    ///     instance of the component exists in the scene. If a duplicate is
    ///     detected during <see cref="Awake" />, the duplicate GameObject is
    ///     destroyed immediately.
    /// </summary>
    /// <typeparam name="T">The concrete MonoBehaviour singleton type.</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        /// <summary>
        ///     Returns the singleton instance. On first access, searches the scene
        ///     for an existing component of type <typeparamref name="T" />.
        ///     Logs a warning if no instance is found (e.g. the prefab is missing
        ///     from the scene).
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                        Debug.LogWarning($"No instance of {typeof(T).Name} found in scene!");
                }

                return _instance;
            }
        }

        /// <summary>
        ///     Unity lifecycle callback. Enforces the singleton contract:
        ///     if an instance already exists and it is not this object,
        ///     this GameObject is destroyed. Otherwise this object registers
        ///     itself as the singleton instance.
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}