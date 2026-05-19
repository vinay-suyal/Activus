using UnityEngine;

public enum SFXType
{
    BoxAppear,
    Drinking,
    Glass,
    PowerUp,
    PickUp,
    NextBtn,
    Sliding,
    Heal
}

[System.Serializable]
public class SFXClip
{
    public SFXType sfxType;
    public AudioClip clip;
    //[Range(0f, 1f)] public float volume = 1f;
    //[Range(0.5f, 1.5f)] public float pitch = 1f;
}

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private SFXClip[] sfxClips;

    private void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play a sound effect by SFXType enum.
    /// </summary>
    public void Play(SFXType type)
    {
        SFXClip sfx = System.Array.Find(sfxClips, s => s.sfxType == type);

        if (sfx == null || sfx.clip == null)
        {
            Debug.LogWarning($"[SFXManager] Clip not found for: {type}");
            return;
        }

        sfxSource.pitch = 1;
        sfxSource.PlayOneShot(sfx.clip, 1);
    }

    /// <summary>
    /// Play with random pitch variation for natural feel.
    /// </summary>
    public void PlayWithVariation(SFXType type, float pitchVariance = 0.1f)
    {
        SFXClip sfx = System.Array.Find(sfxClips, s => s.sfxType == type);

        if (sfx == null || sfx.clip == null)
        {
            Debug.LogWarning($"[SFXManager] Clip not found for: {type}");
            return;
        }

        sfxSource.pitch = 1 + Random.Range(-pitchVariance, pitchVariance);
        sfxSource.PlayOneShot(sfx.clip, 1);
    }

    /// <summary>
    /// Set master SFX volume.
    /// </summary>
    public void SetVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Mute / unmute all SFX.
    /// </summary>
    public void SetMute(bool mute)
    {
        sfxSource.mute = mute;
    }
}