using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Abstract.Console;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using HandlerTypeSimulated = Console.SimulatedHandler;

namespace Console
{
    /// <summary>
    ///     Unity MonoBehaviour that owns the console UI layer.
    ///     Bridges raw keyboard input, the text buffer, cursor rendering,
    ///     command history, and the paged viewer to the active
    ///     <see cref="ACommandHandler{T}" /> instance.
    /// </summary>
    public class Screen : MonoBehaviour
    {
        /// <summary>Block character used as the terminal cursor glyph.</summary>
        private const string CursorChar = "█";

        // ── History ───────────────────────────────────────────────────────────

        /// <summary>Maximum number of entries retained in the command history ring.</summary>
        private const int MaxHistory = 100;

        [Header("UI References")]
        /// <summary>GameObject containing the TMP_Text component for console output.</summary>
        public GameObject textField;

        /// <summary>GameObject containing the Image component used as the console background.</summary>
        public GameObject backgroundPanel;

        [Header("Appearance")]
        /// <summary>Initial background color applied to the background panel on start.</summary>
        public Color backgroundColor = Color.black;

        /// <summary>Initial text/foreground color applied to the TMP_Text component on start.</summary>
        public Color textColor = Color.green;

        /// <summary>Chronological list of previously executed command strings.</summary>
        private readonly List<string> _history = new();

        /// <summary>
        ///     Prompt prefix prepended to the input line.
        ///     Format: <c>pc@username &gt; </c>
        /// </summary>
        private readonly string _linePrefix = "pc@" + Environment.UserName[..5] + " > ";

        // ── Dimensions ────────────────────────────────────────────────────────

        /// <summary>Measured console width in monospaced character columns.</summary>
        private int _charCol;

        /// <summary>Measured console height in character rows.</summary>
        private int _charRow;

        /// <summary>Reference to the TMP_Text component that renders all console output.</summary>
        private TMP_Text _cli;


        private ICommandHandler _commandHandler;

        /// <summary>The text currently being composed on the input line.</summary>
        private string _currentLine = "";

        /// <summary>Caret position within <see cref="_currentLine" /> (0 = before first char).</summary>
        private int _cursorPos;

        // ── Cursor ────────────────────────────────────────────────────────────

        /// <summary>Controls whether the cursor glyph is shown or hidden on the current blink tick.</summary>
        private bool _cursorVisible = true;

        /// <summary>Accumulated output text for all completed lines, excluding the active input line.</summary>
        private string _fullText = "";

        /// <summary>
        ///     Index into <see cref="_history" /> while navigating with arrow keys.
        ///     -1 means the user is editing a fresh line (not browsing history).
        /// </summary>
        private int _historyIndex = -1;

        // ── Pager ─────────────────────────────────────────────────────────────

        /// <summary>True while the paged viewer is active; normal input is suppressed.</summary>
        private bool _isPaging;

        /// <summary>
        ///     Snapshot of the input line saved when the user starts browsing history,
        ///     restored when they navigate back past the most recent entry.
        /// </summary>
        private string _liveLine = "";

        /// <summary>All lines of the text currently being paged through.</summary>
        private List<string> _pageLines = new();

        /// <summary>Index of the first line of <see cref="_pageLines" /> currently visible.</summary>
        private int _pageOffset;

        /// <summary>Reference to the background panel's Image component.</summary>
        private Image _panel;

        /// <summary>The simulated command handler that processes user input by simulating a server.</summary>
        private ACommandHandler<HandlerTypeSimulated> _simulatedHandler;

        /// <summary>The ssh command handler that processes user input per ssh.</summary>
        private ACommandHandler<SshHandler> _sshHandler;

        /// <summary>
        ///     Unity Start callback. Wires up the TMP_Text and Image components,
        ///     measures console dimensions, injects all callbacks into the command
        ///     handler, registers keyboard input, and begins the startup sequence.
        /// </summary>
        private void Start()
        {
            _cli = textField.GetComponent<TMP_Text>();
            _panel = backgroundPanel.GetComponent<Image>();

            CalculateConsoleDimensionsPrecise();

            _cli.color = textColor;
            _panel.color = backgroundColor;

            // Retrieve the singleton handler and inject all UI-layer callbacks.
            _commandHandler = HandlerTypeSimulated.Instance;
            _commandHandler.SetCli(_cli);
            _commandHandler.SetWriter(WriteLine);
            _commandHandler.SetCoroutineRunner(this);
            _commandHandler.SetScreenSize(_charCol, _charRow);
            _commandHandler.SetScreenController(
                ClearScreen,
                _ => _fullText, // Getter:  the current text buffer.
                text => _fullText = text, // Setter: replaces the text buffer directly.
                ShowPaged
            );
            _commandHandler.SetColorControls(
                color =>
                {
                    _cli.color = color;
                    textColor = color;
                },
                color =>
                {
                    _panel.color = color;
                    backgroundColor = color;
                }
            );

            // Register the text input callback, waiting for keyboard init if needed.
            if (Keyboard.current != null)
                Keyboard.current.onTextInput += OnTextInput;
            else
                StartCoroutine(WaitForKeyboard());

            _commandHandler.StartUp();
            _commandHandler.SetPrefix(_linePrefix);

            StartCoroutine(BlinkCursor());
            UpdateText();
        }

