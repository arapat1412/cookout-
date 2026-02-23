using TMPro;
using UnityEngine;
using Unity.Netcode; // Cần thêm thư viện này

public class PlayerRoleUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private Player player;

    private ulong lastTrackedId = 999999; // Biến theo dõi sự thay đổi ID

    private void Start()
    {
        // Lắng nghe sự kiện thay đổi dữ liệu chung
        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;

        // Cập nhật lần đầu
        UpdateVisual();
    }

    // ✅ FIX QUAN TRỌNG: Kiểm tra liên tục xem ID của nhân vật có thay đổi không
    private void Update()
    {
        if (player != null)
        {
            ulong currentId = player.GetPlayerDataId();

            // Nếu ID thay đổi (Ví dụ: Từ 0 -> 9000), cập nhật lại giao diện ngay
            if (currentId != lastTrackedId)
            {
                lastTrackedId = currentId;
                UpdateVisual();
            }
        }
    }

    private void KitchenGameMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (KitchenGameMultiplayer.Instance == null || player == null) return;

        // 1. Lấy dữ liệu của người chơi sở hữu nhân vật này
        ulong playerId = player.GetPlayerDataId();
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(playerId);

        // Kiểm tra an toàn: Nếu ID là Bot (>= 9000) nhưng dữ liệu chưa khớp, tạm thời chưa hiện
        if (playerId >= 9000 && playerData.clientId != playerId)
        {
            // Có thể ẩn text đi nếu muốn: roleText.text = "...";
            return;
        }

        // 2. Kiểm tra Role và hiển thị
        if (playerData.role == PlayerRole.Chef)
        {
            roleText.text = "BẾP TRƯỞNG";
            roleText.color = Color.red;
            roleText.fontSize = 45;
        }
        else
        {
            roleText.text = "PHỤ BẾP";
            roleText.color = Color.blue;
            roleText.fontSize = 30;
        }
    }

    private void OnDestroy()
    {
        if (KitchenGameMultiplayer.Instance != null)
        {
            KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
        }
    }
}