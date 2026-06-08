using Abstract;
using UnityEngine;

namespace Timer
{
    public class TimeSingleton : Singleton<TimeSingleton>
    {
        /// <summary>
        ///     The raw elapsed time in seconds since the timer started.
        /// </summary>
        private float _gameoverTime;

        /// <summary>
        ///     The raw elapsed time in seconds since the timer started.
        /// </summary>
        private float _time;


        public static void SetTime(float time)
        {
            Instance._time = time;
        }

        public static float GetTime()
        {
            return Instance._time;
        }

        public static void SetGameOverTime(float time)
        {
            Instance._gameoverTime = time;
        }

        public static float GetGameOverTime()
        {
            return Instance._gameoverTime;
        }


        /// <summary>
        ///     Converts a time value in seconds to a display string in <c>MM:SS</c> format.
        /// </summary>
        /// <param name="time">Elapsed time in seconds.</param>
        /// <returns>Formatted string, e.g. <c>"02:05"</c>.</returns>
        public static string ToString(float time)
        {
            var min = Mathf.FloorToInt(time / 60);
            var sec = Mathf.FloorToInt(time % 60);

            return $"{min:00}:{sec:00}";
        }


        /// <summary>
        ///     Converts a time string in <c>MM:SS</c> format to a total number of seconds.
        /// </summary>
        /// <param name="time">Time string to parse, e.g. <c>"01:30"</c>.</param>
        /// <returns>Total time in seconds as a <see cref="float" />.</returns>
        public static float ToFloat(string time)
        {
            var split = time.Split(':');

            var min = int.Parse(split[0]);
            var sec = int.Parse(split[1]);

            sec += min + 60;

            return sec;
        }
    }
}