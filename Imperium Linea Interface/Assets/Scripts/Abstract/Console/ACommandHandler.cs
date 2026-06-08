using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Abstract.Console
{
    /// <summary>
    ///     Exposes the full public surface of a command handler that the console
    ///     UI layer needs, without depending on the concrete generic handler type.
    ///     Covers both read-only state and all setup, execution, and output methods.
    /// </summary>
    internal interface ICommandHandler
    {
        // ── Read-only state ───────────────────────────────────────────────────

        /// <summary>True once the startup sequence has fully completed.</summary>
        bool StartedUp { get; }

        /// <summary>
        ///     Optional prefix that is stripped from every raw input string before
        ///     it is forwarded to <see cref="ACommandHandler{T}.SendCommand" />.
        ///     Useful for prompts like "> " that are part of the displayed line.
        /// </summary>
        string LinePrefix { get; }

        /// <summary>The TextMeshPro component that renders console output.</summary>
        TMP_Text CliText { get; }

        /// <summary>The full accumulated text currently displayed on screen.</summary>
        string FullText { get; }

        /// <summary>
        ///     True while the startup coroutine is running. Input is blocked
        ///     during this period.
        /// </summary>
        bool IsProcessingStartup { get; }

        /// <summary>Number of character columns the current screen layout supports.</summary>
        int Columns { get; }

        /// <summary>Number of text rows the current screen layout supports.</summary>
        int Rows { get; }

        // ── Setup / injection ─────────────────────────────────────────────────

        /// <summary>
        ///     Assigns the TMP_Text component used for all console output.
        ///     Must be called before any text is written.
        /// </summary>
        void SetCli(TMP_Text cli);

        /// <summary>
        ///     Injects the line-writer callback that appends text to the console.
        ///     Must be called before any output is produced.
        /// </summary>
        void SetWriter(Action<string> writer);

        /// <summary>
        ///     Injects the MonoBehaviour used to run coroutines.
        ///     Must be called before any coroutine-based output is produced.
        /// </summary>
        void SetCoroutineRunner(MonoBehaviour runner);

        /// <summary>
        ///     Injects the four screen-control callbacks.
        /// </summary>
        /// <param name="clear">Clears the screen.</param>
        /// <param name="getText">Returns the current full text (pass null for unfiltered).</param>
        /// <param name="setText">Replaces the entire text buffer.</param>
        /// <param name="showPaged">Displays text in a paged view.</param>
        void SetScreenController(Action clear, Func<string, string> getText,
            Action<string> setText, Action<string> showPaged);

        /// <summary>
        ///     Tells the handler the dimensions of the current console layout.
        ///     Used by subclasses that need to word-wrap or paginate output.
        /// </summary>
        void SetScreenSize(int columns, int rows);

        /// <summary>Sets the prompt prefix (e.g. "> " or "user@host:~$ ").</summary>
        void SetPrefix(string prefix);

        /// <summary>
        ///     Injects the color-control callbacks for text and background.
        ///     Must be called before <see cref="SetTextColor" /> or
        ///     <see cref="SetBackgroundColor" /> are used.
        /// </summary>
        void SetColorControls(Action<Color> setTextColor, Action<Color> setBackgroundColor);

        // ── Runtime controls ──────────────────────────────────────────────────

        /// <summary>
        ///     Begins the startup sequence. Sets <see cref="IsProcessingStartup" />
        ///     to true and delegates to the handler's boot logic.
        /// </summary>
        void StartUp();

        /// <summary>
        ///     Validates and dispatches a raw input string.
        ///     Silently ignores input while startup is in progress,
        ///     strips the <see cref="LinePrefix" />, trims whitespace,
        ///     and forwards the clean command to the handler's command registry.
        /// </summary>
        void Execute(string command);

        void SetUsername(string username)
        {
        }

        void SetPassword(string password)
        {
        }

        void SetHost(string host)
        {
        }

        /// <summary>
        ///     Displays <paramref name="text" /> in the paged/scrollable viewer
        ///     rather than appending it inline. Suitable for long help or man pages.
        /// </summary>
        void ShowPaged(string text);

        /// <summary>Changes the console foreground/text color.</summary>
        void SetTextColor(Color color);

        /// <summary>Changes the console background color.</summary>
        void SetBackgroundColor(Color color);

        /// <summary>
        ///     Writes a command's output to the console. Called by command
        ///     implementations that need to surface results to the user.
        /// </summary>
        void WriteCommandOutput(string output);
    }

    /// <summary>
    ///     Base class for all console command handlers.
    ///     Implements the singleton pattern via <see cref="Singleton{T}" /> and
    ///     manages the bridge between the Unity UI layer (writer, coroutines,
    ///     screen control) and the game-specific command logic implemented in
    ///     subclasses.
    /// </summary>
    /// <typeparam name="T">The concrete handler type (CRTP / self-referential generic).</typeparam>
    public abstract class ACommandHandler<T> : Singleton<T>, ICommandHandler where T : ACommandHandler<T>, new()
    {
        // ── Injected UI callbacks ─────────────────────────────────────────────
        // These are set by the Unity MonoBehaviour that owns the console screen
        // and must be supplied before any console output or coroutine is used.

        /// <summary>Clears all text from the screen.</summary>
        private Action _clearScreen;

        /// <summary>
        ///     MonoBehaviour used as the coroutine host, since plain C# classes
        ///     cannot call StartCoroutine directly.
        /// </summary>
        private MonoBehaviour _coroutineRunner;

        /// <summary>
        ///     Returns the current full text string. The string argument is
        ///     reserved for future filtering; pass null to get the raw value.
        /// </summary>
        private Func<string, string> _getFullText;

        /// <summary>Sets the console background color.</summary>
        private Action<Color> _setBackgroundColor;

        /// <summary>Sets the console text/foreground color.</summary>
        private Action<Color> _setTextColor;

        /// <summary>Displays a long text block in a paged/scrollable view.</summary>
        private Action<string> _showPaged;

        /// <summary>Replaces the full text buffer with a new string.</summary>
        private Action<string> _updateFullText;

        /// <summary>Appends a single line of text to the console output.</summary>
        private Action<string> _writeLine;

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>Number of character columns the current screen layout supports.</summary>
        public int Columns { get; private set; }

        /// <summary>Number of text rows the current screen layout supports.</summary>
        public int Rows { get; private set; }

        /// <summary>The TextMeshPro component rendering console output.</summary>
        public TMP_Text CliText { get; private set; }

        /// <summary>Snapshot of the full text currently in the text buffer.</summary>
        public string FullText { get; private set; }

        /// <summary>Becomes true after <see cref="FinishStartup" /> is called.</summary>
        public bool StartedUp { get; private set; }

        /// <summary>
        ///     Prefix string displayed before user input (e.g. "> ").
        ///     Stripped from commands before dispatch. Defaults to empty.
        /// </summary>
        public string LinePrefix { get; private set; } = string.Empty;

        /// <summary>True while the startup sequence coroutine is running.</summary>
        public bool IsProcessingStartup { get; private set; }

        // ── Injection setters ─────────────────────────────────────────────────

        /// <summary>
        ///     Injects the line-writer callback that appends text to the console.
        ///     Must be called before any output is produced.
        /// </summary>
        public void SetWriter(Action<string> writer)
        {
            _writeLine = writer;
        }

        /// <summary>
        ///     Injects the MonoBehaviour used to run coroutines.
        ///     Must be called before <see cref="RunCoroutine" /> is used.
        /// </summary>
        public void SetCoroutineRunner(MonoBehaviour runner)
        {
            _coroutineRunner = runner;
        }

        /// <summary>
        ///     Injects the four screen-control callbacks.
        /// </summary>
        /// <param name="clear">Clears the screen.</param>
        /// <param name="getText">Returns the current full text (pass null for unfiltered).</param>
        /// <param name="setText">Replaces the entire text buffer.</param>
        /// <param name="showPaged">Displays text in a paged view.</param>
        public void SetScreenController(Action clear, Func<string, string> getText,
            Action<string> setText, Action<string> showPaged)
        {
            _clearScreen = clear;
            _getFullText = getText;
            _updateFullText = setText;
            _showPaged = showPaged;
        }

        /// <summary>
        ///     Tells the handler the dimensions of the current console layout.
        ///     Used by subclasses that need to word-wrap or paginate output.
        /// </summary>
        public void SetScreenSize(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
        }

        /// <summary>
        ///     Displays <paramref name="text" /> in the paged/scrollable viewer
        ///     rather than appending it inline. Suitable for long help or man pages.
        /// </summary>
        public void ShowPaged(string text)
        {
            _showPaged?.Invoke(text);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        ///     Assigns the TMP_Text component used for all console output.
        ///     Must be called before any text is written.
        /// </summary>
        public void SetCli(TMP_Text cli)
        {
            CliText = cli;
        }

        /// <summary>
        ///     Begins the startup sequence. Sets <see cref="IsProcessingStartup" />
        ///     to true and delegates to <see cref="OnStartUp" />.
        /// </summary>
        public void StartUp()
        {
            IsProcessingStartup = true;
            OnStartUp();
        }

        /// <summary>Sets the prompt prefix (e.g. "> " or "user@host:~$ ").</summary>
        public void SetPrefix(string prefix)
        {
            LinePrefix = prefix ?? string.Empty;
        }

        /// <summary>
        ///     Validates and dispatches a raw input string.
        ///     Silently ignores input while startup is in progress,
        ///     strips the <see cref="LinePrefix" />, trims whitespace,
        ///     and forwards the clean command to <see cref="SendCommand" />.
        /// </summary>
        public void Execute(string command)
        {
            // Block all input until the startup sequence has finished.
            if (IsProcessingStartup) return;

            if (string.IsNullOrWhiteSpace(command))
                return;

            // Strip the displayed prompt prefix if the raw input includes it.
            if (!string.IsNullOrEmpty(LinePrefix) && command.StartsWith(LinePrefix))
                command = command[LinePrefix.Length..];

            command = command.Trim();

            if (string.IsNullOrEmpty(command))
                return;

            SendCommand(command);
        }

        /// <summary>
        ///     Injects the color-control callbacks for text and background.
        ///     Must be called before <see cref="SetTextColor" /> or
        ///     <see cref="SetBackgroundColor" /> are used.
        /// </summary>
        public void SetColorControls(Action<Color> setTextColor, Action<Color> setBackgroundColor)
        {
            _setTextColor = setTextColor;
            _setBackgroundColor = setBackgroundColor;
        }

        /// <summary>Changes the console foreground/text color.</summary>
        public void SetTextColor(Color color)
        {
            _setTextColor?.Invoke(color);
        }

        /// <summary>Changes the console background color.</summary>
        public void SetBackgroundColor(Color color)
        {
            _setBackgroundColor?.Invoke(color);
        }

        public void WriteCommandOutput(string output)
        {
            throw new NotImplementedException();
        }

        public abstract void SetUsername(string username);
        public abstract void SetHost(string server);
        public abstract void SetPassword(string password);

        // ── Protected output helpers ──────────────────────────────────────────

        /// <summary>
        ///     Appends a full line of text via the injected writer callback.
        ///     The writer is responsible for adding a newline if needed.
        /// </summary>
        private protected void WriteLine(string text)
        {
            _writeLine?.Invoke(text);
        }

        /// <summary>
        ///     Appends raw text directly to the text buffer without going through
        ///     the line-writer. Also updates <see cref="CliText" /> immediately.
        /// </summary>
        private protected void WriteRaw(string text)
        {
            var current = _getFullText?.Invoke(null) ?? "";
            _updateFullText?.Invoke(current + text);
            CliText.text = current + text;
        }

        /// <summary>
        ///     Replaces the last line in the buffer with <paramref name="newText" />.
        ///     Useful for in-place progress updates (e.g. loading spinners).
        ///     If the buffer is empty, <paramref name="newText" /> is written as the first line.
        /// </summary>
        private protected void ReplaceLastLine(string newText)
        {
            var current = _getFullText?.Invoke(null) ?? "";

            if (string.IsNullOrEmpty(current))
            {
                _updateFullText?.Invoke(newText + "\n");
                return;
            }

            // Find the second-to-last newline to isolate the final line.
            var lastNewline = current.LastIndexOf('\n', current.Length - 2);

            string updated;
            if (lastNewline >= 0)
                // Keep everything before the last line, then substitute.
                updated = current.Substring(0, lastNewline + 1) + newText + "\n";
            else
                // Only one line in the buffer; replace it entirely.
                updated = newText + "\n";

            _updateFullText?.Invoke(updated);
            CliText.text = updated;
        }

        /// <summary>Clears all text from the screen via the injected callback.</summary>
        private protected void ClearScreen()
        {
            _clearScreen?.Invoke();
        }

        /// <summary>
        ///     Starts a coroutine on the injected runner. Logs an error if the
        ///     runner has not been set yet.
        /// </summary>
        private protected void RunCoroutine(IEnumerator routine)
        {
            if (_coroutineRunner != null)
                _coroutineRunner.StartCoroutine(routine);
            else
                Debug.LogError("No coroutine runner set! Call SetCoroutineRunner() first.");
        }

        /// <summary>
        ///     Convenience coroutine that pauses execution for the given duration.
        ///     Use with <c>yield return</c> inside another coroutine.
        /// </summary>
        private protected IEnumerator Delay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        // ── Abstract contract ─────────────────────────────────────────────────

        /// <summary>
        ///     Dispatches a fully sanitised command string to the handler's
        ///     command registry. Called by <see cref="Execute" /> after stripping
        ///     the prefix and trimming whitespace.
        /// </summary>
        protected abstract void SendCommand(string command);

        /// <summary>
        ///     Runs the handler's startup sequence (e.g. boot animation, MOTD).
        ///     Must call <see cref="FinishStartup" /> when complete to unblock input.
        /// </summary>
        protected abstract void OnStartUp();

        /// <summary>
        ///     Marks startup as complete, enabling normal command input.
        ///     Should be called at the end of the <see cref="OnStartUp" /> coroutine.
        /// </summary>
        protected void FinishStartup()
        {
            IsProcessingStartup = false;
            StartedUp = true;
        }
    }
}