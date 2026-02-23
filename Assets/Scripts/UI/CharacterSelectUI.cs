using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button addbotButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            // Thêm '_ =' 
            _ = KitchenGameLobby.Instance.LeaveLobby();
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        readyButton.onClick.AddListener(() => {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
        addbotButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.Instance.AddBotPlayer();
        });
    }
    private void Start()
    {
        Lobby lobby = KitchenGameLobby.Instance.GetLobby();
        lobbyNameText.text = $"Lobby Name:" + lobby.Name;
        lobbyCodeText.text = $"Lobby Code:" + lobby.LobbyCode;

        // --- ✅ ĐOẠN CODE MỚI: XỬ LÝ ẨN/HIỆN NÚT ADD BOT ---

        // 1. Kiểm tra xem mình có phải là Chủ phòng (Host) không?
        bool isHost = NetworkManager.Singleton.IsServer;

        // 2. Kiểm tra xem chế độ chơi hiện tại có phải là Coop không?
        bool isCoopMode = KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.Coop;

        // 3. Chỉ hiện nút nếu thỏa mãn CẢ HAI điều kiện
        if (isHost && isCoopMode)
        {
            addbotButton.gameObject.SetActive(true);
        }
        else
        {
            addbotButton.gameObject.SetActive(false);
        }
        // ----------------------------------------------------
    }

}
