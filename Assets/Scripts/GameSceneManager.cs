using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [Header("Kitchen Areas")]
    [SerializeField] private GameObject blueTeamKitchen;
    [SerializeField] private GameObject redTeamKitchen;
    [SerializeField] private GameObject yellowTeamKitchen;
    [SerializeField] private GameObject coopKitchen; // Kitchen gốc ở giữa
    [SerializeField] private GameObject middleWall;
    [SerializeField] private GameObject wall1;
    [SerializeField] private GameObject wall2;
    [SerializeField] private GameObject wall3;
    [SerializeField] private GameObject cube1;
    [SerializeField] private GameObject cube2;
    [SerializeField] private GameObject cube3;


    private void Start()
    {
        SetupSceneForGameMode();
    }

    private void SetupSceneForGameMode()
    {
        GameMode mode = KitchenGameMultiplayer.Instance.GetGameMode();

        if (mode == GameMode.PvP)
        {
            // Chế độ PvP: Hiện 2 kitchen, ẩn kitchen coop
            blueTeamKitchen.SetActive(true);
            redTeamKitchen.SetActive(true);
            yellowTeamKitchen.SetActive(false);
            middleWall.SetActive(true);

            if (coopKitchen != null)
                coopKitchen.SetActive(false);
                wall1.SetActive(false);
            wall2.SetActive(false);
            wall3.SetActive(false);
            cube1.SetActive(false);
            cube2.SetActive(false);
            cube3.SetActive(false);
        }
        else if (mode == GameMode.PvP_3Team)
        {
            // Bật cả Xanh, Đỏ, Vàng.
            blueTeamKitchen.SetActive(true);
            redTeamKitchen.SetActive(true);
            yellowTeamKitchen.SetActive(true); // Nhớ khai báo biến này
            middleWall.SetActive(false);
            coopKitchen.SetActive(false);

            if (coopKitchen != null)
                coopKitchen.SetActive(false);
            wall1.SetActive(false);
            wall2.SetActive(false);
            wall3.SetActive(false);
            cube1.SetActive(false);
            cube2.SetActive(false);
            cube3.SetActive(false);
        }
        else // Coop
        {
            // Chế độ Coop: Hiện kitchen giữa, ẩn 2 kitchen PvP
            blueTeamKitchen.SetActive(false);
            redTeamKitchen.SetActive(false);
            yellowTeamKitchen.SetActive(false);
            middleWall.SetActive(false);

            if (coopKitchen != null)
                coopKitchen.SetActive(true);
        }
    }
}
