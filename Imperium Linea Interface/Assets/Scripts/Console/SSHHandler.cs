using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Abstract.Console;
using Renci.SshNet;

namespace Console
{
    /// <summary>
    ///     Concrete command handler that connects to a real SSH server using
    ///     SSH.NET and forwards all user input to a remote shell stream.
    ///     Incoming shell output is read frame-by-frame and written to the console.
    /// </summary>
    public class SshHandler : ACommandHandler<SshHandler>
    {
        /// <summary>The active SSH.NET client managing the TCP connection.</summary>
        private SshClient _client;

        // ── SSH credentials ───────────────────────────────────────────────────
        // These are hardcoded for local development. In a production build,
        // supply them via a config file, environment variables, or a UI prompt.
        private string _command = "";

        /// <summary>Hostname or IP address of the SSH server.</summary>
        private string _host = "";

        /// <summary>Password credential for the SSH connection.</summary>
        private string _password = "";

        /// <summary>
        ///     Interactive shell stream opened on the SSH channel.
        ///     All user input is written here; all output is read from here.
        /// </summary>
        private ShellStream _shellStream;

        /// <summary>Username credential for the SSH connection.</summary>
        private string _username = "";

        /// <summary>
        ///     Initialises the command registry, then starts the startup coroutine
        ///     which attempts the SSH connection.
        /// </summary>
        protected override void OnStartUp()
        {
            RunCoroutine(PlayStartupSequence());
        }

        public override void SetUsername(string username)
        {
            _username = username;
        }

        public override void SetHost(string server)
        {
            _host = server;
        }

        public override void SetPassword(string password)
        {
            _password = password;
        }

        /// <summary>
        ///     Coroutine that attempts an SSH connection and configures the shell
        ///     stream if successful. Writes status messages to the console regardless
        ///     of outcome. Calls <see cref="ACommandHandler{T}.FinishStartup" /> when
        ///     done to unblock user input.
        ///     Note: yield cannot appear inside a try/catch block in C#, so a flag
        ///     is used to carry the error state out of the try/catch before acting on it.
        /// </summary>
        private IEnumerator PlayStartupSequence()
        {
            // Carry any connection error out of the try/catch so we can yield after it
            string connectionError = null;

            try
            {
                _client = new SshClient(_host, _username, _password);
                _client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(5);

                WriteLine("Attempting SSH connection...");
                WriteLine("Connecting to " + _username + "@" + _host);
                _client.Connect();

                if (_client.IsConnected)
                    // Open a PTY-backed shell stream so the remote side allocates
                    // a proper terminal (required for interactive shells and ANSI output).
                    _shellStream = _client.CreateShellStream(
                        "xterm", // Terminal type advertised to the remote host.
                        80, // Terminal columns.
                        24, // Terminal rows.
                        800, // Pixel width (informational only).
                        600, // Pixel height (informational only).
                        1024 // Read buffer size in bytes.
                    );
                else
                    connectionError = "SSH connection could not be established.";
            }
            catch (Exception e)
            {
                // Store the error message — we cannot yield or call FinishStartup here
                connectionError = e.Message;
            }

            // Now outside the try/catch, safe to yield and call FinishStartup
            if (connectionError != null)
            {
                WriteLine("Error connecting to Server. " + _host);
                WriteLine(" ");
                WriteLine("Type 'quit' to exit.");
                WriteLine(" ");

                FinishStartup();
                yield break;
            }

            // Connection succeeded — start the background output reader
            RunCoroutine(ReadShellOutput());

            WriteLine("");
            WriteLine("Type 'help' for available commands.");
            WriteLine("");

            FinishStartup();
        }

        /// <summary>
        ///     Frame-by-frame coroutine that polls the shell stream for available
        ///     data and writes any received bytes to the console as UTF-8 text.
        ///     Strips ANSI escape sequences and shell prompts before display.
        ///     Runs until the SSH connection is closed or the client is null.
        /// </summary>
        private IEnumerator ReadShellOutput()
        {
            var buffer = new byte[4096];

            while (_client != null && _client.IsConnected && _shellStream != null)
            {
                if (_shellStream.DataAvailable)
                {
                    var bytesRead = _shellStream.Read(buffer, 0, buffer.Length);
                    var raw = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Clean the output before writing so the TMP display stays readable
                    var cleaned = StripAnsiCodes(raw);

                    // Only write if something visible remains after cleaning
                    if (!string.IsNullOrWhiteSpace(cleaned))
                        WriteLine(cleaned);
                }

                // Yield each frame to avoid blocking the Unity main thread
                yield return null;
            }
        }

        /// <summary>
        ///     Strips ANSI/VT escape sequences, OSC sequences, Windows shell prompts,
        ///     and other terminal control noise from <paramref name="input" /> so that
        ///     only plain readable text reaches the TMP_Text renderer.
        /// </summary>
        private string StripAnsiCodes(string input)
        {
            // CSI sequences: covers colors, cursor movement, erase, private modes
            // e.g. ESC[?25l  ESC[2J  ESC[?9001h  ESC[4;1H  ESC[m
            input = Regex.Replace(
                input,
                @"\x1B\[[0-9;?]*[ -/]*[@-~]",
                string.Empty
            );

            input = Regex.Replace(
                input,
                _command,
                string.Empty
            );
            _command = "";

            // OSC sequences: window title changes etc.
            // e.g. ESC]0;C:\WINDOWS\system32\conhost.exe BEL
            input = Regex.Replace(
                input,
                @"\x1B\][^\x07]*\x07",
                string.Empty
            );

            // Any remaining bare ESC characters
            input = Regex.Replace(
                input,
                @"\x1B",
                string.Empty
            );

            // Windows cmd/PowerShell prompts:  user@HOST C:\path\to\dir>
            // Matches the full prompt line so it doesn't appear in output
            input = Regex.Replace(
                input,
                @"[\w.-]+@[\w.-]+\s+[A-Za-z]:\\[^\r\n]*>",
                string.Empty,
                RegexOptions.Multiline
            );

            // Collapse runs of blank lines left behind after stripping into one blank
            input = Regex.Replace(
                input,
                @"(\r?\n){2,}",
                "\n"
            );

            return input.Trim();
        }

        /// <summary>
        ///     Writes <paramref name="command" /> directly to the remote shell stream.
        ///     Writes an error to the console if the connection has been lost or if
        ///     the write itself fails.
        /// </summary>
        protected override void SendCommand(string command)
        {
            if (_client != null && _client.IsConnected)
                try
                {
                    _shellStream.WriteLine(command);
                    _command = command;
                }
                catch (Exception e)
                {
                    WriteLine("Command Error: " + e.Message);
                }
            else
                WriteLine("Not connected to SSH server.");
        }
    }
}