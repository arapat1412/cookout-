using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera coopCamera;
    [SerializeField] private Camera pvpCamera;
    [SerializeField] private Camera pvp3TeamCamera; // ✅ THÊM DÒNG NÀY

    [Header("Coop Camera Settings")]
    [SerializeField] private Vector3 coopPosition = new Vector3(0, 15, -10);
    [SerializeField] private Vector3 coopRotation = new Vector3(60, 0, 0);
    [SerializeField] private float coopFieldOfView = 60f;

    [Header("PvP Camera Settings")]
    [SerializeField] private Vector3 pvpPosition = new Vector3(0, 20, -5);
    [SerializeField] private Vector3 pvpRotation = new Vector3(65, 0, 0);
    [SerializeField] private float pvpFieldOfView = 70f;

    [Header("PvP 3 Team Camera Settings")] // ✅ THÊM MỚI SETTING CHO 3 TEAM
    [SerializeField] private Vector3 pvp3TeamPosition = new Vector3(0, 24, -12); // Cao hơn để nhìn bao quát
    [SerializeField] private Vector3 pvp3TeamRotation = new Vector3(75, 0, 0); // Chúc xuống nhiều hơn
    [SerializeField] private float pvp3TeamFieldOfView = 80f; // Góc nhìn rộng hơn

    private void Start()
    {
        // Đợi 1 frame để đảm bảo KitchenGameMultiplayer đã khởi tạo dữ liệu
        Invoke(nameof(SetupCamera), 0.1f);
    }

    private void SetupCamera()
    {
        if (KitchenGameMultiplayer.Instance == null) return;

        GameMode mode = KitchenGameMultiplayer.Instance.GetGameMode();

        if (mode == GameMode.PvP)
        {
            SetupPvPCamera();
        }
        else if (mode == GameMode.PvP_3Team)
        {
            SetupPvP3TeamCamera(); // ✅ GỌI HÀM MỚI
        }
        else
        {
            SetupCoopCamera();
        }
    }

    // --- HÀM MỚI: SETUP CAMERA 3 ĐỘI ---
    private void SetupPvP3TeamCamera()
    {
        // 1. Bật Camera 3 Team
        if (pvp3TeamCamera != null)
        {
            pvp3TeamCamera.enabled = true;
            pvp3TeamCamera.transform.position = pvp3TeamPosition;
            pvp3TeamCamera.transform.eulerAngles = pvp3TeamRotation;
            pvp3TeamCamera.fieldOfView = pvp3TeamFieldOfView;
            pvp3TeamCamera.tag = "MainCamera";

            var listener = pvp3TeamCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        // 2. Tắt các Camera còn lại
        DisableCamera(coopCamera);
        DisableCamera(pvpCamera);
    }
    // ------------------------------------

    private void SetupPvPCamera()
    {
        if (pvpCamera != null)
        {
            pvpCamera.enabled = true;
            pvpCamera.transform.position = pvpPosition;
            pvpCamera.transform.eulerAngles = pvpRotation;
            pvpCamera.fieldOfView = pvpFieldOfView;
            pvpCamera.tag = "MainCamera";

            var listener = pvpCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        DisableCamera(coopCamera);
        DisableCamera(pvp3TeamCamera); // Tắt Camera 3 team nếu có
    }

    private void SetupCoopCamera()
    {
        if (coopCamera != null)
        {
            coopCamera.enabled = true;
            coopCamera.transform.position = coopPosition;
            coopCamera.transform.eulerAngles = coopRotation;
            coopCamera.fieldOfView = coopFieldOfView;
            coopCamera.tag = "MainCamera";

            var listener = coopCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        DisableCamera(pvpCamera);
        DisableCamera(pvp3TeamCamera); // Tắt Camera 3 team nếu có
    }

    // Hàm phụ trợ để tắt camera gọn hơn
    private void DisableCamera(Camera cam)
    {
        if (cam != null)
        {
            cam.enabled = false;
            cam.tag = "Untagged";
            var listener = cam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }
}