using CameraCtrl;
using Doors;
using Platform;
using Tutorial;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MouseControl : MonoBehaviour
{
    public static bool IsZoomed;

    [Header("Rotation Settings")] public float maxRotationX = 5f;

    public float maxRotationY = 5f;
    public float deadzone = 0.1f;

    [Header("Smooth Settings")] public float smoothSpeed = 5f;

    [Header("Hover Target")] public GameObject targetObject;

    [Header("Hover Debounce")] public float enterDelay = 0.2f;

    public float exitDelay = 0.15f;

    private Camera _cam;
    private CameraControl _cameraControl;

    private bool _hoverConfirmed;
    private float _hoverTimer;

    private bool _locked;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        _cameraControl = GetComponent<CameraControl>();

        if (_cam == null)
            GameLogger.Instance.LogError("Camera component missing", "Camera");

        if (_cameraControl == null)
            GameLogger.Instance.LogError("CameraControl component missing", "Camera");

        Cursor.lockState = CursorLockMode.None;

        _cameraControl.MoveToNormal();
        GameLogger.Instance.LogInfo("Camera initialized to normal transform", "Camera");
    }

    private void Update()
    {
        if (_cam == null || _cameraControl == null)
            return;

        var mousePos = Mouse.current.position.ReadValue();

        // ── Raycast hover check ───────────────────────────────────────────────
        var isHoveringRaw = false;
        var ray = _cam.ScreenPointToRay(mousePos);
        ray.origin = transform.position; // Fix for ray entering collider, causing collision to not be registered.

        if (Physics.Raycast(ray, out var hit)) {
            if (hit.collider.gameObject == targetObject) {
                Debug.Log(hit.collider.name);
                isHoveringRaw = true;
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame) {
            _locked = !_locked;
            GameLogger.Instance.LogInfo($"Camera lock toggled: {(_locked ? "ON" : "OFF")}", "Camera");
        }

        // ── External blocking state ───────────────────────────────────────────
        var platformRotating = PlatformController.Instance?.IsRotating == true;
        var doorsMoving = DoorController.Instance?.AnyDoorBusy == true;

        var blocked = platformRotating || doorsMoving;

        if (blocked != _lastBlockedState)
        {
            _lastBlockedState = blocked;

            if (blocked) {
                GameLogger.Instance.LogInfo(
                    $"Hover blocked (Platform: {platformRotating}, Doors: {doorsMoving})",
                    "Camera"
                );
                _locked = false; // Force unlock when blocking state changes to prevent lock-in during transitions
            }
            else
                GameLogger.Instance.LogInfo("Hover unblocked", "Camera");
        }

        // ── Debounce logic ────────────────────────────────────────────────────
        if (!blocked)
        {
            if (isHoveringRaw)
            {
                _hoverTimer += Time.deltaTime;

                if (!_hoverConfirmed && _hoverTimer >= enterDelay)
                {
                    _hoverConfirmed = true;
                    _hoverTimer = 0f;

                    GameLogger.Instance.LogInfo("Hover ENTER confirmed", "Camera");
                }
            }
            else
            {
                _hoverTimer += Time.deltaTime;

                if (_hoverConfirmed && _hoverTimer >= exitDelay)
                {
                    _hoverConfirmed = false;
                    _hoverTimer = 0f;

                    GameLogger.Instance.LogInfo("Hover EXIT confirmed", "Camera");
                }

                if (!_hoverConfirmed)
                    _hoverTimer = 0f;
            }
        }
        else
        {
            if (_hoverConfirmed) GameLogger.Instance.LogDebug("Hover reset due to blocking state", "Camera");

            _hoverConfirmed = false;
        }

        // ── Zoom state tracking ───────────────────────────────────────────────
        if (_lastHoverState != _hoverConfirmed)
        {
            _lastHoverState = _hoverConfirmed;

            GameLogger.Instance.LogInfo(
                _hoverConfirmed ? "Zoom ENABLED" : "Zoom DISABLED",
                "Camera"
            );
        }

        IsZoomed = _hoverConfirmed;

        // ── Camera movement ───────────────────────────────────────────────────
        if (!_locked) {
            if (_hoverConfirmed)
                _cameraControl.MoveToHover();
            else
                _cameraControl.MoveToNormal();
        }
        
        // ── Mouse parallax ────────────────────────────────────────────────────

        if (!TutorialSingleton.Instance.GetLookBlock() && !_locked)
        {
            var mouseX = (mousePos.x / Screen.width - 0.5f) * 2;
            var mouseY = (mousePos.y / Screen.height - 0.5f) * 2;

            mouseX = Mathf.Abs(mouseX) < deadzone ? 0 : mouseX;
            mouseY = Mathf.Abs(mouseY) < deadzone ? 0 : mouseY;

            var rotY = mouseX * maxRotationY;
            var rotX = -mouseY * maxRotationX;

            var mouseRotation = Quaternion.Euler(rotX, rotY, 0);

            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                transform.localRotation * mouseRotation,
                Time.deltaTime * smoothSpeed
            );
        }
    }
    private bool _lastBlockedState;

    // State tracking (prevents log spam)
    private bool _lastHoverState;
}