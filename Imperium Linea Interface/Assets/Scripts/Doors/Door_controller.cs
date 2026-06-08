using System.Collections;
using Abstract;
using CameraCtrl;
using Tutorial;
using UnityEngine;

namespace Doors
{
    /// <summary>
    ///     Identifies which door is being referenced.
    /// </summary>
    public enum DoorIndexes
    {
        Right = 0,
        Left = 1
    }

    /// <summary>
    ///     Represents the possible states a door can be in at any given time.
    /// </summary>
    public enum DoorStates
    {
        /// <summary>Temporarily locked after opening to prevent immediate re-closing.</summary>
        Locked,

        /// <summary>Fully open and idle.</summary>
        Open,

        /// <summary>Currently animating to the open position.</summary>
        Opening,

        /// <summary>Fully closed and idle.</summary>
        Closed,

        /// <summary>Currently animating to the closed position.</summary>
        Closing
    }

    /// <summary>
    ///     Manages the movement, state, and locking behaviour of the left and right doors.
    ///     Runs as a singleton so any system can request a door open or close.
    /// </summary>
    public class DoorController : MonoSingleton<DoorController>
    {
        /// <summary>The Y position a door moves to when fully open.</summary>
        private const float OpenPos = 3.75f;

        /// <summary>The Y position a door moves to when fully closed.</summary>
        private const float ClosedPos = 1.35f;

        /// <summary>Seconds a door stays locked after opening before it can close again.</summary>
        private const float LockTimer = 5f;
        
        /// <summary>Units per second used when closing a door.</summary>
        private const float CloseSpeed = 8f;

        private const float OpenSpeed = (OpenPos - ClosedPos) / (10 - LockTimer - ((OpenPos - ClosedPos) / CloseSpeed));


        /// <summary>References to the two door GameObjects, indexed by <see cref="DoorIndexes"/>.</summary>
        private GameObject[] _doorObjects;

        /// <summary>Current state of each door, indexed by <see cref="DoorIndexes"/>. Both start Open.</summary>
        private readonly DoorStates[] _states = { DoorStates.Open, DoorStates.Open };

        /// <summary>
        ///     Per-door movement flags. Using separate booleans avoids the race condition
        ///     where one door finishing its animation would clear the shared flag while
        ///     the other door is still moving.
        /// </summary>
        private readonly bool[] _isMoving = { false, false };

        /// <summary>Remaining lock time in seconds for each door before it transitions back to Open.</summary>
        private readonly float[] _lockedIn = { LockTimer, LockTimer };

        /// <summary>
        ///     Public snapshot of the remaining lock time for each door.
        ///     Exposed so external systems (e.g. UI, tutorial) can read lock progress.
        /// </summary>
        public double[] isLocked = { 0, 0 };

        public bool IsMoving => _isMoving[0] || _isMoving[1];
        public bool IsDoorMoving(DoorIndexes door) => _isMoving[(int)door];

        public bool AnyDoorBusy =>
            _states[0] is DoorStates.Opening or DoorStates.Closing or DoorStates.Locked ||
            _states[1] is DoorStates.Opening or DoorStates.Closing or DoorStates.Locked;

        /// <summary>Cached reference to the camera controller singleton.</summary>
        private CameraControl _cameraControl;

        /// <summary>Cached reference to the tutorial singleton.</summary>
        private TutorialSingleton _tutorialSingleton;

        /// <summary>
        ///     Resolves singleton dependencies, locates door GameObjects by name,
        ///     and logs a hard error if either door is missing from the scene.
        /// </summary>
        public void Start()
        {
            _tutorialSingleton = TutorialSingleton.Instance;
            _cameraControl = CameraControl.Instance;

            _doorObjects = new[]
            {
                GameObject.Find("DoorRight"),
                GameObject.Find("DoorLeft")
            };

            if (_doorObjects[0] == null || _doorObjects[1] == null)
                GameLogger.Instance.LogError("One or more door GameObjects not found in scene!", "Doors");

            GameLogger.Instance.LogInfo("DoorController initialized", "Doors");
        }

        /// <summary>
        ///     Drives two per-frame behaviours each tick:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Automatically reopens any door that has reached the
        ///                 <see cref="DoorStates.Closed"/> state, provided the tutorial
        ///                 does not currently block door opening.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 Counts down the post-open lock timer for any door in the
        ///                 <see cref="DoorStates.Locked"/> state and transitions it to
        ///                 <see cref="DoorStates.Open"/> once the timer expires.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        public void Update()
        {
            DoorIndexes[] allDoors = { DoorIndexes.Right, DoorIndexes.Left };

            foreach (var door in allDoors)
            {
                if (_states[(int)door] == DoorStates.Closed && !_tutorialSingleton.GetDoorsOpenBlock())
                {
                    OpenDoor(door);
                }
                else if (_states[(int)door] == DoorStates.Locked && !_tutorialSingleton.GetDoorsLock())
                {
                    if (_lockedIn[(int)door] > 0)
                    {
                        _lockedIn[(int)door] -= Time.deltaTime;
                        GameLogger.Instance.LogTrace($"{door} door lock countdown: {_lockedIn[(int)door]}", "Doors");
                        isLocked[(int)door] = _lockedIn[(int)door];
                    }
                    else
                    {
                        GameLogger.Instance.LogDebug($"{door} door lock expired, returning to Open", "Doors");
                        isLocked[(int)door] = 0;
                        _lockedIn[(int)door] = LockTimer;
                        _states[(int)door] = DoorStates.Open;
                    }
                }
            }
        }

