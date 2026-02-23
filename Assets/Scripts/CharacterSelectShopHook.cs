using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectShopHook : MonoBehaviour
{
    [SerializeField] private Button openShopButton;
    [SerializeField] private ShopUI shopUI;

    private void Awake()
    {
        openShopButton.onClick.AddListener(() => {
            shopUI.Show();
        });
    }
}