using Abstract.Console;
using Attributes;
using Platform;

namespace Console.Commands
{
    /// <summary>
    ///     Console command that displays the current platform rotation.
    ///     Registered automatically via the <c>[ConsoleCommand]</c> attribute.
    ///     Usage: <c>rotation</c>
    /// </summary>
    [ConsoleCommand]
    public class GetRotationCommand : ACommand<NoArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "rotation";

        /// <inheritdoc />
        public override string ShortDescription => "Get current platform rotation.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Displays the current platform Y-axis rotation in degrees.\n\n" +
            "Usage: rotation";

        /// <summary>
        ///     Outputs the current platform rotation to the console.
        /// </summary>
        /// <param name="_"></param>
        protected override void Execute(NoArgs _)
        {
            GameLogger.Instance.LogInfo("Command: rotation", "Console");

            var controller = PlatformController.Instance;
            if (controller == null)
            {
                GameLogger.Instance.LogError("PlatformController not found", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    "Error: PlatformController not found in scene."
                );
                return;
            }

            var yRot = controller.CurrentYRotation;

            GameLogger.Instance.LogDebug($"Current rotation: {yRot}", "Console");

            SimulatedHandler.Instance.WriteCommandOutput(
                $"Platform rotation: {yRot:F1}°"
            );
        }
    }
}