        /// <summary>
        ///     Requests that <paramref name="doorIndex"/> close. Only acts if the door is
        ///     currently <see cref="DoorStates.Open"/> and the tutorial does not block closing.
        ///     Optionally resets the camera to its neutral position before the animation begins.
        /// </summary>
        /// <param name="doorIndex">Which door to close.</param>
        /// <param name="lookup">
        ///     When <c>true</c>, returns the camera to its normal position before closing.
        /// </param>
        public void CloseDoor(DoorIndexes doorIndex, bool lookup = false)
        {
            if (_states[(int)doorIndex] != DoorStates.Open || _tutorialSingleton.GetDoorsCloseBlock())
            {
                GameLogger.Instance.LogWarning(
                    $"{doorIndex} door close requested but ignored (state: {_states[(int)doorIndex]})", "Doors");
                return;
            }

            GameLogger.Instance.LogInfo($"{doorIndex} door close requested", "Doors");

            if (lookup)
                _cameraControl.MoveToNormal();

            if (doorIndex == DoorIndexes.Right)
                AudioManager.Instance.PlayDoorRight();
            else
                AudioManager.Instance.PlayDoorLeft();

            _isMoving[(int)doorIndex] = true;
            StartCoroutine(CloseRoutine(doorIndex));
        }

        /// <summary>
        ///     Opens <paramref name="doorIndex"/>. Only acts if the door is currently
        ///     <see cref="DoorStates.Closed"/>. Called automatically from
        ///     <see cref="Update"/> when a door reaches the Closed state.
        /// </summary>
        /// <param name="doorIndex">Which door to open.</param>
        private void OpenDoor(DoorIndexes doorIndex)
        {
            if (_states[(int)doorIndex] != DoorStates.Closed)
            {
                GameLogger.Instance.LogWarning(
                    $"{doorIndex} door open requested but ignored (state: {_states[(int)doorIndex]})", "Doors");
                return;
            }

            GameLogger.Instance.LogInfo($"{doorIndex} door auto-reopening", "Doors");
            _cameraControl.MoveToNormal();

            _isMoving[(int)doorIndex] = true;
            StartCoroutine(OpenRoutine(doorIndex));
        }

        /// <summary>
        ///     Coroutine that slides <paramref name="doorIndex"/> down to
        ///     <see cref="ClosedPos"/> one frame at a time, then sets its state
        ///     to <see cref="DoorStates.Closed"/>.
        /// </summary>
        /// <param name="doorIndex">Which door to animate.</param>
        private IEnumerator CloseRoutine(DoorIndexes doorIndex)
        {
            GameLogger.Instance.LogDebug($"{doorIndex} door closing started", "Doors");
            _states[(int)doorIndex] = DoorStates.Closing;

            var activeDoor = _doorObjects[(int)doorIndex];

            while (!MoveDoor(activeDoor, ClosedPos, CloseSpeed))
            {
                GameLogger.Instance.LogTrace($"{doorIndex} door Y: {activeDoor.transform.position.y:F3}", "Doors");
                yield return null;
            }

            GameLogger.Instance.LogDebug($"{doorIndex} door fully closed", "Doors");
            _states[(int)doorIndex] = DoorStates.Closed;
            _isMoving[(int)doorIndex] = false;
        }

        /// <summary>
        ///     Coroutine that slides <paramref name="doorIndex"/> up to
        ///     <see cref="OpenPos"/> one frame at a time using a reduced speed
        ///     (<see cref="CloseSpeed"/> / 10) for a slower, gentler motion, then briefly
        ///     locks the door to prevent it from being closed the instant it finishes.
        /// </summary>
        /// <param name="doorIndex">Which door to animate.</param>
        private IEnumerator OpenRoutine(DoorIndexes doorIndex)
        {
            GameLogger.Instance.LogDebug($"{doorIndex} door opening started", "Doors");
            _states[(int)doorIndex] = DoorStates.Opening;

            var activeDoor = _doorObjects[(int)doorIndex];

            while (!MoveDoor(activeDoor, OpenPos, OpenSpeed))
            {
                GameLogger.Instance.LogTrace($"{doorIndex} door Y: {activeDoor.transform.position.y:F3}", "Doors");
                yield return null;
            }

            GameLogger.Instance.LogDebug($"{doorIndex} door fully open, locking briefly", "Doors");
            _states[(int)doorIndex] = DoorStates.Locked;
            isLocked[(int)doorIndex] = LockTimer;
            _isMoving[(int)doorIndex] = false;
        }

        /// <summary>
        ///     Moves <paramref name="door"/> one step toward <paramref name="targetY"/>
        ///     along the Y axis using <see cref="Vector3.MoveTowards"/>, keeping X and Z
        ///     unchanged.
        /// </summary>
        /// <param name="door">The door GameObject to move.</param>
        /// <param name="targetY">The target Y world-space position.</param>
        /// <param name="speed">Movement speed in units per second.</param>
        /// <returns><c>true</c> once the door has reached <paramref name="targetY"/>.</returns>
        private static bool MoveDoor(GameObject door, float targetY, float speed)
        {
            door.transform.position = Vector3.MoveTowards(
                door.transform.position,
                new Vector3(door.transform.position.x, targetY, door.transform.position.z),
                speed * Time.deltaTime
            );

            return Mathf.Approximately(door.transform.position.y, targetY);
        }


    }
}