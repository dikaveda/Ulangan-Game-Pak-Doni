using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Details")]
public class ItemDetails : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite itemIcon;

    // Nama field disamakan dengan yang tersimpan di asset Coin/Coin2 ("itemDescription")
    [FormerlySerializedAs("description")]
    [TextArea] public string itemDescription;

    public bool isStackable = true;
    public int maxStackSize = 99;
}
