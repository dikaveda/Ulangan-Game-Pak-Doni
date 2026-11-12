using System.Collections.Generic;
using UnityEngine;

// Menyebar semua koin (tag "Coin") ke posisi acak setiap kali game dimulai,
// dan memindahkan koin ke tempat acak baru setiap kali diambil (respawn terus).
//
// Posisi valid (lihat AreaSpawn): di atas lantai, di dalam dinding,
// tidak di dinding/pohon, tidak terlalu dekat player, tidak berdempetan dengan koin lain.
//
// Pasang di objek GameManager.
public class CoinRandomizer : MonoBehaviour
{
    public static CoinRandomizer Instance { get; private set; }

    [Header("Respawn")]
    [Tooltip("Koin yang diambil langsung muncul lagi di tempat lain")]
    [SerializeField] private bool respawnSaatDiambil = true;

    [Header("Aturan Jarak")]
    [SerializeField] private float jarakMinDariPlayer = 3f;
    [SerializeField] private float jarakMinAntarKoin = 2.5f;
    [Tooltip("Koin tidak muncul sedekat ini dengan enemy")]
    [SerializeField] private float jarakMinDariEnemy = 3f;
    [Tooltip("Radius cek supaya koin tidak menempel di collider dinding/pohon")]
    [SerializeField] private float radiusCekTabrakan = 0.4f;

    [System.Serializable]
    public class JenisKoin
    {
        public string nama = "Gold";
        public ItemDetails item;        // data item untuk inventory (Coin / Coin2)
        public Sprite[] frames;         // animasi koin berputar
        public int jumlahDiPeta = 3;    // berapa koin jenis ini ada di peta sekaligus
        public int nilai = 1;           // jumlah yang masuk inventory saat diambil
    }

    [Header("Jenis Koin (semua jenis muncul bersamaan)")]
    [Tooltip("Kalau diisi, koin dibuat otomatis dari daftar ini dan koin lama di scene diganti.")]
    [SerializeField] private JenisKoin[] jenisKoin;
    [SerializeField] private float ukuranKoin = 1f;
    [SerializeField] private float radiusAmbil = 0.35f;
    [SerializeField] private float framePerDetik = 10f;
    [Tooltip("Kosongkan = pakai material koin lama di scene (Sprite-Lit-Default)")]
    [SerializeField] private Material materialKoin;

    public bool RespawnAktif => respawnSaatDiambil;

    private Transform player;

    private void Awake()
    {
        Instance = this;
        BuatKoinDariDaftar();
    }

    private void BuatKoinDariDaftar()
    {
        if (jenisKoin == null || jenisKoin.Length == 0) return;

        // Sembunyikan + hapus koin lama yang ditaruh manual di scene
        foreach (GameObject lama in GameObject.FindGameObjectsWithTag("Coin"))
        {
            // Pakai material koin lama (Sprite-Lit) supaya koin baru ikut gelap/terang kena senter
            SpriteRenderer srLama = lama.GetComponent<SpriteRenderer>();
            if (materialKoin == null && srLama != null) materialKoin = srLama.sharedMaterial;

            lama.SetActive(false);
            Destroy(lama);
        }

        foreach (JenisKoin jenis in jenisKoin)
        {
            for (int i = 0; i < jenis.jumlahDiPeta; i++)
            {
                BuatKoin(jenis, i);
            }
        }
    }

    private void BuatKoin(JenisKoin jenis, int nomor)
    {
        GameObject koin = new GameObject($"Koin {jenis.nama} ({nomor + 1})");
        koin.tag = "Coin";
        koin.transform.SetParent(transform, false);
        koin.transform.localScale = Vector3.one * ukuranKoin;

        SpriteRenderer sr = koin.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;
        if (materialKoin != null) sr.sharedMaterial = materialKoin;
        if (jenis.frames != null && jenis.frames.Length > 0) sr.sprite = jenis.frames[0];

        SpriteLoop anim = koin.AddComponent<SpriteLoop>();
        anim.frames = jenis.frames;
        anim.framePerDetik = framePerDetik;

        CircleCollider2D col = koin.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radiusAmbil;

        koin.AddComponent<GrabableItem>().Setup(jenis.item, jenis.nilai);
    }

    private void Start()
    {
        Physics2D.SyncTransforms();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        foreach (GameObject k in GameObject.FindGameObjectsWithTag("Coin"))
        {
            PindahkanKeTempatAcak(k.transform);
        }
    }

    // Dipanggil GrabableItem saat koin diambil
    public void Respawn(Transform koin)
    {
        PindahkanKeTempatAcak(koin);
    }

    private void PindahkanKeTempatAcak(Transform koin)
    {
        // Posisi koin lain (selain koin ini) supaya tidak berdempetan
        List<Vector2> koinLain = new List<Vector2>();
        foreach (GameObject k in GameObject.FindGameObjectsWithTag("Coin"))
        {
            if (k.transform != koin) koinLain.Add(k.transform.position);
        }

        for (int percobaan = 0; percobaan < 200; percobaan++)
        {
            if (!AreaSpawn.AmbilTitikAcak(out Vector2 kandidat)) break;

            // Makin banyak percobaan gagal, aturan jarak antar koin dilonggarkan
            float jarakAntar = percobaan < 150 ? jarakMinAntarKoin : jarakMinAntarKoin * 0.5f;

            if (PosisiValid(kandidat, koinLain, jarakAntar))
            {
                koin.position = new Vector3(kandidat.x, kandidat.y, koin.position.z);
                return;
            }
        }

        Debug.LogWarning($"CoinRandomizer: tidak menemukan tempat untuk {koin.name}, posisinya tidak diubah.");
    }

    private bool PosisiValid(Vector2 pos, List<Vector2> koinLain, float jarakAntar)
    {
        if (!AreaSpawn.PosisiValid(pos, radiusCekTabrakan)) return false;

        if (player != null && Vector2.Distance(pos, player.position) < jarakMinDariPlayer) return false;

        // Tidak berdekatan dengan enemy (dilonggarkan bersama jarak antar koin kalau sulit)
        float faktor = jarakMinAntarKoin > 0f ? jarakAntar / jarakMinAntarKoin : 1f;
        if (!AreaSpawn.JauhDariEnemy(pos, jarakMinDariEnemy * faktor)) return false;

        foreach (Vector2 p in koinLain)
        {
            if (Vector2.Distance(pos, p) < jarakAntar) return false;
        }

        return true;
    }
}
