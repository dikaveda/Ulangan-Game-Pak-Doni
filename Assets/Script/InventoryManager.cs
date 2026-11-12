using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;       // Masukkan objek InventoryPanel (atau InventoryCanvas) di sini
    [SerializeField] private Transform slotParent;          // Panel tempat slot item (InventorySlots)
    [SerializeField] private GameObject itemSlotPrefab;     // Prefab ItemSlot
    [SerializeField] private int totalSlots = 20;           // Jumlah total slot
    [SerializeField] private ItemDetailPanel detailPanel;   // Panel detail di samping (objek InventoryDetail)

    [Header("Input Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I; // Tombol untuk buka/tutup inventory (default: 'I')

    private List<ItemSlot> slots = new List<ItemSlot>();
    private bool isInventoryOpen = false;
    private ItemSlot selectedSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializeInventory();
        SetupDetailPanel();

        // Pastikan inventory tertutup saat game dimulai
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;
        }
    }

    private void Update()
    {
        // Mendeteksi tombol ditekan untuk membuka/menutup inventory
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
        }
    }

    private void InitializeInventory()
    {
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        slots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, slotParent);
            ItemSlot slot = slotObj.GetComponent<ItemSlot>();
            slot.ClearSlot();
            slots.Add(slot);
        }
    }

    private void SetupDetailPanel()
    {
        // Cari panel detail kalau belum diisi di Inspector
        if (detailPanel == null)
        {
            detailPanel = FindAnyObjectByType<ItemDetailPanel>(FindObjectsInactive.Include);
        }

        // Kalau komponennya belum dipasang, pasang otomatis di objek "InventoryDetail"
        if (detailPanel == null && inventoryPanel != null)
        {
            Transform detail = inventoryPanel.transform.Find("InventoryDetail");
            if (detail != null) detailPanel = detail.gameObject.AddComponent<ItemDetailPanel>();
        }

        if (detailPanel != null) detailPanel.Clear();
        else Debug.LogWarning("InventoryManager: ItemDetailPanel tidak ditemukan.");
    }

    // Dipanggil oleh ItemSlot saat diklik
    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        // Slot kosong diklik -> kosongkan detail
        if (slot == null || slot.GetItem() == null)
        {
            selectedSlot = null;
            if (detailPanel != null) detailPanel.Clear();
            return;
        }

        selectedSlot = slot;
        selectedSlot.SetSelected(true);
        if (detailPanel != null) detailPanel.Show(slot.GetItem(), slot.GetQuantity());
    }

    // Supaya jumlah di panel detail ikut berubah saat slot yang dipilih bertambah
    private void RefreshSelectedDetail()
    {
        if (selectedSlot != null && detailPanel != null)
        {
            detailPanel.Show(selectedSlot.GetItem(), selectedSlot.GetQuantity());
        }
    }

    public bool AddItem(ItemDetails newItem, int quantity = 1)
    {
        bool berhasil = TambahKeSlot(newItem, quantity);
        if (berhasil) RefreshSelectedDetail();
        return berhasil;
    }

    private bool TambahKeSlot(ItemDetails newItem, int quantity)
    {
        if (newItem == null || quantity <= 0) return false;

        // 1. Tumpuk ke slot yang sudah berisi item yang sama (selama belum penuh)
        foreach (ItemSlot slot in slots)
        {
            if (slot.GetItem() == newItem && slot.GetQuantity() < newItem.maxStackSize)
            {
                int bisaMasuk = Mathf.Min(quantity, newItem.maxStackSize - slot.GetQuantity());
                slot.SetupSlot(newItem, slot.GetQuantity() + bisaMasuk);
                quantity -= bisaMasuk;
                if (quantity <= 0) return true;
            }
        }

        // 2. Sisanya masuk ke slot kosong
        foreach (ItemSlot slot in slots)
        {
            if (slot.GetItem() == null)
            {
                int bisaMasuk = Mathf.Min(quantity, newItem.maxStackSize);
                slot.SetupSlot(newItem, bisaMasuk);
                quantity -= bisaMasuk;
                if (quantity <= 0) return true;
            }
        }

        Debug.Log("Inventory Penuh!");
        return false;
    }
}