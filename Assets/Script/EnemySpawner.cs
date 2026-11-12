using System.Collections.Generic;
using UnityEngine;

// Memunculkan enemy secara berkala di titik acak di seluruh peta (di dalam dinding),
// tidak terlalu dekat player, dan tidak di dalam dinding/pohon.
//
// Cara pakai:
// 1. Buat GameObject kosong bernama "EnemySpawner", pasang script ini.
// 2. Isi "Enemy Prefab": drag prefab enemy (atau objek Circle dari Hierarchy).
// 3. (Opsional) Isi "Area Batas" dengan latar / Tilemap lantai supaya enemy tidak muncul di luar peta.
// 4. (Opsional) Isi "Titik Spawn" kalau ingin enemy muncul di tempat tertentu saja.
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy")]
    [Tooltip("Prefab enemy. Bisa juga objek enemy di scene (misalnya Circle) sebagai cetakan.")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("Sprite sheet enemy (Tiny Enemies 16x16). Setiap spawn memilih sheet + warna acak. Kosongkan = pakai tampilan prefab.")]
    [SerializeField] private Texture2D[] tampilanEnemy;
    [Tooltip("Jumlah maksimal enemy hidup bersamaan")]
    [SerializeField] private int maksEnemyHidup = 8;

    [Header("Waktu")]
    [SerializeField] private float jedaAwal = 2f;        // tunggu sebelum spawn pertama
    [SerializeField] private float jedaSpawn = 4f;       // jeda antar spawn di awal
    [SerializeField] private float jedaMinimal = 1.2f;   // jeda paling cepat
    [Tooltip("Berapa detik jeda berkurang setiap 1 detik permainan (makin lama makin sulit). 0 = tetap.")]
    [SerializeField] private float percepatanPerDetik = 0.02f;

    [Header("Posisi Spawn")]
    [Tooltip("Kalau diisi, enemy hanya muncul di salah satu titik ini (yang tidak terlalu dekat player).")]
    [SerializeField] private Transform[] titikSpawn;
    [Tooltip("Enemy muncul di mana saja di dalam peta, tapi tidak lebih dekat dari jarak ini ke player")]
    [SerializeField] private float jarakMinDariPlayer = 5f;
    [Tooltip("Enemy tidak muncul sedekat ini dengan koin")]
    [SerializeField] private float jarakMinDariKoin = 3f;
    [Tooltip("Enemy tidak muncul sedekat ini dengan enemy lain")]
    [SerializeField] private float jarakMinAntarEnemy = 3f;
    [Tooltip("Renderer / Tilemap Renderer sebagai batas peta. Kosongkan = tanpa batas.")]
    [SerializeField] private Renderer areaBatas;
    [Tooltip("Radius cek supaya enemy tidak muncul di dalam dinding/pohon")]
    [SerializeField] private float radiusCekTabrakan = 0.4f;

    private readonly List<GameObject> enemyHidup = new List<GameObject>();
    private Transform player;
    private float timer;
    private float jedaSekarang;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (enemyPrefab == null)
            Debug.LogWarning("EnemySpawner: 'Enemy Prefab' belum diisi!", this);

        // Kalau cetakannya objek di scene, sembunyikan supaya tidak ikut bertarung
        // (skip kalau itu prefab dari folder Project)
        if (enemyPrefab != null && enemyPrefab.scene.IsValid())
            enemyPrefab.SetActive(false);

        jedaSekarang = jedaSpawn;
        timer = jedaAwal;
    }

    private void Update()
    {
        if (enemyPrefab == null || player == null) return;

        // Buang enemy yang sudah hancur (mati / menabrak player) dari daftar
        enemyHidup.RemoveAll(e => e == null);

        // Makin lama makin cepat
        jedaSekarang = Mathf.Max(jedaMinimal, jedaSekarang - percepatanPerDetik * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = jedaSekarang;

        if (enemyHidup.Count >= maksEnemyHidup) return;

        if (CariPosisiSpawn(out Vector2 posisi))
        {
            GameObject enemy = Instantiate(enemyPrefab, posisi, Quaternion.identity);
            enemy.SetActive(true);
            enemy.name = enemyPrefab.name + " (spawn)";

            // Karakter acak
            if (tampilanEnemy != null && tampilanEnemy.Length > 0)
            {
                Texture2D sheet = tampilanEnemy[Random.Range(0, tampilanEnemy.Length)];
                EnemyLook look = enemy.GetComponent<EnemyLook>();
                if (look == null) look = enemy.AddComponent<EnemyLook>();
                look.Pakai(sheet);
                enemy.name = sheet.name.Replace("_16x16", "") + " (spawn)";
            }
            enemyHidup.Add(enemy);
        }
    }

    private bool CariPosisiSpawn(out Vector2 hasil)
    {
        // Coba beberapa kali sampai ketemu posisi yang valid
        for (int percobaan = 0; percobaan < 40; percobaan++)
        {
            if (!AmbilKandidat(out Vector2 kandidat)) break;
            if (PosisiValid(kandidat))
            {
                hasil = kandidat;
                return true;
            }
        }

        hasil = Vector2.zero;
        return false;
    }

    private bool AmbilKandidat(out Vector2 kandidat)
    {
        if (titikSpawn != null && titikSpawn.Length > 0)
        {
            Transform t = titikSpawn[Random.Range(0, titikSpawn.Length)];
            kandidat = t != null ? (Vector2)t.position : (Vector2)player.position;
            return true;
        }

        // Titik acak di seluruh peta (di dalam dinding)
        return AreaSpawn.AmbilTitikAcak(out kandidat);
    }

    private bool PosisiValid(Vector2 pos)
    {
        // Tidak terlalu dekat player
        if (Vector2.Distance(pos, player.position) < jarakMinDariPlayer) return false;

        // Di dalam peta
        if (areaBatas != null && !areaBatas.bounds.Contains(new Vector3(pos.x, pos.y, areaBatas.bounds.center.z)))
            return false;

        // Tidak berdekatan dengan koin dan enemy lain
        if (!AreaSpawn.JauhDariKoin(pos, jarakMinDariKoin)) return false;
        if (!AreaSpawn.JauhDariEnemy(pos, jarakMinAntarEnemy)) return false;

        // Di atas lantai, di dalam dinding, tidak di dinding/pohon/collider padat
        return AreaSpawn.PosisiValid(pos, radiusCekTabrakan);
    }

    // Gambar jarak aman di Scene view (lingkaran kuning: enemy tidak muncul di dalamnya)
    private void OnDrawGizmosSelected()
    {
        Transform pusat = player;
        if (pusat == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) pusat = p.transform;
        }
        if (pusat == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pusat.position, jarakMinDariPlayer);
    }
}
