using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlateIconsSingleUI : MonoBehaviour
{
    [SerializeField] private Image image; // Biểu tượng hình ảnh để hiển thị
    public void SetKitchenObjectSO(KitchenObjectSO kitchenObjectSO)
   {
        image.sprite = kitchenObjectSO.sprite;
    }
}
