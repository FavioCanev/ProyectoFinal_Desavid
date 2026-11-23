using System.Linq;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Construye Nivel1 (bosque de Dante) partiendo de su escena original, que no se modifica:
/// conserva su mapa, su casa, sus árboles y su fondo animado; quita su jugador e interfaz antiguos;
/// amplía el mapa con las mismas losetas (autotile) y pone las piezas comunes de LevelKit.
/// Menú: Game > Rebuild Level 1 (sobrescribe Nivel1).
/// </summary>
public static class Level1Builder
{
    public const string ScenePath = "Assets/Niveles/1_Dante/Nivel1.unity";
    private const string SourcePath = "Assets/Niveles/1_Dante/Original/LV1_Dante.unity";
    private const string DataFolder = "Assets/Niveles/1_Dante/Data";
    private const string Tiles = "Assets/Niveles/1_Dante/Imports/karsiori/TileMap/Tiles/Tilesheet - WOODS_";
    private const string EnemyFolder = "Assets/Niveles/1_Dante/Enemigo normal/";
    private const string LevelTitle = "NIVEL 1\n<size=34>El Bosque</size>";

    // Lo que se quita de la escena de Dante: lo reemplazan el jugador, la interfaz y los sistemas comunes.
    private static readonly string[] Removed =
        { "Main Camera", "Global Light 2D", "spriteCapi_0", "moneda-Photoroom_0", "enemy_nrml", "HUD", "GameManager", "EventSystem", "SystemPause", "ZonaMuerte" };

    [MenuItem("Game/Rebuild Level 1 (sobrescribe Nivel1)")]
    public static void Build()
    {
        TestRoomBuilder.PrepareAssets(out Sprite white, out GameObject playerPrefab, out PhysicsMaterial2D noFriction);
        LevelKit.PrepareSharedArt();
        GameObject lumberjack = EnsureLumberjack(noFriction);

        var scene = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (Removed.Contains(root.name) || root.name.StartsWith("Tilemap_83")) Object.DestroyImmediate(root);
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
        TestRoomBuilder.AddGlobalLight();

        var map = GameObject.Find("mapa").GetComponent<Tilemap>();
        map.gameObject.layer = LayerMask.NameToLayer("Ground");
        PaintExtension(map);
        Vector2 Surface(Vector2Int c) => OnSurface(map, c);

        var player = LevelKit.CreatePlayer(playerPrefab, Surface(Level1Layout.PlayerStart));
        var objects = new GameObject("Objetos").transform;
        var enemies = new GameObject("Enemigos").transform;
        foreach (var cell in Level1Layout.Enemies)
            ((GameObject)PrefabUtility.InstantiatePrefab(lumberjack, enemies)).transform.position = Surface(cell) + Vector2.up * 0.02f;
        for (int i = 0; i < Level1Layout.Checkpoints.Length; i++)
            LevelKit.CreateCheckpoint(objects, $"Punto de control {i + 1}", Surface(Level1Layout.Checkpoints[i]));
        foreach (var (cell, count) in Level1Layout.CoinRows)
            LevelKit.CreateCoinRow(objects, Surface(cell) + Vector2.up * 0.6f, count);
        foreach (var (cell, text) in Level1Layout.Signs)
            LevelKit.CreateSign(objects, Surface(cell), text);
        CreateVoid(objects, map, white);
        PlaceDecor(map);

        Rect bounds = CellRect(map, Level1Layout.CameraBounds);
        Camera cam = TestRoomBuilder.AddCameraRig(player.transform, bounds.center, bounds.size);
        MakeBackgroundParallax(bounds);

        LevelKit.Hud hud = LevelKit.CreateHud(player.GetComponent<PlayerController>(), LevelTitle,
            "¡Nivel completado!", "Cruzaste el bosque.\n\n<size=28>Siguiente: la Amazonía…</size>");
        FeelAssetsBuilder.CreateGameFeel(hud.DamageFlash);
        LevelKit.CreateGoal(objects, Surface(Level1Layout.Goal), hud.CompletedMessage);

        EditorSceneManager.SaveScene(scene, ScenePath);   // "guardar como": la escena original queda intacta
        EditorSceneManager.OpenScene(ScenePath);
        GroundTilemapFactory.RebuildComposite(GameObject.Find("mapa"));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        LevelKit.SetBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Level1Builder: Nivel1 construido en " + ScenePath);
    }

    // ---------- Terreno ----------

    /// <summary>Pinta los bloques nuevos eligiendo la loseta según sus vecinas (pasto arriba, roca en los bordes).</summary>
    private static void PaintExtension(Tilemap map)
    {
        TileBase T(int n) => AssetDatabase.LoadAssetAtPath<TileBase>($"{Tiles}{n}.asset");
        TileBase topLeft = T(65), top = T(66), topRight = T(67), left = T(78), fill = T(79), right = T(80),
                 bottomLeft = T(85), bottom = T(86), bottomRight = T(87);

        var cells = new System.Collections.Generic.HashSet<Vector2Int>();
        foreach (var b in Level1Layout.Blocks)
            for (int x = b.X0; x <= b.X1; x++)
                for (int y = b.Y0; y <= b.Y1; y++) cells.Add(new Vector2Int(x, y));

        foreach (var c in cells)
        {
            bool l = cells.Contains(c + Vector2Int.left), r = cells.Contains(c + Vector2Int.right);
            bool u = cells.Contains(c + Vector2Int.up), d = cells.Contains(c + Vector2Int.down);
            TileBase tile = !u ? (!l ? topLeft : !r ? topRight : top)
                          : !d ? (!l ? bottomLeft : !r ? bottomRight : bottom)
                          : (!l ? left : !r ? right : fill);
            map.SetTile((Vector3Int)c, tile);
        }
    }

