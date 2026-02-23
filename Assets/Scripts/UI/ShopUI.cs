using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform container;
    [SerializeField] private Transform shopItemTemplate;
    [SerializeField] private HatListSO hatListSO;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        shopItemTemplate.gameObject.SetActive(false);
        closeButton.onClick.AddListener(Hide);
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        UpdateVisual();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateVisual()
    {
        // 1. Cập nhật số vàng
        goldText.text = "GOLD: " + DataManager.Instance.LocalData.gold.ToString();

        // 2. Dọn dẹp danh sách cũ
        foreach (Transform child in container)
        {
            if (child == shopItemTemplate) continue;
            Destroy(child.gameObject);
        }

        // 3. Tạo danh sách mới
        foreach (HatSO hat in hatListSO.hatList)
        {
            if (hat.id == 0) continue; // Bỏ qua mũ mặc định (None)

            Transform shopItemTransform = Instantiate(shopItemTemplate, container);
            shopItemTransform.gameObject.SetActive(true);

            // Cập nhật thông tin cơ bản
            shopItemTransform.Find("NameText").GetComponent<TextMeshProUGUI>().text = hat.hatName;
            shopItemTransform.Find("PriceText").GetComponent<TextMeshProUGUI>().text = hat.price.ToString();

            // Cập nhật Icon (Nếu có)
            Transform iconTransform = shopItemTransform.Find("IconImage");
            if (iconTransform != null)
            {
                iconTransform.GetComponent<Image>().sprite = hat.icon;
            }

            // Lấy nút bấm và chữ trong nút
            Button actionButton = shopItemTransform.Find("ActionButton").GetComponent<Button>();
            TextMeshProUGUI buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();

            // --- LOGIC XỬ LÝ TRẠNG THÁI NÚT ---

            // TRƯỜNG HỢP 1: ĐÃ SỞ HỮU MŨ NÀY
            if (DataManager.Instance.IsHatOwned(hat.id))
            {
                // TRƯỜNG HỢP 1.1: ĐANG ĐỘI CÁI MŨ NÀY -> HIỆN NÚT "HỦY"
                if (DataManager.Instance.LocalData.equippedHatId == hat.id)
                {
                    buttonText.text = "HỦY"; // Hoặc "UNEQUIP"
                    actionButton.onClick.AddListener(() => {
                        // Logic Hủy: Đội cái mũ số 0 (Không đội gì cả)
                        DataManager.Instance.EquipHat(0);
                        KitchenGameMultiplayer.Instance.ChangePlayerHat(0);
                        UpdateVisual(); // Load lại để nút chuyển về "TRANG BỊ"
                    });
                }
                // TRƯỜNG HỢP 1.2: CÓ MŨ NHƯNG KHÔNG ĐỘI -> HIỆN NÚT "TRANG BỊ"
                else
                {
                    buttonText.text = "TRANG BỊ";
                    actionButton.onClick.AddListener(() => {
                        // Logic Trang bị: Đội cái mũ này
                        DataManager.Instance.EquipHat(hat.id);
                        KitchenGameMultiplayer.Instance.ChangePlayerHat(hat.id);
                        UpdateVisual(); // Load lại để nút chuyển thành "HỦY"
                    });
                }
            }
            // TRƯỜNG HỢP 2: CHƯA SỞ HỮU -> HIỆN NÚT "MUA"
            else
            {
                buttonText.text = "MUA";
                actionButton.onClick.AddListener(async () => {
                    bool success = await DataManager.Instance.TryBuyHat(hat.id, hat.price);

                    if (success)
                    {
                        // Mua xong chỉ Load lại UI -> Nút sẽ tự biến thành "TRANG BỊ"
                        // (Tôi đã bỏ dòng code tự động trang bị ở đây theo ý bạn)
                        UpdateVisual();
                    }
                });
            }
        }
    }
}