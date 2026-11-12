using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float kecepatan = 5f;

    [Tooltip("Objek yang diputar mengikuti arah jalan (senter). Badan karakter tidak diputar karena sudah pakai sprite 6 arah.")]
    [SerializeField] private Transform senter;

    [Header("Dash (tombol Left Shift / aksi Sprint)")]
    [SerializeField] private float kecepatanDash = 16f;
    [Tooltip("Lama dash (detik)")]
    [SerializeField] private float durasiDash = 0.18f;
    [Tooltip("Jeda sebelum bisa dash lagi (detik)")]
    [SerializeField] private float cooldownDash = 0.8f;
    [Tooltip("Player kebal damage selama dash")]
    [SerializeField] private bool kebalSaatDash = true;

    private Vector2 arahGerak;
    private Vector2 arahHadap = Vector2.down; // dipakai kalau dash saat diam
    private Rigidbody2D rb;

    private InputAction moveAction;
    private InputAction dashAction;

    private float sisaDash = 0f;
    private float sisaCooldown = 0f;
    private Vector2 arahDash;

    // Dibaca PlayerAnimator & PlayerHealth
    public bool SedangDash => sisaDash > 0f;
    public bool KebalSekarang => kebalSaatDash && SedangDash;
    // 0 = baru saja dash, 1 = siap dash lagi (bisa dipakai untuk UI)
    public float PersenCooldown => cooldownDash <= 0f ? 1f : 1f - Mathf.Clamp01(sisaCooldown / cooldownDash);

    void Awake()
    {
        // Pastikan player punya nyawa supaya bisa diserang enemy
        if (GetComponent<PlayerHealth>() == null) gameObject.AddComponent<PlayerHealth>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        moveAction = InputSystem.actions.FindAction("Move");
        dashAction = InputSystem.actions.FindAction("Sprint"); // Left Shift / L3 gamepad

        // Badan karakter tetap tegak
        transform.rotation = Quaternion.identity;
        rb.freezeRotation = true;

        // Cari senter otomatis kalau belum diisi
        if (senter == null)
        {
            Transform t = transform.Find("Spot Light 2D");
            if (t != null) senter = t;
        }
    }

    void Update()
    {
        BacaInput();
        CekDash();
        Move();
        RotateToDirection();
    }

    void BacaInput()
    {
        arahGerak = moveAction != null ? moveAction.ReadValue<Vector2>().normalized : Vector2.zero;
        if (arahGerak.sqrMagnitude > 0.01f) arahHadap = arahGerak;
    }

    void CekDash()
    {
        if (sisaCooldown > 0f) sisaCooldown -= Time.deltaTime;
        if (sisaDash > 0f) sisaDash -= Time.deltaTime;

        bool tekanDash = dashAction != null
            ? dashAction.WasPressedThisFrame()
            : Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;

        if (tekanDash && sisaCooldown <= 0f && !SedangDash)
        {
            // Dash ke arah gerak; kalau diam, ke arah hadap terakhir
            arahDash = arahGerak.sqrMagnitude > 0.01f ? arahGerak : arahHadap;
            sisaDash = durasiDash;
            sisaCooldown = durasiDash + cooldownDash;
        }
    }

    void Move()
    {
        // Pakai velocity Rigidbody supaya dash tetap tertahan dinding/pohon
        rb.linearVelocity = SedangDash ? arahDash * kecepatanDash : arahGerak * kecepatan;
    }

    void RotateToDirection()
    {
        Vector2 arah = SedangDash ? arahDash : arahGerak;
        if (arah.sqrMagnitude > 0.01f && senter != null)
        {
            // Senter menghadap ke atas secara default, jadi pakai .up
            senter.up = arah;
        }
    }

    void OnDisable()
    {
        // Saat player mati (script dimatikan), hentikan dash
        sisaDash = 0f;
    }

    // Pengambilan koin sekarang ditangani oleh GrabableItem (di objek koin),
    // supaya koin masuk ke inventory dan tidak dihancurkan dua kali.
}
