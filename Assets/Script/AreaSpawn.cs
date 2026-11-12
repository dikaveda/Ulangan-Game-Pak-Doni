using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Pemeriksa posisi spawn yang dipakai bersama oleh EnemySpawner dan CoinRandomizer.
//
// Posisi dianggap VALID kalau:
//   1. Ada tile lantai di bawahnya (tilemap "lantai" atau "lantai2")
//   2. Berada DI DALAM batas dinding (kotak yang dibentuk tilemap "Dinding")
//   3. Bukan di sel dinding / pohon / tanaman
//   4. Tidak menempel collider padat (non-trigger)
//
// Tilemap dicari otomatis berdasarkan nama. Ubah daftar di bawah kalau nama tilemap berbeda.
public static class AreaSpawn
{
    private static readonly string[] NamaLantai = { "lantai", "lantai2" };
    private static readonly string NamaDinding = "Dinding";
    private static readonly string[] NamaPenghalang = { "Dinding", "pohon", "Tanaman" };

    private static List<Tilemap> lantai;
    private static List<Tilemap> penghalang;
    private static Tilemap dinding;
    private static Bounds batasDalamDinding;
    private static bool punyaBatasDinding;
    private static bool sudahSiap;

    // Panggil ulang otomatis saat scene dimuat ulang
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => sudahSiap = false;

    private static void Siapkan()
    {
        // Sudah siap dan tilemap masih ada (belum ganti scene) -> pakai cache
        if (sudahSiap && (lantai.Count == 0 || lantai[0] != null)) return;

        lantai = new List<Tilemap>();
        penghalang = new List<Tilemap>();
        dinding = null;

        foreach (Tilemap t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(NamaLantai, t.name) >= 0) lantai.Add(t);
            if (System.Array.IndexOf(NamaPenghalang, t.name) >= 0) penghalang.Add(t);
            if (t.name == NamaDinding) dinding = t;
        }

        punyaBatasDinding = dinding != null && HitungBatasDalam(dinding, out batasDalamDinding);
        sudahSiap = true;

        if (lantai.Count == 0) Debug.LogWarning("AreaSpawn: tilemap lantai tidak ditemukan.");
        if (!punyaBatasDinding) Debug.LogWarning("AreaSpawn: tilemap Dinding tidak ditemukan / kosong, batas dinding tidak dipakai.");
    }

    // Kotak di dalam dinding = area tile dinding terluar, dikurangi 1 sel di tiap sisi
    private static bool HitungBatasDalam(Tilemap t, out Bounds hasil)
    {
        hasil = new Bounds();
        bool ada = false;
        Vector3Int min = Vector3Int.zero, max = Vector3Int.zero;

        foreach (Vector3Int sel in t.cellBounds.allPositionsWithin)
        {
            if (!t.HasTile(sel)) continue;
            if (!ada) { min = max = sel; ada = true; }
            min = Vector3Int.Min(min, sel);
            max = Vector3Int.Max(max, sel);
        }
        if (!ada) return false;

        Vector3 dunia1 = t.CellToWorld(min + new Vector3Int(1, 1, 0));
        Vector3 dunia2 = t.CellToWorld(max); // pojok kiri-bawah sel dinding terakhir = tepi dalam
        hasil.SetMinMax(Vector3.Min(dunia1, dunia2), Vector3.Max(dunia1, dunia2));
        return true;
    }

    // Area untuk mengambil titik acak (dalam dinding, atau gabungan lantai kalau tanpa dinding)
    public static bool AmbilTitikAcak(out Vector2 titik)
    {
        Siapkan();

        Bounds b;
        if (punyaBatasDinding) b = batasDalamDinding;
        else if (lantai.Count > 0) b = lantai[0].localBounds.Translate(lantai[0].transform.position);
        else { titik = Vector2.zero; return false; }

        titik = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
        return true;
    }

    public static bool PosisiValid(Vector2 pos, float radiusCek)
    {
        Siapkan();

        // 1. Di atas lantai
        if (lantai.Count > 0)
        {
            bool diLantai = false;
            foreach (Tilemap t in lantai)
            {
                if (t.HasTile(t.WorldToCell(pos))) { diLantai = true; break; }
            }
            if (!diLantai) return false;
        }

        // 2. Di dalam dinding
        if (punyaBatasDinding)
        {
            Vector3 p = new Vector3(pos.x, pos.y, batasDalamDinding.center.z);
            if (!batasDalamDinding.Contains(p)) return false;
        }

        // 3. Bukan sel dinding / pohon / tanaman
        foreach (Tilemap t in penghalang)
        {
            if (t.HasTile(t.WorldToCell(pos))) return false;
        }

        // 4. Tidak menempel collider padat
        foreach (Collider2D c in Physics2D.OverlapCircleAll(pos, radiusCek))
        {
            if (!c.isTrigger) return false;
        }

        return true;
    }

    // true kalau tidak ada koin (tag "Coin") dalam jarak tertentu.
    // "kecuali" = koin yang sedang dipindah (diabaikan).
    public static bool JauhDariKoin(Vector2 pos, float jarakMin, Transform kecuali = null)
    {
        if (jarakMin <= 0f) return true;

        foreach (GameObject k in GameObject.FindGameObjectsWithTag("Coin"))
        {
            if (k.transform == kecuali) continue;
            if (Vector2.Distance(pos, k.transform.position) < jarakMin) return false;
        }
        return true;
    }

    // true kalau tidak ada enemy (yang aktif) dalam jarak tertentu
    public static bool JauhDariEnemy(Vector2 pos, float jarakMin)
    {
        if (jarakMin <= 0f) return true;

        foreach (Enemy e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (Vector2.Distance(pos, e.transform.position) < jarakMin) return false;
        }
        return true;
    }
}

static class BoundsExtensions
{
    public static Bounds Translate(this Bounds b, Vector3 geser)
    {
        b.center += geser;
        return b;
    }
}
