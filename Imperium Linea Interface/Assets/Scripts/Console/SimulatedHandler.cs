using System;
using System.Collections;
using Abstract.Console;
using Console.CommandUtility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Console
{
    /// <summary>
    ///     Concrete command handler that simulates a local terminal session.
    ///     Manages the animated boot sequence and dispatches commands to the
    ///     <see cref="CommandCollector" /> registry.
    /// </summary>
    public class SimulatedHandler : ACommandHandler<SimulatedHandler>
    {
        // ── Startup timing constants ──────────────────────────────────────────

        /// <summary>Delay between individual characters during typed output.</summary>
        [Header("Startup Config")] [Range(0.01f, 0.5f)]
        private const float CharDelay = 0.02f;

        /// <summary>Delay between lines during the startup sequence.</summary>
        [Range(0.1f, 2f)] private const float LineDelay = 0.5f;

        /// <summary>OS name displayed in the startup header.</summary>
        private const string SystemName = "Doors 12";

        /// <summary>The command registry populated during startup.</summary>
        private CommandCollector _commands;

        /// <summary>
        ///     Ordered list of status messages shown during the boot sequence.
        ///     Each entry is animated with a spinner before being marked [ OK ].
        /// </summary>
        private string[] StartupSequence { get; } =
        {
            "Initializing kernel modules...",
            "Loading system configuration...",
            "Mounting virtual file systems...",
            "Starting network services...",
            "Running security diagnostics...",
            "Establishing secure shell...",
            "System ready."
        };

        /// <summary>
        ///     Initialises the command registry and kicks off the animated
        ///     boot sequence coroutine.
        /// </summary>
        protected override void OnStartUp()
        {
            GameLogger.Instance.LogInfo("SimulatedHandler starting up...", "Console");

            _commands = CommandCollector.Instance;

            GameLogger.Instance.LogDebug("Collecting commands...", "Console");
            _commands.CollectCommands();

            GameLogger.Instance.LogDebug("Starting boot sequence...", "Console");

            RunCoroutine(PlayStartupSequence());
        }

        /// <summary>
        ///     Coroutine that renders the full boot animation:
        ///     ASCII art header → session metadata block → spinner-animated
        ///     startup tasks → ready prompt.
        ///     Calls <see cref="ACommandHandler{T}.FinishStartup" /> when complete
        ///     to unblock user input.
        /// </summary>
        private IEnumerator PlayStartupSequence()
        {
            GameLogger.Instance.LogInfo("Boot sequence started", "Console");
            // ASCII art logo printed line-by-line with a tiny inter-line delay
            // to give the impression of a fast scroll.
            var header = new[]
            {
                @"   ____     ___     ___    ____    ____   ",
                @"  |  _ \   / _ \   / _ \  |  _ \  / ___|  ",
                @"  | | | | | | | | | | | | | |_) | \___ \  ",
                @"  | |_| | | |_| | | |_| | |  _ <   ___) | ",
                @"  |____/   \___/   \___/  |_| \_\ |____/  ",
                @"                                          ",
                @"                    _  ___                ",
                @"                   | ||__ \               ",
                @"                   | |  / /               ",
                @"                   | | / /                ",
                @"                   | |/ /                 ",
                @"                   |_/___|                ",
                ""
            };

            foreach (var line in header)
            {
                WriteLine(line);
                yield return Delay(0.01f);
            }

            // Session metadata block: timestamp, username, random hex session ID.
            yield return TypeBlock(new[]
            {
                $"[ Boot: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ]",
                $"[ User: {Environment.UserName} ]",
                $"[ Session ID: {Random.Range(1000, 9999):X4} ]",
                ""
            });

            // Each startup task gets a live spinner animation followed by [ OK ].
            foreach (var task in StartupSequence)
            {
                yield return TypeLineWithProgress(task);
                yield return Delay(LineDelay * 0.5f);
            }

            WriteLine("");
            WriteLine("Type 'help' for available commands.");
            WriteLine("");

            GameLogger.Instance.LogInfo("Boot sequence completed - console ready", "Console");

            FinishStartup();
        }

        /// <summary>
        ///     Prints a block of lines with a short delay between each, simulating
        ///     fast sequential output rather than an instant dump.
        /// </summary>
        private IEnumerator TypeBlock(string[] lines)
        {
            foreach (var line in lines)
            {
                WriteLine(line);
                yield return Delay(CharDelay * 5);
            }
        }

        /// <summary>
        ///     Writes a single status line prefixed with an animated spinner, then
        ///     replaces it with a [ OK ] prefix once the simulated task duration elapses.
        ///     Uses <see cref="ACommandHandler{T}.ReplaceLastLine" /> to update the line
        ///     in-place rather than appending new lines on each tick.
        /// </summary>
        private IEnumerator TypeLineWithProgress(string text)
        {
            // Frames of the spinner animation cycled at ~10 fps.
            var spinners = new[] { "|", "/", "-", "\\" };
            var spinnerIdx = 0;
            var startTime = Time.time;
            var duration = LineDelay;

            // Write the initial spinner frame before entering the animation loop.
            WriteRaw("[    ] " + text);

            // Advance the spinner frame every 0.1 s until the task duration expires.
            while (Time.time - startTime < duration)
            {
                yield return Delay(0.1f);
                spinnerIdx = (spinnerIdx + 1) % spinners.Length;
                ReplaceLastLine($"[{spinners[spinnerIdx]}] {text}");
            }

            // Finalise the line with a static OK badge and advance to the next line.
            ReplaceLastLine($"[ OK ] {text}");
        }

        /// <summary>
        ///     Forwards the sanitised command string to the command registry for
        ///     lookup and execution.
        /// </summary>
        protected override void SendCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                GameLogger.Instance.LogDebug("Empty command ignored", "Console");
                return;
            }

            GameLogger.Instance.LogInfo($"Dispatching command: {command}", "Console");

            _commands.Execute(command);
        }

        /// <summary>
        ///     Writes a command's output to the console. Called by command
        ///     implementations that need to surface results to the user.
        /// </summary>
        protected internal new void WriteCommandOutput(string output)
        {
            GameLogger.Instance.LogDebug($"OUTPUT: {output}", "Console");

            WriteLine($"{output}");
        }

        public override void SetUsername(string username)
        {
        }

        public override void SetPassword(string password)
        {
        }

        public override void SetHost(string server)
        {
        }
    }
}