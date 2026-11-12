 using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int totalKoin;
    private int koinTerkumpul = 0;
    private int jumlahZombieMati = 0;

    void Start()
    {
        totalKoin = GameObject.FindGameObjectsWithTag("Coin").Length;
    }

    void SaatZombieMati(Enemy zombie)
    {
        jumlahZombieMati++;
        Debug.Log("GameManager dengar event. Zombie mati: " + jumlahZombieMati + " (" + zombie.name + ")");
    }

    public void AmbilKoin()
    {
        koinTerkumpul++;

        // Mode respawn terus: tidak ada kondisi menang, skor terus bertambah
        if (ModeRespawn()) return;

        if (koinTerkumpul == totalKoin)
        {
            Menang();
        }
    }

    void Menang()
    {
        Debug.Log("KAMU MENANG!");
    }
    private void OnEnable()
    {
        // Mendaftar sebagai penerima event
        Enemy.OnZombieMati += SaatZombieMati;
    }

    private void OnDisable()
    {
        // Melepas pendaftaran saat object dinonaktifkan/hancur agar tidak memicu memory leak
        Enemy.OnZombieMati -= SaatZombieMati;
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        // Baris paling atas dipakai health bar (PlayerHealth), jadi teks digeser ke bawah
        string teks = ModeRespawn() ? "Koin: " + koinTerkumpul : "Koin: " + koinTerkumpul + " / " + totalKoin;
        GUI.Label(new Rect(16, 50, 480, 36), teks);
    }

    bool ModeRespawn()
    {
        return CoinRandomizer.Instance != null && CoinRandomizer.Instance.RespawnAktif;
    }
}
