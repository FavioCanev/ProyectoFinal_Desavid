using System.Linq;
using Game;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enemigos de los niveles 4 y 5 (arte hecho con Gemini) con el sistema de enemigos común:
/// minero de pico (como el guerrero), minero dinamitero (como el escupidor) y el jefe minero.
/// </summary>
public static class MinerAssetsBuilder
{
    public struct Prefabs { public GameObject Pickaxe, Dynamite, Boss; }

    private const string Level4 = "Assets/Niveles/4_Minertown/";
    private const string Level5 = "Assets/Niveles/5_Mansion/";
    private const string MinerFrames = Level4 + "Art/Minero/";
    private const string BossFrames = Level5 + "Art/Jefe/";
    private const string Circle = "Assets/Art/Fx/circle.png";
    private static readonly Vector2 FramePivot = new Vector2(0.5f, 8f / 260f);   // pies a 8 px del borde de cada cuadro
    private static readonly Color Fire = new Color(1f, 0.55f, 0.15f);

    public static Prefabs Prepare(PhysicsMaterial2D noFriction)
    {
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(Circle);
        TestRoomBuilder.ConfigureSprite(MinerFrames + "dinamita.png", new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        Projectile dynamite = EnemyAssetsBuilder.EnsureProjectile(Level4 + "Prefabs", "Dinamita", AssetDatabase.LoadAssetAtPath<Sprite>(MinerFrames + "dinamita.png"),
            new Vector2(1.6f, 1.6f), Color.white, radius: 0.25f, gravity: 1f, destroyOnGround: true, lifetime: 4f, explosionRadius: 1.5f, spin: 600f);
        Projectile fire = EnemyAssetsBuilder.EnsureProjectile(Level5 + "Prefabs", "Fuego", circle, new Vector2(0.6f, 0.6f), Fire,
            radius: 0.25f, gravity: 1f, destroyOnGround: true, lifetime: 4f);
        Projectile wave = EnemyAssetsBuilder.EnsureProjectile(Level5 + "Prefabs", "OndaDeChoque", circle, new Vector2(1f, 0.55f), Fire,
            radius: 0.3f, gravity: 0f, destroyOnGround: false, lifetime: 1.4f);

        var minerAnimator = Animator(MinerFrames, "minero", Level4 + "Data/MineroAnimator.controller", new[]
        {
            ("Idle", 0, 0, 9, 8f, true), ("Walk", 1, 0, 9, 12f, true), ("Windup", 2, 1, 1, 6f, true), ("Swipe", 2, 2, 4, 14f, false),
            ("Charge", 1, 0, 9, 20f, true), ("Spit", 2, 5, 8, 12f, false), ("Hurt", 3, 1, 2, 10f, true), ("Dead", 3, 3, 5, 8f, false),
        });
        var bossAnimator = Animator(BossFrames, "jefe", Level5 + "Data/JefeAnimator.controller", new[]
        {
            ("Idle", 0, 0, 5, 8f, true), ("Walk", 1, 1, 6, 10f, true), ("Windup", 2, 0, 0, 6f, true), ("Swipe", 2, 1, 2, 10f, false),
            ("Charge", 1, 1, 6, 18f, true), ("Spit", 2, 5, 8, 12f, false), ("Leap", 2, 3, 3, 6f, true), ("Slam", 2, 4, 4, 6f, false),
            ("Hurt", 3, 0, 2, 10f, true), ("Dead", 3, 5, 6, 4f, false),
        });
        Sprite minerIdle = AssetDatabase.LoadAssetAtPath<Sprite>(MinerFrames + "minero_r0_0.png");
        Sprite bossIdle = AssetDatabase.LoadAssetAtPath<Sprite>(BossFrames + "jefe_r0_0.png");

        EnemyConfig pickaxe = EnemyAssetsBuilder.Config(Level4 + "Data", "MineroPico", c =>
        {
            c.patrolSpeed = 1.6f; c.chaseSpeed = 3.6f; c.detectRange = 7.5f; c.detectHeight = 2.5f; c.preferredDistance = 0f;
            c.bodyHalfWidth = 0.45f; c.superArmor = false; c.attackPause = 0.45f; c.enragedTimeScale = 1f;
            EnemyAssetsBuilder.Timings(c.swipe, true, 0f, 2f, windup: 0.4f, recover: 0.5f, cooldown: 1.1f);
            c.swipe.activeTime = 0.2f; c.swipe.hitOffset = new Vector2(1f, 0.9f); c.swipe.hitSize = new Vector2(1.6f, 1.4f);
            EnemyAssetsBuilder.Timings(c.charge, true, 3f, 7f, windup: 0.55f, recover: 0.8f, cooldown: 3.5f);
            c.charge.speed = 8.5f; c.charge.duration = 0.6f;
            c.spit.enabled = false; c.leap.enabled = false;
        });
        EnemyConfig thrower = EnemyAssetsBuilder.Config(Level4 + "Data", "MineroDinamita", c =>
        {
            c.patrolSpeed = 1.3f; c.chaseSpeed = 2.6f; c.detectRange = 11f; c.detectHeight = 8f; c.preferredDistance = 6f;
            c.bodyHalfWidth = 0.45f; c.superArmor = false; c.attackPause = 0.6f; c.enragedTimeScale = 1f;
            EnemyAssetsBuilder.Timings(c.spit, true, 2.5f, 11f, windup: 0.55f, recover: 0.6f, cooldown: 2.4f);
            c.spit.count = 1; c.spit.flightTime = 0.95f; c.spit.spreadTime = 0.25f; c.spit.mouthOffset = new Vector2(0.6f, 1.6f);
            EnemyAssetsBuilder.Timings(c.swipe, true, 0f, 1.6f, windup: 0.45f, recover: 0.5f, cooldown: 1.5f);
            c.swipe.activeTime = 0.2f; c.swipe.hitOffset = new Vector2(0.9f, 0.9f); c.swipe.hitSize = new Vector2(1.4f, 1.3f);
            c.charge.enabled = false; c.leap.enabled = false;
        });
        EnemyConfig boss = EnemyAssetsBuilder.Config(Level5 + "Data", "JefeMinero", c =>
        {
            c.patrolSpeed = 1.5f; c.chaseSpeed = 3.4f; c.detectRange = 17f; c.detectHeight = 8f; c.preferredDistance = 0f;
            c.bodyHalfWidth = 0.95f; c.superArmor = true; c.attackPause = 0.45f; c.corpseTime = 3f;
            c.enragedTimeScale = 0.7f;   // furia a mitad de vida
            EnemyAssetsBuilder.Timings(c.swipe, true, 0f, 3f, windup: 0.45f, recover: 0.5f, cooldown: 1.2f);
            c.swipe.activeTime = 0.25f; c.swipe.hitOffset = new Vector2(1.9f, 1.6f); c.swipe.hitSize = new Vector2(2.8f, 2.6f);
            EnemyAssetsBuilder.Timings(c.charge, true, 3f, 12f, windup: 0.6f, recover: 0.9f, cooldown: 4f);
            c.charge.speed = 10.5f; c.charge.duration = 0.8f;
            EnemyAssetsBuilder.Timings(c.spit, true, 4f, 15f, windup: 0.5f, recover: 0.6f, cooldown: 3f);
            c.spit.count = 3; c.spit.flightTime = 0.75f; c.spit.spreadTime = 0.18f; c.spit.mouthOffset = new Vector2(1.6f, 1.9f);
            EnemyAssetsBuilder.Timings(c.leap, true, 3f, 10f, windup: 0.5f, recover: 1f, cooldown: 5f);
            c.leap.flightTime = 0.8f; c.leap.shockwaveSpeed = 8f;
        });

        return new Prefabs
        {
            Pickaxe = EnemyAssetsBuilder.EnsureEnemy(Level4 + "Prefabs", "MineroPico", pickaxe, minerIdle, minerAnimator, noFriction, null, null,
                scale: 1.35f, collider: new Vector2(0.9f, 1.9f), tint: Color.white, health: 3, dormant: false),
            Dynamite = EnemyAssetsBuilder.EnsureEnemy(Level4 + "Prefabs", "MineroDinamita", thrower, minerIdle, minerAnimator, noFriction, dynamite, null,
                scale: 1.3f, collider: new Vector2(0.9f, 1.85f), tint: new Color(1f, 0.85f, 0.75f), health: 3, dormant: false),
            Boss = EnemyAssetsBuilder.EnsureEnemy(Level5 + "Prefabs", "JefeMinero", boss, bossIdle, bossAnimator, noFriction, fire, wave,
                scale: 2.1f, collider: new Vector2(1.8f, 3.3f), tint: Color.white, health: 18, dormant: true),
        };
    }

    /// <summary>Animator con un estado por clip; cada clip recorre cuadros "{prefijo}_r{fila}_{col}" de la carpeta.</summary>
    private static UnityEditor.Animations.AnimatorController Animator(string frames, string prefix, string path,
        (string state, int row, int from, int to, float fps, bool loop)[] clips)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { frames.TrimEnd('/') }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(p).StartsWith(prefix + "_r")) TestRoomBuilder.ConfigureSprite(p, FramePivot, FilterMode.Point);
        }
        string clipFolder = frames + "Animation/";
        TestRoomBuilder.EnsureFolder(clipFolder.TrimEnd('/'));
        TestRoomBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        foreach (var (state, row, from, to, fps, loop) in clips)
            TestRoomBuilder.CreateSpriteClip(clipFolder + state + ".anim", frames,
                Enumerable.Range(from, to - from + 1).Select(c => $"{prefix}_r{row}_{c}").ToArray(), fps, loop);
        return TestRoomBuilder.EnsureAnimator(path, "Idle", clips.Select(c => (c.state, clipFolder + c.state + ".anim", 1f)).ToArray());
    }
}
