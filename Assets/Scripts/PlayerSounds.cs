using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
    // Tham chiếu đến script logic của người chơi
    private Player player;

    // Biến đếm thời gian để tạo nhịp cho tiếng bước chân
    private float footstepTimer;

    // Thời gian tối đa giữa hai lần phát tiếng bước chân (có thể chỉnh trong Inspector)
    [SerializeField] private float footstepTimerMax = .1f;

    

    private void Awake()
    {
        // Lấy các component cần thiết từ chính GameObject này
        player = GetComponent<Player>();
        
    }

    private void Update()
    {
        // Đếm ngược thời gian mỗi frame
        footstepTimer -= Time.deltaTime;

        // Khi bộ đếm thời gian hết hạn
        if (footstepTimer < 0f)
        {
            // Đặt lại bộ đếm
            footstepTimer = footstepTimerMax;
            if(player.IsWalking())
            {
                float volume = 1f;
                SoundManager.Instance.PlayFootstepSound(player.transform.position, volume);
            }
           
        }
    }
}