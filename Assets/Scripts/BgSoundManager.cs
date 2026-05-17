using UnityEngine;

public class BgSoundManager : MonoBehaviour
{
    public static BgSoundManager instance;

    [Header("Background Music")]
    public AudioClip bgMusic;
    public float volume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Persist across scenes if you ever use Unity scene loading
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.Play();
    }
}