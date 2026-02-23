using TMPro;
using UnityEngine;

public class GameStartWarningUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI warningText;

    private void Start()
    {
        // Lắng nghe sự kiện từ CharacterSelectReady
        CharacterSelectReady.Instance.OnGameStartFailed += CharacterSelectReady_OnGameStartFailed;

        // Ẩn đi lúc đầu
        gameObject.SetActive(false);
    }

    private void CharacterSelectReady_OnGameStartFailed(object sender, string message)
    {
        Show(message);
    }

    private void Show(string message)
    {
        gameObject.SetActive(true);
        warningText.text = message;

        // Hủy lệnh ẩn cũ (nếu có) để tránh bị tắt ngang xương
        CancelInvoke(nameof(Hide));

        // Tự động ẩn sau 3 giây
        Invoke(nameof(Hide), 3f);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Dọn dẹp sự kiện khi chuyển scene
        if (CharacterSelectReady.Instance != null)
        {
            CharacterSelectReady.Instance.OnGameStartFailed -= CharacterSelectReady_OnGameStartFailed;
        }
    }
}