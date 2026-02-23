// Trong file CharacterSelectReady.cs
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; }

    public event EventHandler OnReadyChanged;

    // ✅ 1. THÊM SỰ KIỆN BÁO LỖI (Để UI lắng nghe)
    public event EventHandler<string> OnGameStartFailed;

    private Dictionary<ulong, bool> playerReadyDictionary;

    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
    }

    public void SetPlayerReady()
    {
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        SetPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId);
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady)
        {
            // --- ✅ LOGIC KIỂM TRA SỐ LƯỢNG NGƯỜI CHƠI ---
            GameMode gameMode = KitchenGameMultiplayer.Instance.GetGameMode();
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

            bool isCountValid = true;
            string errorMessage = "";

            if (gameMode == GameMode.PvP && playerCount < 2)
            {
                isCountValid = false;
                errorMessage = "Cần ít nhất 2 người để chơi PvP!";
            }
            else if (gameMode == GameMode.PvP_3Team && playerCount < 3)
            {
                isCountValid = false;
                errorMessage = "Cần ít nhất 3 người để chơi 3 Team!";
            }

            if (isCountValid)
            {
                // Đủ người -> Vào game
                _ = KitchenGameLobby.Instance.DeleteLobby();
                Loader.LoadNetwork(Loader.Scene.GameScene);
            }
            else
            {
                // Thiếu người -> Báo lỗi xuống Client
                GameStartFailedClientRpc(errorMessage);
            }
            // ---------------------------------------------
        }
    }

    [ClientRpc]
    private void SetPlayerReadyClientRpc(ulong clientId)
    {
        playerReadyDictionary[clientId] = true;
        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }

    // ✅ 2. HÀM GỬI LỖI XUỐNG CÁC MÁY CON
    [ClientRpc]
    private void GameStartFailedClientRpc(string message)
    {
        // Reset trạng thái Ready để người chơi bấm lại
        playerReadyDictionary.Clear();
        OnReadyChanged?.Invoke(this, EventArgs.Empty);

        // Kích hoạt sự kiện để UI hiện lên
        OnGameStartFailed?.Invoke(this, message);
    }

    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }
}