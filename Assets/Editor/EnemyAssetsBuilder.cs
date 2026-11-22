using System;
using System.Linq;
using Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Genera todo lo de los enemigos: configuraciones (guerrero, escupidor, jefe), animaciones del cocodrilo
/// y del jefe, prefabs y proyectiles (escupitajo de ácido y onda de choque).
/// </summary>
public static class EnemyAssetsBuilder
{
    public struct Prefabs
    {
        public GameObject Warrior, Spitter, Boss;
        public GameObject For(Level1Layout.EnemyType t) =>
            t == Level1Layout.EnemyType.Warrior ? Warrior : t == Level1Layout.EnemyType.Spitter ? Spitter : Boss;
    }

    private const string CrocFrames = "Assets/Art/Crocodile/Frames/";
    private const string CrocClips = "Assets/Art/Crocodile/Animation/";
    private const string BossFrames = "Assets/Art/Boss/Frames/";
    private const string BossClips = "Assets/Art/Boss/Animation/";
    private const string Circle = "Assets/Art/Fx/circle.png";
    // Pivotes comunes (centro del cuerpo y pies), calculados al recortar cada hoja.
    private static readonly Vector2 CrocPivot = new Vector2(0.5f, 0.0226f);
    private static readonly Vector2 BossPivot = new Vector2(0.5f, 0.0319f);
    private static readonly Color AcidGreen = new Color(0.55f, 1f, 0.25f);

    public static Prefabs Prepare(PhysicsMaterial2D noFriction)
    {
        TestRoomBuilder.EnsureFolder("Assets/Data/Enemies");
        TestRoomBuilder.ConfigureSprite(Circle, new Vector2(0.5f, 0.5f), FilterMode.Bilinear, pixelsPerUnit: 64);
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(Circle);

        Projectile spit = EnsureProjectile("AcidSpit", circle, new Vector2(0.45f, 0.45f), radius: 0.2f, gravity: 1f, destroyOnGround: true, lifetime: 4f);
        Projectile wave = EnsureProjectile("Shockwave", circle, new Vector2(0.9f, 0.55f), radius: 0.3f, gravity: 0f, destroyOnGround: false, lifetime: 1.4f);

        AnimatorController crocAnimator = CrocodileAnimator();
        AnimatorController bossAnimator = BossAnimator();
        Sprite crocIdle = AssetDatabase.LoadAssetAtPath<Sprite>(CrocFrames + "croc_r1_00.png");
        Sprite bossIdle = AssetDatabase.LoadAssetAtPath<Sprite>(BossFrames + "croc_r1_00.png");

        return new Prefabs
        {
            Warrior = EnsureEnemy("Warrior", Config("Warrior", WarriorSettings), crocIdle, crocAnimator, noFriction, spit, wave,
                scale: 0.75f, collider: new Vector2(0.9f, 1.5f), tint: Color.white, health: 4, dormant: false),
            Spitter = EnsureEnemy("Spitter", Config("Spitter", SpitterSettings), crocIdle, crocAnimator, noFriction, spit, wave,
                scale: 0.68f, collider: new Vector2(0.8f, 1.35f), tint: new Color(0.95f, 1f, 0.55f), health: 3, dormant: false),
            Boss = EnsureEnemy("Boss", Config("Boss", BossSettings), bossIdle, bossAnimator, noFriction, spit, wave,
                scale: 2.2f, collider: new Vector2(1.8f, 3.2f), tint: Color.white, health: 15, dormant: true),
        };
    }

    // ---------- Configuraciones ----------

    private static void WarriorSettings(EnemyConfig c)
    {
        c.patrolSpeed = 1.8f; c.chaseSpeed = 3.8f; c.detectRange = 7f; c.detectHeight = 2.5f; c.preferredDistance = 0f;
        c.bodyHalfWidth = 0.45f; c.superArmor = false; c.attackPause = 0.4f;
        Timings(c.swipe, true, 0f, 1.7f, windup: 0.35f, recover: 0.45f, cooldown: 1f);
        c.swipe.activeTime = 0.2f; c.swipe.hitOffset = new Vector2(0.9f, 0.8f); c.swipe.hitSize = new Vector2(1.5f, 1.3f);
        Timings(c.charge, true, 2.5f, 7f, windup: 0.55f, recover: 0.8f, cooldown: 3.5f);
        c.charge.speed = 9f; c.charge.duration = 0.6f;
        c.spit.enabled = false; c.leap.enabled = false;
    }

