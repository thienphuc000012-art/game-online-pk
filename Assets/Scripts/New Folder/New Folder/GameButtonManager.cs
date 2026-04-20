using Fusion;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameButtonManager : MonoBehaviour
{
    public Button backToMenuButton;      
    public Button rematchButton;   
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
    }

  
    public async void BackToMenuClicked()
    {
        if (_spawner != null && _spawner.LobbyStateRef != null)
        {
            var runner = _spawner.GetComponent<NetworkRunner>();
            if (runner != null)
            {
                await runner.Shutdown();
                Destroy(runner);
            }
        }
        // load scene Main memu
        SceneManager.LoadScene(0);
    }

    public async void RematchClicked()
    {
        if (_spawner != null)
        {
            await _spawner.LeaveRoomAndReturnToLobby();
        }
    }
    public async void QuitClicked()
    {
        if (_spawner != null && _spawner.gameObject != null) { var runner = _spawner.GetComponent<NetworkRunner>(); 
        if (runner != null) { await runner.Shutdown(); Destroy(runner); } }
        Application.Quit(); 
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
        #endif
    }
    public void ShowWinBecauseOpponentLeft()
    {
        if (_alreadyHandledDisconnect) return;
        _alreadyHandledDisconnect = true;


        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}