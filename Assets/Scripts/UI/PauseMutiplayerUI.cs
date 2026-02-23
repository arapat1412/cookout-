using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMutiplayerUI : MonoBehaviour
{
    private void Start()
    {
        KitchenGameManager.Instance.OnMutiplayerGamePaused += KitchenGameManager_OnMutiplayerGamePaused;
        KitchenGameManager.Instance.OnMutiplayerGameUnPaused += KitchenGameManager_OnMutiplayerGameUnPaused;
        Hide();
    }

    private void KitchenGameManager_OnMutiplayerGameUnPaused(object sender, EventArgs e)
    {
        Hide();
    }

    private void KitchenGameManager_OnMutiplayerGamePaused(object sender, EventArgs e)
    {
        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
