using Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace Utility.MotionBlur
{
    public class MotionBlurManager : MonoBehaviour
    {
        public Volume volume;

        /// <summary>
        /// Awake call when the GameEnvironment is started. Sets the initial value for the motion blur on startup
        /// </summary>
        private void Start()
        {
            SetMotionBlur(!SettingsManager.Instance.GetBool("motionBlur"), volume);
        }

        /// <summary>
        /// Activates/Deacivates the given volume based on the given boolean value
        /// </summary>
        /// <param name="value">Determines whether the motion blur is set to "active" or "inactive"</param>
        /// <param name="volume">The volume whose motion blur is to be changed</param>
        public static void SetMotionBlur(bool value, Volume volume)
        {
            var profile = volume.sharedProfile;
            profile.TryGet<UnityEngine.Rendering.HighDefinition.MotionBlur>(out var motionBlur);

            if (motionBlur == null)
            {
                Debug.Log("No motion blur found");
                return;
            }

            Debug.Log("Disabling motion blur");
            motionBlur.active = value;
        }
    }
}