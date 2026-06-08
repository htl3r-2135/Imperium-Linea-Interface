using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Utility.MotionBlur;

namespace Menues.SettingsMenu
{
    /// <summary>
    ///     Drives the Settings screen UI. Currently only handles navigation back
    ///     to the main menu; additional settings logic can be added here.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        // Changeable values
        public TMP_Dropdown dropdown;
        public Slider volumeSlider;
        public Toggle motionBlurToggle;
        public Toggle vsyncToggle;

        //Containers
        public GameObject graphicsContainer;
        public GameObject audioContainer;
        public GameObject accessibilityContainer;

        /// <summary>
        ///     Initial startup method, sets the settings components' values to match the settings' values
        /// </summary>
        public void Start()
        {
            volumeSlider.value = Settings.SettingsManager.Instance.GetFloat("volume");
            motionBlurToggle.isOn = Settings.SettingsManager.Instance.GetBool("motionBlur");
            vsyncToggle.isOn = Settings.SettingsManager.Instance.GetBool("vsync");
            var index = dropdown.options.FindIndex(option =>
                option.text == Settings.SettingsManager.Instance.Get("resolution", "1920x1080"));
            dropdown.value = index != -1 ? index : 1;
        }

        /// <summary>
        ///     Returns to the main menu scene, discarding any unsaved settings changes.
        ///     Wired to the Back button in the Inspector.
        /// </summary>
        public void Back()
        {
            AudioManager.Instance.PlayButton();
            SettingsManager.Back();
        }

        /// <summary>
        ///     Shows the given settings category and hides the other ones. Acceptable types are: <br />
        ///     - graphics <br />
        ///     - audio <br />
        ///     - accessibility
        /// </summary>
        /// <param name="type">Category type to be shown (others are hidden)</param>
        public void SetSettings(string type)
        {
            AudioManager.Instance.PlayButton();
            if (type == "graphics")
            {
                graphicsContainer.SetActive(true);
                audioContainer.SetActive(false);
                accessibilityContainer.SetActive(false);
            }
            else if (type == "audio")
            {
                graphicsContainer.SetActive(false);
                audioContainer.SetActive(true);
                accessibilityContainer.SetActive(false);
            }
            else
            {
                graphicsContainer.SetActive(false);
                audioContainer.SetActive(false);
                accessibilityContainer.SetActive(true);
            }

            Debug.Log(type);
        }

        /// <summary>
        ///     Applies the current value of the resolution box to the settings
        /// </summary>
        public void ApplyResolutionValue()
        {
            AudioManager.Instance.PlayButton();
            Settings.SettingsManager.Instance.Set("resolution", dropdown.options[dropdown.value].text);

            var parts = dropdown.options[dropdown.value].text.Split('x');
            var width = int.Parse(parts[0]);
            var height = int.Parse(parts[1]);
            Screen.SetResolution(width, height, true);
        }

        /// <summary>
        ///     Applies the current value of the volume label to the settings
        /// </summary>
        public void ApplyVolumeValue()
        {
            AudioManager.Instance.PlayButton();
            Settings.SettingsManager.Instance.Set("volume", volumeSlider.value);
            AudioListener.volume = volumeSlider.value;
        }

        /// <summary>
        ///     Applies the current value of the motion blur toggle to the settings
        /// </summary>
        public void ApplyMotionBlurValue()
        {
            AudioManager.Instance.PlayButton();
            Settings.SettingsManager.Instance.Set("motionBlur", motionBlurToggle.isOn);

            var volumeObject = GameObject.Find("Volume Profile");
            if (volumeObject == null) return;
            var volume = volumeObject.GetComponent<Volume>();
            if (volume != null)
            {
                MotionBlurManager.SetMotionBlur(!motionBlurToggle.isOn, volume);
            }
        }

        /// <summary>
        /// Applies the current value of the vsync toggle to the settings
        /// </summary>
        public void ApplyVSyncValue()
        {
            AudioManager.Instance.PlayButton();
            Settings.SettingsManager.Instance.Set("vsync", vsyncToggle.isOn);
            QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
        }
    }
}