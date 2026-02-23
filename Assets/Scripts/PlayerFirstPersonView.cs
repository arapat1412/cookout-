using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering; // ✅ Cần thư viện này để dùng ShadowCastingMode

public class PlayerFirstPersonView : NetworkBehaviour
{
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private GameObject headVisual;
    [SerializeField] private Player player; // ✅ Kéo script Player vào đây trong Inspector

    private bool isFirstPersonActive = false;

    private void Awake()
    {
        // Tự động lấy component Player nếu quên kéo
        if (player == null) player = GetComponent<Player>();
    }

    private void Start()
    {
        // 🔴 CŨ: if (IsOwner) -> Sai vì Host sở hữu cả Bot
        // 🟢 MỚI: Thêm && NetworkObject.IsPlayerObject
        // Chỉ kích hoạt nếu là chủ sở hữu VÀ là nhân vật người chơi thật (Player Object)
        if (IsOwner && NetworkObject.IsPlayerObject)
        {
            GameInput.Instance.OnToggleViewAction += GameInput_OnToggleViewAction;
        }
        else
        {
            // Nếu không phải nhân vật của mình (hoặc là Bot), xóa/tắt camera đi
            if (firstPersonCamera != null)
            {
                firstPersonCamera.gameObject.SetActive(false);
            }
        }
    }

    private void GameInput_OnToggleViewAction(object sender, EventArgs e)
    {
        ToggleView();
    }

    private void ToggleView()
    {
        isFirstPersonActive = !isFirstPersonActive;

        // 1. GỌI SANG PLAYER ĐỂ ĐỔI CÁCH DI CHUYỂN
        if (player != null)
        {
            player.SetFirstPersonMode(isFirstPersonActive);
        }

        // 2. BẬT/TẮT CAMERA VÀ ẨN/HIỆN ĐẦU
        if (firstPersonCamera != null)
        {
            firstPersonCamera.gameObject.SetActive(isFirstPersonActive);

            if (headVisual != null)
            {
                SetHeadVisibility(!isFirstPersonActive);
            }
        }
    }

    private void SetHeadVisibility(bool isVisible)
    {
        // Lấy tất cả MeshRenderer bao gồm cả đầu, mắt, và MŨ (children)
        MeshRenderer[] renderers = headVisual.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer meshRenderer in renderers)
        {
            if (isVisible)
            {
                // Hiện lại bình thường (Cast Shadows: On)
                meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            }
            else
            {
                // Tàng hình nhưng vẫn đổ bóng xuống đất (Cast Shadows: Shadows Only)
                meshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnToggleViewAction -= GameInput_OnToggleViewAction;
        }
    }
}