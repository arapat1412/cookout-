using System;
using Unity.Netcode;
using UnityEngine;

public class ContainerCounter : BaseCounter
{
    public event EventHandler OnPlayerGrabbedObject;
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    public override void Interact(Player player)
    {
        // 🔴 CŨ: InteractLogicServerRpc(player.OwnerClientId); -> Sai vì Bot và Host trùng ID
        // 🟢 MỚI: Truyền chính cái NetworkObject của người đang tương tác (kể cả Bot)
        InteractLogicServerRpc(player.NetworkObject);
    }

    // Đổi tham số từ ulong clientId -> NetworkObjectReference playerNetworkObjectReference
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc(NetworkObjectReference playerNetworkObjectReference)
    {
        // 🟢 MỚI: Lấy Player từ tham chiếu NetworkObject
        playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject);
        if (playerNetworkObject == null) return;

        Player player = playerNetworkObject.GetComponent<Player>();

        // Logic cũ giữ nguyên
        if (!player.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);
            InteractLogicClientRpc();
        }
    }

    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
    }
 
    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObjectSO;
    }
}