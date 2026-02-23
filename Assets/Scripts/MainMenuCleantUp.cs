using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MainMenuCleantUp : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton !=null)
        {
            // Đóng kết nối mạng đàng hoàng trước khi phá hủy
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        if (KitchenGameMultiplayer.Instance !=null)
        {
            Destroy(KitchenGameMultiplayer.Instance.gameObject);
        }
        if (KitchenGameLobby.Instance != null)
        {
            Destroy(KitchenGameLobby.Instance.gameObject);
        }
    }
}
