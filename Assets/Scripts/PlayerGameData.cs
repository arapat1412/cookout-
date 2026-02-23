using System.Collections.Generic;

[System.Serializable]
public class PlayerGameData
{
    public string playerName;
    public int gold;              // Tiền vàng
    public List<int> ownedHatIds; // Danh sách ID mũ đã mua
    public int equippedHatId;     // ID mũ đang đội

    public long lastOnlineTicks;

    public PlayerGameData()
    {
        playerName = "New Chef";
        gold = 100;               // Tặng 100 vàng khởi nghiệp
        ownedHatIds = new List<int> { 0 }; // 0 là mặc định (không mũ)
        equippedHatId = 0;

        lastOnlineTicks = 0;
    }
}