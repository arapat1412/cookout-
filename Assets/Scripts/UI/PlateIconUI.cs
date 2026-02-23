using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateIconUI : MonoBehaviour
{

    [SerializeField] private PlateKitchenObject plateKitchenObject;
    [SerializeField] private Transform iconTemplate; // Biểu tượng mẫu để tạo các biểu tượng mới


    private void Awake()
    {
       iconTemplate.gameObject.SetActive(false); // Ẩn biểu tượng mẫu ban đầu
    }


    private void Start()
    {
        plateKitchenObject.OnIngredientAdded += PlateKitchenObject_OnIngredientAdded;
        
    }

    private void PlateKitchenObject_OnIngredientAdded(object sender, PlateKitchenObject.OnIngredientAddedEventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // Cập nhật giao diện người dùng PlateIconUI nếu cần
        // Ví dụ: hiển thị số lượng nguyên liệu trên đĩa
        // Hoặc cập nhật hình ảnh biểu tượng dựa trên nguyên liệu hiện có
        foreach(Transform child in transform)
        {
            if (child == iconTemplate) continue;
            Destroy(child.gameObject);
            
        }
        foreach (KitchenObjectSO kitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
        {
            Transform iconTransform = Instantiate(iconTemplate, transform);
            iconTransform.gameObject.SetActive(true); // Hiển thị biểu tượng
            iconTransform.GetComponent<PlateIconsSingleUI>().SetKitchenObjectSO(kitchenObjectSO);

        }
    }
}
