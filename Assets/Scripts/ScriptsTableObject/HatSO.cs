using UnityEngine;
[CreateAssetMenu(menuName = "Shop/Hat")]
public class HatSO : ScriptableObject
{
    public int id;
    public string hatName;
    public int price;
    public GameObject prefab;
    public Sprite icon;
}