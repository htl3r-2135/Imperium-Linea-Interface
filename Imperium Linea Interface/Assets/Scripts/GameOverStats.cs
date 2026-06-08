using Gegner;
using Menues.DeathMenu;
using Timer;
using TMPro;
using UnityEngine;
using Utility;

public class GameOverStats : MonoBehaviour
{
    /// <summary>
    /// UI text element that displays the current elapsed time in <c>MM:SS</c> format.
    /// </summary>
    public TMP_Text timerText;

    /// <summary>
    /// UI text element that displays the current elapsed time in <c>MM:SS</c> format.
    /// </summary>
    public TMP_Text enemyText;

    private void Start()
    {
        var time = TimeSingleton.GetGameOverTime();
        var enemies = GegnerStatsSingleton.GetDefeatedEnemies();

        timerText.text = "You survived for " + TimeSingleton.ToString(time);
        enemyText.text = "You defeated " + enemies + " enemies";

        UsernameModal.Instance.OnUsernameConfirmed += username =>
        {
            SubmitToLeaderboard(time, username);
        };
    }

    private void SubmitToLeaderboard(float timeSeconds, string username)
    {
        var timeMs = Mathf.RoundToInt(timeSeconds * 1000f);

        Leaderboard.Instance.SubmitScore(
            username,
            timeMs,
            onSuccess: () =>
            {
                Debug.Log("Leaderboard submission successful");
            },
            onError: err =>
            {
                Debug.LogWarning(err);
            }
        );
    }
}