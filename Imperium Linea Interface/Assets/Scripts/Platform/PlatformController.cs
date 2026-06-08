using System.Collections;
using Abstract;
using Tutorial;
using UnityEngine;

namespace Platform
{
    // Singleton MonoBehaviour that manages rotation of the platform GameObject.
    // Only Y-axis rotation is permitted; X is permanently locked at fixedXRotation.
    public class PlatformController : MonoSingleton<PlatformController>
    {
        // The platform GameObject to rotate, located at runtime via its tag
        public GameObject platform;

        // How many real-world seconds a 90-degree rotation should take by default
        public float secondsPer90Degrees = 1f;

        // Lock X rotation at -90 degrees
        public float fixedXRotation = -90f;

        // Handle to the currently running rotation coroutine, so it can be
        // stopped and replaced if a new rotation request arrives mid-animation
        private Coroutine _currentRotation;

        // The Y rotation (in degrees) the platform is animating toward;
        // accumulated across successive RotateBy calls so they chain correctly
        private float _targetYRotation;

        // Read-only access to the platform's current Y rotation in local space;
        // returns 0 if the platform reference is missing
        public float CurrentYRotation => platform?.transform.localRotation.eulerAngles.y ?? 0f;

        // True while a rotation coroutine is in progress; false when stationary
        public bool IsRotating { get; private set; }

        // Called once on scene start. Locates the platform by tag, seeds the
        // target Y rotation from its initial orientation, and locks X immediately.
        private void Start()
        {
            platform = GameObject.FindWithTag("Platform");

            if (platform == null)
            {
                GameLogger.Instance.LogError("Platform not found (tag: Platform)", "Platform");
                return;
            }

            _targetYRotation = platform.transform.localRotation.eulerAngles.y;

            GameLogger.Instance.LogInfo(
                $"Platform initialized at Y={_targetYRotation:F1}°",
                "Platform"
            );

            LockXRotation();
        }

        // Called every frame. Guards against external code accidentally modifying
        // the X rotation by re-locking it whenever drift beyond 0.01° is detected.
        private void Update()
        {
            // Safety: enforce X lock every frame in case something else modifies it
            if (platform != null && !IsRotating)
            {
                var euler = platform.transform.localRotation.eulerAngles;
                if (Mathf.Abs(NormalizeAngle(euler.x) - NormalizeAngle(fixedXRotation)) > 0.01f) LockXRotation();
            }
        }

        /// <summary>
        ///     Rotates the platform around Y-axis only. X stays locked at -90°.
        /// </summary>
        /// <param name="angle">Degrees to rotate by; positive values rotate clockwise.</param>
        /// <param name="durationOverride">
        ///     When greater than zero, overrides the secondsPer90Degrees-based duration
        ///     with an explicit number of seconds for the full rotation.
        /// </param>
        public void RotateBy(float angle, float durationOverride = 0f)
        {
            if (platform == null)
            {
                GameLogger.Instance.LogError("RotateBy called but platform is null", "Platform");
                return;
            }

            if (TutorialSingleton.Instance.GetRotateBlock())
            {
                GameLogger.Instance.LogError("RotateBy called but blocked by Tutorial", "Platform");
                return;
            }

            GameLogger.Instance.LogInfo(
                $"Rotate request: angle={angle}, override={durationOverride}",
                "Platform"
            );

            if (_currentRotation != null)
            {
                GameLogger.Instance.LogWarning(
                    "Interrupting current rotation",
                    "Platform"
                );

                StopCoroutine(_currentRotation);
            }

            var previousTarget = _targetYRotation;

            _targetYRotation += angle;
            _targetYRotation = NormalizeAngle(_targetYRotation);

            GameLogger.Instance.LogDebug(
                $"Target Y updated: {previousTarget:F1}° -> {_targetYRotation:F1}°",
                "Platform"
            );

            var duration = durationOverride > 0f
                ? durationOverride
                : Mathf.Abs(angle) / 90f * secondsPer90Degrees;

            GameLogger.Instance.LogDebug(
                $"Rotation duration: {duration:F2}s",
                "Platform"
            );
            AudioManager.Instance.PlayRotationStart();
            _currentRotation = StartCoroutine(RotateCoroutine(duration));
        }

        // Coroutine that smoothly animates the platform's Y rotation from its
        // current angle to _targetYRotation over the given duration (in seconds).
        // Uses a smoothstep curve so the rotation eases in and out.
        private IEnumerator RotateCoroutine(float duration)
        {
            IsRotating = true;

            var startY = platform.transform.localRotation.eulerAngles.y;

            GameLogger.Instance.LogInfo(
                $"Rotation started: {startY:F1}° -> {_targetYRotation:F1}° (duration {duration:F2}s)",
                "Platform"
            );

            var elapsed = 0f;
            var outroTriggered = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (!outroTriggered && elapsed >= duration - AudioManager.Instance.OutroLength)
                {
                    AudioManager.Instance.PlayRotationEnd();
                    outroTriggered = true;
                }

                var t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                var currentY = Mathf.LerpAngle(startY, _targetYRotation, t);
                platform.transform.localRotation = Quaternion.Euler(fixedXRotation, currentY, 0);

                yield return null;
            }

            platform.transform.localRotation =
                Quaternion.Euler(fixedXRotation, _targetYRotation, 0);

            IsRotating = false;
            _currentRotation = null;

            GameLogger.Instance.LogInfo(
                $"Rotation completed at {_targetYRotation:F1}°",
                "Platform"
            );
        }

        // Overwrites the platform's local rotation, keeping the current Y and Z
        // values intact but forcing X to fixedXRotation.
        private void LockXRotation()
        {
            var euler = platform.transform.localRotation.eulerAngles;

            if (Mathf.Abs(NormalizeAngle(euler.x) - NormalizeAngle(fixedXRotation)) > 0.01f)
                GameLogger.Instance.LogDebug(
                    $"Fixing X rotation drift: {euler.x:F2} -> {fixedXRotation:F2}",
                    "Platform"
                );

            platform.transform.localRotation =
                Quaternion.Euler(fixedXRotation, euler.y, 0);
        }

        // Converts any angle to its equivalent in the [0, 360) range.
        // Used to keep _targetYRotation and comparisons consistent.
        private float NormalizeAngle(float angle)
        {
            while (angle >= 360f) angle -= 360f;
            while (angle < 0f) angle += 360f;

            return angle;
        }
    }
}