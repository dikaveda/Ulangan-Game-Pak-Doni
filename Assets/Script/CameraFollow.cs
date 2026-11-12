using UnityEngine;

// Kamera mengikuti player dengan halus, dan tidak keluar dari area peta.
// Pasang di Main Camera.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Kosongkan = otomatis cari objek bertag Player")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Gerakan")]
    [Tooltip("Makin kecil makin cepat menempel ke player (0 = langsung)")]
    [SerializeField] private float kehalusan = 0.15f;

    [Header("Zoom")]
    [Tooltip("Orthographic Size: makin kecil makin dekat (zoom in)")]
    [SerializeField] private float ukuranKamera = 3.5f;

    [Header("Batas Peta")]
    [Tooltip("Renderer / Tilemap Renderer yang jadi batas peta. Kosongkan = tanpa batas.")]
    [SerializeField] private Renderer areaBatas;

    private Camera cam;
    private Vector3 kecepatan; // dipakai SmoothDamp

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = ukuranKamera;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        // Langsung lompat ke posisi player di awal game (tanpa meluncur)
        if (target != null) transform.position = PosisiTujuan();
    }

    // LateUpdate: dijalankan setelah player bergerak, supaya kamera tidak bergetar
    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.SmoothDamp(transform.position, PosisiTujuan(), ref kecepatan, kehalusan);
    }

    private Vector3 PosisiTujuan()
    {
        Vector3 tujuan = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
        return areaBatas != null ? KunciDalamBatas(tujuan) : tujuan;
    }

    private Vector3 KunciDalamBatas(Vector3 pos)
    {
        Bounds batas = areaBatas.bounds;
        float setengahTinggi = cam.orthographicSize;
        float setengahLebar = setengahTinggi * cam.aspect;

        // Kalau peta lebih kecil dari layar, kamera diam di tengah peta
        pos.x = batas.size.x <= setengahLebar * 2
            ? batas.center.x
            : Mathf.Clamp(pos.x, batas.min.x + setengahLebar, batas.max.x - setengahLebar);

        pos.y = batas.size.y <= setengahTinggi * 2
            ? batas.center.y
            : Mathf.Clamp(pos.y, batas.min.y + setengahTinggi, batas.max.y - setengahTinggi);

        return pos;
    }

    // Kalau Orthographic Size diubah di Inspector saat Play, langsung diterapkan
    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null && ukuranKamera > 0.1f) cam.orthographicSize = ukuranKamera;
    }
}
