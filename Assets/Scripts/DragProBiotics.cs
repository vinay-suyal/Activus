using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragProBiotics : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Drop Settings")]
    public RectTransform humanBodyArea;

    [Header("Drag Effects")]
    public float scaleMultiplier = 1.15f;
    public float rotationAngle = 15f;
    public float moveSpeed = 10f;

    [Header("Intro Animation")]
    [Tooltip("Match this to the duration of your intro animation so drag is blocked until it finishes")]
    public float introAnimDuration = 1f;

    [Header("Hold To Snap")]
    [Tooltip("How long user must hold inside the body area before auto-snap triggers")]
    public float holdSnapDuration = 0.5f;
    public float snapMoveSpeed = 6f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Animation _animation;
    private bool introComplete = false;

    [SerializeField] GameObject blueOutlien;
    [SerializeField] GameObject human;
    [SerializeField] GameObject DragAndDrop;
    public ParticleSystem particle;

    private bool droppedOnBody = false;

    // Hold timer state
    private float holdTimer = 0f;
    private bool isHoldingOverBody = false;
    private bool holdSnapTriggered = false;
    private bool isDragging = false;

    // =========================================================================

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        _animation = GetComponent<Animation>();

        StartCoroutine(WaitForIntroAnim());
    }

    private IEnumerator WaitForIntroAnim()
    {
        introComplete = false;
        yield return new WaitForSeconds(introAnimDuration);
        originalPosition = rectTransform.position;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.rotation;
        introComplete = true;
    }

    public void OnIntroAnimComplete()
    {
        StopAllCoroutines();
        originalPosition = rectTransform.position;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.rotation;
        introComplete = true;
    }

    // =========================================================================

    void Update()
    {
        if (!introComplete || !isDragging || holdSnapTriggered) return;

        // Check if currently hovering over body area
        Vector2 screenPos;
#if UNITY_EDITOR
        screenPos = Input.mousePosition;
#else
        if (Input.touchCount == 0) return;
        screenPos = Input.GetTouch(0).position;
#endif

        bool insideBody = RectTransformUtility.RectangleContainsScreenPoint(
            humanBodyArea, screenPos, canvas.worldCamera);

        if (insideBody)
        {
            if (!isHoldingOverBody)
            {
                isHoldingOverBody = true;
                holdTimer = 0f;
            }

            holdTimer += Time.deltaTime;

            if (holdTimer >= holdSnapDuration)
            {
                Vibration.instance.Vibrate(100);
                holdSnapTriggered = true;
                isDragging = false;
                isHoldingOverBody = false;
                DragAndDrop.GetComponent<Animation>().Play("Drag&Drop2");
                StartCoroutine(SnapToCenterAndFade());
            }
        }
        else
        {
            isHoldingOverBody = false;
            holdTimer = 0f;
        }
    }

    // =========================================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        Vibration.instance.Vibrate(30);
        if (!introComplete || holdSnapTriggered) return;

        droppedOnBody = false;
        isDragging = true;
        holdTimer = 0f;
        isHoldingOverBody = false;

        if (_animation != null) _animation.enabled = false;

        rectTransform.localScale = originalScale * scaleMultiplier;
        rectTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);

        SFXManager.instance.Play(SFXType.NextBtn);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!introComplete || holdSnapTriggered) return;
        rectTransform.position = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!introComplete) return;
        if (holdSnapTriggered) return;   // hold-snap already took over

        isDragging = false;
        isHoldingOverBody = false;
        holdTimer = 0f;

        if (RectTransformUtility.RectangleContainsScreenPoint(
                humanBodyArea, eventData.position, canvas.worldCamera))
        {
            SFXManager.instance.Play(SFXType.Sliding);
            TriggerSuccess();
        }
        else
        {
            StartCoroutine(MoveBack());
        }
    }

    // =========================================================================
    // Hold snap: smoothly move to center of humanBodyArea, then fade
    // =========================================================================

    private IEnumerator SnapToCenterAndFade()
    {
        SFXManager.instance.Play(SFXType.Heal);
        GetComponent<Collider2D>().enabled = false;

        // Get canvas center in world space
        Vector3 targetPos = canvas.GetComponent<RectTransform>().position;

        while (Vector3.Distance(rectTransform.position, targetPos) > 2f)
        {
            rectTransform.position = Vector3.Lerp(
                rectTransform.position, targetPos, Time.deltaTime * snapMoveSpeed);
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale, originalScale, Time.deltaTime * snapMoveSpeed);
            rectTransform.rotation = Quaternion.Lerp(
                rectTransform.rotation, originalRotation, Time.deltaTime * snapMoveSpeed);
            yield return null;
        }

        rectTransform.position = targetPos;

        yield return StartCoroutine(FadeOutProBiotics());

        TriggerSuccess();
    }

    // =========================================================================
    // Shared success logic
    // =========================================================================

    private void TriggerSuccess()
    {
        droppedOnBody = true;
        DragAndDrop.SetActive(false);
        particle.gameObject.SetActive(true);
        particle.Play();
        rectTransform.localScale = originalScale;
        rectTransform.rotation = originalRotation;
        SceneManager.instance.SpeedUpParticleSystem();
        transform.parent.parent.GetComponent<Scene>().ChangeSceneIn(3);
        human.transform.GetComponent<Animation>().Play("GlowInstestine");

        // FIX: Only play the "Probiotics Fade" animation when dropped directly
        // (OnPointerUp path). When hold-snap is used, FadeOutProBiotics() already
        // faded the object out, so playing the animation again here would cause
        // a double fade. holdSnapTriggered is the reliable flag for this distinction.
        if (!holdSnapTriggered)
            transform.GetChild(0).GetChild(0).GetComponent<Animation>().Play("Probiotics Fade");
    }

    // =========================================================================

    IEnumerator MoveBack()
    {
        Vector3 startPos = rectTransform.position;
        Vector3 startScale = rectTransform.localScale;
        Quaternion startRot = rectTransform.rotation;
        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * moveSpeed;
            rectTransform.position = Vector3.Lerp(startPos, originalPosition, time);
            rectTransform.localScale = Vector3.Lerp(startScale, originalScale, time);
            rectTransform.rotation = Quaternion.Lerp(startRot, originalRotation, time);
            yield return null;
        }

        rectTransform.position = originalPosition;
        rectTransform.localScale = originalScale;
        rectTransform.rotation = originalRotation;

        if (_animation != null) _animation.enabled = true;
    }

    IEnumerator FadeOutProBiotics()
    {
        Vibration.instance.Vibrate(150);
        //blueOutlien.GetComponent<Animation>().Play("BlueOutline");
        blueOutlien.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(false);
        RawImage img1 = transform.GetChild(0).GetChild(0).GetComponent<RawImage>();
        Image img2 = transform.GetChild(1).GetComponent<Image>();

        float st = 0f, tt = 0.5f;
        Color c1 = img1.color;
        Color c2 = img2.color;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;

        while (st < tt)
        {
            st += Time.deltaTime;
            float t = st / tt;

            // Fade
            c1.a = Mathf.Lerp(1f, 0f, t);
            c2.a = c1.a;
            img1.color = c1;
            img2.color = c2;

            // Scale
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        // Finalize
        c1.a = 0f;
        c2.a = 0f;
        img1.color = c1;
        img2.color = c2;
        transform.localScale = Vector3.zero;

        blueOutlien.SetActive(false);
    }
}