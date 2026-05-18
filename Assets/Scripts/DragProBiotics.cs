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

    // =========================================================================

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        _animation = GetComponent<Animation>();

        // Capture original values AFTER intro anim finishes
        StartCoroutine(WaitForIntroAnim());
    }

    private IEnumerator WaitForIntroAnim()
    {
        introComplete = false;
        yield return new WaitForSeconds(introAnimDuration);

        // Snapshot correct values at scale/position 1 (not mid-animation)
        originalPosition = rectTransform.position;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.rotation;
        introComplete = true;
    }

    // Call this from an Animation Event on the last frame of the intro clip
    // as an alternative to the timer above
    public void OnIntroAnimComplete()
    {
        StopAllCoroutines();
        originalPosition = rectTransform.position;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.rotation;
        introComplete = true;
    }

    // =========================================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!introComplete) return;

        droppedOnBody = false;

        // Stop animation overriding transform during drag
        if (_animation != null) _animation.enabled = false;

        rectTransform.localScale = originalScale * scaleMultiplier;
        rectTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!introComplete) return;
        rectTransform.position = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!introComplete) return;

        if (RectTransformUtility.RectangleContainsScreenPoint(
                humanBodyArea, eventData.position, canvas.worldCamera))
        {
            droppedOnBody = true;
            DragAndDrop.SetActive(false);
            particle.gameObject.SetActive(true);
            particle.Play();
            rectTransform.localScale = originalScale;
            rectTransform.rotation = originalRotation;
            SceneManager.instance.SpeedUpParticleSystem();
            transform.parent.parent.GetComponent<Scene>().ChangeSceneIn(5);
            human.transform.GetComponent<Animation>().Play("GlowInstestine");
            blueOutlien.GetComponent<Animation>().Play("BlueOutline");
            transform.GetChild(0).GetChild(0).GetComponent<Animation>().Play("Probiotics Fade");
        }
        else
        {
            StartCoroutine(MoveBack());
        }
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

        // Re-enable animation after returning to original position
        if (_animation != null) _animation.enabled = true;
    }

    IEnumerator FadeOutProBiotics()
    {
        Image img = GetComponent<Image>();
        float start = 1f, end = 0f, st = 0f, tt = 0.5f;
        Color c = img.color;

        while (st < tt)
        {
            st += Time.deltaTime;
            c.a = Mathf.Lerp(start, end, st / tt);
            img.color = c;
            yield return null;
        }

        c.a = 0f;
        img.color = c;
    }
}