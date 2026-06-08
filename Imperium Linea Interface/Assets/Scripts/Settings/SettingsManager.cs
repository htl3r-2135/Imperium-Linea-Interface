using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Settings
{
    // Imports
    /// <summary>
    ///     Main Manager class for all relevant settings
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        private string _filePath;

        private Dictionary<string, string> _settings = new();

        // Internal values
        public static SettingsManager Instance { get; private set; }

        /// <summary>
        ///     Function called after startup, reads the settings file and loads the correct values from it
        /// </summary>
        private void Awake()
        {
            // Singleton guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set file path for settings
            _filePath = Path.Combine(Application.persistentDataPath, "settings.json");
            Load();
        }

        /// <summary>
        ///     Loads all values from the settings file and stores them in a dict
        /// </summary>
        private void Load()
        {
            SetDefaults();
            Debug.Log(JsonUtility.ToJson(SettingsWrapper.FromDictionary(_settings), true));
            // Check if settings file exists
            if (File.Exists(_filePath))
            {
                // Load JSON, serialize into wrapper, then turn into dict
                var json = File.ReadAllText(_filePath);
                Debug.Log(json);
                var wrapper = JsonUtility.FromJson<SettingsWrapper>(json);
                _settings = wrapper.ToDictionary();
            }
            else
            {
                // Save values to create settings file
                Save();
            }

            //Change Audio System volume level to match saved volume
            AudioListener.volume = GetFloat("volume", 0.5f);

            //Resolution settings applied
            var parts = Get("resolution", "1920x1080").Split('x');
            var width = int.Parse(parts[0]);
            var height = int.Parse(parts[1]);
            Screen.SetResolution(width, height, true);

            bool vsync = GetBool("vsync");
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }

        /// <summary>
        ///     Saves the current dict by turning it into a wrapper, then serializing it into JSON
        /// </summary>
        private void Save()
        {
            //Turn into JSON and save
            var json = JsonUtility.ToJson(SettingsWrapper.FromDictionary(_settings), true);
            Debug.Log(json);
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        ///     Sets default values for the dict to prevent errors from missing entries in the settings file
        /// </summary>
        private void SetDefaults()
        {
            int width = Screen.currentResolution.width;
            int height = Screen.currentResolution.height;
            _settings["volume"] = "0.5";
            _settings["resolution"] = width + "x" + height;
            _settings["motionBlur"] = "true";
            _settings["vsync"] = "false";
        }

        /// <summary>
        ///     Fetches a single string from the settings dict
        /// </summary>
        /// <param name="key">Key to find the correct entry</param>
        /// <param name="fallback">Default value, returned if key does not exist</param>
        /// <returns></returns>
        public string Get(string key, string fallback = "")
        {
            return _settings.GetValueOrDefault(key, fallback);
        }

        /// <summary>
        ///     Fetches a single float from the settings dict
        /// </summary>
        /// <param name="key">Key to find the correct entry</param>
        /// <param name="fallback">Default value, returned if key does not exist</param>
        /// <returns></returns>
        public float GetFloat(string key, float fallback = 0f)
        {
            return float.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var val)
                ? val
                : fallback;
        }

        /// <summary>
        ///     Fetches a single boolean from the settings dict
        /// </summary>
        /// <param name="key">Key to find the correct entry</param>
        /// <param name="fallback">Default value, returned if key does not exist</param>
        /// <returns></returns>
        public bool GetBool(string key, bool fallback = false)
        {
            return bool.TryParse(Get(key), out var val) ? val : fallback;
        }

        /// <summary>
        ///     Fetches a single integer from the settings dict
        /// </summary>
        /// <param name="key">Key to find the correct entry</param>
        /// <param name="fallback">Default value, returned if key does not exist</param>
        /// <returns></returns>
        public int GetInt(string key, int fallback = 0)
        {
            return int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var val)
                ? val
                : fallback;
        }

        /// <summary>
        ///     Sets the given value at the given key
        /// </summary>
        /// <param name="key">Key for the value</param>
        /// <param name="value">New assigned value</param>
        public void Set(string key, string value)
        {
            _settings[key] = value;
            Save();
        }

        /// <summary>
        ///     Same as above, with float
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, float value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Same as above, with boolean
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, bool value)
        {
            Set(key, value.ToString());
        }
    }
}