using System.Collections.Generic;
using UnityEngine;

// Tampilan enemy dari sprite sheet "Tiny Enemies" (16x16).
//
// Susunan setiap sheet:
//   - Baris paling atas = judul (IDLE / WALK / ATK / ...), dilewati
//   - Kolom 0-3 = Idle, kolom 4-7 = Walk
//   - Setiap varian warna = 4 baris berurutan: Bawah, Kanan, Atas, Kiri
//
// EnemySpawner memanggil Pakai(sheet) dengan sheet acak, lalu warna juga dipilih acak.
// Saat diam -> animasi Idle, saat bergerak -> animasi Walk.
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyLook : MonoBehaviour
{
    private const int Ukuran = 16;          // ukuran 1 frame (piksel)
    private const int BarisPerVarian = 4;   // Bawah, Kanan, Atas, Kiri
    private const int KolomIdle = 0;        // idle mulai kolom ke-0
    private const int KolomWalk = 4;        // walk mulai kolom ke-4
    private const int JumlahFrame = 4;

    [SerializeField] private float framePerDetikIdle = 5f;
    [SerializeField] private float framePerDetikWalk = 8f;
    [Tooltip("Berapa lama (detik) enemy harus diam sebelum ganti ke animasi Idle (mencegah kedip)")]
    [SerializeField] private float jedaKeIdle = 0.1f;

    private class SetAnimasi
    {
        public Sprite[][] idle; // [arah][frame]
        public Sprite[][] walk; // [arah][frame]
    }

    // Cache supaya sprite dari sheet + varian yang sama tidak dibuat berulang
    private static readonly Dictionary<(Texture2D, int), SetAnimasi> cache = new Dictionary<(Texture2D, int), SetAnimasi>();

    private SpriteRenderer sr;
    private SetAnimasi animasi;
    private Vector3 posisiSebelum;
    private int arah = 0;            // 0 bawah, 1 kanan, 2 atas, 3 kiri
    private float waktu;
    private float waktuDiam;
    private bool sedangJalan;

    public void Pakai(Texture2D sheet)
    {
        if (sheet == null) return;

        sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white; // hapus warna merah bawaan prefab lama

        int jumlahBaris = sheet.height / Ukuran;
        int jumlahVarian = Mathf.Max(1, (jumlahBaris - 1) / BarisPerVarian);
        int varian = Random.Range(0, jumlahVarian);

        animasi = AmbilAnimasi(sheet, varian);
        sr.sprite = animasi.idle[0][0];

        posisiSebelum = transform.position;
        waktu = Random.Range(0f, 1f); // supaya enemy tidak bergerak serempak
        waktuDiam = jedaKeIdle;
    }

    private static SetAnimasi AmbilAnimasi(Texture2D sheet, int varian)
    {
        if (cache.TryGetValue((sheet, varian), out SetAnimasi ada)) return ada;

        sheet.filterMode = FilterMode.Point; // pixel art tetap tajam

        SetAnimasi hasil = new SetAnimasi
        {
            idle = PotongFrames(sheet, varian, KolomIdle, "idle"),
            walk = PotongFrames(sheet, varian, KolomWalk, "walk"),
        };

        cache[(sheet, varian)] = hasil;
        return hasil;
    }

    private static Sprite[][] PotongFrames(Texture2D sheet, int varian, int kolomAwal, string label)
    {
        Sprite[][] hasil = new Sprite[BarisPerVarian][];
        for (int a = 0; a < BarisPerVarian; a++)
        {
            int barisDariAtas = 1 + varian * BarisPerVarian + a;     // +1 lewati baris judul
            float y = sheet.height - (barisDariAtas + 1) * Ukuran;  // koordinat tekstur dihitung dari bawah

            hasil[a] = new Sprite[JumlahFrame];
            for (int f = 0; f < JumlahFrame; f++)
            {
                Rect rect = new Rect((kolomAwal + f) * Ukuran, y, Ukuran, Ukuran);
                hasil[a][f] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), Ukuran);
                hasil[a][f].name = $"{sheet.name}_v{varian}_{label}_a{a}_f{f}";
            }
        }
        return hasil;
    }

    private void Update()
    {
        if (animasi == null) return;

        // Arah hadap & status jalan dari pergerakan (enemy bergerak lewat transform, bukan Rigidbody)
        Vector3 gerak = transform.position - posisiSebelum;
        posisiSebelum = transform.position;

        bool bergerak = gerak.sqrMagnitude > 0.0000001f;
        if (bergerak)
        {
            waktuDiam = 0f;
            if (Mathf.Abs(gerak.x) > Mathf.Abs(gerak.y))
                arah = gerak.x > 0 ? 1 : 3; // kanan / kiri
            else
                arah = gerak.y > 0 ? 2 : 0; // atas / bawah
        }
        else
        {
            waktuDiam += Time.deltaTime;
        }

        bool jalanSekarang = waktuDiam < jedaKeIdle;
        if (jalanSekarang != sedangJalan)
        {
            sedangJalan = jalanSekarang;
            waktu = 0f; // mulai animasi baru dari frame pertama
        }

        waktu += Time.deltaTime;
        Sprite[] frames = sedangJalan ? animasi.walk[arah] : animasi.idle[arah];
        float fps = sedangJalan ? framePerDetikWalk : framePerDetikIdle;
        sr.sprite = frames[(int)(waktu * fps) % frames.Length];
    }
}
