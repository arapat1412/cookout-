using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameMultiplayer : NetworkBehaviour
{
    public const int MAX_PLAYER_AMOUNT = 4;
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";

    public static KitchenGameMultiplayer Instance { get; private set; }

    // --- SỬA ĐỔI: Dùng NetworkVariable để đồng bộ GameMode ---
    private NetworkVariable<GameMode> currentGameModeNetworkVar = new NetworkVariable<GameMode>(GameMode.Coop);

    // Biến tạm để lưu lựa chọn từ Main Menu trước khi có mạng
    private GameMode localGameModeSelection = GameMode.Coop;

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;

    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;
    [SerializeField] private List<Color> playerColorList;

    private NetworkList<PlayerData> playerDataNetworkList;
    private string playerName;

    private ulong nextBotId = 9000;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load GameMode từ PlayerPrefs vào biến tạm (local)
        if (PlayerPrefs.HasKey("SelectedGameMode"))
        {
            localGameModeSelection = (GameMode)PlayerPrefs.GetInt("SelectedGameMode");
            PlayerPrefs.DeleteKey("SelectedGameMode");
        }

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 1000));

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerdataNetworkList_OnListChanged;
    }

    public void AddBotPlayer()
    {
        // Chỉ Server mới được quyền thêm Bot
        if (!NetworkManager.Singleton.IsServer) return;

        // 1. Kiểm tra giới hạn phòng (Max 4 người/bot)
        if (playerDataNetworkList.Count >= MAX_PLAYER_AMOUNT)
        {
            Debug.Log("Phòng đã đầy! Không thể thêm Bot.");
            return;
        }

        // 2. Kiểm tra số lượng Bot (Giới hạn tối đa 3 Bot để chừa 1 slot cho Host)
        int currentBotCount = 0;
        foreach (var player in playerDataNetworkList)
        {
            if (player.clientId >= 9000) currentBotCount++;
        }

        if (currentBotCount >= 3)
        {
            Debug.Log("Đã đạt giới hạn số lượng Bot!");
            return;
        }

        Debug.Log($"Đang thêm Bot ID {nextBotId} vào chế độ Co-op...");

        // --- ✅ LOGIC CO-OP: BOT LUÔN LÀ BẾP PHÓ ---

        // Mặc định Co-op là Team Blue (hoặc bạn có thể lấy team của Host: playerDataNetworkList[0].teamId)
        Team coOpTeam = Team.Blue;

        // BẮT BUỘC: Bot luôn là SousChef (Bếp phó) vì Host đã là Chef
        PlayerRole botRole = PlayerRole.SousChef;

        // --- THÊM BOT VÀO DANH SÁCH ---
        playerDataNetworkList.Add(new PlayerData
        {
            clientId = nextBotId,
            colorId = GetFirstUnusedColorId(),
            teamId = coOpTeam,  // Cùng team với người chơi
            role = botRole,     // Luôn là Bếp phó
            playerName = "Bot " + (nextBotId - 9000 + 1),
            hatId = 0
        });

        Debug.Log($"✅ Đã thêm Bot Co-op: {nextBotId} | Team: {coOpTeam} | Role: {botRole}");

        // Tăng ID cho Bot tiếp theo
        nextBotId++;
    }

    // --- SỬA ĐỔI: Đồng bộ biến mạng khi Host khởi tạo ---
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Host đẩy giá trị local lên mạng cho mọi người cùng biết
            currentGameModeNetworkVar.Value = localGameModeSelection;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Có thể xử lý logic sau khi load scene xong ở đây nếu cần
    }

    public void SetGameMode(GameMode mode)
    {
        // Lưu vào biến tạm (dùng khi ở MainMenu)
        localGameModeSelection = mode;
    }

    public GameMode GetGameMode()
    {
        // Trả về giá trị từ NetworkVariable (đảm bảo đúng trên cả Client)
        return currentGameModeNetworkVar.Value;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;
        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    private void PlayerdataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        // NetworkManager_Server_OnClientDisconnectCallback đã được chuyển vào OnNetworkSpawn
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        // ✅ THÊM DÒNG NÀY
        if (!IsServer) return;

        // ✅ FIX LỖI CRASH: Kiểm tra kỹ NetworkManager
        if (NetworkManager.Singleton == null || !IsSpawned)
        {
            return;
        }

        // Logic xóa player khỏi danh sách (code cũ giữ nguyên)
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                if (playerDataNetworkList != null && i < playerDataNetworkList.Count)
                {
                    playerDataNetworkList.RemoveAt(i);
                }
                break;
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Server_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        Team assignedTeam = Team.None;
        PlayerRole assignedRole = PlayerRole.Chef; // Mặc định là Bếp trưởng

        GameMode currentMode = GetGameMode();

        // --- Logic Phân Đội & Phân Vai ---
        if (currentMode == GameMode.PvP)
        {
            int blueCount = 0;
            int redCount = 0;

            // Đếm số người hiện tại trong mỗi đội
            foreach (var data in playerDataNetworkList)
            {
                if (data.teamId == Team.Blue) blueCount++;
                else if (data.teamId == Team.Red) redCount++;
            }

            // Phân đội
            if (blueCount <= redCount)
            {
                assignedTeam = Team.Blue;
                // Nếu Blue chưa có ai -> Chef, đã có 1 người -> SousChef
                assignedRole = (blueCount == 0) ? PlayerRole.Chef : PlayerRole.SousChef;
            }
            else
            {
                assignedTeam = Team.Red;
                assignedRole = (redCount == 0) ? PlayerRole.Chef : PlayerRole.SousChef;
            }
        }
        // (Làm tương tự cho PvP_3Team nếu muốn)
        else if (currentMode == GameMode.PvP_3Team)
        {
            int blueCount = 0;
            int redCount = 0;
            int yellowCount = 0;

            // 1. Đếm số lượng thành viên hiện tại
            foreach (var data in playerDataNetworkList)
            {
                if (data.teamId == Team.Blue) blueCount++;
                else if (data.teamId == Team.Red) redCount++;
                else if (data.teamId == Team.Yellow) yellowCount++;
            }
            if (clientId >= 9000)
            {
                assignedRole = PlayerRole.SousChef;
            }
            // 2. Tìm đội ít người nhất để nhét vào
            if (blueCount <= redCount && blueCount <= yellowCount)
            {
                assignedTeam = Team.Blue; // Vào đội Xanh
                assignedRole = (blueCount == 0) ? PlayerRole.Chef : PlayerRole.SousChef;
            }
            else if (redCount <= blueCount && redCount <= yellowCount)
            {
                assignedTeam = Team.Red;  // Vào đội Đỏ
                assignedRole = (redCount == 0) ? PlayerRole.Chef : PlayerRole.SousChef;
            }
            else
            {
                assignedTeam = Team.Yellow; // Vào đội Vàng
                assignedRole = (yellowCount == 0) ? PlayerRole.Chef : PlayerRole.SousChef;
            }
        }
        else
        {
            // Coop: Có thể random hoặc ai vào trước làm Chef
            assignedRole = (playerDataNetworkList.Count == 0) ? PlayerRole.Chef : PlayerRole.SousChef;
        }
        
        

        // Thêm người chơi vào danh sách
        playerDataNetworkList.Add(new PlayerData
        {
            clientId = clientId,
            colorId = GetFirstUnusedColorId(),
            teamId = assignedTeam ,
            role = assignedRole
        });

        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
        // 🔴 BỔ SUNG DÒNG NÀY ĐỂ HOST CŨNG CÓ MŨ:
        SetPlayerHatServerRpc(DataManager.Instance.LocalData.equippedHatId);
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        if (connectionApprovalRequest.ClientNetworkId == NetworkManager.ServerClientId)
        {
            connectionApprovalResponse.Approved = true;
            return;
        }

        if (SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started!";
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Server is full!";
            return;
        }

        connectionApprovalResponse.Approved = true;
    }

    public void StartClient()
    {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        // 1. Lấy dữ liệu chuẩn từ Cloud Save (DataManager)
        string playerName = DataManager.Instance.LocalData.playerName;
        int hatId = DataManager.Instance.LocalData.equippedHatId;

        // 2. Gửi dữ liệu này lên Server
        SetPlayerNameServerRpc(playerName); // ✅ Sửa ở đây (bỏ GetPlayerName() cũ đi)
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
        SetPlayerHatServerRpc(hatId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerHatServerRpc(int hatId, ServerRpcParams serverRpcParams = default)
    {
        int index = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        if (index != -1)
        {
            PlayerData data = playerDataNetworkList[index];
            data.hatId = hatId;
            playerDataNetworkList[index] = data; // Cập nhật List -> Trigger sự kiện OnListChanged
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex != -1)
        {
            PlayerData playerData = playerDataNetworkList[playerDataIndex];
            playerData.playerName = playerName;
            playerDataNetworkList[playerDataIndex] = playerData;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex != -1)
        {
            PlayerData playerData = playerDataNetworkList[playerDataIndex];
            playerData.playerId = playerId;
            playerDataNetworkList[playerDataIndex] = playerData;
        }
    }

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if (kitchenObjectParent.HasKitchenObject())
        {
            return;
        }

        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);
        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenObjectNetworkObject.Spawn(true);
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }

    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        if (!IsServer || !kitchenObject.NetworkObject.IsSpawned)
        {
            return;
        }
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        // Kiểm tra an toàn
        if (kitchenObjectNetworkObject == null) return;

        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        // ✅ BƯỚC 1: Lấy thông tin "Cha" (Người cầm hoặc Bàn) trước khi hủy vật thể
        IKitchenObjectParent parent = kitchenObject.GetKitchenObjectParent();

        if (parent != null)
        {
            // ✅ BƯỚC 2: Gửi lệnh xuống Client bảo "Cái ông Parent này (ParentRef) hãy xóa đồ trên tay đi"
            // Ta truyền NetworkObject của Parent thay vì của KitchenObject
            ClearKitchenObjectOnParentClientRpc(parent.GetNetworkObject());
        }

        // ✅ BƯỚC 3: Hủy vật thể (Gọi hàm DestroySelf đã sửa ở Bước 1)
        kitchenObject.DestroySelf();
    }

    [ClientRpc]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference parentNetworkObjectReference)
    {
        // ✅ BƯỚC 4: Client nhận lệnh, tìm ra ông Parent và xóa dữ liệu đồ cầm trên tay
        parentNetworkObjectReference.TryGet(out NetworkObject parentNetworkObject);

        if (parentNetworkObject != null)
        {
            IKitchenObjectParent parent = parentNetworkObject.GetComponent<IKitchenObjectParent>();
            if (parent != null)
            {
                parent.ClearKitchenObject();
            }
        }
    }

    public bool IsPlayerIndexConnected(int playerIndex)
    {
        return playerIndex < playerDataNetworkList.Count;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }

    public PlayerData GetPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    public Color GetPlayerColor(int colorId)
    {
        return playerColorList[colorId];
    }

    public void ChangePlayerColor(int colorId)
    {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerColorServerRpc(int colorId, ServerRpcParams serverRpcParams = default)
    {
        if (!IsColorAvailable(colorId))
        {
            return;
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        PlayerData playerData = playerDataNetworkList[playerDataIndex];
        playerData.colorId = colorId;
        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private bool IsColorAvailable(int colorId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.colorId == colorId)
            {
                return false;
            }
        }
        return true;
    }

    private int GetFirstUnusedColorId()
    {
        for (int i = 0; i < playerColorList.Count; i++)
        {
            if (IsColorAvailable(i))
            {
                return i;
            }
        }
        return -1;
    }

    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

    // 1. Hàm Public để UI gọi
    public void ChangePlayerHat(int hatId)
    {
        ChangePlayerHatServerRpc(hatId);
    }

    // 2. Server RPC để cập nhật dữ liệu mạng
    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerHatServerRpc(int hatId, ServerRpcParams serverRpcParams = default)
    {
        // Lấy index của người chơi gửi yêu cầu
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        if (playerDataIndex != -1)
        {
            PlayerData playerData = playerDataNetworkList[playerDataIndex];
            playerData.hatId = hatId; // Cập nhật ID mũ mới
            playerDataNetworkList[playerDataIndex] = playerData; // Gán lại để kích hoạt sự kiện OnListChanged
        }
    }

    public NetworkList<PlayerData> GetPlayerDataNetworkList()
    {
        return playerDataNetworkList;
    }
}