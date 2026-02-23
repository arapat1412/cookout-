//FILE:MainMenuUI
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button coopButton;
    [SerializeField] private Button pvpButton;
    [SerializeField] private Button pvp3TeamButton;
    [SerializeField] private Button quitButton;

    private GameMode selectedGameMode = GameMode.Coop; // ✅ LƯU TẠM

    private void Awake()
    {
        coopButton.onClick.AddListener(() => {
            selectedGameMode = GameMode.Coop; // ✅ Lưu lại mode
            Loader.Load(Loader.Scene.LoppyScene);
        });

        pvpButton.onClick.AddListener(() => {
            selectedGameMode = GameMode.PvP; // ✅ Lưu lại mode
            Loader.Load(Loader.Scene.LoppyScene);
        });

        // ✅ THÊM LOGIC NÚT 3 ĐỘI
        pvp3TeamButton.onClick.AddListener(() => {
            selectedGameMode = GameMode.PvP_3Team; // 3 Đội
            Loader.Load(Loader.Scene.LoppyScene);
        });

        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        Time.timeScale = 1f;
    }

    // ✅ HÀM MỚI: Được gọi TRƯỚC KHI load scene
    private void OnDestroy()
    {
        // Set GameMode trước khi chuyển scene
        // Lúc này KitchenGameMultiplayer đã được tạo lại bởi LobbyScene
        if (KitchenGameMultiplayer.Instance != null)
        {
            KitchenGameMultiplayer.Instance.SetGameMode(selectedGameMode);
        }
        else
        {
            // Lưu vào PlayerPrefs để dùng sau
            PlayerPrefs.SetInt("SelectedGameMode", (int)selectedGameMode);
            PlayerPrefs.Save();
        }
    }
}