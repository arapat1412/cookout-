using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;
using Newtonsoft.Json; // Cần cài package Newtonsoft.Json hoặc dùng JsonUtility nếu muốn

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // Dữ liệu local cache để game truy xuất nhanh
    public PlayerGameData LocalData { get; private set; } = new PlayerGameData();

    private const string KEY_PLAYER_DATA = "PLAYER_DATA";

    private void Awake()
    {
        if (Instance != null)
        {
            gameObject.SetActive(false);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        //// Bấm phím G trên bàn phím để nhận 10.000 vàng
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    AddGold(10000);
        //    Debug.Log("CHEAT ACTIVATED: Đã cộng 10.000 vàng!");

        //    // Nếu đang mở ShopUI, bạn cần đóng đi mở lại hoặc gọi hàm cập nhật UI
        //    // để thấy số tiền thay đổi.
        //}
    }

    public async Task SaveDataAsync(PlayerGameData data)
    {
        try
        {
            string json = JsonConvert.SerializeObject(data);
            var dataToSave = new Dictionary<string, object> { { KEY_PLAYER_DATA, json } };

            await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);

            LocalData = data; // Cập nhật cache
            Debug.Log("Đã lưu dữ liệu lên Cloud.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi lưu Cloud Save: " + e.Message);
        }
    }

    public async Task LoadDataAsync()
    {
        try
        {
            var keys = new HashSet<string> { KEY_PLAYER_DATA };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.TryGetValue(KEY_PLAYER_DATA, out var jsonItem))
            {
                string json = jsonItem.Value.GetAsString();
                LocalData = JsonConvert.DeserializeObject<PlayerGameData>(json);
                Debug.Log("Load dữ liệu thành công: " + LocalData.playerName);
            }
            else
            {
                Debug.Log("Người chơi mới. Tạo dữ liệu gốc...");
                LocalData = new PlayerGameData();
                await SaveDataAsync(LocalData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi tải Cloud Save: " + e.Message);
        }
    }

    // --- CÁC HÀM LOGIC CHO SHOP (PHẦN BẠN ĐANG THIẾU) ---

    // 1. Kiểm tra xem đã sở hữu mũ chưa
    public bool IsHatOwned(int hatId)
    {
        if (LocalData.ownedHatIds == null) LocalData.ownedHatIds = new List<int>();
        return LocalData.ownedHatIds.Contains(hatId);
    }

    // 2. Thử mua mũ (Trừ tiền và lưu lại)
    public async Task<bool> TryBuyHat(int hatId, int price)
    {
        if (IsHatOwned(hatId)) return true; // Đã có rồi

        if (LocalData.gold >= price)
        {
            // Trừ tiền
            LocalData.gold -= price;

            // Thêm vào danh sách sở hữu
            LocalData.ownedHatIds.Add(hatId);

            // Lưu ngay lập tức
            await SaveDataAsync(LocalData);

            Debug.Log($"Mua mũ ID {hatId} thành công!");
            return true;
        }

        Debug.Log("Không đủ vàng!");
        return false;
    }

    // 3. Trang bị mũ
    public async void EquipHat(int hatId)
    {
        if (IsHatOwned(hatId))
        {
            LocalData.equippedHatId = hatId;
            await SaveDataAsync(LocalData);
            Debug.Log($"Đã trang bị mũ ID {hatId}");
        }
    }

    // 4. Cộng vàng (Dùng khi thắng game)
    public async void AddGold(int amount)
    {
        LocalData.gold += amount;
        await SaveDataAsync(LocalData);
    }
    // Kiểm tra xem tài khoản có đang được sử dụng không
    public bool IsAccountBusy()
    {
        // 1. Lấy thời gian hiện tại của thế giới (UTC)
        long currentTicks = System.DateTime.UtcNow.Ticks;

        // 2. Lấy thời gian "nhịp tim" cuối cùng được lưu trên Cloud
        // (Biến LocalData.lastOnlineTicks là biến bạn đã thêm vào PlayerGameData ở bước trước)
        long lastTicks = LocalData.lastOnlineTicks;

        // 3. Tính khoảng cách thời gian
        System.TimeSpan difference = System.TimeSpan.FromTicks(currentTicks - lastTicks);

        // 4. QUY TẮC: Nếu lần cuối online cách đây dưới 40 giây -> Nghĩa là đang có người chơi
        // (Vì game sẽ gửi nhịp tim mỗi 20s, nên nếu < 40s tức là người đó vẫn còn đó)
        if (difference.TotalSeconds < 40)
        {
            return true; // BẬN - Đang có người chơi
        }

        return false; // RẢNH - Người kia đã thoát hoặc offline lâu rồi
    }

    // Gọi hàm này khi Login thành công (ở bước 4 bên trên)
    public void StartHeartbeat()
    {
        StopAllCoroutines();
        StartCoroutine(HeartbeatCoroutine());
    }

    private System.Collections.IEnumerator HeartbeatCoroutine()
    {
        while (true)
        {
            // 1. Cập nhật giờ hiện tại vào dữ liệu
            LocalData.lastOnlineTicks = System.DateTime.UtcNow.Ticks;

            // 2. Lưu lên Cloud (Gửi tín hiệu "Tôi còn sống")
            _ = SaveDataAsync(LocalData);

            // 3. Đợi 20 giây rồi lặp lại
            yield return new WaitForSeconds(20f);
        }
    }
}
