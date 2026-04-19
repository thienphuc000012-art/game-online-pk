using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public GameObject panelLobby;
    public GameObject panelRoom;
    public GameObject panelCharacterSelect;

    public TMP_InputField inputPlayerName;
    public Button buttonOk;
    public TMP_InputField inputRoomName;
    public Button buttonCreateRoom;

    public GameObject panelRoomList;
    public GameObject roomItemPrefab;

    public Image[] characterIcons;
    public Image fullCharacterImageHost;
    public Image fullCharacterImageClient;

    public Button lockButtonHost;
    public Button lockButtonClient;
    public Button lockMapButton;
    public Button startGameButton;

    public Image[] mapIcons;
    public TMP_Text hostPlayerLabel;
    public TMP_Text clientPlayerLabel;

    public Button leaveRoomButton;

    public Color selectedMapColor = Color.green;
    private Color _normalMapColor = Color.white;

    public bool IsHost;
    public BasicSpawner spawner;
    [Header("=== FULL CHARACTER SPRITES (hình lớn) ===")]
    public Sprite[] fullCharacterSprites;   // Kéo thả sprite full vào đây trong Inspector
    async void Start()
    {
        panelLobby.SetActive(true);
        panelRoom.SetActive(false);
        panelCharacterSelect.SetActive(false);

        buttonOk.onClick.AddListener(OnClickOk);
        buttonCreateRoom.onClick.AddListener(OnClickCreateRoom);

        spawner = FindFirstObjectByType<BasicSpawner>();
        if (spawner != null)
        {
            spawner.LobbyManager = this;
            if (!spawner.IsInlobby && !spawner.IsStartingLobby)
                await spawner.StarLobby();
        }

        for (int i = 0; i < characterIcons.Length; i++)
        {
            int index = i;
            characterIcons[i].GetComponent<Button>().onClick.AddListener(() => OnClickCharacterIcon(index));
        }

        for (int i = 0; i < mapIcons.Length; i++)
        {
            int index = i;
            mapIcons[i].GetComponent<Button>().onClick.AddListener(() => OnClickMapIcon(index));
        }
    }

    public void OnClickOk()
    {
        string playerName = inputPlayerName.text;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name cannot be empty!");
            return;
        }

        var profile = new PlayerClassInfo() { Name = playerName, Class = PlayerClass.None };
        spawner.SetLocalPlayerProfile(profile);

        panelLobby.SetActive(false);
        panelRoom.SetActive(true);

        Debug.Log("[LobbyManager] PanelRoom activated → Force refresh room list");

        // ====================== FORCE SHOW DANH SÁCH PHÒNG ======================
        if (spawner != null && spawner.LastSessionList != null)
        {
            UpdateRoomList(spawner.LastSessionList);
        }
        // =======================================================================
    }

    public async void OnClickCreateRoom()
    {
        var roomName = inputRoomName.text;
        if (string.IsNullOrEmpty(roomName)) return;

        await spawner.StarLobby();
        var currentScene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        await spawner.StartHost(roomName, currentScene);

        IsHost = true;
        ShowCharacterSelectPanel();
    }

    private readonly List<SessionInfo> _roomEntries = new();

    public void UpdateRoomList(List<SessionInfo> sessionList)
    {
        Debug.Log($"[LobbyManager] UpdateRoomList called - Found {sessionList.Count} rooms");

        // Xóa hết item cũ
        foreach (Transform child in panelRoomList.transform)
            Destroy(child.gameObject);

        _roomEntries.Clear();

        // Tạo item mới
        foreach (var session in sessionList)
        {
            var roomItem = Instantiate(roomItemPrefab, panelRoomList.transform);

            roomItem.GetComponentInChildren<TMP_Text>().text =
                $"{session.Name} ({session.PlayerCount} / {session.MaxPlayers})";

            // ====================== FIX STALE REFERENCE ======================
            // Không capture "spawner" cũ nữa → luôn tìm spawner mới nhất
            roomItem.GetComponentInChildren<Button>().onClick.AddListener(async () =>
            {
                var currentSpawner = FindFirstObjectByType<BasicSpawner>();
                if (currentSpawner == null)
                {
                    Debug.LogError("[Join Room] Không tìm thấy BasicSpawner!");
                    return;
                }

                await currentSpawner.StartClient(session.Name);
                IsHost = false;
                ShowCharacterSelectPanel();
            });
            // ================================================================

            roomItem.SetActive(true);
            _roomEntries.Add(session);
        }
    }

    public void OnClickCharacterIcon(int index)
    {
        var selectedClass = (PlayerClass)index;

        // === SỬA Ở ĐÂY ===
        if (IsHost)
            fullCharacterImageHost.sprite = fullCharacterSprites[index];
        else
            fullCharacterImageClient.sprite = fullCharacterSprites[index];

        if (spawner.LobbyStateRef != null)
            spawner.LobbyStateRef.RPC_SetSelectedClass(selectedClass, IsHost);
    }
    public void OnClickMapIcon(int index)
    {
        if (!IsHost) return;
        spawner.LockMap(index);

        for (int i = 0; i < mapIcons.Length; i++)
            mapIcons[i].color = (i == index) ? selectedMapColor : _normalMapColor;
    }

    public void ShowCharacterSelectPanel()
    {
        Debug.Log("[LobbyManager] ShowCharacterSelectPanel() CALLED - IsHost = " + IsHost);

        panelRoom.SetActive(false);
        panelCharacterSelect.SetActive(true);

        // ====================== FORCE HIỆN CẢ 2 NÚT ======================
        if (lockButtonHost != null)
        {
            lockButtonHost.gameObject.SetActive(true);
            Debug.Log("[LobbyManager] FORCE SetActive(true) cho lockButtonHost");
        }
        else
        {
            Debug.LogError("[LobbyManager] lockButtonHost == null ! Kiểm tra Inspector");
        }

        if (lockButtonClient != null)
        {
            lockButtonClient.gameObject.SetActive(true);
            Debug.Log("[LobbyManager] FORCE SetActive(true) cho lockButtonClient");
        }
        else
        {
            Debug.LogError("[LobbyManager] lockButtonClient == null ! Kiểm tra Inspector");
        }

        SetupLockButtons();

        StartCoroutine(SendPlayerNameCoroutine());

        foreach (var icon in mapIcons)
        {
            var btn = icon.GetComponent<Button>();
            if (btn != null) btn.interactable = IsHost;
        }

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.RemoveAllListeners();
            leaveRoomButton.onClick.AddListener(async () =>
            {
                if (spawner != null)
                    await spawner.LeaveRoomAndReturnToLobby();
            });
            leaveRoomButton.gameObject.SetActive(true);
        }

        if (spawner.LobbyStateRef != null)
            spawner.LobbyStateRef.ForceRefreshUI();

        else
        {
            Debug.LogWarning("[LobbyManager] LobbyStateRef chưa sẵn sàng");
        }
    }

    private IEnumerator SendPlayerNameCoroutine()
    {
        yield return null;
        yield return null;

        if (spawner != null && !string.IsNullOrEmpty(spawner.LocalPlayerProfile.Name))
        {
            bool isHostPlayer = IsHost;

            if (spawner.LobbyStateRef != null)
            {
                spawner.LobbyStateRef.RPC_SetPlayerName(spawner.LocalPlayerProfile.Name, isHostPlayer);
                Debug.Log($"[LobbyManager] Gửi tên: {spawner.LocalPlayerProfile.Name} → {(isHostPlayer ? "Host" : "Client")}");
            }
            else
            {
                Debug.LogWarning("[LobbyManager] LobbyStateRef vẫn null sau delay - không gửi tên được");
            }
        }
    }

    private void SetupLockButtons()
    {
        lockButtonHost.onClick.RemoveAllListeners();
        lockButtonClient.onClick.RemoveAllListeners();
        lockMapButton.onClick.RemoveAllListeners();
        startGameButton.onClick.RemoveAllListeners();

        lockButtonHost.onClick.AddListener(() => { if (spawner.LobbyStateRef != null) spawner.LobbyStateRef.RPC_SetLocked(true, true); });
        lockButtonClient.onClick.AddListener(() => { if (spawner.LobbyStateRef != null) spawner.LobbyStateRef.RPC_SetLocked(false, true); });
        lockMapButton.onClick.AddListener(() => { if (spawner.LobbyStateRef != null) spawner.LobbyStateRef.RPC_SetMapLocked(true); });
        startGameButton.onClick.AddListener(() => spawner.StartGameAfterSelection());
    }
    private void UpdateSelectionInteractables()
{

    if (spawner != null && spawner.LobbyStateRef != null)
        spawner.LobbyStateRef.ForceRefreshUI();
}
}