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
    // ---- Private state ----
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isDragging = false;
    private bool snappedToMouth = false;
    private bool returning = false;
    private bool snapping = false;

    // Hover hold state
    private bool isHoveringOverZone = false;
    private float hoverTimer = 0f;
    private bool hoverTriggered = false;   // true once the 0.5s fires — prevents re-trigger

    private Vector3 offset;
    private float zDepth;

    // =========================================================================

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        zDepth = Camera.main.WorldToScreenPoint(transform.position).z;

        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();
    }

    void Update()
    {
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
            HandleHoverZone();   // check hover hold while dragging
        }
        else if (returning)
        {
            ReturnToOriginal();
        }
    }

    // ---- Mouse (Editor / PC) ----
    void OnMouseDown() { if (!snappedToMouth && !snapping) TryBeginDrag(ScreenToWorld(Input.mousePosition)); }
    void OnMouseDrag()
    {
        if (!snappedToMouth && !snapping && isDragging)
        {
            transform.position = ScreenToWorld(Input.mousePosition) + offset;
        }
    }
    void OnMouseUp() { if (!snappedToMouth && !snapping && isDragging) EndDrag(); }

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
        isDragging = true;
        returning = false;
        hoverTimer = 0f;
        hoverTriggered = false;
        offset = transform.position - worldPos;
    }

    private void EndDrag()
    {
        isDragging = false;
        isHoveringOverZone = false;
        hoverTimer = 0f;

        // If hover already triggered the snap, do nothing here
        if (hoverTriggered) return;

        float distance = Vector3.Distance(transform.position, snapZone.position);

        if (distance <= snapDistance)
        {
            TriggerSnap();
        }
        else
        {
            returning = true;
        }
    }

    // =========================================================================
    // Hover hold zone
    // =========================================================================

    /// <summary>
    /// Called every frame while the user is dragging.
    /// Checks if the glass overlaps hoverTriggerZone and starts/stops the hold timer.
    /// </summary>
    private void HandleHoverZone()
    {
        if (hoverTriggered || hoverTriggerZone == null) return;

        // Check overlap using the glass's own collider bounds vs the trigger zone
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
                isDragging = false;   // take control from user
                TriggerSnap();
            }
        }
        else
        {
            // Left the zone — reset timer
            isHoveringOverZone = false;
            hoverTimer = 0f;
        }
    }

    // =========================================================================
    // Snap to mouth
    // =========================================================================

    private void TriggerSnap()
    {
        GetComponent<Collider2D>().enabled = false;
        transform.GetChild(1).GetComponent<Animation>().Play("MilkAnimFinal");
        mouthTarget.parent.GetChild(0).GetComponent<Animation>().Play("GlowUpBody");
        snapping = true;
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
    // After snap: wait, then fade out
    // =========================================================================

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(stayDuration);

        SceneManager.instance.ActivateParticleSystem();

        dragLine.SetActive(false);
        blueCircle.GetComponent<Animation>().Play("FadeBlueCircle");


        // Hide the glass
        gameObject.SetActive(false);

        snappedToMouth = false;
        returning = true;
        GetComponent<Collider2D>().enabled = false;

        Invoke("SetOff", 0.6f);
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
            GetComponent<Collider2D>().enabled = true;   // re-enable for next drag
            hoverTriggered = false;                       // allow hover trigger again
        }
    }

    // =========================================================================
    // Set Off
    // =========================================================================


    [SerializeField] GameObject dragLine;
    [SerializeField] GameObject blueCircle;
    private void SetOff()
    {
        
    }
}