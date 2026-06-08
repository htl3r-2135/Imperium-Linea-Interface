using System;
using System.Collections.Generic;
using Abstract;
using TMPro;
using UnityEngine;

namespace Timer
{
    /// <summary>
    ///     Singleton manager responsible for tracking elapsed game time and dispatching
    ///     time-based events to registered subscribers.
    /// </summary>
    /// <remarks>
    ///     Attach this component to a persistent GameObject in your scene.
    ///     Time begins at zero when the scene starts and increments each frame via <see cref="Time.deltaTime" />.
    /// </remarks>
    public class TimerManager : MonoSingleton<TimerManager>
    {
        /// <summary>
        ///     UI text element that displays the current elapsed time in <c>MM:SS</c> format.
        /// </summary>
        public TMP_Text timerText;

        /// <summary>
        ///     Maps a target time (in seconds) to an array of callbacks to invoke when that time is reached.
        /// </summary>
        private readonly Dictionary<float, Action<float>[]> _events = new();

        /// <summary>
        ///     Initialises the timer, resetting elapsed time to zero.
        /// </summary>
        private void Start()
        {
            TimeSingleton.SetTime(0);
        }

        /// <summary>
        ///     Advances the timer each frame and fires any events whose target time has been reached.
        /// </summary>
        /// <remarks>
        ///     After an event's callbacks are invoked, the event is removed from the dictionary
        ///     so it only fires once.
        /// </remarks>
        private void Update()
        {
            TimeSingleton.SetTime(TimeSingleton.GetTime() + Time.deltaTime);

            var toRemove = new List<float>();

            foreach (var (time, callbacks) in _events)
            {
                if (!(TimeSingleton.GetTime() >= time)) continue;
                foreach (var callback in callbacks)
                {
                    callback(TimeSingleton.GetTime());
                    GameLogger.Instance.LogInfo($"Event triggered at {time}", "Timer");
                }

                toRemove.Add(time);
            }

            foreach (var key in toRemove) _events.Remove(key);

            timerText.text = TimeSingleton.ToString(TimeSingleton.GetTime());
        }

        /// <summary>
        ///     Registers a callback to be invoked when the timer reaches the specified time.
        /// </summary>
        /// <param name="eventTime">
        ///     Target time in <c>MM:SS</c> format, e.g. <c>"01:30"</c> for one minute and thirty seconds.
        /// </param>
        /// <param name="callback">
        ///     The action to invoke when the target time is reached.
        ///     Receives the actual elapsed time (in seconds) at the moment of invocation.
        /// </param>
        public void Subscribe(string eventTime, Action<float> callback)
        {
            _events.Add(TimeSingleton.ToFloat(eventTime), new[] { callback });
        }

        public void GameOver()
        {
            TimeSingleton.SetGameOverTime(TimeSingleton.GetTime());
        }
    }
}