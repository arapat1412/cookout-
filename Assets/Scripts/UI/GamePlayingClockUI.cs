using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayingClockUI : MonoBehaviour
{
    [SerializeField] private Image timeImage;

    private void Update()
    {
        if (KitchenGameManager.Instance == null) return;
        // Kiểm tra xem game có đang trong trạng thái chơi không
        if (KitchenGameManager.Instance.IsGamePlaying())
        {
            // Lấy giá trị thời gian đã chuẩn hóa (từ 0 đến 1) từ KitchenGameManager
            // và cập nhật trực tiếp Fill Amount của hình ảnh.
            timeImage.fillAmount = KitchenGameManager.Instance.GetGamePlayingTimerNormalized();
        }
        else
        {
            // Nếu game chưa bắt đầu hoặc đã kết thúc, bạn có thể muốn đặt lại đồng hồ
            // Ví dụ, đặt fillAmount về 1 (đầy) hoặc 0 (rỗng) tùy theo logic của bạn.
            // Ở đây, tôi sẽ ẩn nó đi khi không chơi game.
            // gameObject.SetActive(false); // Bỏ comment dòng này nếu bạn muốn ẩn đồng hồ
        }
    }
}