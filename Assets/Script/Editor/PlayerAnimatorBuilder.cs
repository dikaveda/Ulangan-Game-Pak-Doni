using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Membuat Animation Clip Idle & Walk (6 arah) + Animator Controller untuk Player.
// Menu: Tools > Player > Buat Animator Idle & Walk
//
// Hasil:
//   Assets/Animations/Player/Idle_Down.anim, Walk_Down.anim, ... (12 clip)
//   Assets/Animations/Player/Player.controller
//     Parameter: MoveX (float), MoveY (float), IsWalking (bool)
//     State "Idle" dan "Walk" berupa Blend Tree 2D yang memilih arah dari MoveX/MoveY.
public static class PlayerAnimatorBuilder
{
    private const string Folder = "Assets/Animations/Player";
    private const string ControllerPath = Folder + "/Player.controller";

    public const string ParamMoveX = "MoveX";
    public const string ParamMoveY = "MoveY";
    public const string ParamIsWalking = "IsWalking";

    // nama file sprite, posisi arah di blend tree
    private static readonly (string akhiran, Vector2 posisi)[] Arah =
    {
        ("Down",       new Vector2( 0f, -1f)),
        ("Up",         new Vector2( 0f,  1f)),
        ("Left_Down",  new Vector2(-1f, -1f)),
        ("Left_Up",    new Vector2(-1f,  1f)),
        ("Right_Down", new Vector2( 1f, -1f)),
        ("Right_Up",   new Vector2( 1f,  1f)),
    };

    [MenuItem("Tools/Player/Buat Animator Idle & Walk")]
    public static void Buat()
    {
        Directory.CreateDirectory(Folder);

        BlendTree idleTree, walkTree;
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter(ParamMoveX, AnimatorControllerParameterType.Float);
        controller.AddParameter(ParamMoveY, AnimatorControllerParameterType.Float);
        controller.AddParameter(ParamIsWalking, AnimatorControllerParameterType.Bool);

        // Arah awal menghadap ke bawah
        var parameters = controller.parameters;
        parameters.First(p => p.name == ParamMoveY).defaultFloat = -1f;
        controller.parameters = parameters;

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idle = controller.CreateBlendTreeInController("Idle", out idleTree, 0);
        AnimatorState walk = controller.CreateBlendTreeInController("Walk", out walkTree, 0);
        SiapkanTree(idleTree, "Assets/Character/Idle/Idle_", "Idle", 8f);
        SiapkanTree(walkTree, "Assets/Character/Walk/walk_", "Walk", 12f);

        sm.defaultState = idle;

        // Idle -> Walk saat IsWalking = true
        AnimatorStateTransition keWalk = idle.AddTransition(walk);
        keWalk.hasExitTime = false;
        keWalk.duration = 0f;
        keWalk.AddCondition(AnimatorConditionMode.If, 0, ParamIsWalking);

        // Walk -> Idle saat IsWalking = false
        AnimatorStateTransition keIdle = walk.AddTransition(idle);
        keIdle.hasExitTime = false;
        keIdle.duration = 0f;
        keIdle.AddCondition(AnimatorConditionMode.IfNot, 0, ParamIsWalking);

        TambahDeath(controller);
        TambahDash(controller);

        AssetDatabase.SaveAssets();
        PasangKePlayer(controller);

        Debug.Log("Animator Player selesai dibuat: " + ControllerPath);
        Selection.activeObject = controller;
    }

    public const string ParamDie = "Die";

