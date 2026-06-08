using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Abstract;
using Abstract.Console;
using Attributes;

namespace Console.CommandUtility
{
    /// <summary>
    ///     Singleton registry that discovers, stores, and dispatches all console
    ///     commands. Commands are auto-discovered at startup via reflection using
    ///     the <c>[ConsoleCommand]</c> attribute.
    /// </summary>
    public class CommandCollector : Singleton<CommandCollector>
    {
        /// <summary>
        ///     Map of command name tokens to their <see cref="ACommand" /> instances.
        ///     Populated by <see cref="CollectCommands" />.
        /// </summary>
        public Dictionary<string, ACommand> Commands { get; } = new();

        /// <summary>
        ///     Registers a command instance in the registry, overwriting any
        ///     existing entry with the same <see cref="ACommand.CommandName" />.
        /// </summary>
        private void Register(ACommand command)
        {
            if (Commands.ContainsKey(command.CommandName))
                GameLogger.Instance.LogWarning(
                    $"Command '{command.CommandName}' overridden",
                    "Console"
                );
            else
                GameLogger.Instance.LogDebug(
                    $"Registering command '{command.CommandName}'",
                    "Console"
                );

            Commands[command.CommandName] = command;
        }

        /// <summary>
        ///     Parses <paramref name="input" /> into a command name and argument
        ///     tokens, looks up the command in the registry, and executes it.
        ///     Writes an error message to the console if the command is not found.
        /// </summary>
        public void Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            GameLogger.Instance.LogInfo($"RAW INPUT: \"{input}\"", "Console");

            var split = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var commandName = split[0];

            var args = split.Skip(1).ToArray();

            GameLogger.Instance.LogDebug(
                $"Parsed -> Command: '{commandName}', Args: [{string.Join(", ", args)}]",
                "Console"
            );

            if (!Commands.TryGetValue(commandName, out var command))
            {
                GameLogger.Instance.LogWarning($"Unknown command: {commandName}", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    $"Unknown command: '{commandName}'"
                );
                return;
            }

            try
            {
                GameLogger.Instance.LogDebug($"Executing command: {commandName}", "Console");

                command.Execute(args);
            }
            catch (Exception ex)
            {
                GameLogger.Instance.LogError(
                    $"Command '{commandName}' crashed: {ex.Message}",
                    "Console"
                );

                SimulatedHandler.Instance.WriteCommandOutput(
                    $"Error executing command '{commandName}'"
                );
            }
        }

        /// <summary>
        ///     Populates the command registry. Manually registers built-in commands
        ///     (e.g. <see cref="HelpCommand" />), then scans all loaded assemblies for
        ///     types decorated with <c>[ConsoleCommand]</c> and registers them via
        ///     <see cref="Activator.CreateInstance" />.
        /// </summary>
        public void CollectCommands()
        {
            GameLogger.Instance.LogInfo("Collecting console commands...", "Console");

            // Built-in
            Register(new HelpCommand());

            var discovered = 0;
            var failed = 0;

            var commandTypes =
                AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a =>
                    {
                        try
                        {
                            return a.GetTypes();
                        }
                        catch
                        {
                            return Array.Empty<Type>();
                        } // prevent reflection crash
                    })
                    .Where(t => t.GetCustomAttribute<ConsoleCommandAttribute>() != null);

            foreach (var type in commandTypes)
                try
                {
                    var command = (ACommand)Activator.CreateInstance(type);

                    Register(command);
                    discovered++;
                }
                catch (Exception ex)
                {
                    failed++;

                    GameLogger.Instance.LogError(
                        $"Failed to instantiate command '{type.FullName}': {ex.Message}",
                        "Console"
                    );
                }

