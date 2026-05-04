using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    public AudioSource audioSource;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogError("Chưa gán AudioSource hoặc AudioClip!");
            return;
        }

        audioSource.loop = true;
        audioSource.Play();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        Debug.Log("Scene: " + sceneName);

        // ✅ CHỈ CHO PHÁT Ở MAINMENU + LOBBY
        if (sceneName == "mainmenu" || sceneName == "lobby")
        {
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            StopMusic(); // ❌ scene khác → dừng
        }
    }

    public void StopMusic()
    {
        // 🔥 tránh crash
        if (audioSource != null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; // 🔥 tránh callback sau khi destroy
        Destroy(gameObject);
    }
}