using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SessionGoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void Start()
    {
        // Nếu không phải chế độ Coop thì ẩn luôn cái bảng vàng này đi (PvP không có vàng)
        if (KitchenGameMultiplayer.Instance.GetGameMode() != GameMode.Coop)
        {
            gameObject.SetActive(false);
            return;
        }

        // Lắng nghe sự kiện từ DeliveryManager
        DeliveryManager.Instance.OnSessionGoldChanged += DeliveryManager_OnSessionGoldChanged;

        // Cập nhật hiển thị lần đầu (số 0)
        UpdateVisual();
    }

    private void DeliveryManager_OnSessionGoldChanged(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        int earnedGold = DeliveryManager.Instance.GetSessionGoldEarned();
        // Hiển thị text, bạn có thể chỉnh màu vàng trong Unity Editor hoặc dùng thẻ <color>
        goldText.text = "GOLD: " + earnedGold.ToString();
    }

    // Dọn dẹp sự kiện khi object bị hủy để tránh lỗi
    private void OnDestroy()
    {
        if (DeliveryManager.Instance != null)
        {
            DeliveryManager.Instance.OnSessionGoldChanged -= DeliveryManager_OnSessionGoldChanged;
        }
    }
}