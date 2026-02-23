using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        statusText.text = "";
    }

    private async void Start()
    {
        // Khởi tạo Unity Services
        await AuthenticationManager.Instance.InitializeAsync();

        // Đoạn code Auto-Login cũ (đã tắt theo hướng dẫn trước)
        // if (AuthenticationService.Instance.SessionTokenExists) { ... }

        statusText.text = "Vui lòng nhập tài khoản và mật khẩu."; 
    }

    private async void OnLoginClicked()
    {
        // Hiện thông báo đang xử lý...
        statusText.text = "Đang kiểm tra tài khoản...";
        statusText.color = Color.yellow;

        try
        {
            // 1. Đăng nhập vào Unity Services trước (Bắt buộc phải làm bước này mới tải được dữ liệu về check)
            await AuthenticationManager.Instance.LoginAsync(usernameInput.text, passwordInput.text);

            // 2. Tải dữ liệu từ Cloud về máy
            await DataManager.Instance.LoadDataAsync();

            // 3. --- CHỐT CHẶN QUAN TRỌNG Ở ĐÂY ---
            if (DataManager.Instance.IsAccountBusy())
            {
                // ==> PHÁT HIỆN CÓ NGƯỜI ĐANG CHƠI!

                // A. Hiện thông báo lỗi đỏ rực
                statusText.text = "LỖI: Tài khoản này đang có người chơi!";
                statusText.color = Color.red;

                Debug.LogError("Chặn đăng nhập: Tài khoản đang online ở thiết bị khác.");

                // B. Đăng xuất ngay lập tức (Kick người này ra khỏi session vừa tạo)
                // Để họ không thể đi tiếp vào Menu chính
                AuthenticationService.Instance.SignOut();

                // C. Bật lại nút bấm để họ thử lại sau
                ToggleInput(true);

                // D. Dừng hàm tại đây, KHÔNG CHO CHẠY TIẾP
                return;
            }

            // 4. Nếu vượt qua chốt chặn trên (Tài khoản rảnh) -> Cho phép vào game
            statusText.text = "Đăng nhập thành công!";
            statusText.color = Color.green;

            // Bắt đầu gửi "nhịp tim" của chính mình để đánh dấu chủ quyền
            DataManager.Instance.StartHeartbeat();

            // Chuyển cảnh vào Lobby/Menu
            OnAuthSuccess();
        }
        catch (System.Exception ex)
        {
            // Xử lý lỗi sai mật khẩu, mất mạng... như bình thường
            statusText.text = "Lỗi đăng nhập: " + ex.Message;
            statusText.color = Color.red;
            ToggleInput(true);
        }
    }

    private async void OnRegisterClicked()
    {
        statusText.text = "Đang đăng kí ...";
        statusText.color = Color.yellow;
        ToggleInput(false);

        try
        {
            await AuthenticationManager.Instance.RegisterAsync(usernameInput.text, passwordInput.text);

            // Đăng ký xong tạo dữ liệu luôn
            await DataManager.Instance.SaveDataAsync(new PlayerGameData { playerName = usernameInput.text });

            statusText.text = "Đăng kí thành công ! Đang vào game..."; 
            statusText.color = Color.green;
            OnAuthSuccess();
        }
        catch (System.Exception ex)
        {
            // Dịch lỗi tiếng Anh sang tiếng Việt không dấu
            statusText.text = TranslateError(ex.Message);
            statusText.color = Color.red;
            ToggleInput(true);
        }
    }

    private void ToggleInput(bool interactable)
    {
        loginButton.interactable = interactable;
        registerButton.interactable = interactable;
        usernameInput.interactable = interactable;
        passwordInput.interactable = interactable;
    }

    private async void OnAuthSuccess()
    {
        // Load dữ liệu người chơi
        await DataManager.Instance.LoadDataAsync();
        Loader.Load(Loader.Scene.MainMenuScene);
    }

    // --- HÀM MỚI: DỊCH LỖI TỪ UNITY SANG TIẾNG VIỆT KHÔNG DẤU ---
    private string TranslateError(string englishError)
    {
        string error = englishError.ToLower();

        // Các lỗi phổ biến của Unity Authentication
        if (error.Contains("not found") || error.Contains("wrong password") || error.Contains("unauthorized"))
        {
            return "Sai tài khoản hoặc mật khẩu .";
        }
        if (error.Contains("already exists") || error.Contains("conflict"))
        {
            return "Tài khoản này đã tồn tại.";
        }
        if (error.Contains("invalid") && (error.Contains("password") || error.Contains("length")))
        {
            return "Mật khẩu quá ngắn (Cần ít nhất 8 kí tự , 1 chữ hoa, 1 số).";
        }
        if (error.Contains("network") || error.Contains("timeout") || error.Contains("connection"))
        {
            return "Lỗi kết nối mạng!Vui lòng thử lại";
        }
        if (error.Contains("empty") || error.Contains("null"))
        {
            return "Vui lòng không để trống .";
        }

        // Nếu lỗi lạ chưa dịch được thì hiện nguyên văn (để mình còn biết đường sửa)
        return "Lỗi: " + englishError;
    }
}