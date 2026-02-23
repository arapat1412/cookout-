using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounterSound : MonoBehaviour
{
    // Tham chiếu đến script logic của bếp lò
    [SerializeField] private StoveCounter stoveCounter;

    // Tham chiếu đến component AudioSource để phát âm thanh
    private AudioSource audioSource;
    private float warningSoundTimer;
    private bool playWarningSound;

    private void Awake()
    {
        // Lấy component AudioSource được gắn cùng GameObject
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện thay đổi trạng thái từ stoveCounter
        // Khi sự kiện OnStateChanged của bếp được kích hoạt, phương thức StoveCounter_OnStateChanged sẽ được gọi
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
        stoveCounter.OnProgressChanged += StoveCounter_OnProgessChanged;
    }

    private void StoveCounter_OnProgessChanged(object sender, IHasProgress.OnProgessChangedEventArgs e)
    {
        float burnShowProgessAmout = .5f;
        playWarningSound = stoveCounter.IsFried() && e.progessNormalized >= burnShowProgessAmout;
    }

    private void StoveCounter_OnStateChanged(object sender, StoveCounter.OnStateChangedEventArgs e)
    {
        // Kiểm tra xem trạng thái mới có phải là đang nấu hoặc đã nấu xong không
        bool playSound = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Fried;

        if (playSound)
        {
            // Nếu đúng, bật âm thanh
            audioSource.Play();
        }
        else
        {
            // Nếu không, tắt âm thanh
            audioSource.Pause();
        }
    }
    private void Update()
    {
        if (playWarningSound)
        {
            warningSoundTimer -= Time.deltaTime;
            if (warningSoundTimer <= 0f)
            {
                float warningSoundTimerMax = .2f;
                warningSoundTimer = warningSoundTimerMax;
                SoundManager.Instance.PlayWarningShound(stoveCounter.transform.position);
            }
        }
    }
}