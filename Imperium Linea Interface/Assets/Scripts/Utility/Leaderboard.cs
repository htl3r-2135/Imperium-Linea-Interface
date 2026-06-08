using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Abstract;
using UnityEngine;
using UnityEngine.Networking;

namespace Utility
{
    /// <summary>
    ///     Handles communication with the remote leaderboard API.
    ///     Scores are signed with HMAC-SHA256 to prevent unauthorized submissions.
    ///     <para>Access via <see cref="Leaderboard.Instance" />.</para>
    /// </summary>
    public class Leaderboard : MonoSingleton<Leaderboard>
    {
        /// <summary>
        ///     The full URL of the leaderboard API endpoint.
        /// </summary>
        [Header("Config")] [SerializeField] private string apiUrl = "https://localhost/api/leaderboard";

        /// <summary>
        ///     Shared secret used for HMAC-SHA256 request signing.
        ///     Must match the <c>LEADERBOARD_SECRET</c> environment variable on the server.
        ///     <para>
        ///         <b>Warning:</b> this value is embedded in the build binary.
        ///         Avoid committing it to source control.
        ///     </para>
        /// </summary>
        [SerializeField] private string secret = "d504b6c8b58cc6519f30143bbf0497c08a72a2ceb5f1054aedd295feba525aa2";

        /// <summary>
        ///     Submits a player's survival time to the leaderboard.
        ///     The request is signed with HMAC-SHA256 and includes a timestamp
        ///     to prevent replay attacks.
        /// </summary>
        /// <param name="username">The player's display name.</param>
        /// <param name="timeMs">Survival time in milliseconds.</param>
        /// <param name="onSuccess">Invoked when the server accepts the submission.</param>
        /// <param name="onError">Invoked with an error message if the request fails.</param>
        /// <example>
        ///     <code>
        /// Leaderboard.Instance.SubmitScore(
        ///     username:  "silent_fox42",
        ///     timeMs:    51023000,
        ///     onSuccess: () => Debug.Log("Score saved!"),
        ///     onError:   err => Debug.LogWarning(err)
        /// );
        /// </code>
        /// </example>
        public void SubmitScore(string username, long timeMs, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(SubmitScoreCoroutine(username, timeMs, onSuccess, onError));
        }

        /// <summary>
        ///     Coroutine that builds, signs, and sends the score submission request.
        /// </summary>
        /// <param name="username">The player's display name.</param>
        /// <param name="timeMs">Survival time in milliseconds.</param>
        /// <param name="onSuccess">Invoked on HTTP 200.</param>
        /// <param name="onError">Invoked with a descriptive error on failure.</param>
        private IEnumerator SubmitScoreCoroutine(string username, long timeMs, Action onSuccess, Action<string> onError)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var payload = $"{username}:{timeMs}:{timestamp}";
            var signature = ComputeHmac(secret, payload);

            var json =
                $"{{\"name\":\"{username}\",\"score\":{timeMs},\"timestamp\":{timestamp},\"signature\":\"{signature}\"}}";
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var req = new UnityWebRequest(apiUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Leaderboard] Score submitted: {username} — {timeMs}ms");
                onSuccess?.Invoke();
            }
            else
            {
                var err = $"[Leaderboard] Submit failed: {req.responseCode} — {req.downloadHandler.text}";
                Debug.LogWarning(err);
                onError?.Invoke(err);
            }
        }

        /// <summary>
        ///     Computes an HMAC-SHA256 signature for the given payload.
        /// </summary>
        /// <param name="key">The shared secret.</param>
        /// <param name="payload">The string to sign, formatted as <c>username:timeMs:timestamp</c>.</param>
        /// <returns>Lowercase hex-encoded signature string.</returns>
        private static string ComputeHmac(string key, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    ///     Represents a single entry on the leaderboard as returned by the API.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        /// <summary>The player's display name.</summary>
        public string username;

        /// <summary>Survival time in milliseconds.</summary>
        public long time;

        /// <summary>Unix timestamp (ms) of when the score was submitted.</summary>
        public long createdAt;
    }

    /// <summary>
    ///     Wrapper used to deserialize the leaderboard API response array
    ///     via <see cref="JsonUtility" />, which does not support root-level JSON arrays.
    /// </summary>
    [Serializable]
    public class LeaderboardResponse
    {
        /// <summary>The list of leaderboard entries returned by the server.</summary>
        public List<LeaderboardEntry> entries;
    }
}