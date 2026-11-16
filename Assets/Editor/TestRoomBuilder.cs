using System.IO;
using System.Linq;
using Game;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera los assets base de la Fase 1: configuración, prefab del jugador y una sala de pruebas
/// con cajas grises. Se ejecuta desde el menú Game > Build Test Room (o por línea de comandos).
/// </summary>
public static class TestRoomBuilder
{
    private const string ScenePath = "Assets/Scenes/SalaDePruebas.unity";
    private const string ConfigPath = "Assets/Data/PlayerConfig.asset";
    private const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private const string MaterialPath = "Assets/Physics/NoFriction.physicsMaterial2D";
    private const string WhitePath = "Assets/Art/Debug/white.png";
    private const string PlayerSheet = "Assets/Art/Player/spriteCapi.png";

    [MenuItem("Game/Build Test Room")]
    public static void Build()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Physics");
        EnsureFolder("Assets/Art/Debug");
        EnsureFolder("Assets/Scenes");

        Sprite white = EnsureWhiteSprite();
        PhysicsMaterial2D noFriction = EnsureNoFriction();
        PlayerConfig config = EnsureConfig();
        GameObject playerPrefab = EnsurePlayerPrefab(config, noFriction);
        BuildScene(playerPrefab, white);

        AssetDatabase.SaveAssets();
        Debug.Log("TestRoomBuilder: sala de pruebas creada en " + ScenePath);
    }

    [MenuItem("Game/Open Test Room")]
    public static void OpenTestRoom() => EditorSceneManager.OpenScene(ScenePath);

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

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * 0.65f;
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(PlayerSheet)
            .OfType<Sprite>().First(s => s.name == "spriteCapi_0");
        sr.sortingOrder = 10;

        var controller = go.AddComponent<PlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("config").objectReferenceValue = config;
        so.FindProperty("visual").objectReferenceValue = visual.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void BuildScene(GameObject playerPrefab, Sprite white)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGo = new GameObject("Global Light 2D");
        lightGo.AddComponent<Light2D>().lightType = Light2D.LightType.Global;

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(-14f, -0.99f, 0f);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        camGo.AddComponent<CinemachineBrain>();

        var vcamGo = new GameObject("CM Player Camera");
        var vcam = vcamGo.AddComponent<CinemachineCamera>();
        vcam.Lens.OrthographicSize = 6f;
        vcam.Target.TrackingTarget = player.transform;
        vcamGo.AddComponent<CinemachinePositionComposer>();

        // Nivel de pruebas (cajas grises). Suelo: borde superior en y = -1.
        var level = new GameObject("Level");
        Box(level.transform, white, "Suelo", new Vector2(0f, -2f), new Vector2(42f, 2f));
        Box(level.transform, white, "Pared izquierda", new Vector2(-21.5f, 2f), new Vector2(1f, 10f));
        Box(level.transform, white, "Pared derecha", new Vector2(21.5f, 2f), new Vector2(1f, 10f));
        Box(level.transform, white, "Plataforma 1", new Vector2(-8f, 0.9f), new Vector2(5f, 0.6f));
        Box(level.transform, white, "Plataforma 2", new Vector2(-2f, 1.8f), new Vector2(5f, 0.6f));
        Box(level.transform, white, "Plataforma 3", new Vector2(5f, 1.2f), new Vector2(4f, 0.6f));
        Box(level.transform, white, "Muro de prueba", new Vector2(11f, 1.5f), new Vector2(1f, 5f));
        Box(level.transform, white, "Plataforma alta", new Vector2(15f, 3.2f), new Vector2(4f, 0.6f));

        EditorSceneManager.SaveScene(scene, ScenePath);
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

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
