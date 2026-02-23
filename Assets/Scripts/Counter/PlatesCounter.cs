using System;
using Unity.Netcode;
using UnityEngine;

public class PlatesCounter : BaseCounter
{
    public event EventHandler OnPlateSpawned;
    public event EventHandler OnPlateRemoved;
    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;

    private float spawnPlatesTimer;
    private float spawnPlatesTimerMax = 4f;
    private NetworkVariable<int> platesSpawnedAmount = new NetworkVariable<int>(0);
    private int platesSpawnedAmountMax = 4;

    private void Update()
    {
        if (!IsServer) return;
        spawnPlatesTimer += Time.deltaTime;
        if (spawnPlatesTimer > spawnPlatesTimerMax)
        {
            spawnPlatesTimer = 0f;
            if (KitchenGameManager.Instance.IsGamePlaying() && platesSpawnedAmount.Value < platesSpawnedAmountMax)
            {
                SpawnPlateServerRpc();
            }
        }
    }

    [ServerRpc]
    private void SpawnPlateServerRpc()
    {
        platesSpawnedAmount.Value++;
        SpawnPlateClientRpc();
    }

    [ClientRpc]
    private void SpawnPlateClientRpc()
    {
        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    public override void Interact(Player player)
    {
        InteractLogicServerRpc(player.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc(NetworkObjectReference playerRef)
    {
        playerRef.TryGet(out NetworkObject playerNetworkObject);
        if (playerNetworkObject == null) return;
        Player player = playerNetworkObject.GetComponent<Player>();

        if (!player.HasKitchenObject())
        {
            if (platesSpawnedAmount.Value > 0)
            {
                platesSpawnedAmount.Value--;
                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);
                InteractLogicClientRpc();
            }
        }
    }

    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }

    public bool HasPlates()
    {
        return platesSpawnedAmount.Value > 0;
    }
}