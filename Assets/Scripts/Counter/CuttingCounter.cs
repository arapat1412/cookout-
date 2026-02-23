using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CuttingCounter : BaseCounter, IHasProgress
{
    public static event EventHandler OnAnyCut;

    new public static void ResetStaticData()
    {
        OnAnyCut = null;
    }

    public event EventHandler<IHasProgress.OnProgessChangedEventArgs> OnProgressChanged;
    public event EventHandler OnCut;

    [SerializeField] private CuttingRecipeSO[] cultingRecipeSOArray;
    private int cuttingProgress;

    public override void Interact(Player player)
    {
        InteractServerRpc(player.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(NetworkObjectReference playerRef)
    {
        playerRef.TryGet(out NetworkObject playerNetworkObject);
        if (playerNetworkObject == null) return;
        Player player = playerNetworkObject.GetComponent<Player>();

        if (!HasKitchenObject())
        {
            if (player.HasKitchenObject())
            {
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);
                    InteractLogicPlaceObjectCounterOnServerRpc();
                }
            }
        }
        else
        {
            if (player.HasKitchenObject())
            {
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                }
            }
            else
            {
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicPlaceObjectCounterOnServerRpc()
    {
        InteractLogicPlaceObjectCounterOnClientRpc();
    }

    [ClientRpc]
    private void InteractLogicPlaceObjectCounterOnClientRpc()
    {
        cuttingProgress = 0;
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgessChangedEventArgs
        {
            progessNormalized = 0f
        });
    }

    public override void InteractAlternate(Player player)
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            // Truyền Player vào để tính sức mạnh cắt
            CutObjectServerRpc(player.NetworkObject);
            TestCultingProressDoneServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CutObjectServerRpc(NetworkObjectReference playerRef)
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            // 1. Lấy thông tin người đang cắt
            playerRef.TryGet(out NetworkObject playerNetworkObject);
            if (playerNetworkObject == null) return;
            Player player = playerNetworkObject.GetComponent<Player>();

            // Dùng GetPlayerDataId() để phân biệt Bot (9000+) và Host (0)
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(player.GetPlayerDataId());

            // 2. Xác định sức mạnh nhát cắt
            int cutPower = 1;
            if (playerData.role == PlayerRole.SousChef)
            {
                cutPower = 2; // PHỤ BẾP: Cắt siêu nhanh
            }
            else
            {
                cutPower = 1; // BẾP TRƯỞNG: Cắt bình thường
            }

            // 3. Thực hiện cắt
            for (int i = 0; i < cutPower; i++)
            {
                CuttingRecipeSO cultingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                if (cuttingProgress >= cultingRecipeSO.cuttingProgressMax) break;

                CutObjectClientRpc();
            }

            TestCultingProressDoneServerRpc();
        }
    }

    [ClientRpc]
    private void CutObjectClientRpc()
    {
        cuttingProgress++;
        OnCut?.Invoke(this, EventArgs.Empty);
        OnAnyCut?.Invoke(this, EventArgs.Empty);

        CuttingRecipeSO cultingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgessChangedEventArgs
        {
            progessNormalized = (float)cuttingProgress / cultingRecipeSO.cuttingProgressMax
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void TestCultingProressDoneServerRpc()
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            CuttingRecipeSO cultingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
            if (cuttingProgress >= cultingRecipeSO.cuttingProgressMax)
            {
                KitchenObjectSO outputKitchenObjectSO = GetOutputForInput(GetKitchenObject().GetKitchenObjectSO());
                KitchenObject.DestroyKitchenObject(GetKitchenObject());
                KitchenObject.SpawnKitchenObject(outputKitchenObjectSO, this);
            }
        }
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CuttingRecipeSO cultingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        return cultingRecipeSO != null;
    }

    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CuttingRecipeSO cultingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        if (cultingRecipeSO != null)
        {
            return cultingRecipeSO.output;
        }
        else
        {
            return null;
        }
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (CuttingRecipeSO cultingRecipeSO in cultingRecipeSOArray)
        {
            if (cultingRecipeSO.input == inputKitchenObjectSO)
            {
                return cultingRecipeSO;
            }
        }
        return null;
    }
}