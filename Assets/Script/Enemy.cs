using UnityEngine;
using System;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 100;
    public float ms = 2f;

    protected Transform player;

    [Header("State Machine")]
    [SerializeField] private float JarakDeteksi = 6f;
    [SerializeField] private float JarakSerang = 1.5f;
    [SerializeField] private float JedaSerang = 1f;

    // State Now
    private StateZombie state = StateZombie.IDLE;
    private float waktuSerangTerakhir;

    // Action<Enemy> = delegate siap pakai, parameter-nya adalah zombie yang mati.
    public static event Action<Enemy> OnZombieMati;

    [SerializeField] private int damageSaatTabrakan = 10;
    [Tooltip("Kalau jarak ke player sekecil ini, enemy dianggap menabrak (cadangan kalau collider tidak memicu event).")]
    [SerializeField] private float jarakTabrak = 0.6f;

    private bool sudahMenabrak = false;

    [Header("Patrol (saat player jauh)")]
    [Tooltip("Kecepatan patrol dibanding kecepatan mengejar (0.5 = setengahnya)")]
    [SerializeField] private float faktorKecepatanPatrol = 0.4f;
    [Tooltip("Seberapa jauh titik patrol berikutnya dari posisi enemy")]
    [SerializeField] private float radiusPatrol = 4f;
    [Tooltip("Lama diam (detik) setelah sampai di titik patrol: acak antara min dan maks")]
    [SerializeField] private Vector2 waktuDiamPatrol = new Vector2(1f, 3f);
    [Tooltip("Kalau titik patrol tidak tercapai dalam waktu ini, cari titik baru (misal terhalang)")]
    [SerializeField] private float batasWaktuKeTitik = 6f;

    private Vector2 titikPatrol;
    private bool punyaTitikPatrol = false;
    private float sisaDiam = 0f;
    private float waktuMenujuTitik = 0f;

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        // Sampai di player -> serang sekali lalu hilang
        if (player != null && JarakKePlayer() <= jarakTabrak)
        {
            TabrakPlayer(player.gameObject);
            return;
        }

        PeriksaTransisi();

        switch (state)
        {
            case StateZombie.IDLE: PerilakuIdle(); break;
            case StateZombie.PATROL: PerilakuPatrol(); break;
            case StateZombie.CHASE: PerilakuChase(); break;
            case StateZombie.ATTACK: PerilakuAttack(); break;
        }
    }

    public float JarakKePlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, player.position);
    }

    void PeriksaTransisi()
    {
        float jarak = JarakKePlayer();

        if (jarak <= JarakSerang)
        {
            state = StateZombie.ATTACK;
        }
        else if (jarak <= JarakDeteksi)
        {
            state = StateZombie.CHASE;
        }
        else
        {
            state = StateZombie.PATROL;
        }
    }

    public void Kejar()
    {
        if (player == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            ms * Time.deltaTime
        );
    }

    public virtual void Serang()
    {
        Debug.Log("Enemy Serang");
    }

    // Dipanggil otomatis oleh Unity saat collider enemy menabrak collider lain.
    // Karena ada di class induk, SEMUA turunan zombie ikut punya perilaku ini.
    // Catatan: jangan pakai Debug.Log di sini. Fungsi-fungsi ini dipanggil SETIAP FRAME
    // untuk SETIAP enemy, sehingga log-nya membuat Editor.log membengkak sampai GB.
    void PerilakuIdle()
    {
    }

    // Berkeliling pelan: jalan ke titik acak di dekatnya, diam sebentar, lalu cari titik baru.
    void PerilakuPatrol()
    {
        // Sedang istirahat di titik patrol (EnemyLook otomatis memutar animasi Idle)
        if (sisaDiam > 0f)
        {
            sisaDiam -= Time.deltaTime;
            return;
        }

        if (!punyaTitikPatrol && !CariTitikPatrol()) return;

        Vector2 posisi = transform.position;
        Vector2 berikutnya = Vector2.MoveTowards(posisi, titikPatrol, ms * faktorKecepatanPatrol * Time.deltaTime);

        waktuMenujuTitik += Time.deltaTime;
        bool sampai = Vector2.Distance(berikutnya, titikPatrol) < 0.05f;
        bool terlaluLama = waktuMenujuTitik > batasWaktuKeTitik;

        if (!AdaPenghalang(posisi, berikutnya))
            transform.position = berikutnya;
        else
            terlaluLama = true; // menabrak dinding/pohon -> ganti tujuan

        if (sampai || terlaluLama)
        {
            punyaTitikPatrol = false;
            sisaDiam = UnityEngine.Random.Range(waktuDiamPatrol.x, waktuDiamPatrol.y);
        }
    }

    bool CariTitikPatrol()
    {
        Vector2 asal = transform.position;

        for (int i = 0; i < 15; i++)
        {
            Vector2 kandidat = asal + UnityEngine.Random.insideUnitCircle * radiusPatrol;

            // Harus di lantai, di dalam dinding, dan jalannya tidak terhalang
            if (AreaSpawn.PosisiValid(kandidat, 0.3f) && !AdaPenghalang(asal, kandidat))
            {
                titikPatrol = kandidat;
                punyaTitikPatrol = true;
                waktuMenujuTitik = 0f;
                return true;
            }
        }

        // Tidak ketemu: diam sebentar lalu coba lagi
        sisaDiam = 1f;
        return false;
    }

    // Cek apakah ada collider padat (dinding, pohon) di garis antara dua titik
    static bool AdaPenghalang(Vector2 dari, Vector2 ke)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(dari, ke))
        {
            if (hit.collider != null && !hit.collider.isTrigger) return true;
        }
        return false;
    }

    void PerilakuChase()
    {
        Kejar();
    }

    void PerilakuAttack()
    {
        // Tetap maju sampai benar-benar menabrak player (lihat TabrakPlayer)
        Kejar();
    }

    public void KenaDamage(int damage)
    {
        hp -= damage;
        Debug.Log("Enemy kena damage: " + damage + ", HP sekarang: " + hp);

        if (hp <= 0)
        {
            Mati();
        }
    }

    protected virtual void Mati()
    {
        Debug.Log("Enemy mati!");
        OnZombieMati?.Invoke(this);
        Destroy(gameObject);
    }

    // Enemy menyentuh player: kurangi health player, lalu enemy langsung hilang.
    // Tidak dihitung "zombie mati" karena bukan dibunuh player.
    void TabrakPlayer(GameObject target)
    {
        if (sudahMenabrak || !target.CompareTag("Player")) return;
        sudahMenabrak = true;

        IDamageable playerScript = target.GetComponentInParent<IDamageable>();
        if (playerScript != null)
        {
            playerScript.KenaDamage(damageSaatTabrakan);
        }

        Destroy(gameObject);
    }

    // Collider dengan "Is Trigger" dicentang
    void OnTriggerEnter2D(Collider2D other)
    {
        TabrakPlayer(other.gameObject);
    }

    // Collider biasa (tidak trigger)
    void OnCollisionEnter2D(Collision2D collision)
    {
        TabrakPlayer(collision.gameObject);
    }
}
