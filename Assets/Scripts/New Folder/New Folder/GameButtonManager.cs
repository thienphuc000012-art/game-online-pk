using Fusion;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameButtonManager : MonoBehaviour
{
    [Header("=== BUTTONS (kéo thả từ Inspector) ===")]
    public Button quitButton;      
    public Button rematchButton;   

    [Header("=== PANELS (kéo từ GameUI) ===")]
    public GameObject winPanel;


    private BasicSpawner _spawner;
    private bool _alreadyHandledDisconnect = false;

    private void Awake()
    {
        _spawner = FindFirstObjectByType<BasicSpawner>();
    }

    private void Start()
    {

        if (winPanel != null) winPanel.SetActive(false);
     

        Debug.Log("[GameButtonManager] Ready - Quit = Thoát game hoàn toàn");
    }

  
    public async void QuitClicked()
    {
      //  Debug.Log("[GameButtonManager] QuitClicked → Thoát game hoàn toàn");

    
        if (_spawner != null && _spawner.gameObject != null)
        {
            var runner = _spawner.GetComponent<NetworkRunner>();
            if (runner != null)
            {
                await runner.Shutdown();
                Destroy(runner);
            }
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public async void RematchClicked()
    {
        Debug.Log("[GameButtonManager] RematchClicked → Quay về Lobby");
        if (_spawner != null)
        {
            await _spawner.LeaveRoomAndReturnToLobby();
        }
    }

    public void ShowWinBecauseOpponentLeft()
    {
        if (_alreadyHandledDisconnect) return;
        _alreadyHandledDisconnect = true;

        Debug.Log("[GameButtonManager] Đối thủ thoát → Hiển thị WIN");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}