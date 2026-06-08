using Abstract.Console;
using Attributes;
using UnityEngine;

namespace Console.Commands
{
    /// <summary>
    ///     Console command that changes the background color of the console screen.
    ///     Registered automatically via the <c>[ConsoleCommand]</c> attribute.
    ///     Usage: <c>color-bg #RRGGBB</c>
    /// </summary>
    [ConsoleCommand]
    public class SetBackgroundColorCommand : ACommand<ColorArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "color-bg";

        /// <inheritdoc />
        public override string ShortDescription => "Set the background color.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Sets the console background color.\n\nUsage: color-bg <hex>\n\nArguments:\n  hex   Color in hex format, e.g. #1A1A2E";

        /// <summary>
        ///     Parses the hex color from <paramref name="args" /> and applies it as the
        ///     console background. Writes an error message to the console if the
        ///     hex string is malformed instead of throwing an exception.
        /// </summary>
        protected override void Execute(ColorArgs args)
        {
            GameLogger.Instance.LogInfo($"Command: color-bg {args.HexCode}", "Console");

            if (!ColorUtility.TryParseHtmlString(args.HexCode, out var color))
            {
                GameLogger.Instance.LogWarning($"Invalid background color: {args.HexCode}", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    $"Invalid hex color: '{args.HexCode}'\nExpected format: #RRGGBB or #RGB"
                );
                return;
            }

            SimulatedHandler.Instance.SetBackgroundColor(color);

            GameLogger.Instance.LogInfo($"Background color set to {args.HexCode}", "Console");

            SimulatedHandler.Instance.WriteCommandOutput(
                $"Background color set to {args.HexCode}"
            );
        }
    }
}