    // Menambahkan state Death (Blend Tree 6 arah, tidak loop) ke controller yang SUDAH ADA,
    // tanpa membuat ulang controller (supaya referensi di scene tetap aman).
    [MenuItem("Tools/Player/Tambah Animasi Death")]
    public static void TambahDeathKeControllerLama()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogWarning("Controller belum ada. Jalankan dulu Tools > Player > Buat Animator Idle & Walk.");
            return;
        }

        if (TambahDeath(controller))
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Animasi Death ditambahkan ke " + ControllerPath);
        }
        else
        {
            Debug.Log("Animasi Death sudah ada di " + ControllerPath);
        }
    }

    // Otomatis menambahkan Death ke controller lama setelah script ini dikompilasi
    [InitializeOnLoadMethod]
    private static void TambahDeathOtomatis()
    {
        EditorApplication.delayCall += () =>
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null && !controller.parameters.Any(p => p.name == ParamDie))
            {
                TambahDeathKeControllerLama();
            }
            if (controller != null && !controller.parameters.Any(p => p.name == ParamIsDashing))
            {
                TambahDashKeControllerLama();
            }
        };
    }

    public const string ParamIsDashing = "IsDashing";

    [MenuItem("Tools/Player/Tambah Animasi Dash")]
    public static void TambahDashKeControllerLama()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogWarning("Controller belum ada. Jalankan dulu Tools > Player > Buat Animator Idle & Walk.");
            return;
        }

        if (TambahDash(controller))
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Animasi Dash ditambahkan ke " + ControllerPath);
        }
        else
        {
            Debug.Log("Animasi Dash sudah ada di " + ControllerPath);
        }
    }

    // State Dash: Blend Tree 6 arah. Masuk dari mana saja saat IsDashing = true,
    // kembali ke Idle saat IsDashing = false (lalu Idle -> Walk kalau masih bergerak).
    private static bool TambahDash(AnimatorController controller)
    {
        if (controller.parameters.Any(p => p.name == ParamIsDashing)) return false;

        controller.AddParameter(ParamIsDashing, AnimatorControllerParameterType.Bool);

        AnimatorState dash = controller.CreateBlendTreeInController("Dash", out BlendTree dashTree, 0);
        dashTree.blendType = BlendTreeType.SimpleDirectional2D;
        dashTree.blendParameter = ParamMoveX;
        dashTree.blendParameterY = ParamMoveY;

        foreach (var (akhiran, posisi) in Arah)
        {
            // 8 frame dalam ~0.18 detik dash -> diputar cepat
            AnimationClip clip = BuatClip("Assets/Character/Dash/Dash_" + akhiran + ".png", "Dash_" + akhiran, 40f, true);
            if (clip != null) dashTree.AddChild(clip, posisi);
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorStateTransition keDash = sm.AddAnyStateTransition(dash);
        keDash.hasExitTime = false;
        keDash.duration = 0f;
        keDash.canTransitionToSelf = false;
        keDash.AddCondition(AnimatorConditionMode.If, 0, ParamIsDashing);

        AnimatorState idle = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle");
        if (idle != null)
        {
            AnimatorStateTransition keluar = dash.AddTransition(idle);
            keluar.hasExitTime = false;
            keluar.duration = 0f;
            keluar.AddCondition(AnimatorConditionMode.IfNot, 0, ParamIsDashing);
        }

        EditorUtility.SetDirty(controller);
        return true;
    }

    private static bool TambahDeath(AnimatorController controller)
    {
        if (controller.parameters.Any(p => p.name == ParamDie)) return false;

        controller.AddParameter(ParamDie, AnimatorControllerParameterType.Trigger);

        AnimatorState death = controller.CreateBlendTreeInController("Death", out BlendTree deathTree, 0);
        deathTree.blendType = BlendTreeType.SimpleDirectional2D;
        deathTree.blendParameter = ParamMoveX;
        deathTree.blendParameterY = ParamMoveY;

        foreach (var (akhiran, posisi) in Arah)
        {
            AnimationClip clip = BuatClip("Assets/Character/Death/death_" + akhiran + ".png", "Death_" + akhiran, 10f, false);
            if (clip != null) deathTree.AddChild(clip, posisi);
        }

        // Dari state mana pun -> Death saat trigger "Die"
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorStateTransition keDeath = sm.AddAnyStateTransition(death);
        keDeath.hasExitTime = false;
        keDeath.duration = 0f;
        keDeath.canTransitionToSelf = false;
        keDeath.AddCondition(AnimatorConditionMode.If, 0, ParamDie);

        EditorUtility.SetDirty(controller);
        return true;
    }

    private static void SiapkanTree(BlendTree tree, string awalanSprite, string namaClip, float fps)
    {
        tree.blendType = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter = ParamMoveX;
        tree.blendParameterY = ParamMoveY;

        foreach (var (akhiran, posisi) in Arah)
        {
            AnimationClip clip = BuatClip(awalanSprite + akhiran + ".png", namaClip + "_" + akhiran, fps);
            if (clip != null) tree.AddChild(clip, posisi);
        }
    }

    private static AnimationClip BuatClip(string pathSprite, string namaClip, float fps, bool loop = true)
    {
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(pathSprite)
            .OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
            .ToArray();

        if (frames.Length == 0)
        {
            Debug.LogWarning("Sprite tidak ditemukan: " + pathSprite);
            return null;
        }

        AnimationClip clip = new AnimationClip { frameRate = fps };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        }
        // Key terakhir supaya frame terakhir tampil penuh sebelum loop
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; // Death tidak loop: berhenti di frame terakhir
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string path = Folder + "/" + namaClip + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // Tambahkan Animator ke Player di scene yang sedang terbuka
    private static void PasangKePlayer(AnimatorController controller)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Objek bertag Player tidak ada di scene. Pasang Animator secara manual.");
            return;
        }

        Animator animator = player.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(player);

        Undo.RecordObject(animator, "Pasang Animator Player");
        animator.runtimeAnimatorController = controller;

        if (player.GetComponent<PlayerAnimator>() == null) Undo.AddComponent<PlayerAnimator>(player);

        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
    }
}
