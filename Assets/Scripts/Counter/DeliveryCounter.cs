using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryCounter : BaseCounter
{
    public static DeliveryCounter Instance { get; private set; }

    [Header("Team Assignment")]
    [SerializeField] private Team team = Team.None;

    private void Awake()
    {
        Instance = this;
    }

    public Team GetTeam() => team;

    public override void Interact(Player player)
    {
        if (player.HasKitchenObject())
        {
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
            {
                // Gửi tham chiếu Player
                InteractServerRpc(player.NetworkObject);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(NetworkObjectReference playerRef)
    {
        playerRef.TryGet(out NetworkObject playerNetworkObject);
        if (playerNetworkObject == null) return;
        Player player = playerNetworkObject.GetComponent<Player>();

        // QUAN TRỌNG: Lấy ID thật (Bot = 9000, Host = 0)
        ulong realClientId = player.GetPlayerDataId();

        if (player.HasKitchenObject())
        {
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
            {
                // --- LOGIC KIỂM TRA TEAM TRONG PVP ---
                if (KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.PvP)
                {
                    // Dùng realClientId để lấy data
                    PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(realClientId);

                    if (team == Team.None)
                    {
                        Debug.LogWarning($"[DeliveryCounter] Counter chưa được gán team!");
                    }
                    else if (playerData.teamId != team)
                    {
                        Debug.Log($"[DeliveryCounter] ❌ Player team {playerData.teamId} không thể giao vào counter team {team}");
                        ShowWrongTeamCounterClientRpc(realClientId);
                        return; // Dừng lại, không cho giao hàng
                    }
                }

                // Truyền realClientId vào để DeliveryManager biết ai là người giao
                DeliveryManager.Instance.DeliverRecipe(plateKitchenObject, realClientId);

                KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            }
        }
    }

    [ClientRpc]
    private void ShowWrongTeamCounterClientRpc(ulong targetClientId)
    {
        // Chỉ hiện thông báo cho đúng người chơi bị lỗi (Bot không cần xem thông báo này)
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log("❌ Bạn đang giao hàng nhầm vào quầy của đội đối phương!");
        }
    }
}