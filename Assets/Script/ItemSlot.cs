using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    // Dipanggil otomatis oleh EventSystem saat slot diklik
    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SelectSlot(this);
        }
    }

    [Header("UI References")]
    [SerializeField] private Image itemImage;          // Sesuaikan dengan "ItemImage"
    [SerializeField] private TextMeshProUGUI itemQuantity; // Sesuaikan dengan "ItemQuantity"
    [SerializeField] private GameObject itemSelected;  // Sesuaikan dengan "ItemSelected" (untuk highlight/pilih slot)

    private ItemDetails currentItem;
    private int quantity;

    // Cadangan: kalau referensi di Inspector kosong, cari otomatis berdasarkan nama child
    private void EnsureReferences()
    {
        if (itemImage == null)
        {
            Transform t = transform.Find("ItemImage");
            if (t != null) itemImage = t.GetComponent<Image>();
        }
        if (itemQuantity == null)
        {
            Transform t = transform.Find("ItemQuantity");
            if (t != null) itemQuantity = t.GetComponent<TextMeshProUGUI>();
        }
        if (itemSelected == null)
        {
            Transform t = transform.Find("ItemSelected");
            if (t != null) itemSelected = t.gameObject;
        }
    }

    // Menampilkan data item pada slot
    public void SetupSlot(ItemDetails item, int itemAmount)
    {
        EnsureReferences();
        currentItem = item;
        quantity = itemAmount;

        if (currentItem != null)
        {
            if (itemImage != null)
            {
                itemImage.sprite = currentItem.itemIcon;
                itemImage.gameObject.SetActive(true);
            }

            if (itemQuantity != null)
            {
                itemQuantity.text = quantity > 1 ? quantity.ToString() : "";
                itemQuantity.gameObject.SetActive(quantity > 1);
            }
        }
        else
        {
            ClearSlot();
        }
    }

    // Mengosongkan slot
    public void ClearSlot()
    {
        EnsureReferences();
        currentItem = null;
        quantity = 0;

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.gameObject.SetActive(false);
        }

        if (itemQuantity != null)
        {
            itemQuantity.text = "";
            itemQuantity.gameObject.SetActive(false);
        }

        SetSelected(false);
    }

    // Untuk mengatur status terpilih (highlight) pada slot
    public void SetSelected(bool isSelected)
    {
        if (itemSelected != null)
        {
            itemSelected.SetActive(isSelected);
        }
    }

    public ItemDetails GetItem() => currentItem;
    public int GetQuantity() => quantity;
}