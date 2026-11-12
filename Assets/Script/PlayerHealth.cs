using UnityEngine;
using System;

// Nyawa player + health bar di kiri atas layar.
// Pasang di objek Player. Enemy memanggil KenaDamage() lewat interface IDamageable.
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    private int health;

    [Header("Health Bar (kiri atas)")]
    [SerializeField] private Vector2 posisiBar = new Vector2(16, 16);
    [SerializeField] private Vector2 ukuranBar = new Vector2(240, 24);
    [SerializeField] private Color warnaPenuh = new Color(0.2f, 0.85f, 0.3f);
    [SerializeField] private Color warnaKritis = new Color(0.9f, 0.15f, 0.15f);

    // Dikirim saat health berubah: (health sekarang, health maksimal)
    public event Action<int, int> OnHealthBerubah;
    public static event Action OnPlayerMati;

    public int Health => health;
    public int MaxHealth => maxHealth;
    public bool SudahMati => health <= 0;

    private void Awake()
    {
        health = maxHealth;
    }

    public void KenaDamage(int jumlah)
    {
        if (SudahMati || jumlah <= 0) return;

        // Kebal selama dash
        PlayerMovement gerak = GetComponent<PlayerMovement>();
        if (gerak != null && gerak.KebalSekarang) return;

        health = Mathf.Max(health - jumlah, 0);
        Debug.Log("Player kena damage: " + jumlah + ", HP sekarang: " + health);
        OnHealthBerubah?.Invoke(health, maxHealth);

        if (health == 0) Mati();
    }

    public void Sembuh(int jumlah)
    {
        if (SudahMati || jumlah <= 0) return;

        health = Mathf.Min(health + jumlah, maxHealth);
        OnHealthBerubah?.Invoke(health, maxHealth);
    }

    private void Mati()
    {
        Debug.Log("PLAYER MATI - GAME OVER");
        waktuMati = Time.unscaledTime;

        // Player tidak bisa digerakkan lagi
        PlayerMovement gerak = GetComponent<PlayerMovement>();
        if (gerak != null) gerak.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        OnPlayerMati?.Invoke(); // PlayerAnimator memutar animasi Death
        Time.timeScale = 0f;    // hentikan enemy & spawner (animasi Death tetap jalan)
    }

    [Tooltip("Jeda (detik) sebelum tulisan GAME OVER muncul, supaya animasi Death terlihat dulu")]
    [SerializeField] private float jedaGameOver = 1f;
    private float waktuMati;

    private void OnDestroy()
    {
        Time.timeScale = 1f; // kembalikan saat scene ditutup / restart
    }

    private void OnGUI()
    {
        float persen = (float)health / maxHealth;
        Rect bar = new Rect(posisiBar, ukuranBar);

        // Bingkai hitam
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), Texture2D.whiteTexture);

        // Latar abu-abu gelap (bagian health yang hilang)
        GUI.color = new Color(0.25f, 0.25f, 0.25f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);

        // Isi health: hijau saat penuh, makin merah saat menipis
        GUI.color = Color.Lerp(warnaKritis, warnaPenuh, persen);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * persen, bar.height), Texture2D.whiteTexture);

        // Tulisan angka di tengah bar
        GUI.color = Color.white;
        GUIStyle gaya = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(bar, "HP " + health + " / " + maxHealth, gaya);

        if (SudahMati && Time.unscaledTime - waktuMati >= jedaGameOver)
        {
            GUIStyle gayaGameOver = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 48,
                fontStyle = FontStyle.Bold
            };
            GUI.color = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER", gayaGameOver);
            GUI.color = Color.white;
        }
    }
}