    private static void SpitterSettings(EnemyConfig c)
    {
        c.patrolSpeed = 1.4f; c.chaseSpeed = 2.6f; c.detectRange = 10f; c.detectHeight = 8f; c.preferredDistance = 5.5f;
        c.bodyHalfWidth = 0.4f; c.superArmor = false; c.attackPause = 0.5f;
        Timings(c.spit, true, 2f, 10f, windup: 0.45f, recover: 0.5f, cooldown: 1.8f);
        c.spit.count = 1; c.spit.flightTime = 0.85f; c.spit.spreadTime = 0.25f; c.spit.mouthOffset = new Vector2(0.7f, 1.1f);
        Timings(c.swipe, true, 0f, 1.4f, windup: 0.4f, recover: 0.5f, cooldown: 1.5f);   // defensa si te acercas
        c.swipe.activeTime = 0.2f; c.swipe.hitOffset = new Vector2(0.8f, 0.7f); c.swipe.hitSize = new Vector2(1.3f, 1.2f);
        c.charge.enabled = false; c.leap.enabled = false;
    }

    private static void BossSettings(EnemyConfig c)
    {
        c.patrolSpeed = 1.5f; c.chaseSpeed = 3.2f; c.detectRange = 16f; c.detectHeight = 7f; c.preferredDistance = 0f;
        c.bodyHalfWidth = 0.95f; c.superArmor = true; c.attackPause = 0.5f; c.corpseTime = 3f;
        Timings(c.swipe, true, 0f, 2.8f, windup: 0.45f, recover: 0.5f, cooldown: 1.2f);
        c.swipe.activeTime = 0.25f; c.swipe.hitOffset = new Vector2(1.8f, 1.6f); c.swipe.hitSize = new Vector2(2.8f, 2.6f);
        Timings(c.charge, true, 3f, 12f, windup: 0.6f, recover: 0.9f, cooldown: 4f);
        c.charge.speed = 11f; c.charge.duration = 0.8f;
        Timings(c.spit, true, 4f, 14f, windup: 0.5f, recover: 0.6f, cooldown: 3.5f);
        c.spit.count = 3; c.spit.flightTime = 0.9f; c.spit.spreadTime = 0.22f; c.spit.mouthOffset = new Vector2(1.7f, 2.6f);
        Timings(c.leap, true, 3f, 10f, windup: 0.5f, recover: 1f, cooldown: 5f);
        c.leap.flightTime = 0.8f; c.leap.shockwaveSpeed = 8f;
    }

    private static void Timings(AttackTimings t, bool enabled, float min, float max, float windup, float recover, float cooldown)
    {
        t.enabled = enabled; t.minRange = min; t.maxRange = max; t.windup = windup; t.recover = recover; t.cooldown = cooldown;
    }

