using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class KitchenAI : NetworkBehaviour
{
    private Player player;
    private bool isAIActive = false;
    private float interactTimer;
    private float interactTimerMax = 1.2f;

    // --- MỤC 4: TỐI ƯU HÓA (CACHING) ---
    // Lưu danh sách các loại bàn vào bộ nhớ đệm để không phải tìm lại mỗi khung hình
    private BaseCounter[] allCounters;
    private ContainerCounter[] containerCounters;
    private ClearCounter[] clearCounters;
    private CuttingCounter[] cuttingCounters;
    private StoveCounter[] stoveCounters;
    private DeliveryCounter[] deliveryCounters;
    private PlatesCounter[] platesCounters;

    private void Awake()
    {
        player = GetComponent<Player>();
        enabled = false;
    }

    private void Start()
    {
        // Chỉ Server mới chạy AI, nhưng Client cũng cần cache nếu dùng script này cho mục đích khác (tùy logic)
        // Ở đây ta cache luôn để an toàn.
        CacheAllCounters();
    }

    public void StartAI()
    {
        isAIActive = true;
        enabled = true;
        player.SetAsAI();
        Debug.Log("BOT STARTED: Chế độ 'Mỗi người một đơn' (Optimized)!");
    }

    private void Update()
    {
        if (!IsServer || !isAIActive || !IsSpawned) return;

        if (interactTimer > 0) interactTimer -= Time.deltaTime;
        ThinkAndAct();
    }

    // --- HÀM CACHE (Chạy 1 lần duy nhất) ---
    private void CacheAllCounters()
    {
        allCounters = FindObjectsOfType<BaseCounter>();
        containerCounters = FindObjectsOfType<ContainerCounter>();
        clearCounters = FindObjectsOfType<ClearCounter>();
        cuttingCounters = FindObjectsOfType<CuttingCounter>();
        stoveCounters = FindObjectsOfType<StoveCounter>();
        deliveryCounters = FindObjectsOfType<DeliveryCounter>();
        platesCounters = FindObjectsOfType<PlatesCounter>();
    }

    private void ThinkAndAct()
    {
        if (player.HasKitchenObject())
        {
            ProcessHoldingObject();
        }
        else
        {
            ProcessEmptyHand();
        }
    }

    // --- 1. XÁC ĐỊNH THỨ TỰ BOT ---
    private int GetBotRank()
    {
        ulong myId = player.GetPlayerDataId();
        if (myId < 9000) return 0;
        return (int)(myId - 9000);
    }

    // --- 2. XỬ LÝ KHI TAY KHÔNG ---
    private void ProcessEmptyHand()
    {
        if (HandleLocalProcessing()) return;

        List<RecipeSO> waitingList = DeliveryManager.Instance.GetWaitingRecipeSOList();
        if (waitingList.Count == 0) return;

        int myRank = GetBotRank();
        int myTargetIndex = myRank % waitingList.Count;
        RecipeSO myTargetRecipe = waitingList[myTargetIndex];

        if (ShouldFetchPlate(myTargetRecipe))
        {
            FetchPlate();
            return;
        }

        if (TryFetchIngredientForRecipe(myTargetRecipe))
        {
            return;
        }
    }

    private bool TryFetchIngredientForRecipe(RecipeSO recipe)
    {
        foreach (var itemSO in recipe.kitchenObjectSOList)
        {
            string rawName = GetRawIngredientName(itemSO.objectName);
            string cookedName = itemSO.objectName;

            // Kiểm tra xem nguyên liệu này đã nằm trên cái Dĩa nào chưa
            bool isOnAPlate = IsIngredientAlreadyOnPlate(itemSO);
            if (isOnAPlate) continue; // Nếu đã lên mâm rồi thì thôi, tìm món khác

            // Kiểm tra xem nguyên liệu này có đang nằm vất vưởng đâu đó trong bếp không
            bool isExistInKitchen = IsIngredientInKitchen(rawName) || IsIngredientInKitchen(cookedName);

            if (isExistInKitchen)
            {
                // --- LOGIC MỚI: NẾU CÓ SẴN THÌ ĐI TÌM VÀ NHẶT ---

                // 1. Ưu tiên tìm đồ đã nấu chín (nếu cần)
                KitchenObject foundItem = FindKitchenObjectOnCounters(cookedName);

                // 2. Nếu không có đồ chín, tìm đồ sống
                if (foundItem == null) foundItem = FindKitchenObjectOnCounters(rawName);

                if (foundItem != null)
                {
                    // Ra lệnh cho Bot đi đến cái bàn đang chứa vật phẩm đó để nhặt
                    MoveAndInteract(foundItem.GetKitchenObjectParent() as BaseCounter, false);
                    return true; // Đã tìm thấy việc để làm, kết thúc hàm
                }
            }
            else
            {
                // --- LOGIC CŨ: NẾU KHÔNG CÓ THÌ RA THÙNG CHỨA LẤY ---
                BaseCounter container = FindContainerByName(rawName);
                if (container != null)
                {
                    MoveAndInteract(container, false);
                    return true;
                }
            }
        }
        return false;
    }

    // --- 3. XỬ LÝ KHI ĐANG CẦM ĐỒ ---
    private void ProcessHoldingObject()
    {
        KitchenObject playerObject = player.GetKitchenObject();

        // A. CẦM DĨA
        if (playerObject.TryGetPlate(out PlateKitchenObject plate))
        {
            RecipeSO matchingRecipe = GetMatchingRecipeForPlate(plate);

            if (matchingRecipe != null && IsPlateComplete(plate, matchingRecipe))
            {
                GoToDelivery();
            }
            else
            {
                RecipeSO targetRecipe = matchingRecipe ?? GetAssignedRecipe() ?? GetFirstWaitingRecipe();
                if (targetRecipe == null) return;

                KitchenObjectSO missingItem = GetMissingIngredient(plate, targetRecipe);
                if (missingItem != null)
                {
                    KitchenObject foundItem = FindKitchenObjectOnCounters(missingItem.objectName);
                    if (foundItem != null)
                    {
                        MoveAndInteract(foundItem.GetKitchenObjectParent() as BaseCounter, false);
                    }
                    else
                    {
                        BaseCounter freeCounter = FindFreeClearCounter();
                        if (freeCounter != null) MoveAndInteract(freeCounter, false);
                    }
                }
            }
            return;
        }

        // B. CẦM NGUYÊN LIỆU
        string itemName = playerObject.GetKitchenObjectSO().objectName;
        if (IsItemNeedProcessing(itemName))
        {
            GoToProcess(itemName);
        }
        else
        {
            PlateKitchenObject validPlate = FindValidPlateForIngredient(playerObject.GetKitchenObjectSO());
            if (validPlate != null)
            {
                MoveAndInteract(validPlate.GetKitchenObjectParent() as BaseCounter, false);
            }
            else
            {
                BaseCounter freeCounter = FindFreeClearCounter();
                if (freeCounter != null) MoveAndInteract(freeCounter, false);
            }
        }
    }

    // --- CÁC HÀM HỖ TRỢ (ĐÃ TỐI ƯU SỬ DỤNG CACHE) ---

    private bool HandleLocalProcessing()
    {
        // Duyệt mảng cache thay vì FindObjectsOfType
        foreach (var counter in cuttingCounters)
        {
            if (counter.HasKitchenObject())
            {
                string objectName = counter.GetKitchenObject().GetKitchenObjectSO().objectName;
                if (IsItemNeedProcessing(objectName))
                {
                    MoveAndInteract(counter, true);
                    return true;
                }
            }
        }
        return false;
    }

    private void FetchPlate()
    {
        // 1. Tìm dĩa trên bàn (Duyệt qua tất cả bàn đã cache)
        foreach (var counter in allCounters)
        {
            if (counter.HasKitchenObject() && counter.GetKitchenObject().TryGetPlate(out PlateKitchenObject plate))
            {
                MoveAndInteract(counter, false);
                return;
            }
        }

        // 2. Lấy dĩa mới từ chồng dĩa
        PlatesCounter platesCounter = FindNearestPlatesCounter();
        if (platesCounter != null && platesCounter.HasPlates())
        {
            MoveAndInteract(platesCounter, false);
        }
    }

    private bool ShouldFetchPlate(RecipeSO recipe)
    {
        foreach (var itemSO in recipe.kitchenObjectSOList)
        {
            if (IsIngredientReadyOnCounter(itemSO.objectName)) return true;
        }
        return false;
    }

    // --- DI CHUYỂN & TƯƠNG TÁC (MỤC 6: FIX CRASH) ---
    private void MoveAndInteract(BaseCounter targetCounter, bool useAlternateInteract)
    {
        // 🔴 MỤC 6: CHECK NULL ĐỂ TRÁNH CRASH
        if (targetCounter == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, targetCounter.transform.position);

        if (distance >= 2.3f)
        {
            player.MoveToPosition(targetCounter.transform.position);
        }
        else
        {
            if (TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            Vector3 dirToCounter = (targetCounter.transform.position - transform.position).normalized;
            dirToCounter.y = 0;
            if (dirToCounter != Vector3.zero)
                transform.forward = Vector3.Slerp(transform.forward, dirToCounter, Time.deltaTime * 10f);

            if (interactTimer <= 0f)
            {
                if (targetCounter is StoveCounter stove) HandleStoveInteraction(stove);
                else
                {
                    if (useAlternateInteract) player.AI_InteractAlternate_Direct(targetCounter);
                    else player.AI_Interact_Direct(targetCounter);
                }
                interactTimer = interactTimerMax;
            }
        }
    }

    private void HandleStoveInteraction(StoveCounter stove)
    {
        if (stove.HasKitchenObject())
        {
            string itemOnStove = stove.GetKitchenObject().GetKitchenObjectSO().objectName;
            if (itemOnStove == "MeatPattyCooked" || itemOnStove == "MeatPattyBurned")
                player.AI_Interact_Direct(stove);
        }
        else player.AI_Interact_Direct(stove);
    }

    private void GoToProcess(string itemName)
    {
        if (itemName == "MeatPattyUncooked")
        {
            StoveCounter stove = FindFreeStoveCounter();
            if (stove != null) MoveAndInteract(stove, false);
        }
        else
        {
            CuttingCounter cuttingCounter = FindEmptyCuttingCounter();
            if (cuttingCounter != null) MoveAndInteract(cuttingCounter, false);
        }
    }

    private void GoToDelivery()
    {
        DeliveryCounter deliveryCounter = FindNearestActiveDeliveryCounter();
        if (deliveryCounter != null) MoveAndInteract(deliveryCounter, false);
    }

    // --- HELPERS (OPTIMIZED) ---
    private RecipeSO GetAssignedRecipe()
    {
        List<RecipeSO> waitingList = DeliveryManager.Instance.GetWaitingRecipeSOList();
        if (waitingList.Count == 0) return null;
        int myRank = GetBotRank();
        return waitingList[myRank % waitingList.Count];
    }

    private RecipeSO GetMatchingRecipeForPlate(PlateKitchenObject plate)
    {
        var waitingList = DeliveryManager.Instance.GetWaitingRecipeSOList();
        foreach (var recipe in waitingList)
        {
            bool match = true;
            foreach (var item in plate.GetKitchenObjectSOList())
            {
                if (!recipe.kitchenObjectSOList.Contains(item)) { match = false; break; }
            }
            if (match) return recipe;
        }
        return waitingList.Count > 0 ? waitingList[0] : null;
    }

    private bool IsItemNeedProcessing(string objectName)
    {
        return (objectName == "Tomato" || objectName == "Cabbage" || objectName == "CheeseBlock" || objectName == "MeatPattyUncooked");
    }

    private string GetRawIngredientName(string cookedName)
    {
        if (cookedName == "MeatPattyCooked" || cookedName == "MeatPattyBurned") return "MeatPattyUncooked";
        if (cookedName == "CabbageSlices") return "Cabbage";
        if (cookedName == "TomatoSlices") return "Tomato";
        if (cookedName == "CheeseSlices") return "CheeseBlock";
        return cookedName;
    }

    private RecipeSO GetFirstWaitingRecipe()
    {
        var list = DeliveryManager.Instance.GetWaitingRecipeSOList();
        return list.Count > 0 ? list[0] : null;
    }

    private bool IsPlateComplete(PlateKitchenObject plate, RecipeSO recipe)
    {
        var plateItems = plate.GetKitchenObjectSOList();
        var recipeItems = recipe.kitchenObjectSOList;
        return plateItems.Count == recipeItems.Count && !recipeItems.Except(plateItems).Any();
    }

    private KitchenObjectSO GetMissingIngredient(PlateKitchenObject plate, RecipeSO recipe)
    {
        var plateItems = plate.GetKitchenObjectSOList();
        foreach (var item in recipe.kitchenObjectSOList)
        {
            if (!plateItems.Contains(item)) return item;
        }
        return null;
    }

    // --- CÁC HÀM TÌM KIẾM SỬ DỤNG ARRAY CACHE (KHÔNG DÙNG FindObjectsOfType) ---

    private BaseCounter FindContainerByName(string nameToFind)
    {
        foreach (var c in containerCounters)
            if (c.GetKitchenObjectSO().objectName == nameToFind) return c;
        return null;
    }

    private BaseCounter FindFreeClearCounter()
    {
        foreach (var c in clearCounters) if (!c.HasKitchenObject()) return c;
        return null;
    }

    private PlateKitchenObject FindValidPlateForIngredient(KitchenObjectSO ingredientSO)
    {
        // Duyệt qua tất cả bàn để tìm bàn nào đang có Dĩa
        foreach (var counter in allCounters)
        {
            if (counter.HasKitchenObject() && counter.GetKitchenObject().TryGetPlate(out PlateKitchenObject plate))
            {
                if (plate.GetKitchenObjectSOList().Contains(ingredientSO)) continue;
                return plate;
            }
        }
        return null;
    }

    private bool IsIngredientInKitchen(string objectName)
    {
        // Chỉ kiểm tra đồ trên bàn (nhanh hơn tìm cả scene)
        foreach (var counter in allCounters)
        {
            if (counter.HasKitchenObject() && counter.GetKitchenObject().GetKitchenObjectSO().objectName == objectName)
                return true;
        }
        return false;
    }

    private bool IsIngredientReadyOnCounter(string objectName)
    {
        foreach (var counter in allCounters)
        {
            if (counter.HasKitchenObject() && counter.GetKitchenObject().GetKitchenObjectSO().objectName == objectName)
                return true;
        }
        return false;
    }

    private bool IsIngredientAlreadyOnPlate(KitchenObjectSO itemSO)
    {
        foreach (var counter in allCounters)
        {
            if (counter.HasKitchenObject() && counter.GetKitchenObject().TryGetPlate(out PlateKitchenObject plate))
            {
                if (plate.GetKitchenObjectSOList().Contains(itemSO)) return true;
            }
        }
        return false;
    }

    private KitchenObject FindKitchenObjectOnCounters(string objectName)
    {
        foreach (var counter in allCounters)
        {
            if (counter.HasKitchenObject() && counter.GetKitchenObject().GetKitchenObjectSO().objectName == objectName)
                return counter.GetKitchenObject();
        }
        return null;
    }

    private CuttingCounter FindEmptyCuttingCounter()
    {
        foreach (var c in cuttingCounters) if (!c.HasKitchenObject()) return c;
        return cuttingCounters.Length > 0 ? cuttingCounters[0] : null;
    }

    private StoveCounter FindFreeStoveCounter()
    {
        foreach (var s in stoveCounters)
        {
            if (!s.HasKitchenObject()) return s;
            if (s.HasKitchenObject() && s.GetKitchenObject().GetKitchenObjectSO().objectName == "MeatPattyCooked") return s;
        }
        return stoveCounters.Length > 0 ? stoveCounters[0] : null;
    }

    private DeliveryCounter FindNearestActiveDeliveryCounter()
    {
        DeliveryCounter best = null;
        float minDst = float.MaxValue;
        foreach (var c in deliveryCounters)
        {
            if (!c.gameObject.activeInHierarchy) continue;
            float dst = Vector3.Distance(transform.position, c.transform.position);
            if (dst < minDst) { minDst = dst; best = c; }
        }
        return best;
    }

    private PlatesCounter FindNearestPlatesCounter()
    {
        PlatesCounter best = null;
        float minDst = float.MaxValue;
        foreach (var c in platesCounters)
        {
            float dst = Vector3.Distance(transform.position, c.transform.position);
            if (dst < minDst) { minDst = dst; best = c; }
        }
        return best;
    }
}