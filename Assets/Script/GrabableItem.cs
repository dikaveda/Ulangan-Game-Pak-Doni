using UnityEngine;

// Nama class HARUS sama dengan nama file (GrabableItem.cs) supaya Unity bisa menjalankan komponen ini.
public class GrabableItem : MonoBehaviour
{
    [SerializeField] private ItemDetails coinItemDetails; // Masukkan asset Coin (ScriptableObject) di sini
    [SerializeField] private int coinAmount = 1;          // Jumlah koin yang didapat

    private bool sudahDiambil = false;

    // Dipakai saat koin dibuat lewat kode (CoinRandomizer)
    public void Setup(ItemDetails item, int jumlah)
    {
        coinItemDetails = item;
        coinAmount = jumlah;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (sudahDiambil || !collision.CompareTag("Player")) return;

        if (coinItemDetails == null)
        {
            Debug.LogWarning($"{name}: 'Coin Item Details' belum diisi di Inspector!", this);
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager tidak ada di scene!");
            return;
        }

        // Masukkan ke inventory
        if (InventoryManager.Instance.AddItem(coinItemDetails, coinAmount))
        {
            // Update hitungan koin di GameManager
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null) gameManager.AmbilKoin();

            // Respawn: pindahkan koin ke tempat acak lain, bukan dihancurkan
            if (CompareTag("Coin") && CoinRandomizer.Instance != null && CoinRandomizer.Instance.RespawnAktif)
            {
                CoinRandomizer.Instance.Respawn(transform);
                return;
            }

            sudahDiambil = true;
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory penuh, koin tidak bisa diambil!");
        }
    }
}
