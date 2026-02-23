using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KitchenObject : NetworkBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent kitchenObjectParent;
    private FollowTransform followTransform;

    // --- BIẾN BAY ---
    private NetworkVariable<bool> isFlying = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isOnGround = new NetworkVariable<bool>(false);
    private NetworkVariable<Vector3> flyDirection = new NetworkVariable<Vector3>(Vector3.zero);
    private NetworkVariable<Vector3> groundPosition = new NetworkVariable<Vector3>(Vector3.zero);
    private float flySpeed = 10f;
    private float flyDistancePassed;
    private float flyDistanceMax = 2.5f;
    private float groundHeight = 0.1f;

    protected virtual void Awake()
    {
        followTransform = GetComponent<FollowTransform>();
    }

    private void Update()
    {
        // TỐI ƯU HÓA: Nếu vật thể không bay VÀ không nằm trên sàn
        // (tức là nó đang được giữ hoặc nằm trên quầy), thoát ngay lập tức.
        if (!isFlying.Value && !isOnGround.Value)
        {
            return;
        }

        // --- Logic cũ giữ nguyên ---

        // Trong Update()
        if (isFlying.Value)
        {
            // Smooth movement
            Vector3 targetPos = transform.position + flyDirection.Value * (flySpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

            if (IsServer)
            {
                flyDistancePassed += flySpeed * Time.deltaTime;
                HandleFlying();
            }
        }

        // Nếu đang nằm trên sàn, giữ vị trí cố định
        // Bỏ 'if (isOnGround.Value)' vì đã check ở trên, giờ chỉ cần check else
        else // (Tức là isFlying = false VÀ isOnGround = true)
        {
            if (!HasKitchenObjectParent())
            {
                transform.position = groundPosition.Value;
            }
        }
    }

    // ✅ HÀM KÍCH HOẠT BAY
    [ServerRpc(RequireOwnership = false)]
    public void ThrowServerRpc(Vector3 direction, Vector3 startPosition)
    {
        // Reset trạng thái
        isOnGround.Value = false;

        // Đặt vị trí bắt đầu
        transform.position = startPosition;

        // Kích hoạt bay
        flyDirection.Value = direction;
        isFlying.Value = true;
        flyDistancePassed = 0f;

        // ✅ TẮT FollowTransform ngay lập tức
        DisableFollowTransformClientRpc();
    }

    [ClientRpc]
    private void DisableFollowTransformClientRpc()
    {
        if (followTransform != null)
        {
            followTransform.enabled = false;
        }
    }

    private void HandleFlying()
    {
        float moveAmount = flySpeed * Time.deltaTime;
        transform.position += flyDirection.Value * moveAmount;
        flyDistancePassed += moveAmount;

        // 1. Kiểm tra va chạm với Bàn (Counter)
        float reachDistance = 0.7f;
        if (Physics.Raycast(transform.position, flyDirection.Value, out RaycastHit raycastHit, reachDistance))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                if (!baseCounter.HasKitchenObject())
                {
                    // Đặt lên bàn
                    this.SetKitchenObjectParent(baseCounter);
                    StopFlying();
                    return;
                }
            }
        }

        // 2. Kiểm tra va chạm tường hoặc vật cản
        if (Physics.Raycast(transform.position, flyDirection.Value, moveAmount, LayerMask.GetMask("Default")))
        {
            LandOnGround();
            return;
        }

        // 3. Hết tầm xa -> Rơi xuống sàn
        if (flyDistancePassed >= flyDistanceMax)
        {
            LandOnGround();
        }
    }

    private void LandOnGround()
    {
        // Dừng bay
        isFlying.Value = false;

        // ✅ GỌI CLIENTRPC ĐỂ XÓA PARENT TRÊN TẤT CẢ CLIENT
        ClearParentOnAllClientsClientRpc();

        // Tìm vị trí sàn bên dưới
        Vector3 currentPos = transform.position;

        // Raycast xuống dưới để tìm sàn
        if (Physics.Raycast(currentPos, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Default")))
        {
            groundPosition.Value = hit.point + Vector3.up * groundHeight;
        }
        else
        {
            groundPosition.Value = new Vector3(currentPos.x, groundHeight, currentPos.z);
        }

        transform.position = groundPosition.Value;
        isOnGround.Value = true;
    }

    // ✅ HÀM MỚI: XÓA PARENT TRÊN TẤT CẢ CLIENT
    [ClientRpc]
    private void ClearParentOnAllClientsClientRpc()
    {
        if (kitchenObjectParent != null)
        {
            kitchenObjectParent.ClearKitchenObject();
            kitchenObjectParent = null;
        }
    }

    private void StopFlying()
    {
        isFlying.Value = false;
        isOnGround.Value = false;
    }

    public bool HasKitchenObjectParent()
    {
        return kitchenObjectParent != null;
    }

    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObjectSO;
    }

    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
    {
        SetKitchenObjectParentServerRpc(kitchenObjectParent.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKitchenObjectParentServerRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        SetKitchenObjectParentClientRpc(kitchenObjectParentNetworkObjectReference);
    }

    [ClientRpc]
    private void SetKitchenObjectParentClientRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if (this.kitchenObjectParent != null)
        {
            this.kitchenObjectParent.ClearKitchenObject();
        }

        this.kitchenObjectParent = kitchenObjectParent;

        if (kitchenObjectParent.HasKitchenObject())
        {
            Debug.LogError("Quầy đã có một KitchenObject!");
        }
        kitchenObjectParent.SetKitchenObject(this);

        // Reset trạng thái nằm trên sàn
        isOnGround.Value = false;

        // ✅ BẬT LẠI FollowTransform khi có parent mới
        if (followTransform != null)
        {
            followTransform.enabled = true;
            followTransform.SetTargetTransform(kitchenObjectParent.GetKichenObjectFollowTranform());
        }
    }

    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }

    public void DestroySelf()
    {
        NetworkObject.Despawn(true);
    }

    public void ClearKitchenObjectOnParent()
    {
        if (kitchenObjectParent != null)
        {
            kitchenObjectParent.ClearKitchenObject();
        }
    }

    public bool TryGetPlate(out PlateKitchenObject plateKitchenObject)
    {
        if (this is PlateKitchenObject)
        {
            plateKitchenObject = this as PlateKitchenObject;
            return true;
        }
        else
        {
            plateKitchenObject = null;
            return false;
        }
    }

    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        KitchenGameMultiplayer.Instance.SpawnKitchenObject(kitchenObjectSO, kitchenObjectParent);
    }

    public static void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        // ✅ BỔ SUNG: Chỉ Server được phép hủy
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[KitchenObject] Client đang cố hủy object! Chặn lại.");
            return;
        }

        KitchenGameMultiplayer.Instance.DestroyKitchenObject(kitchenObject);
    }
}