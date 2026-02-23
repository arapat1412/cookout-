using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }

    private async void Awake()
    {
        if (Instance != null)
        {
            gameObject.SetActive(false);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Tự động khởi tạo Unity Services khi game bật
        await InitializeAsync();
    }

    // ✅ Hàm 1: LoginUI cần gọi hàm này để đảm bảo Unity Services đã chạy
    public async Task InitializeAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
    }

    // ✅ Hàm 2: Đăng ký (Đổi tên thành RegisterAsync cho khớp với LoginUI)
    public async Task RegisterAsync(string username, string password)
    {
        // 1. Đăng ký tài khoản
        await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

        // 2. Tạo dữ liệu người chơi mới trên Cloud ngay lập tức
        // (Lưu ý: Đảm bảo DataManager đã có hàm SaveDataAsync)
        await DataManager.Instance.SaveDataAsync(new PlayerGameData { playerName = username });

        Debug.Log("Đăng ký thành công: " + username);
    }

    // ✅ Hàm 3: Đăng nhập (Đổi tên thành LoginAsync cho khớp với LoginUI)
    public async Task LoginAsync(string username, string password)
    {
        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        Debug.Log("Đăng nhập thành công: " + username);
    }


}