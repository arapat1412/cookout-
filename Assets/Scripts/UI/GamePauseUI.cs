using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button optionsButton;
    private void Awake()
    {
        resumeButton.onClick.AddListener(() => {
            KitchenGameManager.Instance.TooglePauseGame();
        });
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        optionsButton.onClick.AddListener(() => {
            Hide();
           OptionsUI.Instance.Show(Show);
        });
    }
    private void Start()
    {
        KitchenGameManager.Instance.OnLocalGamePaused += KitchenGameManager_OnLocalGamePause;
        KitchenGameManager.Instance.OnLocalGameUnPaused += KitchenGameManager_OnLocalGameUnPause;

        Hide();
    }

    private void KitchenGameManager_OnLocalGamePause(object sender, System.EventArgs e)
    {
        Show();
    }

    private void KitchenGameManager_OnLocalGameUnPause(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void Show() {
        gameObject.SetActive(true);
        resumeButton.Select();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
