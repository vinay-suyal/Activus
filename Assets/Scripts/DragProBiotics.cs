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

    private RectTransform rectTransform;
    private Canvas canvas;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    [SerializeField] GameObject human;

    private bool droppedOnBody = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        originalPosition = rectTransform.position;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.rotation;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        droppedOnBody = false;

        // Increase size
        rectTransform.localScale = originalScale * scaleMultiplier;

        // Rotate a little
        rectTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(humanBodyArea,eventData.position,canvas.worldCamera))
        {
            droppedOnBody = true;

            rectTransform.localScale = originalScale;
            rectTransform.rotation = originalRotation;
            SceneManager.instance.SpeedUpParticleSystem();

            transform.parent.parent.GetComponent<Scene>().ChangeSceneIn(10);
            human.transform.GetComponent<Animation>().Play("GlowInstestine");
            Disable();
            //StartCoroutine(FadeOutProBiotics());
            transform.GetChild(0).GetChild(0).GetComponent<Animation>().Play("Probiotics Fade");
        }
        else
        {
            StartCoroutine(MoveBack());
        }
    }

    System.Collections.IEnumerator MoveBack()
    {
        Vector3 startPos = rectTransform.position;
        Vector3 startScale = rectTransform.localScale;
        Quaternion startRot = rectTransform.rotation;

        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * moveSpeed;

            rectTransform.position =
                Vector3.Lerp(startPos, originalPosition, time);

            rectTransform.localScale =
                Vector3.Lerp(startScale, originalScale, time);

            rectTransform.rotation =
                Quaternion.Lerp(startRot, originalRotation, time);

            yield return null;
        }

        rectTransform.position = originalPosition;
        rectTransform.localScale = originalScale;
        rectTransform.rotation = originalRotation;
    }

    IEnumerator FadeOutProBiotics()
    {
        Image img = GetComponent<Image>();

        float start = 1f;
        float end = 0f;

        float st = 0f;
        float tt = 0.5f;

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

    public GameObject BlueCircle;
    //public GameObject text;
    public GameObject line;

    void Disable()
    {
        BlueCircle.SetActive(false);
      
        line.SetActive(false);
    }
}