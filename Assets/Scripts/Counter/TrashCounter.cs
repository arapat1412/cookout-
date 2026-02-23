using System;
using Unity.Netcode;
using UnityEngine;

public class TrashCounter : BaseCounter
{
    public static event EventHandler OnAnyObjectTrashed;

    new public static void ResetStaticData()
    {
        OnAnyObjectTrashed = null;
    }

    public override void Interact(Player player)
    {
        if (player.HasKitchenObject())
        {
            InteractServerRpc(player.NetworkObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(NetworkObjectReference playerRef)
    {
        playerRef.TryGet(out NetworkObject playerNetworkObject);
        if (playerNetworkObject == null) return;
        Player player = playerNetworkObject.GetComponent<Player>();

        if (player.HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            InteractClientRpc();
        }
    }

    [ClientRpc]
    private void InteractClientRpc()
    {
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }
}