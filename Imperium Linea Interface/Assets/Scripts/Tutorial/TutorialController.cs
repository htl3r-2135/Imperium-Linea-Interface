using System;
using System.Collections;
using Console.Commands;
using Console.CommandUtility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Tutorial
{
    public class TutorialController : MonoBehaviour
    {
        public Canvas canvas;
        public TMP_Text title;
        public TMP_Text description;

        public Button continueButton;
        public Button backButton;

        private ArrayList _titles = new ArrayList() { "Welcome to I.L.I", "General Controls", "Timer & Leaderboard", "Scanning the Hallways", "CLI Shortcuts", "Spotting Enemies", "Defeating Enemies", "Doors", "Actually defeating enemies", "Get Help", "Try it out yourself", "" };
        private ArrayList _descriptions = new ArrayList()
        {
            "In this Tutorial you will learn the primary commands and actions you can perform to play. \nThe Goal is to scan the Hallways for Enemies, defeat them using the built in Doors and survive as long as possible.",
            "All actions are to be performed through the CLI on the PC in front of you. \nYou can get a closer look by hovering over it using your mouse. \nBy Right Clicking you can also lock the camera to your current view.",
            "Next to the PC you can see a Timer showing you how long you have survived. \nYou can submit this time to our public Leaderboard so you can compare your time with others. \nThe Leaderboard is accessible through our Website: \n\n {{WEBSITE}}",
            "You are on a rotating Platform, which you can control using the \n\n{{ROTATE_COMMAND}} command. \n\nYou can choose to either rotate in set increments or add an angle after the command as a number. \n\n F.E.: {{ROTATE_COMMAND}} 180 -> Rotates 180°",
            "You can go through your command history by using the Arrow Keys. \nUP to see an earlier command \nDOWN to see a more recent command",
            "Enemies spawn periodically in any given Hallway. Try to spot them by rotating your Platform using {{ROTATE_COMMAND}}.",
            "After spawning Enemies slowly move towards you. \nThe only way to stop them is to defeat them using the doors built into the Hallways. \nYou can close doors using \n\n{{CLOSE_DOOR_COMMAND}} \n\nTo specify which door to close, use the numbers above them behind the command.\n\n F.E.: {{CLOSE_DOOR_COMMAND}} 0 -> Door 0 gets closed.",
            "Doors will open imediately after closing to avoid a perpetual closed state. \nAny given Door has a 10s delay between closing and being able to be closed again. \nSo be careful when you close them.",
            "Now that the delay has run out, you can try again to defeat the enemy. \nWait until the Enemy gets close enough to be in range of the doors. \nWhen they are they will get indicated with a light. \nAt that point they are sure to be defeated by the doors.",
            "At any point when you need help with a command you can use the \n\n{{HELP_COMMAND}} command \n\nto get information about available commands.",
            "Now that you know the basics, try it out yourself and see how long you can survive!\n\nRemember to use {{HELP_COMMAND}} if you need assistance.\nYou can also always redo this tutorial by chosing it in the Main Menu\n\nGood Luck!",
            ""
        };

        private int _phase = 0;
        
        private TutorialSingleton _tutorialSingleton = TutorialSingleton.Instance;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _tutorialSingleton.SetIsTutorial(true);
            Phases();
        }

        // Update is called once per frame
        void Update()
        {
            backButton.interactable = (_phase != 0);
            continueButton.interactable = (_phase != _titles.Count - 1);

            title.text = _titles[_phase] as string;
            description.text = ((string)_descriptions[_phase])
                .Replace("{{ROTATE_COMMAND}}", CommandCollector.Instance.GetCommand<SetRotationCommand>().CommandName)
                .Replace("{{HELP_COMMAND}}", CommandCollector.Instance.GetCommand<HelpCommand>().CommandName)
                .Replace("{{CLOSE_DOOR_COMMAND}}", CommandCollector.Instance.GetCommand<CloseDoorCommand>().CommandName)
                .Replace("{{WEBSITE}}", "www.WEBSITE.com/leaderboard");
        }

        public void Continue()
        {
            AudioManager.Instance.PlayButton();
            GameLogger.Instance.LogInfo("Button Continue clicked", "Tutorial");
            if (_phase < _titles.Count - 1)
            {
                _phase++;
                Phases();
            }
        }

        public void Back()
        {
            AudioManager.Instance.PlayButton();
            GameLogger.Instance.LogInfo("Button Back clicked", "Tutorial");
            if (_phase > 0)
            {
                _phase--;
            }
        }

        private void SetReadState()
        {
            _tutorialSingleton.SetSpawnBlock(true);
            _tutorialSingleton.SetLookBlock(true);
            _tutorialSingleton.SetDoorsCloseBlock(true);
            _tutorialSingleton.SetDoorsOpenBlock(true);
            _tutorialSingleton.SetDoorsLock(true);
            _tutorialSingleton.SetRotateBlock(true);
            _tutorialSingleton.SetMoveBlock(true);
        }

        private void Phases()
        {
            switch (_phase)
            {
                case 0:
                    SetReadState();
                    break;
                case 1 or 2:
                    _tutorialSingleton.SetLookBlock(false);
                    break;
                case 3:
                    _tutorialSingleton.SetRotateBlock(false);
                    break;
                case 5:
                    _tutorialSingleton.SetSpawnBlock(false);
                    break;
                case 6:
                    _tutorialSingleton.SetDoorsCloseBlock(false);
                    _tutorialSingleton.SetDoorsOpenBlock(false);
                    break;
                case 7:
                    _tutorialSingleton.SetDoorsCloseBlock(true);
                    _tutorialSingleton.SetDoorsLock(false);
                    break;
                case 8:
                    _tutorialSingleton.SetDoorsCloseBlock(false);
                    _tutorialSingleton.SetMoveBlock(false);
                    break;
                case 11:
                    _tutorialSingleton.SetIsTutorial(false);
                    _tutorialSingleton.SetMoveBlock(false);
                    _tutorialSingleton.SetSpawnBlock(false);
                    canvas.enabled = false;
                    SceneManager.LoadScene("GameEnvironment");
                    break;
            }
        }
    }
}