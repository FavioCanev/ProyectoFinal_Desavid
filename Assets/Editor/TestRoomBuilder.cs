using System.IO;
using System.Linq;
using Game;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// Assets compartidos del jugador (configuración, prefab, Animator), utilidades de escena (luz, tilemaps,
/// cámara) y la sala de pruebas con cajas grises. Los enemigos y los efectos tienen su propio constructor.
/// Menú: Game.
/// </summary>
public static class TestRoomBuilder
{
    public const string TestRoomPath = "Assets/Scenes/SalaDePruebas.unity";
    private const string ConfigPath = "Assets/Data/PlayerConfig.asset";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string PlayerAnimatorPath = "Assets/Data/PlayerAnimator.controller";
    private const string MaterialPath = "Assets/Physics/NoFriction.physicsMaterial2D";
    private const string WhitePath = "Assets/Art/Debug/white.png";
    private const string PlayerSheet = "Assets/Art/Player/spriteCapi.png";
    private const string PlayerClips = "Assets/Art/Player/Animation/";

    public const float CameraSize = 6f;
    private const float PlayerScale = 0.65f;

    [MenuItem("Game/Build Test Room")]
    public static void Build()
    {
        PrepareAssets(out Sprite white, out GameObject playerPrefab, out PhysicsMaterial2D _);
        BuildTestRoom(playerPrefab, white);
        AssetDatabase.SaveAssets();
        Debug.Log("TestRoomBuilder: sala de pruebas creada en " + TestRoomPath);
    }

    [MenuItem("Game/Open Test Room")]
    public static void OpenTestRoom() => EditorSceneManager.OpenScene(TestRoomPath);

    // ---------- Assets del jugador ----------

    public static void PrepareAssets(out Sprite white, out GameObject playerPrefab, out PhysicsMaterial2D noFriction)
    {
        foreach (var f in new[] { "Assets/Data", "Assets/Prefabs", "Assets/Physics", "Assets/Art/Debug", "Assets/Scenes" })
            EnsureFolder(f);

        // El jugador y los enemigos se atraviesan (como en Hollow Knight); el daño es por contacto.
        // Los enemigos también se atraviesan entre sí, para no bloquearse al patrullar.
        int player = LayerMask.NameToLayer("Player"), enemy = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(player, enemy, true);
        Physics2D.IgnoreLayerCollision(enemy, enemy, true);

        white = EnsureWhiteSprite();
        noFriction = EnsureNoFriction();
        playerPrefab = EnsurePlayerPrefab(EnsureConfig(), noFriction);
    }

    private static Sprite EnsureWhiteSprite()
    {
        if (!File.Exists(WhitePath))
        {
            var tex = new Texture2D(4, 4);
            tex.SetPixels32(Enumerable.Repeat(new Color32(255, 255, 255, 255), 16).ToArray());
            File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(WhitePath);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(WhitePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4;   // 4 px = 1 unidad -> el cuadro mide 1x1
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
    }

    private static PhysicsMaterial2D EnsureNoFriction()
    {
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
        if (mat != null) return mat;
        mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    private static PlayerConfig EnsureConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        config.groundMask = LayerMask.GetMask("Ground");
        config.enemyMask = LayerMask.GetMask("Enemy");
        EditorUtility.SetDirty(config);
        return config;
    }

    private static GameObject EnsurePlayerPrefab(PlayerConfig config, PhysicsMaterial2D noFriction)
    {
        var go = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = noFriction;

        // Pivote en los pies: el collider se apoya sobre el origen del objeto.
        var capsule = go.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = new Vector2(0.8f, 1.6f);
        capsule.offset = new Vector2(0f, 0.8f);
        capsule.sharedMaterial = noFriction;

        AnimatorController animatorController = EnsureAnimator(PlayerAnimatorPath, "Idle",
            ("Idle", PlayerClips + "idle.anim", 1f),
            ("Run", PlayerClips + "correr.anim", 1f),
            ("Jump", PlayerClips + "saltar.anim", 1f),
            ("Dash", PlayerClips + "correr.anim", 2.2f),
            ("Attack", PlayerClips + "atacar.anim", 0.55f / config.attackDuration),
            ("Hurt", PlayerClips + "daño.anim", 1f),
            ("Dead", PlayerClips + "derrota.anim", 1f));
        Sprite playerSprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(PlayerSheet).OfType<Sprite>().First(s => s.name == "spriteCapi_0");
        var visual = CreateVisual(go.transform, playerSprite, 10, PlayerScale, animatorController, out SpriteRenderer sr, out Animator animator);
        visual.gameObject.AddComponent<AnimationEventSink>();   // las animaciones importadas llaman a eventos

        var health = go.AddComponent<Health>();
        Set(health, "maxHealth", 5);
        Set(health, "invulnerabilityTime", 1.0f);

        var controller = go.AddComponent<PlayerController>();
        Set(controller, "config", config);
        Set(controller, "visual", visual);
        go.AddComponent<PlayerCombat>();
        Set(go.AddComponent<PlayerLife>(), "sprite", sr);
        Set(go.AddComponent<PlayerAnimator>(), "animator", animator);
        var feel = go.AddComponent<PlayerFeel>();
        Set(feel, "visual", visual);
        Set(feel, "sprite", sr);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- Utilidades de personajes (también las usan los enemigos) ----------

    public static Transform CreateVisual(Transform parent, Sprite sprite, int sortingOrder, float scale,
        RuntimeAnimatorController controller, out SpriteRenderer sr, out Animator animator)
    {
        var visual = new GameObject("Visual");
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = Vector3.one * scale;
        sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        return visual.transform;
    }

    /// <summary>Animator con estados sueltos (sin transiciones): el código elige cuál reproducir.</summary>
    public static AnimatorController EnsureAnimator(string path, string defaultState, params (string state, string clip, float speed)[] states)
    {
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var (state, clipPath, speed) in states)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) Debug.LogWarning("TestRoomBuilder: falta el clip " + clipPath);
            var s = machine.AddState(state);
            s.motion = clip;
            s.speed = speed;
            if (state == defaultState) machine.defaultState = s;
        }
        return controller;
    }

