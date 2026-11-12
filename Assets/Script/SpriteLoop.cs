using UnityEngine;

// Animasi sederhana: mengganti sprite secara berulang (dipakai untuk koin berputar).
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    public Sprite[] frames;
    public float framePerDetik = 10f;

    private SpriteRenderer sr;
    private float waktu;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Mulai di frame acak supaya koin tidak berputar serempak
        waktu = Random.Range(0f, 1f);
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        waktu += Time.deltaTime;
        sr.sprite = frames[(int)(waktu * framePerDetik) % frames.Length];
    }
}
