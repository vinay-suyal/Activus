using UnityEngine;
using System.Collections;

public class MilkGlassDrag : MonoBehaviour
{
    [Header("References")]
    public Transform mouthTarget;

    [Header("Scale Settings")]
    public float minScale = 0.4f;
    public float shrinkSpeed = 0.5f;
    public float returnSpeed = 5f;

    [Header("Tilt Settings")]
    public float tiltAngle = -45f;
    public float tiltDistance = 2f;
    public float rotateSpeed = 8f;

    [Header("Snap Settings")]
    public float snapDistance = 1f;

    [Header("Delay")]
    public float stayDuration = 3f;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isDragging = false;
    private bool snappedToMouth = false;
    private bool returning = false;

    private Vector3 offset;
    private float zDepth; // distance from camera to this object

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Cache z-depth once — how far this object is from the camera
        zDepth = Camera.main.WorldToScreenPoint(transform.position).z;

        // Auto-add a Collider2D if none exists so OnMouse* events fire
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
            Debug.LogWarning("MilkGlassDrag: No Collider2D found — added BoxCollider2D automatically.");
        }
    }

    void Update()
    {
        if (snappedToMouth)
            return;

        // ---- Touch input (mobile) ----
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
                    if (isDragging)
                        transform.position = touchWorld + offset;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                        EndDrag();
                    break;
            }
        }

        // ---- Animations that run every frame ----
        if (isDragging)
        {
            HandleScale();
            HandleTilt();
        }
        else if (returning)
        {
            ReturnToOriginal();
        }
    }

    // ---------------------------------------------------------------
    // Mouse input (editor / PC) — still works alongside touch
    // ---------------------------------------------------------------
    void OnMouseDown()
    {
        Debug.Log("HIT: " + gameObject.name);
        if (snappedToMouth) return;
        TryBeginDrag(ScreenToWorld(Input.mousePosition));
    }

    void OnMouseDrag()
    {
        if (snappedToMouth || !isDragging) return;
        transform.position = ScreenToWorld(Input.mousePosition) + offset;
    }

    void OnMouseUp()
    {
        if (snappedToMouth || !isDragging) return;
        EndDrag();
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    /// <summary>Convert screen point to world XY, keeping the sprite's z-depth.</summary>
    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        screenPos.z = zDepth;
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    private void TryBeginDrag(Vector3 worldPos)
    {
        isDragging = true;
        returning = false;
        offset = transform.position - worldPos;
    }

    private void EndDrag()
    {
        isDragging = false;

        float distance = Vector3.Distance(transform.position, mouthTarget.position);

        if (distance <= snapDistance)
        {
            transform.GetChild(0).GetComponent<Animation>().Play("MilkAnim");

            snappedToMouth = true;
            transform.position = mouthTarget.position;
            transform.rotation = Quaternion.Euler(0, 0, tiltAngle);

            print("distance = " + distance);
            mouthTarget.parent.GetChild(0).GetComponent<Animation>().Play("GlowUpBody");
            StartCoroutine(ReturnAfterDelay());
            return;
        }

        returning = true;
    }

    // ---------------------------------------------------------------

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(stayDuration);

        print("Hellow");
        print(mouthTarget.parent.GetChild(0).name);
        SceneManager.instance.ActivateParticleSystem();

        snappedToMouth = false;
        returning = true;
    }

    private void HandleScale()
    {
        Vector3 s = transform.localScale;
        s -= Vector3.one * shrinkSpeed * Time.deltaTime;
        s.x = Mathf.Max(s.x, minScale);
        s.y = Mathf.Max(s.y, minScale);
        s.z = Mathf.Max(s.z, minScale);
        transform.localScale = s;
    }

    private void HandleTilt()
    {
        float distance = Vector3.Distance(transform.position, mouthTarget.position);

        Quaternion target = distance <= tiltDistance
            ? Quaternion.Euler(0, 0, tiltAngle)
            : originalRotation;

        transform.rotation = Quaternion.Lerp(
            transform.rotation, target, Time.deltaTime * rotateSpeed);
    }

   

    private void ReturnToOriginal()
    {
        transform.position = Vector3.Lerp(
            transform.position, originalPosition, Time.deltaTime * returnSpeed);

        transform.localScale = Vector3.Lerp(
            transform.localScale, originalScale, Time.deltaTime * returnSpeed);

        transform.rotation = Quaternion.Lerp(
            transform.rotation, originalRotation, Time.deltaTime * rotateSpeed);

        if (Vector3.Distance(transform.position, originalPosition) < 0.01f)
        {
            transform.position = originalPosition;
            transform.localScale = originalScale;
            transform.rotation = originalRotation;
            returning = false;
            print("true");
        }
    }
}