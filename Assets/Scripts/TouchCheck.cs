using UnityEngine;
using System.Collections;

public class TouchScript : MonoBehaviour
{
    [Header("References")]
    public Transform snapZone;        // Point 1 — radius check only
    public Transform mouthTarget;     // Point 2 — final destination

    [Header("Scale Settings")]
    public float minScale = 0.4f;
    public float shrinkSpeed = 0.5f;
    public float returnSpeed = 5f;

    [Header("Snap Settings")]
    public float snapDistance = 1f;
    public Vector3 snapTargetRotationEuler;
    public float snapMoveSpeed = 6f;

    [Header("Delay")]
    public float stayDuration = 3f;

    [Header("Hover Trigger")]
    [Tooltip("Assign the Box Collider trigger zone here")]
    public Collider2D hoverTriggerZone;
    [Tooltip("How long the glass must be held over the zone before auto-snap (seconds)")]
    public float hoverHoldDuration = 0.5f;

    [Header("Intro Animation")]
    [Tooltip("Match this to the duration of your 0 to 1 scale animation")]
    public float introAnimDuration = 1f;

    // ---- Private state ----
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isDragging = false;
    private bool snappedToMouth = false;
    private bool returning = false;
    private bool snapping = false;
    private bool introComplete = false;   // blocks drag until intro anim finishes

    // Hover hold state
    private bool isHoveringOverZone = false;
    private float hoverTimer = 0f;
    private bool hoverTriggered = false;

    private Vector3 offset;
    private float zDepth;
    private Animation _animation;

    [SerializeField] GameObject dragLine;
    [SerializeField] GameObject blueCircle;

    // =========================================================================

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        // originalScale captured AFTER intro anim finishes — see WaitForIntroAnim

        zDepth = Camera.main.WorldToScreenPoint(transform.position).z;

        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();

        _animation = GetComponent<Animation>();

