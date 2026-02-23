using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Player : NetworkBehaviour, IKitchenObjectParent
{
    public static Player LocalInstance { get; private set; }

    public static event EventHandler OnAnyPlayerSpawned;
    public static event EventHandler OnAnyPickedSomething;

    public static void ResetStaticData()
    {
        OnAnyPlayerSpawned = null;
        OnAnyPickedSomething = null;
    }

    public event EventHandler OnPickedSomething;
    public event EventHandler<OnSelectCounterChangedEventArgs> OnSelectCounterChanged;
    public class OnSelectCounterChangedEventArgs : EventArgs
    {
        public BaseCounter selectedCounter;
    }

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float dashSpeedMultiplier = 4f;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private LayerMask collisionsLayerMask;

    [Header("First Person Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    private bool isFirstPersonMode = false;

    [Header("References")]
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private List<Vector3> spawnPositionList;
    [SerializeField] private PlayerVisual playerVisual;

    private bool isDashing;
    private float dashTimer;
    private float dashTimerMax = 0.2f;
    private float dashCooldown = 1f;
    private float dashCooldownTimer;

    // Biến mạng đồng bộ trạng thái đi bộ (Owner được quyền ghi)
    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Biến mạng lưu ID thiết bị (để phân biệt Bot và người chơi thật)
    private NetworkVariable<ulong> playerDeviceId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector3 lastInteractDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;
    private NavMeshAgent navMeshAgent;
    private bool isAI = false;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null) navMeshAgent.enabled = false;
    }

    private void Start()
    {
        // ✅ ĐÃ SỬA: Tên hàm khớp với định nghĩa bên dưới
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;

        GameInput.Instance.OnDashAction += GameInput_OnDashAction;
        GameInput.Instance.OnThrowAction += GameInput_OnThrowAction;

        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(GetPlayerDataId());
        playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));

        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;

        UpdateSpawnPosition();
    }

    public override void OnNetworkSpawn()
    {
        playerDeviceId.OnValueChanged += OnPlayerIdChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            // Nếu là người chơi thật (không phải Bot do code spawn thủ công), gán ID
            if (NetworkObject.IsPlayerObject)
            {
                playerDeviceId.Value = OwnerClientId;
            }
        }

        if (IsOwner && NetworkObject.IsPlayerObject)
        {
            LocalInstance = this;
        }

        // Nếu đã có ID (trường hợp Bot reconnect hoặc spawn muộn), cập nhật vị trí
        if (playerDeviceId.Value > 0)
        {
            UpdateSpawnPosition();
        }

        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        // QUAN TRỌNG: Chỉ chủ sở hữu (Owner) mới được chạy logic điều khiển
        if (!IsOwner) return;

        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

        HandleMovement();
        HandleInteractions();
    }

    private void HandleMovement()
    {
        // 1. XỬ LÝ DI CHUYỂN CHO BOT (AI)
        if (isAI)
        {
            if (navMeshAgent != null)
            {
                // Cập nhật hoạt ảnh đi bộ dựa trên vận tốc của Agent
                isWalking.Value = navMeshAgent.velocity.magnitude > 0.1f;

                // Quay mặt theo hướng di chuyển
                if (navMeshAgent.velocity.sqrMagnitude > 0.1f)
                {
                    transform.forward = Vector3.Slerp(transform.forward, navMeshAgent.velocity.normalized, Time.deltaTime * 10f);
                }
            }
            return; // Bot xong việc, thoát hàm
        }

        // 2. XỬ LÝ DI CHUYỂN CHO NGƯỜI CHƠI
        if (GameInput.Instance == null) return;

        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (isFirstPersonMode)
        {
            HandleMovementFPS(inputVector);
        }
        else
        {
            HandleMovementTopDown(inputVector, moveDir);
        }

        // Cập nhật trạng thái đi bộ
        isWalking.Value = (isFirstPersonMode && inputVector != Vector2.zero) || (!isFirstPersonMode && moveDir != Vector3.zero);
    }

    private void HandleMovementTopDown(Vector2 inputVector, Vector3 moveDir)
    {
        // Logic lướt (Dash)
        if (isDashing)
        {
            float dashDistance = moveSpeed * dashSpeedMultiplier * Time.deltaTime;
            Vector3 dashDir = transform.forward;
            bool canDash = !Physics.BoxCast(transform.position, Vector3.one * .6f, dashDir, Quaternion.identity, dashDistance, collisionsLayerMask);

            if (canDash) transform.position += dashDir * dashDistance;
            else isDashing = false;

            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) isDashing = false;
            return;
        }

        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = .6f;
        bool canMove = !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDir, Quaternion.identity, moveDistance, collisionsLayerMask);

        if (!canMove)
        {
            // Thử trượt theo trục X
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirX, Quaternion.identity, moveDistance, collisionsLayerMask);
            if (canMove) moveDir = moveDirX;
            else
            {
                // Thử trượt theo trục Z
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirZ, Quaternion.identity, moveDistance, collisionsLayerMask);
                if (canMove) moveDir = moveDirZ;
            }
        }

        if (canMove) transform.position += moveDir * moveDistance;

        float rotateSpeed = 10f;
        if (moveDir != Vector3.zero)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }
    }

    private void HandleMovementFPS(Vector2 inputVector)
    {
        float mouseX = 0f;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mouseX = UnityEngine.InputSystem.Mouse.current.delta.x.ReadValue();
        }
        transform.Rotate(Vector3.up * mouseX * mouseSensitivity * Time.deltaTime * 50f);

        Vector3 moveDirFPS = transform.right * inputVector.x + transform.forward * inputVector.y;
        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = .6f;

        bool canMove = !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirFPS, Quaternion.identity, moveDistance, collisionsLayerMask);
        if (canMove)
        {
            transform.position += moveDirFPS * moveDistance;
        }
    }

    private void HandleInteractions()
    {
        // Bot tự xử lý tương tác qua code AI, không dùng Raycast
        if (isAI) return;

        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        if (isFirstPersonMode) lastInteractDir = transform.forward;
        else if (moveDir != Vector3.zero) lastInteractDir = moveDir;

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                if (baseCounter != selectedCounter) SetSelectedCounter(baseCounter);
            }
            else SetSelectedCounter(null);
        }
        else SetSelectedCounter(null);
    }

    // --- CÁC HÀM XỬ LÝ INPUT (ĐÃ SỬA TÊN CHO KHỚP) ---

    // ✅ SỬA TÊN HÀM: GameInput_OnInteraction -> GameInput_OnInteractAction
    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (isAI || !KitchenGameManager.Instance.IsGamePlaying()) return;
        if (selectedCounter != null) selectedCounter.Interact(this);
    }

    // ✅ SỬA TÊN HÀM: GameInput_OnInteractcAlternateAction -> GameInput_OnInteractAlternateAction (Bỏ chữ 'c' thừa)
    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
    {
        if (isAI || !KitchenGameManager.Instance.IsGamePlaying()) return;
        if (selectedCounter != null) selectedCounter.InteractAlternate(this);
    }

    private void GameInput_OnDashAction(object sender, EventArgs e)
    {
        if (isAI || isDashing || dashCooldownTimer > 0) return;
        isDashing = true;
        dashTimer = dashTimerMax;
        dashCooldownTimer = dashCooldown;
    }

    private void GameInput_OnThrowAction(object sender, EventArgs e)
    {
        if (isAI || !IsOwner || !HasKitchenObject()) return;
        ThrowObjectServerRpc(GetKitchenObject().NetworkObject, transform.forward, kitchenObjectHoldPoint.position);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ThrowObjectServerRpc(NetworkObjectReference objRef, Vector3 dir, Vector3 pos, ServerRpcParams rpcParams = default)
    {
        if (objRef.TryGet(out NetworkObject netObj))
        {
            KitchenObject ko = netObj.GetComponent<KitchenObject>();
            ko.ClearKitchenObjectOnParent();
            ko.ThrowServerRpc(dir, pos);
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        // Chỉ Server mới được quyền hủy Object khi client ngắt kết nối
        if (!IsServer) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        if (clientId == OwnerClientId && HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }

    // --- CÁC HÀM GET/SET & UTILS ---
    public bool IsWalking() => isWalking.Value;
    public ulong GetPlayerDataId() => playerDeviceId.Value;
    public Transform GetKichenObjectFollowTranform() => kitchenObjectHoldPoint;

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;
        if (kitchenObject != null)
        {
            if (IsServer) HandleRolePickupMechanic(kitchenObject.GetKitchenObjectSO());
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
            OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }
    public KitchenObject GetKitchenObject() => kitchenObject;
    public void ClearKitchenObject() => kitchenObject = null;
    public bool HasKitchenObject() => kitchenObject != null;
    public NetworkObject GetNetworkObject() => NetworkObject;

    private void SetSelectedCounter(BaseCounter selectedCounter)
    {
        this.selectedCounter = selectedCounter;
        OnSelectCounterChanged?.Invoke(this, new OnSelectCounterChangedEventArgs { selectedCounter = selectedCounter });
    }

    private void HandleRolePickupMechanic(KitchenObjectSO inputKitchenObjectSO)
    {
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(GetPlayerDataId());
        string objName = inputKitchenObjectSO.objectName;
        if (objName == "Tomato" || objName == "Cabbage" || objName == "Salad")
        {
            float randomValue = UnityEngine.Random.value;
            if (playerData.role == PlayerRole.Chef && randomValue < 0.5f)
            {
                KitchenGameManager.Instance.AddTeamScore(playerData.teamId);
                ShowPickupPopupClientRpc(true);
            }
            else if (playerData.role == PlayerRole.SousChef && randomValue < 0.5f)
            {
                KitchenGameManager.Instance.ReduceTeamScore(playerData.teamId);
                ShowPickupPopupClientRpc(false);
            }
        }
    }

    [ClientRpc]
    private void ShowPickupPopupClientRpc(bool isBonus)
    {
        if (isBonus) Debug.Log("TING TING! (+1)");
        else Debug.Log("OOPS! (-1)");
    }

    private void OnPlayerIdChanged(ulong previousValue, ulong newValue)
    {
        UpdateSpawnPosition();

        // --- THÊM DÒNG NÀY ---
        // Bắt buộc Visual cập nhật lại vì bây giờ ID đã chính xác
        if (playerVisual != null)
        {
            playerVisual.ForceUpdateColor();
        }
    }

    private void UpdateSpawnPosition()
    {
        ulong myId = GetPlayerDataId();

        // Chỉ cập nhật vị trí cho bản thân hoặc Bot
        if (!IsOwner && myId < 9000) return;

        int playerIndex = KitchenGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(myId);
        if (playerIndex == -1)
        {
            if (myId >= 9000) StartCoroutine(RetryUpdateSpawnPosition());
            return;
        }

        // --- ✅ LOGIC MỚI: TÍNH VỊ TRÍ DỰA TRÊN TEAM ---
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(myId);
        GameMode gameMode = KitchenGameMultiplayer.Instance.GetGameMode();

        int spawnIndex = 0;

        if (gameMode == GameMode.Coop)
        {
            // Chế độ Coop: Lấy theo thứ tự vào phòng (0, 1, 2, 3)
            spawnIndex = playerIndex;
        }
        else
        {
            // Chế độ PvP: Phải tính xem mình là người thứ mấy TRONG ĐỘI
            int indexInTeam = 0;
            var playerList = KitchenGameMultiplayer.Instance.GetPlayerDataNetworkList();

            for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i].clientId == myId) break; // Tìm thấy mình thì dừng
                // Nếu người trước mặt cùng đội với mình -> tăng biến đếm
                if (playerList[i].teamId == playerData.teamId) indexInTeam++;
            }

            // Gán index dựa trên cấu hình List trong Editor của bạn
            if (playerData.teamId == Team.Blue)
            {
                spawnIndex = 4 + indexInTeam; // Element 4, 5
            }
            else if (playerData.teamId == Team.Red)
            {
                spawnIndex = 6 + indexInTeam; // Element 6, 7
            }
            else if (playerData.teamId == Team.Yellow)
            {
                spawnIndex = 8 + indexInTeam; // Element 8, 9
            }
        }
        // --------------------------------------------------

        // Kiểm tra an toàn để không bị lỗi OutOfRange
        if (spawnIndex < spawnPositionList.Count)
        {
            // Nếu là Bot thì dùng Warp để dịch chuyển tức thời
            if (isAI && navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.Warp(spawnPositionList[spawnIndex]);
            }
            else
            {
                transform.position = spawnPositionList[spawnIndex];
            }
        }
        else
        {
            Debug.LogError($"Lỗi Spawn: Không tìm thấy vị trí số {spawnIndex} trong SpawnPositionList!");
        }
    }

    private IEnumerator RetryUpdateSpawnPosition()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateSpawnPosition();
    }

    private void KitchenGameMultiplayer_OnPlayerDataNetworkListChanged(object sender, EventArgs e) => UpdateSpawnPosition();

    public void SetFirstPersonMode(bool isActive) => isFirstPersonMode = isActive;

    // --- CÁC HÀM HỖ TRỢ BOT (ĐÃ SỬA LẠI ĐỂ KÍCH HOẠT KITCHEN AI) ---

    // 1. Kích hoạt chế độ di chuyển NavMesh cho Bot
    public void SetAsAI()
    {
        isAI = true;
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.angularSpeed = 360f;
            navMeshAgent.acceleration = 100f;
        }
    }

    // 2. Setup ban đầu cho Bot (được gọi từ BotSpawner)
    public void SetupAsBot(ulong botId)
    {
        if (!IsServer) return;
        playerDeviceId.Value = botId;

        // Gọi hàm SetAsAI để bật NavMesh
        SetAsAI();

        // 🔥 FIX QUAN TRỌNG: Tìm và bật "não" cho Bot (KitchenAI)
        // Nếu không có dòng này, Bot sẽ chỉ có NavMesh nhưng không biết đi đâu
        if (TryGetComponent(out KitchenAI botBrain))
        {
            botBrain.StartAI();
        }

        UpdateSpawnPosition();

        if (playerVisual != null)
        {
            playerVisual.ForceUpdateColor();
        }
    }

    // 3. Ra lệnh di chuyển đến vị trí (được gọi từ KitchenAI)
    public void MoveToPosition(Vector3 targetPosition)
    {
        if (isAI && navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.SetDestination(targetPosition);
        }
    }

    // 4. Bot tương tác trực tiếp với đồ vật (được gọi từ KitchenAI)
    public void AI_Interact_Direct(BaseCounter targetCounter)
    {
        if (!IsServer || !isAI) return;
        if (targetCounter != null)
        {
            SetSelectedCounter(targetCounter);
            targetCounter.Interact(this);
        }
    }

    // 5. Bot tương tác phụ (cắt, nấu) (được gọi từ KitchenAI)
    public void AI_InteractAlternate_Direct(BaseCounter targetCounter)
    {
        if (!IsServer || !isAI) return;
        if (targetCounter != null)
        {
            SetSelectedCounter(targetCounter);
            targetCounter.InteractAlternate(this);
        }
    }
    // -----------------------------------------------------------

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (KitchenGameMultiplayer.Instance != null)
            KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
        if (GameInput.Instance != null)
        {
            // ✅ ĐÃ SỬA: Tên hàm trong OnDestroy khớp với định nghĩa
            GameInput.Instance.OnInteractAction -= GameInput_OnInteractAction;
            GameInput.Instance.OnInteractAlternateAction -= GameInput_OnInteractAlternateAction;

            GameInput.Instance.OnDashAction -= GameInput_OnDashAction;
            GameInput.Instance.OnThrowAction -= GameInput_OnThrowAction;
        }
    }
}