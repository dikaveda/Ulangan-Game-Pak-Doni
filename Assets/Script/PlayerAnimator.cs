using UnityEngine;

// Mengirim parameter ke Animator berdasarkan gerakan player.
// Animator Controller dibuat lewat menu: Tools > Player > Buat Animator Idle & Walk
//
// Parameter Animator:
//   MoveX, MoveY (float) -> arah hadap terakhir (dipakai Blend Tree untuk memilih arah)
//   IsWalking   (bool)   -> true saat bergerak (Idle <-> Walk)
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int IsDashing = Animator.StringToHash("IsDashing");

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement gerakan;
    private bool punyaParamDash;
    private Vector2 arahTerakhir = Vector2.down; // awal game menghadap ke bawah
    private bool sudahMati = false;

    private void OnEnable() => PlayerHealth.OnPlayerMati += MainkanDeath;
    private void OnDisable() => PlayerHealth.OnPlayerMati -= MainkanDeath;

    // Dipanggil saat HP habis
    private void MainkanDeath()
    {
        if (sudahMati || animator == null) return;
        sudahMati = true;

        // Game di-pause (Time.timeScale = 0), jadi animasi harus jalan pakai waktu asli
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetBool(IsWalking, false);
        if (punyaParamDash) animator.SetBool(IsDashing, false);
        animator.SetTrigger(Die);
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        gerakan = GetComponent<PlayerMovement>();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("PlayerAnimator: Animator Controller belum dipasang. Jalankan menu Tools > Player > Buat Animator Idle & Walk.", this);
            return;
        }

        // Cek parameter IsDashing ada (supaya tidak muncul warning kalau controller lama)
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == IsDashing) punyaParamDash = true;
        }
    }

    private void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null || sudahMati) return;

        Vector2 gerak = rb != null ? rb.linearVelocity : Vector2.zero;
        bool sedangJalan = gerak.sqrMagnitude > 0.01f;

        if (sedangJalan) arahTerakhir = KeArahSprite(gerak);

        animator.SetBool(IsWalking, sedangJalan);
        if (punyaParamDash) animator.SetBool(IsDashing, gerakan != null && gerakan.SedangDash);
        animator.SetFloat(MoveX, arahTerakhir.x);
        animator.SetFloat(MoveY, arahTerakhir.y);
    }

    // Asset hanya punya 6 arah (tanpa kiri/kanan murni), jadi arah gerak
    // dibulatkan ke salah satu dari 6 titik di Blend Tree supaya tidak "galau" di tengah.
    private static Vector2 KeArahSprite(Vector2 arah)
    {
        bool keAtas = arah.y > 0.1f;

        if (Mathf.Abs(arah.x) < 0.35f) // hampir lurus vertikal
            return keAtas ? Vector2.up : Vector2.down;

        float x = arah.x < 0 ? -1f : 1f;
        return new Vector2(x, keAtas ? 1f : -1f);
    }
}
