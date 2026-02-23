using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingForOtherPlayerUI : MonoBehaviour
{
    private void Start()
    {
        KitchenGameManager.Instance.OnLocalPlayerReadyChanged += KitchenGameManager_OnLocalPlayerReadyChanged;
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        Hide();
    }

    private void KitchenGameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsCountdownToStartActive())
        {
            Debug.Log($"[{Time.time}] WaitingForOtherPlayerUI - State changed to Countdown, calling Hide()."); // Log lý do Hide
            Hide();
        }
    }

    private void KitchenGameManager_OnLocalPlayerReadyChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsLocalPlayerReady())
        {
            Debug.Log($"[{Time.time}] WaitingForOtherPlayerUI - Local Player Ready, calling Show()."); // Log lý do Show
            Show();
        }
    }

    private void Show()
    {
        Debug.Log($"[{Time.time}] WaitingForOtherPlayerUI - Show() called."); // Log thời gian
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        Debug.Log($"[{Time.time}] WaitingForOtherPlayerUI - Hide() called."); gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
    
    //private void OnDestroy()
    //{
    //    // Rất quan trọng: Hủy đăng ký các event khi đối tượng UI này bị hủy
    //    if (KitchenGameManager.Instance != null)
    //    {
    //        KitchenGameManager.Instance.OnLocalPlayerReadyChanged -= KitchenGameManager_OnLocalPlayerReadyChanged;
    //        KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    //    }
    //}
}