            GameLogger.Instance.LogInfo(
                $"Command collection complete: {discovered} loaded, {failed} failed",
                "Console"
            );
        }

        /// <summary>
        ///     Returns the <see cref="ACommand" /> registered under
        ///     <paramref name="commandName" />, or null if no such command exists.
        /// </summary>
        public ACommand GetCommand(string commandName)
        {
            var cmd = Commands.GetValueOrDefault(commandName);

            if (cmd == null)
                GameLogger.Instance.LogDebug(
                    $"GetCommand: '{commandName}' not found",
                    "Console"
                );

            return cmd;
        }

        public T GetCommand<T>() where T : ACommand
        {
            return Commands.Values.OfType<T>().FirstOrDefault();
        }
    }

    /// <summary>
    ///     Argument struct for the <c>help</c> command.
    ///     The optional positional argument specifies a command to show detail for.
    /// </summary>
    public struct HelpArgs
    {
        /// <summary>
        ///     Name of the command to show detailed help for.
        ///     Empty string means no specific command was requested (show the full list).
        /// </summary>
        [CommandArg(0, "")] public string CommandName;
    }

    /// <summary>
    ///     Built-in command that renders either a paginated list of all registered
    ///     commands or detailed help for a single command.
    ///     Usage: <c>help [command]</c>
    /// </summary>
    internal class HelpCommand : ACommand<HelpArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "help";

        /// <inheritdoc />
        public override string ShortDescription => "Shows available commands.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Shows a help menu with all available commands.\n" +
            "\n" +
            "Usage: help [command]\n" +
            "\n" +
            "Arguments:\n" +
            "  command   (optional) Command to show detailed help for.";

        /// <summary>
        ///     Delegates to <see cref="ShowCommandList" /> when no specific command
        ///     is requested, or <see cref="ShowCommandDetail" /> otherwise.
        /// </summary>
        protected override void Execute(HelpArgs args)
        {
            GameLogger.Instance.LogInfo(
                string.IsNullOrWhiteSpace(args.CommandName)
                    ? "Command: help (list)"
                    : $"Command: help {args.CommandName}",
                "Console"
            );

            if (string.IsNullOrWhiteSpace(args.CommandName))
                ShowCommandList();
            else
                ShowCommandDetail(args.CommandName);
        }

        // ── Command List ──────────────────────────────────────────────────────

        /// <summary>
        ///     Builds a two-column table of all registered commands sorted
        ///     alphabetically and sends it to the paged viewer.
        ///     Column widths are calculated dynamically from the terminal width
        ///     and the longest command name in the registry.
        /// </summary>
        private void ShowCommandList()
        {
            var commands = CommandCollector.Instance.Commands
                .OrderBy(c => c.Key)
                .ToList();

            var cols = SimulatedHandler.Instance.Columns;

            const string col1Name = "command";
            const string col2Name = "description";

            // Padding + spacing between columns
            const int padding = 2;
            const int gap = 3;

            // Column 1 width: max of header or command names, but capped
            var nameWidth = Math.Min(
                Math.Max(commands.Max(c => c.Key.Length), col1Name.Length),
                30 // prevents huge command names from breaking layout
            );

            var descWidth = Math.Max(10, cols - nameWidth - gap - padding * 2);

            var sb = new StringBuilder();

            sb.AppendLine(HorizontalRule("Available Commands", cols));
            sb.AppendLine();

            // Header
            sb.AppendLine(
                $"  {col1Name}{new string(' ', gap + (nameWidth - col1Name.Length))}{col2Name}"
            );

            // Separator (matches actual widths)
            sb.AppendLine(
                $"  {new string('-', nameWidth)}{new string(' ', gap)}{new string('-', Math.Min(descWidth, col2Name.Length))}"
            );

            foreach (var (name, command) in commands)
            {
                var cmd = Truncate(name, nameWidth);
                var desc = Truncate(command.ShortDescription, descWidth);

                var deltaCmd = (nameWidth - name.Length);

                var commandGap = (int)Math.Floor(deltaCmd / 2.0);

                sb.AppendLine($"  {cmd}{new string(' ', gap + deltaCmd  + commandGap)}{desc}");
            }
            sb.AppendLine(HorizontalRule("help <command> for details", cols));

            SimulatedHandler.Instance.ShowPaged(sb.ToString());
        }

        // ── Command Detail ────────────────────────────────────────────────────

        /// <summary>
        ///     Renders the full long-form help text for a single command, wrapped
        ///     to fit the terminal width, and writes it directly to the console output.
        /// </summary>
        private void ShowCommandDetail(string commandName)
        {
            var command = CommandCollector.Instance.GetCommand(commandName);
            if (command == null)
            {
                GameLogger.Instance.LogWarning(
                    $"Help requested for unknown command: {commandName}",
                    "Console"
                );

                SimulatedHandler.Instance.WriteCommandOutput(
                    $"Unknown command: '{commandName}'\nType 'help' for a list of available commands."
                );
                return;
            }

            var cols = SimulatedHandler.Instance.Columns;

            var bodyWidth = cols - 4;

            var sb = new StringBuilder();

            sb.AppendLine(HorizontalRule($"help: {command.CommandName}", cols));
            sb.AppendLine();

            foreach (var line in WrapText(command.LongDescription, bodyWidth))
                sb.AppendLine("  " + line);

            sb.AppendLine();
            sb.AppendLine(new string('─', cols));

            SimulatedHandler.Instance.WriteCommandOutput(sb.ToString());
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        ///     Produces a titled horizontal rule of exactly <paramref name="totalWidth" />
        ///     characters: <c>─── Title ──────────────────</c>.
        /// </summary>
        private static string HorizontalRule(string title, int totalWidth)
        {
            const int leadDashes = 3;
            var lead = new string('─', leadDashes);
            var label = $" {title} ";
            var remaining = totalWidth - leadDashes - label.Length;
            var tail = remaining > 0 ? new string('─', remaining) : "";
            return lead + label + tail;
        }

        /// <summary>
        ///     Truncates <paramref name="text" /> to at most <paramref name="maxWidth" />
        ///     characters, appending an ellipsis if truncation occurs.
        ///     Returns an empty string if <paramref name="maxWidth" /> is zero or negative.
        /// </summary>
        private static string Truncate(string text, int maxWidth)
        {
            if (maxWidth <= 0) return "";
            if (text.Length <= maxWidth) return text;
            return text[..(maxWidth - 1)] + "…";
        }

        /// <summary>
        ///     Wraps <paramref name="text" /> to lines of at most
        ///     <paramref name="maxWidth" /> characters, breaking on word boundaries.
        ///     Preserves blank lines from the source text.
        /// </summary>
        private static IEnumerable<string> WrapText(string text, int maxWidth)
        {
            foreach (var paragraph in text.Split('\n'))
            {
                if (string.IsNullOrEmpty(paragraph))
                {
                    yield return "";
                    continue;
                }

                if (paragraph.Length <= maxWidth)
                {
                    yield return paragraph;
                    continue;
                }

                var words = paragraph.Split(' ');
                var current = new StringBuilder();

                foreach (var word in words)
                {
                    if (current.Length > 0 && current.Length + word.Length + 1 > maxWidth)
                    {
                        yield return current.ToString().TrimEnd();
                        current.Clear();
                    }

                    current.Append(word + " ");
                }

                if (current.Length > 0)
                    yield return current.ToString().TrimEnd();
            }
        }
    }
}