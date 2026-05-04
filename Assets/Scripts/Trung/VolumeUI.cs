using UnityEngine;
using UnityEngine.UI;
public class VolumeUI : MonoBehaviour
{
    [Header("Music")]
    public Slider musicSlider;
    public AudioSource musicSource;
    public Image musicIcon;
    public Sprite iconOn;
    public Sprite iconOff;
    private float lastMusicVolume = 1f;
    [Header("SFX")]
    public Slider sfxSlider;
    public AudioSource sfxSource;
    public Image sfxIcon;
    public Sprite sfxOn;
    public Sprite sfxOff;
    private float lastSFXVolume = 1f;
    private float tempMusicVol;
    private float tempSFXVol;
    private void Start()
    {
       // LẤY MUSIC SOURCE TỪ MUSIC MANAGER
        if (MusicManager.Instance != null)
        {
            musicSource = MusicManager.Instance.audioSource;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy MusicManager!");
            return;
        }

        float vol = PlayerPrefs.GetFloat("MusicVol", 1f);

        musicSlider.value = vol;
        musicSource.volume = vol;

        lastMusicVolume = vol;

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        UpdateMusicIcon(vol);

        // ===== SFX giữ nguyên =====
        float sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);
        sfxSlider.value = sfxVol;
        sfxSource.volume = sfxVol;

        lastSFXVolume = sfxVol;
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        UpdateSFXIcon(sfxVol);
    }
    void OnMusicChanged(float value)
    {
        musicSource.volume = value;

        if (value > 0)
            lastMusicVolume = value;

        UpdateMusicIcon(value);
    }

    // ================= ICON CLICK =================
    public void ToggleMusic()
    {
        if (musicSlider.value > 0)
        {
            // 🔇 mute
            musicSlider.value = 0;
            UpdateMusicIcon(0);
        }
        else
        {
            // 🔊 bật lại
            float restore = lastMusicVolume > 0 ? lastMusicVolume : 1f;
            musicSlider.value = restore;
            UpdateMusicIcon(restore);
        }
    }

    // ================= ICON UPDATE =================
    void UpdateMusicIcon(float value)
    {
        if (value <= 0)
            musicIcon.sprite = iconOff;
        else
            musicIcon.sprite = iconOn;
    }
    void OnSFXChanged(float value)
    {
        sfxSource.volume = value;

        // 🔥 update toàn bộ audio trong scene
        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();

        foreach (AudioSource a in allAudio)
        {
            // bỏ qua music
            if (a != musicSource)
            {
                a.volume = value;
            }
        }

        if (value > 0)
            lastSFXVolume = value;

        UpdateSFXIcon(value);
    }

    public void ToggleSFX()
    {
        if (sfxSlider.value > 0)
        {
            sfxSlider.value = 0;
            UpdateSFXIcon(0); 
        }
        else
        {
            float restore = lastSFXVolume > 0 ? lastSFXVolume : 1f;
            sfxSlider.value = restore;
            UpdateSFXIcon(restore); 
        }
    }

    void UpdateSFXIcon(float value)
    {
        sfxIcon.sprite = value <= 0 ? sfxOff : sfxOn;
    }
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);

        PlayerPrefs.Save();

        Debug.Log("Saved!");
    }
    public void CacheCurrentVolume()
    {
        tempMusicVol = musicSlider.value;
        tempSFXVol = sfxSlider.value;
    }
    public void RevertVolume()
    {
       musicSlider.value = tempMusicVol;
        sfxSlider.value = tempSFXVol;
    }
}
