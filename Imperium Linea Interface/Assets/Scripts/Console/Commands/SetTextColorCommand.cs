using Abstract.Console;
using Attributes;
using UnityEngine;

namespace Console.Commands
{
    /// <summary>
    ///     Shared argument struct for color commands.
    ///     The single positional argument is the hex color string.
    /// </summary>
    public struct ColorArgs
    {
        /// <summary>
        ///     Hex color string supplied as the first positional argument.
        ///     Expected formats: <c>#RRGGBB</c> or <c>#RGB</c>.
        /// </summary>
        [CommandArg(0, "#RRGGBB")] public string HexCode;
    }

    /// <summary>
    ///     Console command that changes the foreground/text color of the console.
    ///     Registered automatically via the <c>[ConsoleCommand]</c> attribute.
    ///     Usage: <c>color-text #RRGGBB</c>
    /// </summary>
    [ConsoleCommand]
    public class SetTextColorCommand : ACommand<ColorArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "color-text";

        /// <inheritdoc />
        public override string ShortDescription => "Set the text color.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Sets the console text color.\n\nUsage: color-text <hex>\n\nArguments:\n  hex   Color in hex format, e.g. #00FF00";

        /// <summary>
        ///     Parses the hex color from <paramref name="args" /> and applies it as the
        ///     console text color. Writes an error message to the console if the
        ///     hex string is malformed instead of throwing an exception.
        /// </summary>
        protected override void Execute(ColorArgs args)
        {
            GameLogger.Instance.LogInfo($"Command: color-text {args.HexCode}", "Console");

            if (!ColorUtility.TryParseHtmlString(args.HexCode, out var color))
            {
                GameLogger.Instance.LogWarning($"Invalid text color: {args.HexCode}", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    $"Invalid hex color: '{args.HexCode}'\nExpected format: #RRGGBB or #RGB"
                );
                return;
            }

            SimulatedHandler.Instance.SetTextColor(color);

            GameLogger.Instance.LogInfo($"Text color set to {args.HexCode}", "Console");

            SimulatedHandler.Instance.WriteCommandOutput(
                $"Text color set to {args.HexCode}"
            );
        }
    }
}