using Abstract.Console;
using Attributes;
using Doors;
using Platform;
using UnityEngine;

namespace Console.Commands
{
    /// <summary>
    ///     Arguments for the close-door command.
    /// </summary>
    public class DoorArgs
    {
        /// <summary>
        ///     Which door to close. 0 = Back, 1 = Front. Defaults to 0.
        /// </summary>
        [CommandArg(0)] public int Door;
    }

    [ConsoleCommand]
    public class CloseDoorCommand : ACommand<DoorArgs>
    {
        /// <inheritdoc />
        public override string CommandName => "close-door";

        /// <inheritdoc />
        public override string ShortDescription => "Close a door by index.";

        /// <inheritdoc />
        public override string LongDescription =>
            "Closes the specified door.\n\n" +
            "Usage: close-door [door]\n\n" +
            "Arguments:\n" +
            "  door   Which door to close. 0 = Back, 1 = Front. Default: 0";

        /// <summary>
        ///     Closes the specified door via DoorController.
        /// </summary>
        protected override void Execute(DoorArgs args)
        {
            GameLogger.Instance.LogInfo($"Command: close-door {args.Door}", "Console");

            var controller = DoorController.Instance;
            if (controller == null)
            {
                GameLogger.Instance.LogError("DoorController not found", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    "Error: DoorController not found in scene."
                );
                return;
            }

            if (args.Door is < 0 or > 1)
            {
                GameLogger.Instance.LogWarning($"Invalid door index: {args.Door}", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    $"Error: Invalid door index '{args.Door}'. Use 0 (Back) or 1 (Front)."
                );
                return;
            }

            var door = (DoorIndexes)args.Door;

            GameLogger.Instance.LogDebug($"Closing door: {door}", "Console");

            var platformController = PlatformController.Instance;
            if (platformController == null)
            {
                GameLogger.Instance.LogError("PlatformController not found", "Console");

                SimulatedHandler.Instance.WriteCommandOutput(
                    "Error: PlatformController not found in scene."
                );
                return;
            }

            var yRot = platformController.CurrentYRotation;

            if ((IsNear(yRot, 0f) && args.Door == 1) || (IsNear(yRot, 180f) && args.Door == 0))
                controller.CloseDoor(door, true);
            else
                controller.CloseDoor(door);

            SimulatedHandler.Instance.WriteCommandOutput(
                DoorController.Instance.isLocked[args.Door] > 0 || DoorController.Instance.IsDoorMoving(door)
                    ? $"Error: Door at index '{args.Door}' is currently locked for '{DoorController.Instance.isLocked[args.Door]}' s... please try again."
                    : $"Closing {door} door."
            );
            return;

            bool IsNear(float value, float target)
            {
                return Mathf.Abs(Mathf.DeltaAngle(value, target)) <= 10f;
            }
        }
    }
}