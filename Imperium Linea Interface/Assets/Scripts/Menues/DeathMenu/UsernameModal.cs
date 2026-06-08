using Abstract;
using TMPro;
using UnityEngine;

namespace Menues.DeathMenu
{
    public class UsernameModal : MonoSingleton<UsernameModal>
    {
        public GameObject panel;
        public TMP_InputField inputField;

        public string Username { get; private set; } = "Player";
        public System.Action<string> OnUsernameConfirmed;

        private void Awake()
        {
            var defaultName = System.Environment.UserName;
            Username = PlayerPrefs.GetString("username", defaultName);
        }

        private void Start()
        {
            panel.SetActive(true);

            inputField.text = Username;
            inputField.caretPosition = Username.Length;
            inputField.ActivateInputField();

            Time.timeScale = 0f;
        }

        public void Confirm()
        {
            var input = inputField.text.Trim();

            if (!string.IsNullOrEmpty(input))
                Username = input;

            PlayerPrefs.SetString("username", Username);
            PlayerPrefs.Save();

            panel.SetActive(false);

            Time.timeScale = 1f;

            OnUsernameConfirmed?.Invoke(Username);
        }
        
        public void Dismiss()
        {
            panel.SetActive(false);

            Time.timeScale = 1f;
        }
    }
}