    /// <summary>Punto apoyado sobre la superficie de la celda (centro en X, borde superior en Y).</summary>
    private static Vector2 OnSurface(Tilemap map, Vector2Int c)
    {
        Vector3 cornerTop = map.CellToWorld(new Vector3Int(c.x, c.y + 1, 0));
        return new Vector2(cornerTop.x + Level1Layout.CellWorld * 0.5f, cornerTop.y);
    }

    private static Rect CellRect(Tilemap map, RectInt cells)
    {
        Vector3 min = map.CellToWorld(new Vector3Int(cells.xMin, cells.yMin, 0));
        Vector3 max = map.CellToWorld(new Vector3Int(cells.xMax, cells.yMax, 0));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// <summary>Zona invisible bajo todo el nivel: caer cuesta un corazón y devuelve a la última bandera.</summary>
    private static void CreateVoid(Transform parent, Tilemap map, Sprite white)
    {
        RectInt b = Level1Layout.CameraBounds;
        Rect r = CellRect(map, new RectInt(b.xMin - 10, Level1Layout.VoidRow - 6, b.width + 20, 6));
        var go = new GameObject("Vacío");
        go.transform.SetParent(parent, false);
        go.transform.position = r.center;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = r.size;
        go.AddComponent<KillZone>();
    }

    // ---------- Decorado ----------

    /// <summary>Repite los árboles de Dante sobre el terreno nuevo y lleva su robot junto a la meta.</summary>
    private static void PlaceDecor(Tilemap map)
    {
        var tree = GameObject.Find("Pixel_Woods_Tileset_3");
        if (tree != null)
        {
            float above = tree.transform.position.y - OnSurface(map, new Vector2Int(0, -9)).y;   // su árbol está sobre la fila -9
            foreach (var (col, row) in new[] { (45, -5), (68, -5), (96, 1), (118, -6), (136, -6) })
            {
                var copy = Object.Instantiate(tree);
                copy.name = "Árbol";
                Vector2 s = OnSurface(map, new Vector2Int(col, row));
                copy.transform.position = new Vector3(s.x, s.y + above, tree.transform.position.z);
            }
        }
        var robot = GameObject.Find("minbss (1)_0");
        if (robot != null)
        {
            Vector2 s = OnSurface(map, Level1Layout.RobotDecor);
            float half = robot.GetComponent<SpriteRenderer>().bounds.extents.y;
            robot.transform.position = new Vector3(s.x, s.y + half, robot.transform.position.z);
        }
    }

    /// <summary>El fondo animado de Dante pasa a cubrir todo el nivel, repetido y con parallax.</summary>
    private static void MakeBackgroundParallax(Rect levelRect)
    {
        var background = GameObject.Find("Background 2");
        if (background == null) return;
        background.GetComponent<SpriteRenderer>().sortingOrder = -100;
        var layer = background.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(layer);
        so.FindProperty("follow").vector2Value = new Vector2(0.85f, 0.9f);
        so.FindProperty("anchor").enumValueIndex = (int)ParallaxLayer.Anchor.LevelCenter;
        so.FindProperty("fitToCover").boolValue = true;
        so.FindProperty("levelBounds").rectValue = levelRect;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Enemigo: el leñador de Dante con nuestro sistema de enemigos ----------

    private static GameObject EnsureLumberjack(PhysicsMaterial2D noFriction)
    {
        string clips = EnemyFolder + "Animation/";
        TestRoomBuilder.EnsureFolder(DataFolder);
        var controller = TestRoomBuilder.EnsureAnimator(DataFolder + "/LumberjackAnimator.controller", "Idle",
            ("Walk", clips + "Caminar.anim", 1f), ("Idle", clips + "IDLE.anim", 1f), ("Windup", clips + "IDLE.anim", 1f),
            ("Swipe", clips + "attack.anim", 1.3f), ("Charge", clips + "Caminar.anim", 1.8f), ("Spit", clips + "attack.anim", 1f),
            ("Hurt", clips + "takeDamage.anim", 1f), ("Dead", clips + "died.anim", 1f));

        EnemyConfig config = EnemyAssetsBuilder.Config(DataFolder, "Lumberjack", c =>
        {
            // Nivel tutorial: lento y con aviso largo, para aprender a esquivar y a golpear.
            c.patrolSpeed = 1.4f; c.chaseSpeed = 3f; c.detectRange = 7f; c.detectHeight = 2.5f; c.preferredDistance = 0f;
            c.bodyHalfWidth = 0.5f; c.superArmor = false; c.attackPause = 0.5f;
            EnemyAssetsBuilder.Timings(c.swipe, true, 0f, 1.9f, windup: 0.5f, recover: 0.55f, cooldown: 1.3f);
            c.swipe.activeTime = 0.2f; c.swipe.hitOffset = new Vector2(1f, 1f); c.swipe.hitSize = new Vector2(1.6f, 1.6f);
            c.charge.enabled = false; c.spit.enabled = false; c.leap.enabled = false;
        });

        Sprite idle = AssetDatabase.LoadAllAssetsAtPath(EnemyFolder + "enemy (1).png").OfType<Sprite>().First(s => s.name == "enemy (1)_0");
        const float scale = 0.95f;
        return EnemyAssetsBuilder.EnsureEnemy(DataFolder, "Lumberjack", config, idle, controller, noFriction, null, null,
            scale, collider: new Vector2(1f, 2f), tint: Color.white, health: 3, dormant: false,
            visualOffsetY: idle.bounds.extents.y * scale);   // su sprite tiene el pivote en el centro
    }
}
