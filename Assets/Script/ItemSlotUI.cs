using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI amountText;

    // Menampilkan data item ke UI Slot
    public void SetSlot(ItemDetails item, int amount)
    {
        if (item != null && amount > 0)
        {
            iconImage.sprite = item.itemIcon;
            iconImage.enabled = true;

            // Tampilkan angka jumlah hanya jika lebih dari 1
            if (amount > 1)
            {
                amountText.text = amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.enabled = false;
            }
        }
        else
        {
            ClearSlot();
        }
    }

    // Mengosongkan tampilan slot
    public void ClearSlot()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
        amountText.enabled = false;
    }
}