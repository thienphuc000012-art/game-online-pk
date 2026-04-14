using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyState : NetworkBehaviour
{
    [Networked] public PlayerClass HostSelectedClass { get; set; } = PlayerClass.None;
    [Networked] public PlayerClass ClientSelectedClass { get; set; } = PlayerClass.None;
    [Networked] public string HostPlayerName { get; set; } = "";
    [Networked] public string ClientPlayerName { get; set; } = "";
    [Networked] public bool HostLocked { get; set; } = false;
    [Networked] public bool ClientLocked { get; set; } = false;
    [Networked] public bool MapLocked { get; set; } = false;

    [SerializeField] private BasicSpawner _spawner;

    private PlayerClass _lastHostClass = PlayerClass.None;
    private PlayerClass _lastClientClass = PlayerClass.None;
    private string _lastHostName = "";
    private string _lastClientName = "";
    private bool _lastHostLocked, _lastClientLocked, _lastMapLocked;

    private LobbyManager _lobbyManagerCache;

    private void Awake()
    {
        if (_spawner == null)
            _spawner = FindFirstObjectByType<BasicSpawner>();
    }

    public void SetSpawner(BasicSpawner spawner) => _spawner = spawner;

    private void Update()
    {
        if (HostSelectedClass != _lastHostClass)
        {
            _lastHostClass = HostSelectedClass;
            UpdateHostUI();
            if (_spawner != null) _spawner.HostSelectedClass = HostSelectedClass;
        }
        if (ClientSelectedClass != _lastClientClass)
        {
            _lastClientClass = ClientSelectedClass;
            UpdateClientUI();
            if (_spawner != null) _spawner.ClientSelectedClass = ClientSelectedClass;
        }
        if (HostPlayerName != _lastHostName)
        {
            _lastHostName = HostPlayerName;
            UpdateHostNameUI();
            Debug.Log($"[LobbyState] Host name updated: {HostPlayerName}");
        }
        if (ClientPlayerName != _lastClientName)
        {
            _lastClientName = ClientPlayerName;
            UpdateClientNameUI();
            Debug.Log($"[LobbyState] Client name updated: {ClientPlayerName}");
        }
        if (HostLocked != _lastHostLocked || ClientLocked != _lastClientLocked || MapLocked != _lastMapLocked)
        {
            _lastHostLocked = HostLocked;
            _lastClientLocked = ClientLocked;
            _lastMapLocked = MapLocked;
            UpdateLockUI();
            if (_spawner != null)
            {
                _spawner.HostLocked = HostLocked;
                _spawner.ClientLocked = ClientLocked;
                _spawner.MapLocked = MapLocked;
            }
        }
    }


    private void LateUpdate()
    {
        if (_lobbyManagerCache == null)
            _lobbyManagerCache = FindFirstObjectByType<LobbyManager>();

        if (_lobbyManagerCache != null)
        {
            if (_lobbyManagerCache.lockButtonHost != null)
                _lobbyManagerCache.lockButtonHost.gameObject.SetActive(true);

            if (_lobbyManagerCache.lockButtonClient != null)
                _lobbyManagerCache.lockButtonClient.gameObject.SetActive(true);
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        var spawnerObj = FindFirstObjectByType<BasicSpawner>();
        if (spawnerObj != null)
        {
            spawnerObj.LobbyStateRef = this;
            Debug.Log(" LobbyState spawned and referenced on all clients");
        }
        ForceRefreshUI();
    }

    public void ForceRefreshUI()
    {
        UpdateHostUI();
        UpdateClientUI();
        UpdateHostNameUI();
        UpdateClientNameUI();
        UpdateLockUI();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetSelectedClass(PlayerClass newClass, bool isHost)
    {
        if (isHost) HostSelectedClass = newClass;
        else ClientSelectedClass = newClass;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetLocked(bool isHost, bool locked)
    {
        if (isHost) HostLocked = locked;
        else ClientLocked = locked;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetMapLocked(bool locked) => MapLocked = locked;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(string newName, bool isHost)
    {
        if (isHost) HostPlayerName = newName;
        else ClientPlayerName = newName;
        Debug.Log($"[RPC] Nhận tên {(isHost ? "Host" : "Client")}: {newName}");
    }

    private void UpdateHostUI()
    {
        if (_lobbyManagerCache == null)
            _lobbyManagerCache = FindFirstObjectByType<LobbyManager>();

        if (_lobbyManagerCache != null && _lobbyManagerCache.fullCharacterImageHost != null)
        {
            int index = (int)HostSelectedClass;
            if (index >= 0 && index < _lobbyManagerCache.characterIcons.Length)
            {
                _lobbyManagerCache.fullCharacterImageHost.sprite = _lobbyManagerCache.characterIcons[index].sprite;

                _lobbyManagerCache.fullCharacterImageHost.rectTransform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }

    private void UpdateClientUI()
    {
        if (_lobbyManagerCache == null)
            _lobbyManagerCache = FindFirstObjectByType<LobbyManager>();

        if (_lobbyManagerCache != null && _lobbyManagerCache.fullCharacterImageClient != null)
        {
            int index = (int)ClientSelectedClass;
            if (index >= 0 && index < _lobbyManagerCache.characterIcons.Length)
            {
                _lobbyManagerCache.fullCharacterImageClient.sprite = _lobbyManagerCache.characterIcons[index].sprite;

    
                _lobbyManagerCache.fullCharacterImageClient.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            }
        }
    }

    private void UpdateHostNameUI()
    {
        if (_lobbyManagerCache == null) _lobbyManagerCache = FindFirstObjectByType<LobbyManager>();
        if (_lobbyManagerCache != null && _lobbyManagerCache.hostPlayerLabel != null)
            _lobbyManagerCache.hostPlayerLabel.text = string.IsNullOrEmpty(HostPlayerName) ? "P1" : HostPlayerName;
    }

    private void UpdateClientNameUI()
    {
        if (_lobbyManagerCache == null) _lobbyManagerCache = FindFirstObjectByType<LobbyManager>();
        if (_lobbyManagerCache != null && _lobbyManagerCache.clientPlayerLabel != null)
            _lobbyManagerCache.clientPlayerLabel.text = string.IsNullOrEmpty(ClientPlayerName) ? "P2" : ClientPlayerName;
    }

    private void UpdateLockUI()
    {
        if (_lobbyManagerCache == null)
            _lobbyManagerCache = FindFirstObjectByType<LobbyManager>();
        if (_lobbyManagerCache == null)
        {
            Debug.LogError("[UpdateLockUI] KHÔNG TÌM THẤY LobbyManagerCache!");
            return;
        }

        bool isLocalHost = _lobbyManagerCache.IsHost;
        Debug.Log($"[UpdateLockUI] === BẮT ĐẦU === IsLocalHost = {isLocalHost} | HostLocked = {HostLocked} | ClientLocked = {ClientLocked} | MapLocked = {MapLocked}");


        if (isLocalHost)
        {
            _lobbyManagerCache.lockButtonHost.interactable = !HostLocked;
            var hostText = _lobbyManagerCache.lockButtonHost.GetComponentInChildren<TMP_Text>();
            if (hostText != null) hostText.text = HostLocked ? "Locked" : "Lock Character";
        }
        else
        {
            _lobbyManagerCache.lockButtonClient.interactable = !ClientLocked;
            var clientText = _lobbyManagerCache.lockButtonClient.GetComponentInChildren<TMP_Text>();
            if (clientText != null) clientText.text = ClientLocked ? "Locked" : "Lock Character";
        }

        if (isLocalHost)
        {
            _lobbyManagerCache.lockButtonClient.interactable = false;
            var clientText = _lobbyManagerCache.lockButtonClient.GetComponentInChildren<TMP_Text>();
            if (clientText != null) clientText.text = ClientLocked ? "Locked" : "Lock Character";
        }
        else
        {
            _lobbyManagerCache.lockButtonHost.interactable = false;
            var hostText = _lobbyManagerCache.lockButtonHost.GetComponentInChildren<TMP_Text>();
            if (hostText != null) hostText.text = HostLocked ? "Locked" : "Lock Character";
        }


        _lobbyManagerCache.lockMapButton.gameObject.SetActive(!MapLocked && isLocalHost);
        _lobbyManagerCache.startGameButton.gameObject.SetActive(isLocalHost);
        _lobbyManagerCache.startGameButton.interactable = HostLocked && ClientLocked && MapLocked;


        if (_lobbyManagerCache.characterIcons != null)
        {
            bool canSelectCharacter = isLocalHost ? !HostLocked : !ClientLocked;

            foreach (var icon in _lobbyManagerCache.characterIcons)
            {
                Button btn = icon.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = canSelectCharacter;

                    icon.color = canSelectCharacter ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }


        if (_lobbyManagerCache.mapIcons != null)
        {
            bool canSelectMap = !MapLocked && isLocalHost;
            foreach (var icon in _lobbyManagerCache.mapIcons)
            {
                Button btn = icon.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = canSelectMap;
                    icon.color = canSelectMap ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }

        Debug.Log("[UpdateLockUI] === KẾT THÚC ===");
    }
}