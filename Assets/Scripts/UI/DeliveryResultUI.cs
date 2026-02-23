using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryResultUI : MonoBehaviour
{
    private const string POPUP = "Popup";

    [SerializeField] private Image backgoundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Color succesColor;
    [SerializeField] private Color failedColor;
    [SerializeField] private Sprite succesSprite;
    [SerializeField] private Sprite failedSprite;

    // --- SỬA ĐỔI 1: Thêm biến để xác định UI này thuộc về đội nào ---
    [Header("PvP Settings")]
    [SerializeField] private Team assignedTeam = Team.None;
    // Trong Unity Editor:
    // - Với UI bên bếp Xanh, bạn chọn assignedTeam = Blue
    // - Với UI bên bếp Đỏ, bạn chọn assignedTeam = Red
    // - Với UI dùng cho chế độ Coop (nếu có), để None

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        DeliveryManager.Instance.OnRecipeSuccess += DeliveryManager_OnRecipeSuccess;
        DeliveryManager.Instance.OnRecipeFailed += DeliveryManager_OnRecipeFailed;
        gameObject.SetActive(false);
    }

    private void DeliveryManager_OnRecipeFailed(object sender, DeliveryManager.OnRecipeFailedEventArgs e)
    {
        // --- SỬA ĐỔI 2: Logic hiển thị theo Team của UI ---

        // Nếu UI này đã được gán cho một đội cụ thể (Blue hoặc Red)
        if (assignedTeam != Team.None)
        {
            // Nếu đội thất bại KHÔNG PHẢI là đội của UI này -> Không hiện
            if (e.teamId != assignedTeam)
            {
                return;
            }
        }
        // ----------------------------------------------------

        gameObject.SetActive(true);
        animator.SetTrigger(POPUP);
        backgoundImage.color = failedColor;
        iconImage.sprite = failedSprite;
        messageText.text = "Delivery\nFailed";
    }

    private void DeliveryManager_OnRecipeSuccess(object sender, DeliveryManager.OnRecipeSuccessEventArgs e)
    {
        // --- SỬA ĐỔI 3: Logic hiển thị theo Team của UI ---

        // Nếu UI này đã được gán cho một đội cụ thể (Blue hoặc Red)
        if (assignedTeam != Team.None)
        {
            // Nếu đội thành công KHÔNG PHẢI là đội của UI này -> Không hiện
            if (e.teamId != assignedTeam)
            {
                return;
            }
        }
        // ----------------------------------------------------

        gameObject.SetActive(true);
        animator.SetTrigger(POPUP);
        backgoundImage.color = succesColor;
        iconImage.sprite = succesSprite;
        messageText.text = "Delivery\nSucces";
    }
}