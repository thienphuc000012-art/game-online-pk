using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("=== AUDIO SOURCES ===")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("=== SFX CLIPS ===")]
    public AudioClip punchClip;
    public AudioClip kickClip;
    public AudioClip superHitClip;
    public AudioClip shootClip;
    public AudioClip koClip;

    [Header("=== BACKGROUND MUSIC ===")]
    public AudioClip backgroundMusic;

    [Header("=== SETTINGS ===")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;   

    [SerializeField] private bool randomPitch = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);


        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmVolume = 1f;   
        // sfxVolume = 1f; 

        sfxSource.volume = sfxVolume;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
    }
    public void PlayPunch() => PlaySFX(punchClip, 0.95f, 1.05f);
    public void PlayKick() => PlaySFX(kickClip, 0.9f, 1.1f);
    public void PlaySuperHit() => PlaySFX(superHitClip, 0.95f, 1.05f);
    public void PlayShoot() => PlaySFX(shootClip);
    public void PlayKO() => PlaySFX(koClip, 0.95f, 1.05f);

    public void PlayBGM()
    {
        if (backgroundMusic != null && bgmSource != null)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.Play();
        }
    }

    public void StopBGM() => bgmSource?.Stop();

    private void PlaySFX(AudioClip clip, float minPitch = 1f, float maxPitch = 1f)
    {
        if (clip == null || sfxSource == null) return;

        if (randomPitch && minPitch != maxPitch)
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
        else
            sfxSource.pitch = 1f;

        sfxSource.PlayOneShot(clip);
    }
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }
}