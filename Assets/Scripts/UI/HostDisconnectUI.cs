using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HostDisconnectUI : MonoBehaviour
{


    [SerializeField] private Button playAgainButton;


    private void Awake()
    {
        playAgainButton.onClick.AddListener(() => {
            // Khi nhấn Play Again, tắt NetworkManager đi (nếu còn)
            // và tải lại Menu chính
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            Loader.Load(Loader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        // Luôn kiểm tra Singleton trước khi đăng ký
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }

        Hide();
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
       
        if (!NetworkManager.Singleton.IsHost)
        {
            Show();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    // Luôn hủy đăng ký sự kiện để tránh lỗi MissingReferenceException
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
        }
    }

}