        /// <summary>
        ///     Polls special keys (arrows, Enter, Delete, etc.) each frame after
        ///     the startup sequence has finished.
        /// </summary>
        private void LateUpdate()
        {
            if (_commandHandler.IsProcessingStartup) return;
            HandleSpecialKeys();
        }

        /// <summary>
        ///     Unregisters the text input callback when the GameObject is destroyed
        ///     to avoid dangling event subscriptions.
        /// </summary>
        private void OnDestroy()
        {
            if (Keyboard.current != null)
                Keyboard.current.onTextInput -= OnTextInput;
        }

        private void ChangeHandler(string handler, string user = "", string host = "", string password = "")
        {
            ClearScreen();

            if (handler == "ssh")
            {
                _commandHandler = SshHandler.Instance;
                _commandHandler.SetUsername(user);
                _commandHandler.SetHost(host);
                _commandHandler.SetPassword(password);
            }
            else if (handler == "simulated")
            {
                _commandHandler = HandlerTypeSimulated.Instance;
            }

            _commandHandler.SetCli(_cli);
            _commandHandler.SetWriter(WriteLine);
            _commandHandler.SetCoroutineRunner(this);
            _commandHandler.SetScreenSize(_charCol, _charRow);
            _commandHandler.SetScreenController(
                ClearScreen,
                _ => _fullText, // Getter: returns the current text buffer.
                text => _fullText = text, // Setter: replaces the text buffer directly.
                ShowPaged
            );
            _commandHandler.SetColorControls(
                color =>
                {
                    _cli.color = color;
                    textColor = color;
                },
                color =>
                {
                    _panel.color = color;
                    backgroundColor = color;
                }
            );
            _commandHandler.StartUp();
            _commandHandler.SetPrefix(_linePrefix);
        }

        // ── Input ─────────────────────────────────────────────────────────────

        /// <summary>
        ///     Handles printable character input from the New Input System's
        ///     <c>onTextInput</c> event. Ignores input during startup and paging.
        ///     Backspace removes the character to the left of the cursor; all other
        ///     printable characters are inserted at the cursor position.
        /// </summary>
        private void OnTextInput(char c)
        {
            if (char.IsControl(c) && c != '\u0008') return;
            if (_commandHandler.IsProcessingStartup) return;
            if (_isPaging) return;

            switch (c)
            {
                case '\n':
                case '\r':
                    return;

                case '\u0008':
                    if (_cursorPos > 0)
                    {
                        _currentLine = _currentLine.Remove(_cursorPos - 1, 1);
                        _cursorPos--;
                        _historyIndex = -1;
                    }
                    else
                    {
                        return; // Nothing happened, skip sound
                    }

                    break;

                default:
                    _currentLine = _currentLine.Insert(_cursorPos, c.ToString());
                    _cursorPos++;
                    _historyIndex = -1;
                    break;
            }

            AudioManager.Instance.PlayKeyPress(); // After the change, not before
            ResetCursorBlink();
            UpdateText();
        }

        /// <summary>
        ///     Polls until <c>Keyboard.current</c> becomes available, then registers
        ///     the text input callback. Required when the keyboard device initialises
        ///     after the first frame.
        /// </summary>
        private IEnumerator WaitForKeyboard()
        {
            while (Keyboard.current == null)
                yield return null;

            Keyboard.current.onTextInput += OnTextInput;
        }

        // ── Cursor ────────────────────────────────────────────────────────────

