using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ModelViewController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Target")]
    public Transform target;

    [Header("Intro Animation")]
    public float introDuration = 3.0f;
    public float introPitchAngle = 20f;
    public float introSettleDuration = 0.8f;
    public float introFinalYaw = 0f;
    public float introFinalPitch = 0f;
    public AnimationCurve introEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Orbit")]
    public float orbitSensitivity = 1f;
    // smoothTime is now ONLY used for zoom and pivot pan — NOT for orbit rotation
    public float smoothTime = 0.10f;
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    [Header("Inertia")]
    [Tooltip("How quickly the inertia spin decelerates. Higher = stops faster. 0 = never stops.")]
    public float inertiaDamping = 4f;
    [Tooltip("Minimum speed (deg/s) before inertia is considered stopped.")]
    public float inertiaMinSpeed = 0.5f;
    [Tooltip("Multiplier applied to the captured swipe velocity when inertia starts. Tune feel here.")]
    public float inertiaVelocityScale = 1.0f;
    [Tooltip("Max number of frames averaged to calculate release velocity. Higher = smoother but lazier.")]
    [Range(1, 10)]
    public int inertiaVelocitySampleFrames = 4;
    [Tooltip("Release velocity (deg/s) that must be exceeded to trigger inertia. Below this threshold the model stops instantly on release.")]
    public float inertiaActivationThreshold = 80f;

    [Header("Zoom")]
    public float scrollZoomSensitivity = 2f;
    public float pinchZoomSensitivity = 0.02f;
    public float zoomSmoothTime = 0.12f;

    [Header("Pan")]
    public bool enablePan = true;
    public float panSensitivity = 0.005f;

    [Header("Auto Rotate")]
    public bool autoRotate = false;
    public float autoRotateSpeed = 20f;
    public float autoRotateResumeDelay = 2f;

    [Header("Collision")]
    public float collisionPadding = 0.1f;

    [Header("UI")]
    public GameObject ChangeSensitivityBtn;
    public GameObject sensitivityPage;
    public Slider zoomSlider;
    public Slider orbitSlider;

    [Header("Sensitivity Labels")]
    public TMPro.TMP_Text zoomValueText;
    public TMPro.TMP_Text orbitValueText;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private float _currentYaw, _targetYaw;
    private float _currentPitch, _targetPitch;
    private float _currentDist, _targetDist;
    private float _distVel;

    private Vector3 _currentPivot, _targetPivot;
    private Vector3 _pivotVel;

    // Orbit smooth damp vels — kept but only used for non-inertia smoothing (intro settle etc.)
    private float _yawVel, _pitchVel;

    // Inertia state
    private float _inertiaYawVel = 0f;
    private float _inertiaPitchVel = 0f;
    private bool _inertiaActive = false;

    // Rolling average velocity buffer
    private Vector2[] _velocitySamples;
    private int _velocitySampleIndex = 0;
    private bool _wasOrbitingLastFrame = false;

    private Vector2 _prevMousePos;
    private bool _mouseOrbitActive;
    private bool _mousePanActive;

    private Vector2 _lastSinglePos;
    private float _lastPinchDist;
    private Vector2 _lastMidpoint;
    private bool _touchInitialised;

    private float _lastInteractionTime;
    private bool _introPlaying = true;
    private bool isUiActive = false;

    private float _defaultDist;
    private Vector3 _defaultPivot;
    private Vector3 _modelCenter;
    private float _modelRadius;
    private float minZoomDistance;
    private float maxZoomDistance;

    private bool _pinchActive = false;

    // =========================================================================
    // Properties
    // =========================================================================

    float FinalZoomSens
    {
        get
        {
            float t = zoomSlider != null ? zoomSlider.value : 1f;
            float normalized = Mathf.InverseLerp(zoomSlider != null ? zoomSlider.minValue : 1f,
                                                 zoomSlider != null ? zoomSlider.maxValue : 10f, t);
            float curved = normalized * normalized;
            return scrollZoomSensitivity * Mathf.Lerp(0.1f, 3f, curved);
        }
    }

    float FinalOrbitSens => orbitSensitivity * (orbitSlider != null ? orbitSlider.value : 1f);

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Start()
    {
        _velocitySamples = new Vector2[inertiaVelocitySampleFrames];

        SetupTarget();
        StartCoroutine(InitAfterLoad());

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (zoomSlider != null) zoomSlider.onValueChanged.AddListener(_ => UpdateSensitivityLabels());
        if (orbitSlider != null) orbitSlider.onValueChanged.AddListener(_ => UpdateSensitivityLabels());

        UpdateSensitivityLabels();
    }

    void UpdateSensitivityLabels()
    {
        if (zoomValueText != null && zoomSlider != null) zoomValueText.text = zoomSlider.value.ToString("F1");
        if (orbitValueText != null && orbitSlider != null) orbitValueText.text = orbitSlider.value.ToString("F1");
    }

    void Update()
    {
        if (_introPlaying || isUiActive) return;

        HandleMouse();
        HandleTouch();
        HandleInertia();
        HandleAutoRotate();
        ApplySmoothedTransform();
    }

    // =========================================================================
    // Initialisation
    // =========================================================================

    IEnumerator InitAfterLoad()
    {
        _introPlaying = true;
        yield return null;
        yield return null;
        FitCameraToModel();
        StartCoroutine(PlayIntroAnimation());
    }

    void SetupTarget()
    {
        if (target != null) return;
        Renderer r = FindFirstObjectByType<Renderer>();
        target = r != null ? r.transform.root : new GameObject("ModelViewPivot").transform;
    }

    void FitCameraToModel()
    {
        Bounds bounds = GetModelBounds();

        _modelCenter = bounds.center;
        _modelRadius = bounds.extents.magnitude;
        _targetPivot = bounds.center;
        _currentPivot = bounds.center;

        float fov = Camera.main ? Camera.main.fieldOfView : 60f;
        float fit = _modelRadius / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

        minZoomDistance = _modelRadius * 1.7f + collisionPadding;
        maxZoomDistance = _modelRadius * 4f;

        _targetDist = Mathf.Clamp(fit * 1.6f, minZoomDistance, maxZoomDistance);
        _currentDist = _targetDist;
        _defaultDist = _targetDist;
        _defaultPivot = _targetPivot;

        _currentYaw = 0f;
        _targetYaw = 0f;
        _currentPitch = introPitchAngle;
        _targetPitch = introPitchAngle;

        ApplyTransformImmediate();
        _lastInteractionTime = Time.time;
    }

    Bounds GetModelBounds()
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(target.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // =========================================================================
    // Pivot clamp
    // =========================================================================

    void ClampPivotInsideModel()
    {
        Vector3 offset = _targetPivot - _modelCenter;
        float maxOffset = _modelRadius * 0.5f;

        if (offset.magnitude > maxOffset)
        {
            offset = offset.normalized * maxOffset;
            _targetPivot = _modelCenter + offset;
        }
    }

    // =========================================================================
    // Safe distance
    // =========================================================================

    float GetSafeMinDist(Vector3 pivot, Quaternion rot)
    {
        Vector3 camDir = rot * Vector3.back;
        Vector3 toCenter = _modelCenter - pivot;
        float along = Vector3.Dot(toCenter, camDir);
        float perpDist = (toCenter - camDir * along).magnitude;
        float sphereRad = _modelRadius + collisionPadding;

        if (perpDist >= sphereRad) return minZoomDistance;

        float halfChord = Mathf.Sqrt(Mathf.Max(0f, sphereRad * sphereRad - perpDist * perpDist));
        return Mathf.Max(along - halfChord, minZoomDistance);
    }

    void ClampTargetDist()
    {
        Quaternion targetRot = Quaternion.Euler(_targetPitch, _targetYaw, 0f);
        float safeMin = GetSafeMinDist(_targetPivot, targetRot);
        _targetDist = Mathf.Clamp(_targetDist, Mathf.Max(minZoomDistance, safeMin), maxZoomDistance);
    }

    // =========================================================================
    // Zoom to pointer
    // =========================================================================

    void ZoomToScreenPoint(Vector2 screenPos, float oldDist, float newDist)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
        Plane plane = new Plane(-transform.forward, _targetPivot);

        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 cursorWorldPos = ray.GetPoint(enter);
        float ratio = newDist / Mathf.Max(oldDist, 0.0001f);
        Vector3 newPivot = Vector3.Lerp(cursorWorldPos, _targetPivot, ratio);

        _targetPivot = new Vector3(_targetPivot.x, newPivot.y, _targetPivot.z);
        ClampPivotInsideModel();
    }

    // =========================================================================
    // Intro animation
    // =========================================================================

    IEnumerator PlayIntroAnimation()
    {
        _introPlaying = true;

        float startYaw = _currentYaw;
        float endYaw = startYaw + 360f;
        float elapsed = 0f;

        while (elapsed < introDuration)
        {
            elapsed += Mathf.Min(Time.deltaTime, 0.05f);
            float t = Mathf.Clamp01(elapsed / introDuration);
            float te = introEase.Evaluate(t);

            _currentYaw = Mathf.Lerp(startYaw, endYaw, te);
            ApplyTransformImmediate();
            yield return null;
        }

        _targetYaw = introFinalYaw;
        _targetPitch = introFinalPitch;
        _currentYaw = introFinalYaw;
        _currentPitch = introFinalPitch;
        _introPlaying = false;
    }

    // =========================================================================
    // Inertia helpers
    // =========================================================================

    /// <summary>
    /// Record the per-frame delta (deg/s) into the rolling average buffer.
    /// </summary>
    void RecordOrbitVelocity(Vector2 deltaDegs)
    {
        // Reinitialise buffer size if inspector value changed at runtime
        if (_velocitySamples == null || _velocitySamples.Length != inertiaVelocitySampleFrames)
            _velocitySamples = new Vector2[inertiaVelocitySampleFrames];

        _velocitySamples[_velocitySampleIndex % inertiaVelocitySampleFrames] = deltaDegs / Time.deltaTime;
        _velocitySampleIndex++;
    }

    /// <summary>
    /// Average all buffered samples to get a stable release velocity.
    /// </summary>
    Vector2 GetAverageVelocity()
    {
        if (_velocitySamples == null) return Vector2.zero;
        Vector2 sum = Vector2.zero;
        foreach (var s in _velocitySamples) sum += s;
        return sum / _velocitySamples.Length;
    }

    void ClearVelocityBuffer()
    {
        if (_velocitySamples != null)
            for (int i = 0; i < _velocitySamples.Length; i++)
                _velocitySamples[i] = Vector2.zero;
        _velocitySampleIndex = 0;
    }

    /// <summary>
    /// Called when the user lifts their finger/mouse — kicks off inertia.
    /// </summary>
    void StartInertia()
    {
        Vector2 avgVel = GetAverageVelocity() * inertiaVelocityScale;
        _inertiaYawVel = avgVel.x;
        _inertiaPitchVel = -avgVel.y;   // y delta is inverted during orbit input

        float speed = Mathf.Abs(_inertiaYawVel) + Mathf.Abs(_inertiaPitchVel);

        // Only activate inertia for fast swipes. Slow/normal drags stop dead on release.
        _inertiaActive = speed > inertiaActivationThreshold;

        if (!_inertiaActive)
        {
            _inertiaYawVel = 0f;
            _inertiaPitchVel = 0f;
        }

        ClearVelocityBuffer();
    }

    void StopInertia()
    {
        _inertiaActive = false;
        _inertiaYawVel = 0f;
        _inertiaPitchVel = 0f;
    }

    void HandleInertia()
    {
        if (!_inertiaActive) return;

        // Apply current inertia velocity directly to the TARGET angles (no smoothdamp)
        _targetYaw += _inertiaYawVel * Time.deltaTime;
        _targetPitch += _inertiaPitchVel * Time.deltaTime;
        _targetPitch = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
        ClampTargetDist();

        // Exponential damping — smooth deceleration, tunable via inertiaDamping
        float decay = Mathf.Exp(-inertiaDamping * Time.deltaTime);
        _inertiaYawVel *= decay;
        _inertiaPitchVel *= decay;

        // Stop once speed is negligible
        if (Mathf.Abs(_inertiaYawVel) + Mathf.Abs(_inertiaPitchVel) < inertiaMinSpeed)
            StopInertia();
    }

    // =========================================================================
    // Mouse input
    // =========================================================================

    void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // ── Orbit ─────────────────────────────────────────────────────────

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _prevMousePos = mousePos;
            _mouseOrbitActive = true;
            StopInertia();          // cancel any running inertia when user grabs again
            ClearVelocityBuffer();
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            _mouseOrbitActive = false;
            StartInertia();         // launch inertia on release
        }

        if (_mouseOrbitActive && mouse.leftButton.isPressed)
        {
            Vector2 delta = mousePos - _prevMousePos;

            // ── KEY CHANGE: apply directly to _current* (instant, no smoothdamp lag) ──
            float dYaw = delta.x * FinalOrbitSens;
            float dPitch = -delta.y * FinalOrbitSens;

            _currentYaw += dYaw;
            _currentPitch += dPitch;
            _currentPitch = Mathf.Clamp(_currentPitch, minVerticalAngle, maxVerticalAngle);

            // Keep target in sync so there is no "snap" when dragging stops
            _targetYaw = _currentYaw;
            _targetPitch = _currentPitch;

            ClampTargetDist();
            RecordOrbitVelocity(new Vector2(dYaw, -dPitch));  // store for inertia
            Interact();
        }

        // ── Pan ───────────────────────────────────────────────────────────

        if (enablePan)
        {
            if (mouse.middleButton.wasPressedThisFrame)
            {
                _prevMousePos = mousePos;
                _mousePanActive = true;
            }

            if (mouse.middleButton.wasReleasedThisFrame)
                _mousePanActive = false;

            if (_mousePanActive && mouse.middleButton.isPressed)
            {
                Vector2 delta = mousePos - _prevMousePos;

                _targetPivot -=
                    (transform.right * delta.x + transform.up * delta.y)
                    * panSensitivity * _currentDist;

                ClampPivotInsideModel();
                Interact();
            }
        }

        _prevMousePos = mousePos;

        // ── Scroll zoom ───────────────────────────────────────────────────

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float oldDist = _targetDist;
            float newDist = oldDist - scroll * FinalZoomSens * 0.01f * oldDist;

            Quaternion targetRot = Quaternion.Euler(_targetPitch, _targetYaw, 0f);
            float safeMin = GetSafeMinDist(_targetPivot, targetRot);
            newDist = Mathf.Clamp(newDist, Mathf.Max(minZoomDistance, safeMin), maxZoomDistance);

            ZoomToScreenPoint(mousePos, oldDist, newDist);
            _targetDist = newDist;
            Interact();
        }
    }

    // =========================================================================
    // Touch input
    // =========================================================================

    void HandleTouch()
    {
        var touches = Touch.activeTouches;

        if (touches.Count == 0)
        {
            if (_touchInitialised)
            {
                StartInertia();     // finger lifted — launch inertia
                Interact();
            }
            _touchInitialised = false;
            _pinchActive = false;
            return;
        }

        // ── Single finger — orbit ─────────────────────────────────────────

        if (touches.Count == 1)
        {
            var t = touches[0];

            if (!_touchInitialised || t.phase == TouchPhase.Began || _pinchActive)
            {
                _lastSinglePos = t.screenPosition;
                _touchInitialised = true;
                _pinchActive = false;
                StopInertia();
                ClearVelocityBuffer();
                return;
            }

            if (t.phase == TouchPhase.Moved)
            {
                Vector2 delta = t.screenPosition - _lastSinglePos;

                // ── KEY CHANGE: instant apply, same pattern as mouse orbit ──
                float dYaw = delta.x * FinalOrbitSens * 0.3f;
                float dPitch = -delta.y * FinalOrbitSens * 0.3f;

                _currentYaw += dYaw;
                _currentPitch += dPitch;
                _currentPitch = Mathf.Clamp(_currentPitch, minVerticalAngle, maxVerticalAngle);

                _targetYaw = _currentYaw;
                _targetPitch = _currentPitch;

                ClampTargetDist();
                RecordOrbitVelocity(new Vector2(dYaw, -dPitch));
                Interact();
            }

            _lastSinglePos = t.screenPosition;
        }

        // ── Two fingers — pinch zoom + pan ────────────────────────────────

        else if (touches.Count == 2)
        {
            // Starting a two-finger gesture cancels any pending inertia
            if (!_pinchActive) StopInertia();
            _pinchActive = true;

            var t0 = touches[0];
            var t1 = touches[1];

            Vector2 pos0 = t0.screenPosition;
            Vector2 pos1 = t1.screenPosition;
            float pinchNow = Vector2.Distance(pos0, pos1);
            Vector2 midpoint = (pos0 + pos1) * 0.5f;

            if (!_touchInitialised ||
                t0.phase == TouchPhase.Began ||
                t1.phase == TouchPhase.Began)
            {
                _lastPinchDist = pinchNow;
                _lastMidpoint = midpoint;
                _touchInitialised = true;
                return;
            }

            // Zoom
            float pinchDelta = _lastPinchDist - pinchNow;
            float oldDist = _targetDist;
            float newDist = oldDist + pinchDelta * (pinchZoomSensitivity * FinalZoomSens / scrollZoomSensitivity) * oldDist;

            Quaternion targetRot = Quaternion.Euler(_targetPitch, _targetYaw, 0f);
            float safeMin = GetSafeMinDist(_targetPivot, targetRot);
            newDist = Mathf.Clamp(newDist, Mathf.Max(minZoomDistance, safeMin), maxZoomDistance);

            ZoomToScreenPoint(midpoint, oldDist, newDist);
            _targetDist = newDist;

            // Pan
            if (enablePan)
            {
                Vector2 panDelta = midpoint - _lastMidpoint;

                _targetPivot -=
                    (transform.right * panDelta.x + transform.up * panDelta.y)
                    * panSensitivity * _currentDist * 0.5f;

                ClampPivotInsideModel();
            }

            _lastPinchDist = pinchNow;
            _lastMidpoint = midpoint;
            Interact();
        }
    }

    // =========================================================================
    // Auto-rotate
    // =========================================================================

    void HandleAutoRotate()
    {
        if (!autoRotate) return;
        if (Time.time - _lastInteractionTime < autoRotateResumeDelay) return;
        _targetYaw += autoRotateSpeed * Time.deltaTime;
    }

    // =========================================================================
    // Camera transform
    // =========================================================================

    void ApplySmoothedTransform()
    {
        // Yaw and Pitch are now set directly during orbit — SmoothDamp is
        // intentionally NOT used for them. We still smooth dist and pivot.
        _currentDist = Mathf.SmoothDamp(
            _currentDist, _targetDist, ref _distVel, zoomSmoothTime);

        _currentPivot = Vector3.SmoothDamp(
            _currentPivot, _targetPivot, ref _pivotVel, smoothTime);

        // During inertia the target angles drift — keep current in sync instantly
        if (_inertiaActive)
        {
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
        }

        Quaternion targetRot = Quaternion.Euler(_targetPitch, _targetYaw, 0f);
        float safeMin = GetSafeMinDist(_currentPivot, targetRot);
        _targetDist = Mathf.Max(_targetDist, safeMin);
        _currentDist = Mathf.Max(_currentDist, safeMin);

        ApplyTransformImmediate();
    }

    void ApplyTransformImmediate()
    {
        Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

        float safeMin = GetSafeMinDist(_currentPivot, rot);
        _currentDist = Mathf.Max(_currentDist, safeMin);
        _targetDist = Mathf.Max(_targetDist, safeMin);

        Vector3 offset = rot * Vector3.back * _currentDist;
        transform.position = _currentPivot + offset;
        transform.LookAt(_currentPivot);
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void ResetView()
    {
        StopInertia();
        _targetYaw = introFinalYaw;
        _targetPitch = introFinalPitch;
        _currentYaw = introFinalYaw;
        _currentPitch = introFinalPitch;
        _targetDist = _defaultDist;
        _targetPivot = _defaultPivot;
        Interact();
    }

    public void ChangeSensitivity()
    {
        isUiActive = true;
        sensitivityPage.SetActive(true);
        ChangeSensitivityBtn.SetActive(false);
    }

    public void CloseBtn()
    {
        isUiActive = false;
        sensitivityPage.SetActive(false);
        ChangeSensitivityBtn.SetActive(true);
    }

    void Interact() => _lastInteractionTime = Time.time;
}