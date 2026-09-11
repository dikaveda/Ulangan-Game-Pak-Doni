using UnityEngine;
using UnityEngine.InputSystem;   // WAJIB untuk Input System

public class PlayerMovement : MonoBehaviour
{
    private int hp = 100;
    public float kecepatan = 5f;
    public int skor = 0;
    public GameManager gameManager;

    private Vector2 arahGerak;   // nilai dari action "Move"

    // Dipanggil OTOMATIS oleh komponen Player Input
    // saat action "Move" pada asset InputSystem_Actions aktif.
    // Nama method WAJIB: On + nama action -> OnMove
    void OnMove(InputValue value)
    {
        // Ambil nilai Vector2 dari input
        arahGerak = value.Get<Vector2>();
    }

    void Update()
    {
        // Gerakkan objek
        Vector3 arah = new Vector3(arahGerak.x, arahGerak.y, 0);
        transform.position += arah * kecepatan * Time.deltaTime;
    }

    // Dipanggil otomatis saat Player menyentuh objek ber-Trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        // Cek apakah yang disentuh punya tag "Coin"
        if (other.CompareTag("Coin"))
        {
            // Hancurkan koin yang tersentuh
            Destroy(other.gameObject);

            // Tambah skor
            skor++;

            // Tampilkan skor ke Console
            Debug.Log("Skor: " + skor);

            gameManager.AmbilKoin();  // Panggil method AmbilKoin() di GameManager
        }
    }
    public void KenaDamage(int jumlahDamage)
    {
        hp -= jumlahDamage;
        Debug.Log("Player terkena damage! Sisa HP: " + hp);
    }
}