    private static EnemyConfig Config(string name, Action<EnemyConfig> setup)
    {
        string path = $"Assets/Data/Enemies/{name}Config.asset";
        var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<EnemyConfig>();
            AssetDatabase.CreateAsset(config, path);
        }
        setup(config);
        EditorUtility.SetDirty(config);
        return config;
    }

    // ---------- Animaciones ----------

    private static string[] Row(int row, int from, int to) =>
        Enumerable.Range(from, to - from + 1).Select(i => $"croc_r{row}_{i:00}").ToArray();

    private static AnimatorController CrocodileAnimator()
    {
        ImportFrames(CrocFrames, CrocPivot);
        TestRoomBuilder.EnsureFolder(CrocClips.TrimEnd('/'));
        Clip(CrocClips, CrocFrames, "Walk", Row(0, 0, 9), 12f, true);
        Clip(CrocClips, CrocFrames, "Idle", Row(1, 0, 9), 8f, true);
        Clip(CrocClips, CrocFrames, "Windup", Row(2, 0, 0), 6f, true);
        Clip(CrocClips, CrocFrames, "Swipe", Row(2, 0, 2), 16f, false);
        Clip(CrocClips, CrocFrames, "Charge", Row(0, 0, 9), 22f, true);
        Clip(CrocClips, CrocFrames, "Spit", Row(2, 3, 5), 12f, false);
        Clip(CrocClips, CrocFrames, "Hurt", new[] { "croc_r3_00", "croc_r3_02" }, 8f, true);
        Clip(CrocClips, CrocFrames, "Dead", Row(3, 3, 5), 8f, false);
        return States("Assets/Data/Enemies/CrocodileAnimator.controller", CrocClips, "Walk", "Idle", "Windup", "Swipe", "Charge", "Spit", "Hurt", "Dead");
    }

    private static AnimatorController BossAnimator()
    {
        ImportFrames(BossFrames, BossPivot);
        TestRoomBuilder.EnsureFolder(BossClips.TrimEnd('/'));
        Clip(BossClips, BossFrames, "Walk", Row(0, 0, 9), 10f, true);
        Clip(BossClips, BossFrames, "Idle", Row(1, 0, 9), 8f, true);
        Clip(BossClips, BossFrames, "Windup", Row(2, 0, 1), 6f, true);
        Clip(BossClips, BossFrames, "Swipe", Row(2, 1, 3), 14f, false);
        Clip(BossClips, BossFrames, "Charge", Row(2, 4, 4), 8f, true);
        Clip(BossClips, BossFrames, "Spit", Row(1, 2, 4), 10f, false);
        Clip(BossClips, BossFrames, "Leap", Row(2, 5, 5), 8f, true);
        Clip(BossClips, BossFrames, "Slam", Row(2, 5, 6), 10f, false);
        Clip(BossClips, BossFrames, "Hurt", Row(3, 0, 2), 10f, true);
        Clip(BossClips, BossFrames, "Dead", Row(3, 3, 5), 6f, false);
        return States("Assets/Data/Enemies/BossAnimator.controller", BossClips, "Walk", "Idle", "Windup", "Swipe", "Charge", "Spit", "Leap", "Slam", "Hurt", "Dead");
    }

    private static void ImportFrames(string folder, Vector2 pivot)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') }))
            TestRoomBuilder.ConfigureSprite(AssetDatabase.GUIDToAssetPath(guid), pivot, FilterMode.Point);
    }

    private static void Clip(string clipFolder, string framesFolder, string name, string[] frames, float fps, bool loop) =>
        TestRoomBuilder.CreateSpriteClip(clipFolder + name + ".anim", framesFolder, frames, fps, loop);

    private static AnimatorController States(string path, string clipFolder, params string[] names) =>
        TestRoomBuilder.EnsureAnimator(path, "Idle", names.Select(n => (n, clipFolder + n + ".anim", 1f)).ToArray());

    // ---------- Prefabs ----------

    private static GameObject EnsureEnemy(string name, EnemyConfig config, Sprite sprite, AnimatorController animatorController,
        PhysicsMaterial2D noFriction, Projectile spit, Projectile wave, float scale, Vector2 collider, Color tint, int health, bool dormant)
    {
        var go = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sharedMaterial = noFriction;

        var capsule = go.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = collider;
        capsule.offset = new Vector2(0f, collider.y * 0.5f);
        capsule.sharedMaterial = noFriction;

        var visual = TestRoomBuilder.CreateVisual(go.transform, sprite, 9, scale, animatorController, out SpriteRenderer sr, out Animator animator);
        sr.color = tint;

        var hp = go.AddComponent<Health>();
        TestRoomBuilder.Set(hp, "maxHealth", health);
        TestRoomBuilder.Set(hp, "invulnerabilityTime", 0.15f);

        var enemy = go.AddComponent<EnemyController>();
        TestRoomBuilder.Set(enemy, "config", config);
        TestRoomBuilder.Set(enemy, "visual", visual);
        TestRoomBuilder.Set(enemy, "animator", animator);
        TestRoomBuilder.Set(enemy, "sprite", sr);
        TestRoomBuilder.Set(enemy, "spitPrefab", spit);
        TestRoomBuilder.Set(enemy, "shockwavePrefab", wave);
        TestRoomBuilder.Set(enemy, "startsDormant", dormant);
        TestRoomBuilder.SetMask(enemy, "groundMask", LayerMask.GetMask("Ground"));
        TestRoomBuilder.SetMask(enemy, "playerMask", LayerMask.GetMask("Player"));

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    private static Projectile EnsureProjectile(string name, Sprite circle, Vector2 visualSize, float radius, float gravity, bool destroyOnGround, float lifetime)
    {
        var go = new GameObject(name);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravity;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;
        col.offset = new Vector2(0f, gravity == 0f ? radius : 0f);   // la onda de choque se desliza apoyada en el suelo

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = col.offset;
        visual.transform.localScale = new Vector3(visualSize.x, visualSize.y, 1f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = AcidGreen;
        sr.sortingOrder = 15;

        var p = go.AddComponent<Projectile>();
        TestRoomBuilder.Set(p, "destroyOnGround", destroyOnGround);
        TestRoomBuilder.Set(p, "lifetime", lifetime);
        TestRoomBuilder.SetMask(p, "groundMask", LayerMask.GetMask("Ground"));

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab.GetComponent<Projectile>();
    }
}
