using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer headMeshRenderer;
    [SerializeField] private MeshRenderer bodyMeshRenderer;

    [Header("Hat Settings")]
    [SerializeField] private HatListSO hatListSO;
    [SerializeField] private Transform headAnchor;

    private Material material;
    private GameObject currentHatGO;

    // ✅ FIX: Biến để theo dõi sự thay đổi ID
    //private ulong lastTrackedId = 999999;

    private void Awake()
    {
        if (headMeshRenderer != null)
        {
            material = new Material(headMeshRenderer.material);
            headMeshRenderer.material = material;
            if (bodyMeshRenderer != null) bodyMeshRenderer.material = material;
        }
    }

    private void Start()
    {
        // Đăng ký sự kiện: Khi danh sách người chơi thay đổi -> Cập nhật màu
        if (KitchenGameMultiplayer.Instance != null)
        {
            KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
        }

        // --- THÊM DÒNG NÀY ---
        // Cập nhật ngay lập tức khi nhân vật vừa sinh ra để lấy dữ liệu đã có sẵn
        UpdatePlayerVisuals();
    }

    // ✅ FIX: Thêm hàm Update để tự động refresh visual khi ID thay đổi
    // (Lý do: Khi Bot mới spawn, ID là 0. Sau vài frame NetworkVariable mới sync về là 9xxx.
    // Nếu chỉ update ở Start, Client sẽ thấy Bot màu trắng của Host).
    //private void Update()
    //{
    //    Player player = GetComponentInParent<Player>();
    //    if (player != null)
    //    {
    //        ulong currentId = player.GetPlayerDataId();
    //        if (currentId != lastTrackedId)
    //        {
    //            lastTrackedId = currentId;
    //            UpdatePlayerVisuals();
    //        }
    //    }
    //}


    private void KitchenGameMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayerVisuals();
    }
    public void ForceUpdateColor()
    {
        UpdatePlayerVisuals();
    }

    private void UpdatePlayerVisuals()
    {
        if (KitchenGameMultiplayer.Instance == null) return;
        Player player = GetComponentInParent<Player>();
        if (player == null) return;

        ulong playerId = player.GetPlayerDataId();
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(playerId);

        // Check an toàn
        if (playerData.clientId != playerId && playerId < 9000) return;

        // 1. XỬ LÝ MÀU SẮC
        if (KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.PvP ||
            KitchenGameMultiplayer.Instance.GetGameMode() == GameMode.PvP_3Team)
        {
            if (playerData.teamId == Team.Blue) SetPlayerColor(Color.blue);
            else if (playerData.teamId == Team.Red) SetPlayerColor(Color.red);
            else if (playerData.teamId == Team.Yellow) SetPlayerColor(Color.yellow);
            else SetPlayerColor(Color.white);
        }
        else
        {
            SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
        }

        // 2. XỬ LÝ MŨ
        SetPlayerHat(playerData.hatId);
    }

    public void SetPlayerHat(int hatId)
    {
        if (currentHatGO != null) Destroy(currentHatGO);

        foreach (var hat in hatListSO.hatList)
        {
            if (hat.id == hatId)
            {
                if (hat.prefab != null)
                {
                    currentHatGO = Instantiate(hat.prefab, headAnchor);
                    currentHatGO.transform.localPosition = Vector3.zero;
                    currentHatGO.transform.localRotation = Quaternion.identity;
                }
                return;
            }
        }
    }

    public void SetPlayerColor(Color color)
    {
        if (material != null) material.color = color;
    }

    private void OnDestroy()
    {
        // Nhớ hủy đăng ký sự kiện để tránh lỗi
        if (KitchenGameMultiplayer.Instance != null)
        {
            KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
        }
    }
}