        /// <summary>
        ///     Toggles cursor visibility on a ~0.53 s interval to produce the
        ///     classic terminal blink effect. Runs for the lifetime of the object.
        /// </summary>
        private IEnumerator BlinkCursor()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.53f);
                _cursorVisible = !_cursorVisible;
                UpdateText();
            }
        }

        /// <summary>
        ///     Forces the cursor visible and resets the blink timer by making the
        ///     cursor visible immediately. Called on any keypress so the cursor
        ///     doesn't disappear mid-input.
        /// </summary>
        private void ResetCursorBlink()
        {
            _cursorVisible = true;
        }

        // ── History ───────────────────────────────────────────────────────────

        /// <summary>
        ///     Appends a command string to the history ring, skipping blank lines
        ///     and consecutive duplicates. Evicts the oldest entry when the ring
        ///     exceeds <see cref="MaxHistory" />.
        /// </summary>
        private void PushHistory(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            if (_history.Count > 0 && _history[^1] == line) return; // Skip duplicate of most recent.

            _history.Add(line);
            if (_history.Count > MaxHistory)
                _history.RemoveAt(0);
        }

        /// <summary>
        ///     Navigates one step backward through history (toward older commands).
        ///     Saves the current live input on the first press so it can be restored
        ///     when the user navigates back down past the most recent entry.
        /// </summary>
        private void HistoryUp()
        {
            if (_history.Count == 0) return;

            if (_historyIndex == -1)
            {
                // First press up: save the current draft before entering history.
                _liveLine = _currentLine;
                _historyIndex = _history.Count - 1;
            }
            else if (_historyIndex > 0)
            {
                _historyIndex--;
            }

            SetCurrentLine(_history[_historyIndex]);
        }

        /// <summary>
        ///     Navigates one step forward through history (toward newer commands).
        ///     Restores the saved live draft when advancing past the most recent entry.
        /// </summary>
        private void HistoryDown()
        {
            if (_historyIndex == -1) return;

            _historyIndex++;

            if (_historyIndex >= _history.Count)
            {
                // Navigated past the end of history: restore the live draft.
                _historyIndex = -1;
                SetCurrentLine(_liveLine);
            }
            else
            {
                SetCurrentLine(_history[_historyIndex]);
            }
        }

        /// <summary>
        ///     Replaces the current input line and moves the cursor to the end.
        ///     Used by history navigation to populate the input field.
        /// </summary>
        private void SetCurrentLine(string line)
        {
            _currentLine = line;
            _cursorPos = line.Length;
        }

        // ── Pager ─────────────────────────────────────────────────────────────

        /// <summary>
        ///     Enters paged display mode for <paramref name="text" />, splitting it
        ///     into lines and rendering the first page. Normal input is suppressed
        ///     until the user quits the pager.
        /// </summary>
        private void ShowPaged(string text)
        {
            _pageLines = text.Split('\n').ToList();
            _pageOffset = 0;
            _isPaging = true;
            RenderPage();
        }

        /// <summary>
        ///     Renders the current page of content to the TMP_Text component.
        ///     Shows a "--MORE--" footer if more content follows, or "--END--"
        ///     on the final page.
        /// </summary>
        private void RenderPage()
        {
            // Reserve one row for the pager footer.
            var usableRows = _commandHandler.Rows;

            var visible = _pageLines.Skip(_pageOffset).Take(usableRows).ToList();
            var hasMore = _pageOffset + visible.Count < _pageLines.Count - 1;

            _fullText = string.Join("\n", visible) + "\n";
            var footer = hasMore
                ? "-- MORE -- (Space/Enter: next page, Q: quit)"
                : "-- END --  (Q: quit)";

            _cli.SetText(_fullText + footer);
        }

        /// <summary>
        ///     Handles all keyboard input that is not printable text:
        ///     pager navigation, cursor movement, history browsing, Delete, and Enter.
        /// </summary>
        private void HandleSpecialKeys()
        {
            if (Keyboard.current == null) return;
            if (_commandHandler.IsProcessingStartup) return;

            // ── Pager mode ────────────────────────────────────────────────────
            // While paging, only Q and Space/Enter are processed; all other
            // input is swallowed to prevent accidental command execution.
            if (_isPaging)
            {
                var usableRows = _commandHandler.Rows;

                var visible = _pageLines.Skip(_pageOffset).Take(usableRows).ToList();
                var hasMore = _pageOffset + visible.Count < _pageLines.Count - 1;

                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    // Q quits the pager and clears the screen.
                    _isPaging = false;
                    ClearScreen();
                }
                else if ((Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) && hasMore)
                {
                    _pageOffset += usableRows;
                    RenderPage();
                }

                return;
            }

            // ── Cursor movement ───────────────────────────────────────────────
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                if (_cursorPos > 0)
                {
                    _cursorPos--;
                    ResetCursorBlink();
                    UpdateText();
                }
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                if (_cursorPos < _currentLine.Length)
                {
                    _cursorPos++;
                    ResetCursorBlink();
                    UpdateText();
                }
            }
            else if (Keyboard.current.homeKey.wasPressedThisFrame)
            {
                _cursorPos = 0;
                ResetCursorBlink();
                UpdateText();
            }
            else if (Keyboard.current.endKey.wasPressedThisFrame)
            {
                _cursorPos = _currentLine.Length;
                ResetCursorBlink();
                UpdateText();
            }

            // ── History ───────────────────────────────────────────────────────
            else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                HistoryUp();
                ResetCursorBlink();
                UpdateText();
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                HistoryDown();
                ResetCursorBlink();
                UpdateText();
            }

            // ── Delete (forward) ──────────────────────────────────────────────
            // Removes the character at the cursor position (not behind it).
            else if (Keyboard.current.deleteKey.wasPressedThisFrame)
            {
                if (_cursorPos < _currentLine.Length)
                {
                    _currentLine = _currentLine.Remove(_cursorPos, 1);
                    _historyIndex = -1;
                    ResetCursorBlink();
                    UpdateText();
                }
            }

            // ── Enter ─────────────────────────────────────────────────────────
            else if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                var trimmed = _currentLine.Trim();

                // Commit the current line to the output buffer before execution
                // so it appears in the scroll-back as part of the session transcript.
                _fullText += _linePrefix + _currentLine + "\n";

                PushHistory(trimmed);

                if (trimmed.StartsWith("ssh"))
                {
                    var parameters = trimmed.Split(' ');
                    parameters[0] = parameters[1].Split('@')[0];
                    parameters[1] = parameters[1].Split('@')[1];
                    ChangeHandler("ssh", parameters[0], parameters[1], parameters[2]);
                }
                else if (trimmed.StartsWith("quit"))
                {
                    ChangeHandler("simulated");
                }
                else
                {
                    _commandHandler.Execute(trimmed);
                }

                // Reset all input state for the next command.
                _currentLine = "";
                _cursorPos = 0;
                _historyIndex = -1;
                _liveLine = "";
                UpdateText();
            }
        }

        // ── Render ────────────────────────────────────────────────────────────

        /// <summary>
        ///     Redraws the TMP_Text component with the accumulated output buffer
        ///     followed by the current input line and blinking cursor.
        ///     No-ops while the pager is active (pager manages its own rendering).
        /// </summary>
        private void UpdateText()
        {
            if (_isPaging) return;
            if (_commandHandler.IsProcessingStartup) return;

            var before = _currentLine[.._cursorPos];
            var after = _currentLine[_cursorPos..];

            // Show the block cursor or a space placeholder depending on blink state.
            var cursor = _cursorVisible ? CursorChar : " ";

            _cli.SetText(_fullText + _linePrefix + before + cursor + after);
        }

        /// <summary>
        ///     Appends a completed line to the output buffer and refreshes the display.
        ///     Injected into the command handler as the <c>SetWriter</c> callback.
        /// </summary>
        private void WriteLine(string text)
        {
            _fullText += text + "\n";
            UpdateText();
        }

        /// <summary>
        ///     Resets the output buffer and current input line, then refreshes the display.
        ///     Injected into the command handler via <c>SetScreenController</c>.
        /// </summary>
        private void ClearScreen()
        {
            _fullText = "";
            _currentLine = "";
            _cursorPos = 0;
            UpdateText();
        }

        // ── Dimensions ────────────────────────────────────────────────────────

        /// <summary>
        ///     Measures the TMP_Text component's rendered character dimensions and
        ///     calculates how many columns and rows fit within the RectTransform.
        ///     Uses a single "W" character as a reference glyph, as it represents
        ///     the maximum width in a monospaced context.
        /// </summary>
        private void CalculateConsoleDimensionsPrecise()
        {
            _cli.ForceMeshUpdate();
            _cli.text = "W";
            _cli.ForceMeshUpdate();

            var charWidth = _cli.renderedWidth;
            var charHeight = _cli.renderedHeight;

            var rect = textField.GetComponent<RectTransform>();
            _charCol = Mathf.FloorToInt(rect.rect.width / charWidth);
            _charRow = Mathf.FloorToInt(rect.rect.height / charHeight);

            _cli.text = "";
        }
    }
}