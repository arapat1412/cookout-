using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour
{
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler<OnRecipeSuccessEventArgs> OnRecipeSuccess;
    public event EventHandler<OnRecipeFailedEventArgs> OnRecipeFailed;
    // ✅ 1. THÊM DÒNG NÀY: Khai báo sự kiện thay đổi vàng
    public event EventHandler OnSessionGoldChanged;


    public class OnRecipeFailedEventArgs : EventArgs
    {
        public Team teamId;
    }

    public class OnRecipeSuccessEventArgs : EventArgs
    {
        public Team teamId;
    }

    public static DeliveryManager Instance { get; private set; }

    [SerializeField] private RecipeListSO recipeListSO;
    private List<RecipeSO> waitingRecipeSOList;
    [SerializeField] private float spawnRecipeTimer = 0.5f;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;

    // 1. KHAI BÁO BIẾN MỚI
    private int sessionGoldEarned = 0;
    private const int GOLD_REWARD_PER_RECIPE = 5; // Phần thưởng mỗi món

    // 2. HÀM ĐỂ LẤY SỐ VÀNG KIẾM ĐƯỢC (Public)
    public int GetSessionGoldEarned()
    {
        return sessionGoldEarned;
    }

    private void Awake()
    {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>();
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f)
        {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax)
            {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex)
    {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];
        waitingRecipeSOList.Add(waitingRecipeSO);

        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    // ✅ SỬA ĐỔI: Nhận thêm clientId để biết chính xác ai là người giao hàng
    public void DeliverRecipe(PlateKitchenObject plateKitchenObject, ulong clientId)
    {
        for (int i = 0; i < waitingRecipeSOList.Count; i++)
        {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count)
            {
                // Has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList)
                {
                    // Cycling through all ingredients in the Recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
                    {
                        // Cycling through all ingredients in the Plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO)
                        {
                            // Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound)
                    {
                        // This Recipe ingredient was not found on the Plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe)
                {
                    // Player delivered the correct recipe!
                    // ✅ GỌI HÀM NỘI BỘ, TRUYỀN CLIENT ID VÀO
                    DeliverCorrectRecipeInternal(i, clientId);
                    return;
                }
            }
        }

        // No matches found!
        // Player did not deliver a correct recipe
        // ✅ GỌI HÀM NỘI BỘ, TRUYỀN CLIENT ID VÀO
        DeliverIncorrectRecipeInternal(clientId);
    }

    // ✅ ĐÃ SỬA: Hàm nội bộ (Internal), bỏ [ServerRpc] vì đã chạy trên Server
    private void DeliverIncorrectRecipeInternal(ulong clientId)
    {
        // Sử dụng clientId được truyền vào trực tiếp
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(clientId);

        // Gửi TeamID của người làm sai xuống Client
        DeliverIncorrectRecipeClientRpc(playerData.teamId);

        // Logic trừ điểm
        if (KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.PvP ||
        KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.PvP_3Team)
        {
            if (playerData.teamId != Team.None)
            {
                KitchenGameManager.Instance.ReduceTeamScore(playerData.teamId);
                Debug.Log($"[PvP] Trừ điểm Team {playerData.teamId} (Client: {clientId})");
            }
        }
    }

    [ClientRpc]
    private void DeliverIncorrectRecipeClientRpc(Team failedTeamId)
    {
        // Bắn sự kiện kèm theo TeamID
        OnRecipeFailed?.Invoke(this, new OnRecipeFailedEventArgs
        {
            teamId = failedTeamId
        });
    }

    private void DeliverCorrectRecipeInternal(int waitingRecipeSOListIndex, ulong clientId)
    {
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(clientId);

        // Kiểm tra chế độ chơi
        GameMode currentMode = KitchenGameMultiplayer.Instance.GetGameMode();

        if (currentMode == GameMode.PvP || currentMode == GameMode.PvP_3Team)
        {
            // --- CHẾ ĐỘ PVP ---
            // Chỉ cộng điểm cho Team, KHÔNG cộng vàng
            KitchenGameManager.Instance.AddTeamScore(playerData.teamId);
            Debug.Log($"[PvP] Team {playerData.teamId} ghi điểm!");
        }
        else
        {
            // --- CHẾ ĐỘ COOP ---
            // Cộng vàng cho tất cả mọi người
            AddSessionGoldClientRpc(GOLD_REWARD_PER_RECIPE);
        }

        // Logic cũ: Báo giao hàng thành công
        DeliverCorrectRecipeClientRpc(waitingRecipeSOListIndex, playerData.teamId);
    }

    [ClientRpc]
    private void AddSessionGoldClientRpc(int amount)
    {
        // Chỉ cộng vàng ở chế độ Coop
        if (KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.Coop)
        {
            sessionGoldEarned += amount;

            // ✅ 2. THÊM DÒNG NÀY: Báo cho UI biết để cập nhật
            OnSessionGoldChanged?.Invoke(this, EventArgs.Empty);

            Debug.Log($"[Coop] Nhận được +{amount} vàng! (Tổng trong trận: {sessionGoldEarned})");
        }
    }


    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex, Team successTeamId)
    {
        successfulRecipesAmount++;
        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);

        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);

        // Gửi kèm thông tin Team ra ngoài UI
        OnRecipeSuccess?.Invoke(this, new OnRecipeSuccessEventArgs
        {
            teamId = successTeamId
        });
    }

    public List<RecipeSO> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount()
    {
        return successfulRecipesAmount;
    }
}