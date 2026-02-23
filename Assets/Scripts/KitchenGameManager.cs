using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;
using UnityEngine.SceneManagement;

public class KitchenGameManager : NetworkBehaviour
{
    
    public static KitchenGameManager Instance { get; private set; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnLocalGamePaused;
    public event EventHandler OnLocalGameUnPaused;
    public event EventHandler OnMutiplayerGamePaused;
    public event EventHandler OnMutiplayerGameUnPaused;
    public event EventHandler OnLocalPlayerReadyChanged;

    private enum State
   {
       WaitingToStart,
       CountdownToStart,
       GamePlaying,
       GameOver
    }

    [SerializeField] private Transform playerPrefab;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);
    private bool isLocalPlayerReady;
    //private float waitingToStartTimer = 3f;
    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f);
    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f);
    private float gamePlayingTimerMax = 90f;
    private bool isLocalPauseGame = false;
    private NetworkVariable<bool> isGamePaused = new NetworkVariable<bool>(false);
    private Dictionary<ulong, bool> playerReadyDictionary;
    private Dictionary<ulong, bool> playerPauseDictionary;
    private bool autoTestGamePausedState;
    // ✅ HỆ THỐNG ĐIỂM PVP
    public event EventHandler OnTeamScoreChanged;
    private NetworkVariable<int> blueTeamScore = new NetworkVariable<int>(0);
    private NetworkVariable<int> redTeamScore = new NetworkVariable<int>(0);
    private NetworkVariable<int> yellowTeamScore = new NetworkVariable<int>(0);

    public Dictionary<Team, int> GetTeamScores()
    {
        return new Dictionary<Team, int>
    {
        { Team.Blue, blueTeamScore.Value },
        { Team.Red, redTeamScore.Value },
        { Team.Yellow, yellowTeamScore.Value }
    };
    }

    public void AddTeamScore(Team team)
    {
        if (!IsServer) return;

        if (team == Team.Blue)
        {
            blueTeamScore.Value++;
            // NetworkVariable tự động đồng bộ, không cần gọi RPC thủ công nữa
        }
        else if (team == Team.Red)
        {
            redTeamScore.Value++;
        }
        else if (team == Team.Yellow) 
            yellowTeamScore.Value++;

        // XÓA DÒNG NÀY: OnTeamScoreChangedClientRpc(); 
        // LÝ DO: Gây ra lỗi Race Condition, Client cập nhật UI trước khi số điểm kịp đồng bộ.
    }

    public void ReduceTeamScore(Team team)
    {
        if (!IsServer) return; // Chỉ Server mới được quyền trừ điểm

        if (team == Team.Blue)
        {
            // Tùy bạn muốn cho điểm âm hay không. 
            // Nếu không muốn âm thì thêm: if (blueTeamScore.Value > 0)
            blueTeamScore.Value--;
        }
        else if (team == Team.Red)
        {
            redTeamScore.Value--;
        }
        else if (team == Team.Yellow)
            yellowTeamScore.Value--;

        // Không cần gọi RPC hay Event gì cả, 
        // vì OnValueChanged ở Client (đã làm ở bài trước) sẽ tự bắt sự kiện này và cập nhật UI.
    }

    //[ClientRpc]
    //private void OnTeamScoreChangedClientRpc()
    //{
    //    OnTeamScoreChanged?.Invoke(this, System.EventArgs.Empty);
    //}
    //-----------------------------
    private void Awake()
    {
        Instance = this;

        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerPauseDictionary = new Dictionary<ulong, bool>();
    }

    private void Start()
    {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }
    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += State_OnValueChanged;
        isGamePaused.OnValueChanged += IsGamePaused_OnValueChanged;

        // --- SỬA LỖI: Thêm lắng nghe thay đổi điểm số ---
        blueTeamScore.OnValueChanged += OnScoreNetworkVariableChanged;
        redTeamScore.OnValueChanged += OnScoreNetworkVariableChanged;
        yellowTeamScore.OnValueChanged += OnScoreNetworkVariableChanged;
        // ------------------------------------------------

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    private void OnScoreNetworkVariableChanged(int previousValue, int newValue)
    {
        // Kích hoạt sự kiện để UI cập nhật
        OnTeamScoreChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            // ✅ QUAN TRỌNG: Nếu ID >= 9000 thì là Bot, Server đã spawn rồi, KHÔNG spawn lại
            if (clientId >= 9000)
            {
                continue;
            }

            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        autoTestGamePausedState= true;
    }

    private void IsGamePaused_OnValueChanged(bool previousValue, bool newValue)
    {
        if (isGamePaused.Value)
        {
            Time.timeScale = 0f;
            OnMutiplayerGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;
            OnMutiplayerGameUnPaused?.Invoke(this, EventArgs.Empty);
        }
    }

    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);

        // --- ĐOẠN CODE CẦN THÊM ---
        if (newValue == State.GameOver)
        {
            // Kiểm tra nếu là chế độ Coop mới lưu tiền (PvP tiền luôn là 0 nên cũng không sao, nhưng check cho chắc)
            if (KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.Coop)
            {
                int goldEarned = DeliveryManager.Instance.GetSessionGoldEarned();

                if (goldEarned > 0)
                {
                    // Gọi DataManager để cộng dồn vào tổng tài sản và lưu Cloud
                    DataManager.Instance.AddGold(goldEarned);
                    Debug.Log($"GAME OVER! Đã lưu {goldEarned} vàng vào ví.");
                }
            }
        }
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (state.Value == State.WaitingToStart)
        {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
            SetPlayerReadyServerRpc();
            
        }
    } 
    [ServerRpc(RequireOwnership =false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams=default)
    {
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                //nguoi choi chua san sang
                allClientsReady = false;
                break;
            }
            
        }
        if (allClientsReady)
        {
            state.Value = State.CountdownToStart;
        }
    }
    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        TooglePauseGame();
    }

    private void Update()
    {
        if (!IsServer){
            return;
        }
        switch (state.Value)
        {
            case State.WaitingToStart:
                break;
            case State.CountdownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f)
                {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f)
                {
                    state.Value = State.GameOver;
                    CheckPvPWinnerServerRpc();
                }
                break;
            case State.GameOver:
                
                break;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void CheckPvPWinnerServerRpc()
    {
        if (KitchenGameMultiplayer.Instance.GetGameMode() != GameMode.PvP ) return;

        CheckPvPWinnerClientRpc();
    }

    [ClientRpc]
    private void CheckPvPWinnerClientRpc()
    {
        // Logic sẽ xử lý ở GameOverUI
    }

    public Team GetWinningTeam()
    {
        int blue = blueTeamScore.Value;
        int red = redTeamScore.Value;
        int yellow = yellowTeamScore.Value;

        // Logic tìm đội điểm cao nhất trong 3 đội
        if (blue > red && blue > yellow)
        {
            return Team.Blue;
        }
        else if (red > blue && red > yellow)
        {
            return Team.Red;
        }
        else if (yellow > blue && yellow > red)
        {
            return Team.Yellow;
        }
        else
        {
            return Team.None; // Hòa
        }
    }

    private void LateUpdate()
    {
        if (autoTestGamePausedState)
        {
            autoTestGamePausedState= false;
            TestGamePausestate();
        }
    }
    public bool IsGamePlaying()
    {
        return state.Value == State.GamePlaying;
    }

    public bool IsCountdownToStartActive()
    {
        return state.Value == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer()
    {
        return countdownToStartTimer.Value;
    }

    public bool IsGameOver()
    {
        return state.Value == State.GameOver;
    }

    public bool IsWaitingToStart()
    {
        return state.Value == State.WaitingToStart;
    }

    public bool IsLocalPlayerReady()
    {
        return isLocalPlayerReady;
    }

    public float GetGamePlayingTimerNormalized()
    {
        return 1 - (gamePlayingTimer.Value / gamePlayingTimerMax);
    }

    public void TooglePauseGame() {
        isLocalPauseGame = !isLocalPauseGame;
        if (isLocalPauseGame)
        {
            PauseGameServerRpc();
            OnLocalGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            UnPauseGameServerRpc();
            OnLocalGameUnPaused?.Invoke(this, EventArgs.Empty);
        }
    }
    [ServerRpc(RequireOwnership =false)]
    private void PauseGameServerRpc(ServerRpcParams serverRpcParams= default)
    {
        playerPauseDictionary[serverRpcParams.Receive.SenderClientId] = true;
        TestGamePausestate();
    }
    [ServerRpc(RequireOwnership = false)]
    private void UnPauseGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerPauseDictionary[serverRpcParams.Receive.SenderClientId] = false;
        TestGamePausestate();
    }

    private void TestGamePausestate()
    {
               foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playerPauseDictionary.ContainsKey(clientId) && playerPauseDictionary[clientId])
            {
                //nguoi choi chua pause
                isGamePaused.Value = true;
                return;
            }
        }
        //tat ca nguoi choi deu pause
        isGamePaused.Value = false;
    }
}


