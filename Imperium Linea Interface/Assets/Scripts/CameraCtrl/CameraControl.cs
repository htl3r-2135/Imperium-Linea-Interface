using Abstract;
using Tutorial;
using UnityEngine;

namespace CameraCtrl
{
    public class CameraControl : MonoSingleton<CameraControl>
    {
        [Header("Camera States")] public Transform normalTransform;

        public Transform hoverTransform;

        [Header("Smooth Settings")] public float smoothSpeed = 5f;

        private Camera _cam;

        private CameraState _currentState = CameraState.Normal;

        private bool _hoverConfirmed;
        private float _hoverTimer;
        private bool _lockedState;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            Cursor.lockState = CursorLockMode.None;

            GameLogger.Instance.LogInfo("CameraControl initialized", "Camera");

            MoveToNormal(true);
        }

        public void MoveToNormal(bool instant = false)
        {
            if (_lockedState || TutorialSingleton.Instance.GetLookBlock())
            {
                GameLogger.Instance.LogDebug("MoveToNormal blocked (locked state)", "Camera");
                return;
            }

            if (_currentState != CameraState.Normal)
            {
                GameLogger.Instance.LogInfo("Switching to NORMAL view", "Camera");
                _currentState = CameraState.Normal;
            }

            if (instant)
            {
                _cam.transform.position = normalTransform.position;
                _cam.transform.rotation = normalTransform.rotation;

                GameLogger.Instance.LogDebug("Instant snap to NORMAL", "Camera");
                return;
            }

            _cam.transform.position = Vector3.Lerp(
                _cam.transform.position,
                normalTransform.position,
                Time.deltaTime * smoothSpeed
            );

            _cam.transform.rotation = Quaternion.Lerp(
                _cam.transform.rotation,
                normalTransform.rotation,
                Time.deltaTime * smoothSpeed
            );
        }

        public void MoveToHover()
        {
            if (_lockedState || TutorialSingleton.Instance.GetLookBlock())
            {
                GameLogger.Instance.LogDebug("MoveToHover blocked (locked state)", "Camera");
                return;
            }

            if (_currentState != CameraState.Hover)
            {
                GameLogger.Instance.LogInfo("Switching to HOVER view", "Camera");
                _currentState = CameraState.Hover;
                _hoverTimer = 0f;
                _hoverConfirmed = false;
            }

            _cam.transform.position = Vector3.Lerp(
                _cam.transform.position,
                hoverTransform.position,
                Time.deltaTime * smoothSpeed
            );

            _cam.transform.rotation = Quaternion.Lerp(
                _cam.transform.rotation,
                hoverTransform.rotation,
                Time.deltaTime * smoothSpeed
            );

            // Hover confirmation logic
            _hoverTimer += Time.deltaTime;
            if (_hoverConfirmed || !(_hoverTimer > 0.5f)) return;
            _hoverConfirmed = true;
            GameLogger.Instance.LogInfo("Hover state confirmed", "Camera");
        }

        public void SetLocked(bool locked)
        {
            if (_lockedState == locked) return;

            _lockedState = locked;

            if (locked)
                GameLogger.Instance.LogWarning("Camera LOCKED", "Camera");
            else
                GameLogger.Instance.LogInfo("Camera UNLOCKED", "Camera");
        }

        // Track state to avoid log spam
        private enum CameraState
        {
            Normal,
            Hover
        }
    }
}