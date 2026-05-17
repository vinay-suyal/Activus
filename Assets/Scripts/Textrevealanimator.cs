using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TextRevealAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-grab from this GameObject.")]
    public TMP_Text textComponent;

    // -------------------------------------------------------------------------
    [Header("Audio (Optional)")]
    [Tooltip("Drag your voiceover AudioClip here. Leave empty for text-only animation.")]
    public AudioClip voiceoverClip;

    [Tooltip("AudioSource used to play the clip. Leave empty to auto-create one — " +
             "only created if a Voiceover Clip is assigned.")]
    public AudioSource audioSource;

    [Tooltip("Volume of the voiceover (0 = silent, 1 = full).")]
    [Range(0f, 1f)]
    public float voiceoverVolume = 1f;

    // -------------------------------------------------------------------------
    [Header("Sound Manager Hook")]
    [Tooltip("Fired the moment the text animation begins (after textStartDelay).")]
    public UnityEvent onAnimationStarted;

    [Tooltip("Fired when every letter has finished animating.")]
    public UnityEvent onAnimationCompleted;

    // -------------------------------------------------------------------------
    [Header("Timing")]
    [Tooltip("Seconds to wait before the TEXT animation begins. Text is fully hidden during this time.")]
    public float textStartDelay = 0f;

    [Tooltip("Seconds to wait before the AUDIO plays.")]
    public float audioStartDelay = 0f;

    [Tooltip("Seconds between each letter appearing.")]
    public float delayBetweenLetters = 0.05f;

    [Tooltip("Duration of the bounce + fade animation for each letter.")]
    public float letterAnimDuration = 0.35f;

    // -------------------------------------------------------------------------
    [Header("Bounce Settings")]
    [Tooltip("How many units (in TMP local space) the letter starts below its rest position.")]
    public float bounceHeight = 20f;

    [Tooltip("Animation curve controlling the bounce. Default: overshoot then settle.")]
    public AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 4f),
        new Keyframe(0.6f, 1.1f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    // -------------------------------------------------------------------------
    [Header("Fade Settings")]
    [Tooltip("Animation curve controlling opacity (0 = transparent, 1 = opaque).")]
    public AnimationCurve fadeCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f)
    );

    // -------------------------------------------------------------------------
    private Vector3[][] _originalVerts;

    private Coroutine _audioCoroutine;
    private Coroutine _textCoroutine;

    // -------------------------------------------------------------------------
    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        if (voiceoverClip != null && audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>()
                       ?? gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        PlayAnimation();
    }

    // -------------------------------------------------------------------------
    /// <summary>Stops all coroutines cleanly when the object is destroyed.</summary>
    private void OnDestroy()
    {
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        if (_textCoroutine != null) StopCoroutine(_textCoroutine);
    }

    // -------------------------------------------------------------------------
    /// <summary>Call this to (re)start the reveal animation.</summary>
    public void PlayAnimation()
    {
        // Guard — if the component is already destroyed, do nothing.
        if (this == null || !gameObject.activeInHierarchy) return;

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        if (_textCoroutine != null) StopCoroutine(_textCoroutine);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        // ForceMeshUpdate FIRST so meshInfo exists before we touch alpha.
        textComponent.ForceMeshUpdate();
        SetAllCharactersAlpha(0);

        // Disable renderer to prevent any flash during the delay.
        textComponent.enabled = false;

        if (voiceoverClip != null && audioSource != null)
            _audioCoroutine = StartCoroutine(AudioDelayRoutine());

        _textCoroutine = StartCoroutine(AnimateText());
    }

    // -------------------------------------------------------------------------
    private IEnumerator AudioDelayRoutine()
    {
        if (audioStartDelay > 0f)
            yield return new WaitForSeconds(audioStartDelay);

        // Guard in case object was destroyed during the delay.
        if (this == null || audioSource == null) yield break;

        audioSource.clip = voiceoverClip;
        audioSource.volume = voiceoverVolume;
        audioSource.Play();
    }

    // -------------------------------------------------------------------------
    private IEnumerator AnimateText()
    {
        if (textStartDelay > 0f)
            yield return new WaitForSeconds(textStartDelay);

        // Guard in case object was destroyed during the delay.
        if (this == null || textComponent == null) yield break;

        textComponent.enabled = true;
        textComponent.ForceMeshUpdate();
        SetAllCharactersAlpha(0);

        onAnimationStarted?.Invoke();

        TMP_TextInfo textInfo = textComponent.textInfo;
        int charCount = textInfo.characterCount;

        _originalVerts = new Vector3[textInfo.meshInfo.Length][];
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            Vector3[] src = textInfo.meshInfo[m].vertices;
            _originalVerts[m] = new Vector3[src.Length];
            src.CopyTo(_originalVerts[m], 0);
        }

        for (int i = 0; i < charCount; i++)
        {
            // Guard each iteration in case scene changes mid-animation.
            if (this == null || textComponent == null) yield break;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            StartCoroutine(AnimateLetter(i));
            yield return new WaitForSeconds(delayBetweenLetters);
        }

        yield return new WaitForSeconds(letterAnimDuration);

        if (this == null || textComponent == null) yield break;

        onAnimationCompleted?.Invoke();
    }

    // -------------------------------------------------------------------------
    private IEnumerator AnimateLetter(int charIndex)
    {
        // Guard before starting.
        if (this == null || textComponent == null) yield break;

        TMP_TextInfo textInfo = textComponent.textInfo;
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

        int meshIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        float elapsed = 0f;

        while (elapsed < letterAnimDuration)
        {
            // Guard every frame — exits cleanly if object is destroyed mid-animation.
            if (this == null || textComponent == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / letterAnimDuration);

            float bounceT = bounceCurve.Evaluate(t);
            float yOffset = Mathf.Lerp(-bounceHeight, 0f, bounceT);
            byte alpha = (byte)(fadeCurve.Evaluate(t) * 255f);

            Vector3[] liveVerts = textComponent.textInfo.meshInfo[meshIndex].vertices;
            for (int v = 0; v < 4; v++)
            {
                Vector3 orig = _originalVerts[meshIndex][vertexIndex + v];
                liveVerts[vertexIndex + v] = new Vector3(orig.x, orig.y + yOffset, orig.z);
            }

            Color32[] colors = textComponent.textInfo.meshInfo[meshIndex].colors32;
            for (int v = 0; v < 4; v++)
                colors[vertexIndex + v].a = alpha;

            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices |
                                           TMP_VertexDataUpdateFlags.Colors32);

            yield return null;
        }

        if (this == null || textComponent == null) yield break;

        // Snap to exact final state.
        Vector3[] finalVerts = textComponent.textInfo.meshInfo[meshIndex].vertices;
        Color32[] finalColors = textComponent.textInfo.meshInfo[meshIndex].colors32;
        for (int v = 0; v < 4; v++)
        {
            finalVerts[vertexIndex + v] = _originalVerts[meshIndex][vertexIndex + v];
            finalColors[vertexIndex + v].a = 255;
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices |
                                       TMP_VertexDataUpdateFlags.Colors32);
    }

    // -------------------------------------------------------------------------
    private void SetAllCharactersAlpha(byte alpha)
    {
        if (textComponent == null) return;

        TMP_TextInfo textInfo = textComponent.textInfo;
        if (textInfo == null || textInfo.meshInfo == null) return;

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            Color32[] colors = textInfo.meshInfo[m].colors32;
            if (colors == null) continue;

            for (int c = 0; c < colors.Length; c++)
                colors[c].a = alpha;
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}