        StartCoroutine(WaitForIntroAnim());
    }

    /// <summary>
    /// Waits for the 0->1 intro scale animation to finish, then snapshots
    /// the correct originalScale and unlocks dragging.
    /// </summary>
    private IEnumerator WaitForIntroAnim()
    {
        introComplete = false;
        yield return new WaitForSeconds(introAnimDuration);
        originalScale = transform.localScale;   // captured at scale 1, not scale 0
        introComplete = true;
    }

    /// <summary>
    /// Alternative: call this from an Animation Event at the last frame of
    /// the intro clip instead of relying on the timer above.
    /// </summary>
    public void OnIntroAnimComplete()
    {
        StopAllCoroutines();
        originalScale = transform.localScale;
        introComplete = true;
    }

    void Update()
    {
        if (!introComplete) return;   // wait until scale anim is done
        if (snappedToMouth) return;

        if (snapping)
        {
            LerpToMouth();
            return;
        }

        // ---- Touch input ----
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorld = ScreenToWorld(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryBeginDrag(touchWorld);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging) transform.position = touchWorld + offset;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging) EndDrag();
                    break;
            }
        }

        if (isDragging)
        {
            HandleScale();
            HandleSnapFeedback();
            HandleHoverZone();
        }
        else if (returning)
        {
            ReturnToOriginal();
        }
    }

    // ---- Mouse (Editor / PC) ----
    void OnMouseDown() { if (introComplete && !snappedToMouth && !snapping) TryBeginDrag(ScreenToWorld(Input.mousePosition)); }
    void OnMouseDrag()
    {
        if (introComplete && !snappedToMouth && !snapping && isDragging)
            transform.position = ScreenToWorld(Input.mousePosition) + offset;
    }
    void OnMouseUp() { if (introComplete && !snappedToMouth && !snapping && isDragging) EndDrag(); }

    // =========================================================================
    // Core drag
    // =========================================================================

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        screenPos.z = zDepth;
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    private void TryBeginDrag(Vector3 worldPos)
    {
        Vibration.instance.Vibrate(5);
        SFXManager.instance.Play(SFXType.Glass);
        isDragging = true;
        returning = false;
        hoverTimer = 0f;
        hoverTriggered = false;
        offset = transform.position - worldPos;

        // Stop legacy Animation overriding localScale during drag
        if (_animation != null) _animation.enabled = false;
    }

    private void EndDrag()
    {
        isDragging = false;
        isHoveringOverZone = false;
        hoverTimer = 0f;

        if (hoverTriggered) return;

        float distance = Vector3.Distance(transform.position, snapZone.position);

        if (distance <= snapDistance)
        {
            TriggerSnap();
        }
        else
        {
            // Re-enable legacy Animation
            if (_animation != null) _animation.enabled = true;
            returning = true;
        }
    }

    // =========================================================================
    // Hover hold zone
    // =========================================================================

    private void HandleHoverZone()
    {
        if (hoverTriggered || hoverTriggerZone == null) return;

        bool inside = hoverTriggerZone.OverlapPoint(transform.position);

        if (inside)
        {
            if (!isHoveringOverZone)
            {
                isHoveringOverZone = true;
                hoverTimer = 0f;
            }

            hoverTimer += Time.deltaTime;

            if (hoverTimer >= hoverHoldDuration)
            {
                hoverTriggered = true;
                isHoveringOverZone = false;
                isDragging = false;
                TriggerSnap();
            }
        }
        else
        {
            isHoveringOverZone = false;
            hoverTimer = 0f;
        }
    }

    // =========================================================================
    // Snap to mouth
    // =========================================================================

    private void TriggerSnap()
    {
        //Vibration.instance.Vibrate(100);
        GetComponent<Collider2D>().enabled = false;
        //SceneManager.instance.DeActivateSecondScene();

        DragAndDrop.GetComponent<Animation>().Play("Drag&Drop2");
        blueCircle.GetComponent<Animation>().Play("FadeBlueCircle");

        transform.GetChild(1).GetComponent<Animation>().Play("MilkAnimFinal");

        Invoke("PlayDrinkingSound", 0.5f);
        mouthTarget.parent.GetChild(0).GetComponent<Animation>().Play("GlowUpBody");
        
        
        snapping = true;
    }

    void PlayDrinkingSound()
    {
        Vibration.instance.Vibrate(200);
        SFXManager.instance.Play(SFXType.Drinking);
    }

    private void LerpToMouth()
    {
        Quaternion targetRot = Quaternion.Euler(snapTargetRotationEuler);

        transform.position = Vector3.Lerp(transform.position, mouthTarget.position, Time.deltaTime * snapMoveSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * snapMoveSpeed);

        bool posArrived = Vector3.Distance(transform.position, mouthTarget.position) < 0.01f;
        bool rotArrived = Quaternion.Angle(transform.rotation, targetRot) < 0.5f;

        if (posArrived && rotArrived)
        {
            transform.position = mouthTarget.position;
            transform.rotation = targetRot;
            snapping = false;
            snappedToMouth = true;
            
            StartCoroutine(ReturnAfterDelay());
        }


    }

    // =========================================================================
    // Feedback while dragging
    // =========================================================================

    private void HandleSnapFeedback()
    {
        float distance = Vector3.Distance(transform.position, snapZone.position);

        if (distance <= snapDistance)
        {
            float wobble = Mathf.Sin(Time.time * 10f) * 5f;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0, 0, wobble),
                Time.deltaTime * 10f);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation, originalRotation, Time.deltaTime * 10f);
        }
    }

    private void HandleScale()
    {
        float distance = Vector3.Distance(transform.position, snapZone.position);
        if (distance > snapDistance)
        {
            Vector3 s = transform.localScale;
            s -= Vector3.one * shrinkSpeed * Time.deltaTime;
            s.x = Mathf.Max(s.x, minScale);
            s.y = Mathf.Max(s.y, minScale);
            s.z = Mathf.Max(s.z, minScale);
            transform.localScale = s;
        }
    }

    // =========================================================================
    // After snap: wait, then set off
    // =========================================================================

    [SerializeField] GameObject DragAndDrop;
    private IEnumerator ReturnAfterDelay()
    {
        
        yield return new WaitForSeconds(stayDuration);

        SceneManager.instance.ActivateParticleSystem();

        dragLine.SetActive(false);


        SetOff();

        gameObject.SetActive(false);

        snappedToMouth = false;
        returning = true;
        GetComponent<Collider2D>().enabled = false;
    }

    // =========================================================================
    // Return to original position
    // =========================================================================

    private void ReturnToOriginal()
    {
        transform.position = Vector3.Lerp(transform.position, originalPosition, Time.deltaTime * returnSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * returnSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, Time.deltaTime * returnSpeed);

        if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
        {
            transform.position = originalPosition;
            transform.localScale = originalScale;
            transform.rotation = originalRotation;
            returning = false;
            GetComponent<Collider2D>().enabled = true;
            hoverTriggered = false;
            if (_animation != null) _animation.enabled = true;
        }
    }

    // =========================================================================
    // Set Off
    // =========================================================================

    private void SetOff()
    {
        // Fill in logic here
    }
}