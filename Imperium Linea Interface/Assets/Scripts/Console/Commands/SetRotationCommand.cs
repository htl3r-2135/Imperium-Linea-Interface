using Abstract.Console;
using Attributes;
using Platform;
using UnityEngine;

namespace Console.Commands
{
    /// <summary>
    ///     Arguments for the rotate command.
    /// </summary>
    public class RotateArgs
    {
        /// <summary>
        ///     Angle to rotate by in degrees. Defaults to 90.
        /// </summary>
        [CommandArg(0, 90f)] public float Angle;
    }

    /// <summary>
    ///     Console command that rotates the platform by a given angle.
    ///     Registered automatically via the <c>[ConsoleCommand]</c> attribute.
    ///     Usage: <c>rotate [angle]</c>
    /// </summary>
    [ConsoleCommand]
    public class SetRotationCommand : ACommand<RotateArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "rotate";

        /// <inheritdoc />
        public override string ShortDescription => "Rotate the platform by an angle.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Rotates the platform by the specified number of degrees.\n\n" +
            "Usage: rotate [angle]\n\n" +
            "Arguments:\n" +
            "  angle   Degrees to rotate by, range -360 to 360. " +
            "Positive = clockwise, negative = counter-clockwise. Default: 90";

        /// <summary>
        ///     Applies the rotation to the platform immediately.
        /// </summary>
        protected override void Execute(RotateArgs args)
        {
            GameLogger.Instance.LogInfo($"Command: rotate {args.Angle}", "Console");

            var controller = PlatformController.Instance;
            if (controller == null)
            {
                GameLogger.Instance.LogError("PlatformController not found", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    "Error: PlatformController not found in scene."
                );
                return;
            }

            if (controller.IsRotating)
            {
                GameLogger.Instance.LogWarning("Rotate ignored - already rotating", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    "Platform already rotating... command queued or ignored"
                );
                return;
            }

            var clampedAngle = Mathf.Clamp(args.Angle, -360f, 360f);

            if (clampedAngle != args.Angle)
                GameLogger.Instance.LogDebug(
                    $"Angle clamped from {args.Angle} to {clampedAngle}",
                    "Console"
                );

            controller.RotateBy(clampedAngle);

            var direction = clampedAngle > 0 ? "clockwise" :
                clampedAngle < 0 ? "counter-clockwise" : "none";

            GameLogger.Instance.LogInfo(
                $"Platform rotated {clampedAngle}° ({direction})",
                "Console"
            );

            SimulatedHandler.Instance.WriteCommandOutput(
                $"Rotated platform by {clampedAngle:F0}° ({direction})"
            );
        }
    }
}