    public static void ConfigureSprite(string path, Vector2 pivot, FilterMode filter, int pixelsPerUnit = 100)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;   // necesario para repetir el sprite en mosaico
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    /// <summary>Crea un clip que recorre fotogramas (PNG sueltos) de una carpeta.</summary>
    public static void CreateSpriteClip(string path, string framesFolder, string[] frames, float fps, bool loop)
    {
        AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framesFolder + frames[i] + ".png");
            if (sprite == null) Debug.LogWarning("TestRoomBuilder: falta el fotograma " + framesFolder + frames[i]);
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprite };
        }
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = keys[frames.Length - 1].value };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        AssetDatabase.CreateAsset(clip, path);
    }

    // ---------- Piezas de escena reutilizables ----------

    public static void AddGlobalLight()
    {
        var lightGo = new GameObject("Global Light 2D");
        lightGo.AddComponent<Light2D>().lightType = Light2D.LightType.Global;
    }

    public static Tilemap NewTilemap(Transform grid, string name, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid, false);
        var tm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
        return tm;
    }

    /// <summary>
    /// Cámara con Cinemachine: sigue al jugador con suavizado, se adelanta hacia donde corre,
    /// no sale de los límites (objeto "Camera Bounds") y escucha las sacudidas (impulsos).
    /// </summary>
    public static Camera AddCameraRig(Transform target, Vector2 boundsCenter, Vector2 boundsSize)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(target.position.x, target.position.y + 1f, -10f);
        camGo.AddComponent<CinemachineBrain>();

        var bounds = new GameObject("Camera Bounds") { layer = LayerMask.NameToLayer("Ignore Raycast") };
        bounds.transform.position = boundsCenter;
        var poly = bounds.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;
        Vector2 h = boundsSize * 0.5f;
        poly.SetPath(0, new[] { new Vector2(-h.x, -h.y), new Vector2(-h.x, h.y), new Vector2(h.x, h.y), new Vector2(h.x, -h.y) });

        var vcamGo = new GameObject("CM Player Camera");
        var vcam = vcamGo.AddComponent<CinemachineCamera>();
        vcam.Lens.OrthographicSize = CameraSize;
        vcam.Target.TrackingTarget = target;

        var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
        composer.TargetOffset = new Vector3(0f, 1f, 0f);               // enfoca el torso, no los pies
        composer.Damping = new Vector3(0.45f, 0.6f, 0f);               // suavizado en X e Y
        composer.Composition.DeadZone.Enabled = true;                  // zona central donde la cámara no se mueve
        composer.Composition.DeadZone.Size = new Vector2(0.08f, 0.15f);
        composer.Lookahead.Enabled = true;                             // se adelanta hacia donde corre
        composer.Lookahead.Time = 0.25f;
        composer.Lookahead.Smoothing = 8f;
        composer.Lookahead.IgnoreY = true;

        var confiner = vcamGo.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = poly;
        vcamGo.AddComponent<CinemachineImpulseListener>();             // recibe las sacudidas de GameFeel
        return cam;
    }

    // ---------- Sala de pruebas ----------

    private static void BuildTestRoom(GameObject playerPrefab, Sprite white)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AddGlobalLight();

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(-14f, -0.99f, 0f);
        AddCameraRig(player.transform, new Vector2(0f, 2f), new Vector2(44f, 14f));

        // Suelo: borde superior en y = -1.
        var level = new GameObject("Level");
        Box(level.transform, white, "Suelo", new Vector2(0f, -2f), new Vector2(42f, 2f));
        Box(level.transform, white, "Pared izquierda", new Vector2(-21.5f, 2f), new Vector2(1f, 10f));
        Box(level.transform, white, "Pared derecha", new Vector2(21.5f, 2f), new Vector2(1f, 10f));
        Box(level.transform, white, "Plataforma 1", new Vector2(-8f, 0.9f), new Vector2(5f, 0.6f));
        Box(level.transform, white, "Plataforma 2", new Vector2(-2f, 1.8f), new Vector2(5f, 0.6f));
        Box(level.transform, white, "Plataforma 3", new Vector2(5f, 1.2f), new Vector2(4f, 0.6f));
        Box(level.transform, white, "Muro de prueba", new Vector2(11f, 1.5f), new Vector2(1f, 5f));
        Box(level.transform, white, "Plataforma alta", new Vector2(15f, 3.2f), new Vector2(4f, 0.6f));

        EditorSceneManager.SaveScene(scene, TestRoomPath);
    }

    private static void Box(Transform parent, Sprite white, string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = new Color(0.45f, 0.5f, 0.55f);
        go.AddComponent<BoxCollider2D>();   // el sprite mide 1x1, la escala lo agranda
    }

    // ---------- Utilidades ----------

    public static void Set(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Set(Object target, string property, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Set(Object target, string property, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void Set(Object target, string property, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetMask(Object target, string property, LayerMask mask)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).intValue = mask.value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetArray(Object target, string property, Object[] values)
    {
        var so = new SerializedObject(target);
        var arr = so.FindProperty(property);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
