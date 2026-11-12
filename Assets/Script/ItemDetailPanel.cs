using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Panel di samping inventory yang menampilkan detail item yang diklik.
// Pasang di objek "InventoryDetail". Kalau referensi di Inspector kosong,
// script mencari child berdasarkan nama (ItemImage, ItemNameText, ItemQTYText, ItemDescriptionText).
public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemQuantityText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    private bool referensiSiap = false;

    private void EnsureReferences()
    {
        if (referensiSiap) return;

        if (itemImage == null) itemImage = CariGambar("ItemImage");
        if (itemNameText == null) itemNameText = CariChild<TextMeshProUGUI>("ItemNameText");
        if (itemQuantityText == null) itemQuantityText = CariChild<TextMeshProUGUI>("ItemQTYText");
        if (itemDescriptionText == null) itemDescriptionText = CariChild<TextMeshProUGUI>("ItemDescriptionText");

        referensiSiap = true;
    }

    // Cari objek bernama tertentu: di child panel ini dulu, lalu di "saudara" (child dari parent).
    // Objek di dalam slot inventory (ItemSlot) dilewati supaya tidak salah ambil.
    private Transform CariObjek(string nama)
    {
        Transform[] lokasi = transform.parent != null
            ? new[] { transform, transform.parent }
            : new[] { transform };

        foreach (Transform akar in lokasi)
        {
            foreach (Transform t in akar.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nama && t.GetComponentInParent<ItemSlot>(true) == null) return t;
            }
        }
        return null;
    }

    private T CariChild<T>(string namaChild) where T : Component
    {
        Transform t = CariObjek(namaChild);
        T komponen = t != null ? t.GetComponent<T>() : null;
        if (komponen == null) Debug.LogWarning($"ItemDetailPanel: '{namaChild}' tidak ditemukan.", this);
        return komponen;
    }

    // ItemImage di scene belum punya komponen Image, jadi ditambahkan otomatis
    private Image CariGambar(string nama)
    {
        Transform t = CariObjek(nama);
        if (t == null)
        {
            Debug.LogWarning($"ItemDetailPanel: '{nama}' tidak ditemukan.", this);
            return null;
        }

        Image img = t.GetComponent<Image>();
        if (img == null) img = t.gameObject.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    // Tampilkan detail item
    public void Show(ItemDetails item, int quantity)
    {
        EnsureReferences();

        if (item == null)
        {
            Clear();
            return;
        }

        if (itemImage != null)
        {
            itemImage.sprite = item.itemIcon;
            itemImage.preserveAspect = true;
            itemImage.enabled = item.itemIcon != null;
        }

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemQuantityText != null) itemQuantityText.text = "Jumlah: " + quantity;
        if (itemDescriptionText != null) itemDescriptionText.text = item.itemDescription;
    }

    // Kosongkan panel (tidak ada item yang dipilih)
    public void Clear()
    {
        EnsureReferences();

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.enabled = false;
        }

        if (itemNameText != null) itemNameText.text = "";
        if (itemQuantityText != null) itemQuantityText.text = "";
        if (itemDescriptionText != null) itemDescriptionText.text = "";
    }
}
