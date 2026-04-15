using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{

    private NetworkRunner _runner;

    async void StartGame(GameMode mode)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
    public NetworkPrefabRef[] CharacterPrefabs; 
    public NetworkPrefabRef LobbyStatePrefab;  
    private NetworkObject _lobbyStateObject;
    public LobbyState LobbyStateRef { get; set; }


    [Header("join - quit room")]
    public string lobbySceneName = "Lobby";
    private int _lobbyBuildIndex = -1;
    private bool _isReturningToLobby;
    private bool _isProcessingShutdown;
    private bool _callbackRegistered;

    [Header("Spawn Positions in Game Scene")]
    public Vector2 HostSpawnPosition = new Vector2(-5f, 0f);
    public Vector2 ClientSpawnPosition = new Vector2(5f, 0f);
    public bool IsInlobby { get; private set; }
    public bool IsStartingLobby { get; private set; }
    public bool IsInRoom { get; private set; }

    public PlayerClass HostSelectedClass { get; set; } = PlayerClass.None;
    public PlayerClass ClientSelectedClass { get; set; } = PlayerClass.None;
    public int SelectedMapIndex { get; private set; }
    public bool HostLocked { get; set; } = false;
    public bool ClientLocked { get; set; } = false;
    public bool MapLocked { get; set; } = false;
    public string HostPlayerName { get; set; } = "Host";
    public string ClientPlayerName { get; set; } = "Client";
    public List<SessionInfo> LastSessionList { get; private set; } = new List<SessionInfo>();

    private void Awake()
    {
        var spawners = FindObjectsByType<BasicSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (spawners.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        DontDestroyOnLoad(gameObject);

    }
    public static BasicSpawner Instance { get; private set; }
    void RegisterCallback()
    {
        if (_runner == null || _callbackRegistered) return;
        _runner.AddCallbacks(this);
        _callbackRegistered = true;
    }


    public async Task LeaveRoomAndReturnToLobby()
    {
        if (_isReturningToLobby || _isProcessingShutdown) return;
        _isReturningToLobby = true;

        try
        {
            if (_runner != null)
            {
                await _runner.Shutdown();
                Destroy(_runner);
                _runner = null;
            }

            await ReturnToLobbyAndRestart();
        }
        finally
        {
            _isReturningToLobby = false;
            _isProcessingShutdown = false;
            IsInlobby = false;
            IsInRoom = false;
            IsStartingLobby = false;
        }
    }

    public LobbyManager LobbyManager { set; get; }
    private async Task ReturnToLobbyAndRestart()
    {
        if (string.IsNullOrEmpty(lobbySceneName) || !Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            Debug.LogError("Lobby scene name invalid or not in Build Settings!");
            return;
        }

        var asyncLoad = SceneManager.LoadSceneAsync(lobbySceneName);
        while (asyncLoad != null && !asyncLoad.isDone)
            await Task.Yield();

        await Task.Yield(); 

        LobbyManager = FindFirstObjectByType<LobbyManager>();
        if (LobbyManager == null)
            Debug.LogError("[ReturnToLobbyAndRestart] Không tìm thấy LobbyManager sau khi load scene!");
        else
            LobbyManager.spawner = this; 


        if (_runner == null || _runner.gameObject == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            RegisterCallback();
        }

        await StarLobby();
    }
    private async Task ReturnToLobbySceneOnly()
    {
        if (string.IsNullOrEmpty(lobbySceneName))
        {
            Debug.LogWarning("Lobby scene name is not set!");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogError($"Scene {lobbySceneName} not found in Build Settings!");
        }

        await Task.CompletedTask;
    }


    public async Task StarLobby()
    {
        if (IsStartingLobby || IsInlobby) return;

        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();

        IsStartingLobby = true;
        _runner.ProvideInput = true;
        RegisterCallback();
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        if (result.Ok)
        {
            IsInlobby = true;
            IsInRoom = false;
            Debug.Log("Joined Lobby successfully");
        }
        else
        {
            Debug.LogError("Failed to join lobby: " + result.ShutdownReason);
        }

    }

    public async Task StartHost(string sessionName, SceneRef scene)
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        RegisterCallback();

        await StarLobby();

  
        _lobbyBuildIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"[BasicSpawner] Lobby buildIndex được ghi nhận: {_lobbyBuildIndex}");
 

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        });
        if (result.Ok)
        {
            Debug.Log("Host started successfully");
        }
        else
        {
            Debug.LogError("Failed to start host: " + result.ShutdownReason);
        }
    }

    public async Task StartClient(string sessionName)
    {
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        RegisterCallback();


        await StarLobby();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,

            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
    
        });

        if (result.Ok)
        {
            Debug.Log("Client started successfully");
        }
        else
        {
            Debug.LogError("Failed to start client: " + result.ShutdownReason);
        }
    }



    public PlayerClassInfo LocalPlayerProfile { get; set; }
    public void SetLocalPlayerProfile(PlayerClassInfo profile)
    {
        LocalPlayerProfile = profile;
        Debug.Log($"Local Player Profile set: {profile.Name} - {profile.Class}");
    }



    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player Joined: " + player);


        if (runner.IsServer && _lobbyStateObject == null)
        {
            _lobbyStateObject = runner.Spawn(LobbyStatePrefab, Vector3.zero, Quaternion.identity);
            LobbyStateRef = _lobbyStateObject.GetComponent<LobbyState>();
            LobbyStateRef.SetSpawner(this);
        }

        if (runner.ActivePlayers.Count() > 2)
        {
            Debug.LogWarning($"[BasicSpawner] Phòng đã đầy (max 2 người). Player {player} bị từ chối.");
            if (!runner.IsServer) runner.Shutdown();
            return;
        }


        if (LobbyManager != null)
        {
            LobbyManager.IsHost = runner.IsServer;   
            if (!runner.IsServer)
                LobbyManager.ShowCharacterSelectPanel();
        }


        if (LobbyStateRef != null && runner.LocalPlayer == player)
        {
            if (!string.IsNullOrEmpty(LocalPlayerProfile.Name))
            {
                LobbyStateRef.RPC_SetPlayerName(LocalPlayerProfile.Name, runner.IsServer);
            }
        }

        if (LobbyStateRef != null)
            LobbyStateRef.ForceRefreshUI();
    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        var currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex == _lobbyBuildIndex)
        {
            Debug.Log($"[OnSceneLoadDone] Lobby scene loaded → BỎ QUA spawn character");
            return;
        }

        Debug.Log($"[OnSceneLoadDone] Game scene loaded → Spawn characters");

        foreach (var p in runner.ActivePlayers)
        {
            if (_spawnedCharacters.ContainsKey(p)) continue;

            PlayerClass selectedClass = (p == runner.LocalPlayer) ? HostSelectedClass : ClientSelectedClass;
            if (selectedClass == PlayerClass.None) selectedClass = PlayerClass.None;

            int index = Mathf.Clamp((int)selectedClass, 0, CharacterPrefabs.Length - 1);
            Vector2 spawnPos = (p == runner.LocalPlayer) ? HostSpawnPosition : ClientSpawnPosition;

            var no = runner.Spawn(CharacterPrefabs[index], spawnPos, Quaternion.identity, p);
            runner.SetPlayerObject(p, no);

            var controller = no.GetComponent<NetworkedPlayerController>();

            // === FIX TÊN + ISHOST ===
            controller.IsHost = (p == runner.LocalPlayer);
            controller.PlayerName = (p == runner.LocalPlayer) ? HostPlayerName : ClientPlayerName;

            _spawnedCharacters.Add(p, no);

            Debug.Log($"Spawned {controller.PlayerName} | IsHost: {controller.IsHost} | PlayerRef: {p}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player Left: " + player
                                      + "Client count: "
                                      + runner.ActivePlayers.ToList().Count);

        if (_spawnedCharacters.TryGetValue(player, out var networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var inputData = new NetworkInputData
        {
            MoveDirection = new Vector2(Input.GetAxis("Horizontal"), 0), 
            Jump = Input.GetKeyDown(KeyCode.Space),        
            Attack = Input.GetKeyDown(KeyCode.J),        
            Block = Input.GetKeyDown(KeyCode.K),          
            SuperHit = Input.GetKeyDown(KeyCode.I),
            Shoot = Input.GetKeyDown(KeyCode.U),
            Flash = Input.GetKeyDown(KeyCode.L),
            ChargePower = Input.GetKey(KeyCode.H)   // giữ phím H để charge

        };
        input.Set(inputData);
    }
    public void HostSelectCharacter(PlayerClass character)
    {
        HostSelectedClass = character;
    }
    public void ClientSelectCharacter(PlayerClass character)
    {
        ClientSelectedClass = character;
    }
    public void LockHostCharacter() => HostLocked = true;
    public void LockClientCharacter() => ClientLocked = true;
    public void LockMap(int mapIndex)
    {
        SelectedMapIndex = mapIndex;
        MapLocked = true;
    }
    public async void StartGameAfterSelection()
    {
        if (!HostLocked || !ClientLocked || !MapLocked)
        {
            Debug.LogWarning("Not all selections are locked yet!");
            return;
        }
        if (_runner == null || !_runner.IsServer) return;

        if (LobbyStateRef != null)
        {
            HostPlayerName = LobbyStateRef.HostPlayerName;  
            ClientPlayerName = LobbyStateRef.ClientPlayerName;
            Debug.Log($"[BasicSpawner] Copied names → Host: {HostPlayerName} | Client: {ClientPlayerName}");
        }

        var scene = SceneRef.FromIndex(SelectedMapIndex);
        Debug.Log($"Host đang load game map: buildIndex = {SelectedMapIndex}");

        var loadOperation = _runner.LoadScene(scene, LoadSceneMode.Single);
        await loadOperation;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (_isProcessingShutdown) return;
        _isProcessingShutdown = true;

        IsInlobby = false;
        IsInRoom = false;
        IsStartingLobby = false;

        _spawnedCharacters.Clear();

        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
        _callbackRegistered = false;

        if (runner != null)
        {
            var oldRunner = _runner;
            _runner = null;

            var oldsceneManager = oldRunner.GetComponent<NetworkSceneManagerDefault>();
            if (oldsceneManager != null)
            {
                Destroy(oldsceneManager);
            }
            if (oldRunner != null)
            {
                Destroy(oldRunner);
            }
        }
        await ReturnToLobbyAndRestart();

        _isProcessingShutdown = false;
        _isReturningToLobby = false;
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[BasicSpawner] Session List Updated: {sessionList.Count} sessions available");

        LastSessionList = new List<SessionInfo>(sessionList);

        if (LobbyManager == null)
            LobbyManager = FindFirstObjectByType<LobbyManager>();

        if (LobbyManager != null)
            LobbyManager.UpdateRoomList(sessionList);
        else
            Debug.LogWarning("[BasicSpawner] LobbyManager is null when receiving session list.");
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}