using System;
using System.Collections.Generic;

namespace Settings
{
    /// <summary>
    ///     utility class used to merge the settings map into a correct JSON structure, pretty much just abstraction
    ///     to avoid cluttering the manager class
    /// </summary>
    [Serializable]
    public class SettingsWrapper
    {
        // Serializable values for json
        public List<string> keys = new();
        public List<string> values = new();

        /// <summary>
        ///     Turns the wrapper values into a dictionary (used for loading the settings)
        /// </summary>
        /// <returns>The dictionary with all the json's data</returns>
        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < keys.Count; i++)
                dict[keys[i]] = values[i];
            return dict;
        }

        /// <summary>
        ///     Turns the given dictionary into a wrapper that can then be used for serializing/saving the settings
        /// </summary>
        /// <param name="dict">Dictionary that should be turned into a wrapper</param>
        /// <returns>A new Settings Wrapper with all the dict's data</returns>
        public static SettingsWrapper FromDictionary(Dictionary<string, string> dict)
        {
            var w = new SettingsWrapper();
            foreach (var kv in dict)
            {
                w.keys.Add(kv.Key);
                w.values.Add(kv.Value);
            }

            return w;
        }
    }
}