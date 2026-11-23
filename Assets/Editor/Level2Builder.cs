using System.Linq;
using Amazonia;
using System.Collections.Generic;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Construye el Nivel 2 (Amazonía de Edward) a partir de sus escenas originales, que no se modifican:
/// Nivel2_Tierra (selva + arena del caimán) y Nivel2_Lago (ruta bajo el agua + arena acuática).
/// Su arena, su panorama, su caimán, sus diálogos y videos quedan tal cual (x de 0 a 66); a la izquierda
/// (x negativa) se añade un tramo nuevo de ~140 unidades con su arte de parallax y sus piezas sueltas.
/// El jugador, la vida, la interfaz y las banderas son los comunes del juego.
/// Menú: Game > Rebuild Level 2 (sobrescribe Nivel2_Tierra y Nivel2_Lago).
/// </summary>
public static class Level2Builder
{
    private const string Folder = "Assets/Niveles/2_Edward/";
    public const string LandPath = Folder + "Nivel2_Tierra.unity";
    public const string LakePath = Folder + "Nivel2_Lago.unity";
    private const string LandSource = Folder + "Original/AmazoniaTerrestre.unity";
    private const string LakeSource = Folder + "Original/AmazoniaAcuatica.unity";
    private const string Pieces = Folder + "Art/Piezas/";
    private const string ParallaxLand = Folder + "Art/V2/Sprites/Parallax/Terrestre/";
    private const string ParallaxLake = Folder + "Art/V2/Sprites/Parallax/Acuatico/";
    private const string Prefabs = Folder + "Prefabs/Amazonia/";
    private const string Level3Prefabs = "Assets/Niveles/3_Favio/Prefabs/";

    // Medidas de las escenas de Edward (no cambian): suelo con el borde superior en y = 0, techo en y = 20,
    // arena de x = 0 a 66. La parte nueva va de ExtensionStart a 0.
    private const float Floor = 0f, Ceiling = 20f, ArenaEnd = 66f, ExtensionStart = -142f;
    private const int PiecePixelsPerUnit = 33;   // su panorama: 2172 px para 66 unidades


    [MenuItem("Game/Rebuild Level 2 (sobrescribe Nivel2_Tierra y Nivel2_Lago)")]
    public static void BuildAll()
    {
        BuildLand();
        BuildLake();
        LevelKit.SetBuildScenes();
        AssetDatabase.SaveAssets();
    }

    // ---------- Tierra ----------

    public static void BuildLand()
    {
        var kit = Prepare(LandSource, aquatic: false, bossHealth: 6, out GameObject player, out AdventureBoss boss);
        var objects = new GameObject("Tramo nuevo · selva").transform;

        // Suelo (en celdas de 1,2): tres tramos separados por un foso de 4 celdas (salto) y otro de 7 (dash),
        // y una loma de 2 celdas en el último. Las filas -7 a -1 tienen su borde superior en y = 0.
        PaintTiles("Suelo de la selva", "selva", new[]
        {
            new RectInt(-125, -7, 42, 7), new RectInt(-79, -7, 28, 7), new RectInt(-44, -7, 44, 7), new RectInt(-33, 0, 8, 2),
        });
        foreach (var (x, y) in new[] { (-82f, 3f), (-72f, 4.2f), (-35f, 5.4f), (-18f, 3.2f) }) Log(objects, x, y);
        Void(objects, ExtensionStart - 10f, 0f);

        Sprite Piece(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Pieces}Bosque/bosque_{n}.png");
        foreach (var (n, x, s) in new[] { ("28", -128f, 0.7f), ("19", -108f, 0.8f), ("20", -90f, 0.7f), ("17", -66f, 0.6f), ("31", -47f, 0.8f), ("22", -25f, 0.8f), ("32", -8f, 0.6f) })
            Decor(objects, Piece(n), new Vector2(x, Floor - 0.15f), s, order: -3);
        foreach (var (n, x, s) in new[] { ("12", -138f, 0.6f), ("15", -118f, 0.6f), ("13", -97f, 0.5f), ("24", -78f, 0.6f), ("11", -57f, 0.5f), ("16", -42f, 0.6f), ("25", -14f, 0.6f) })
            Decor(objects, Piece(n), new Vector2(x, Floor - 0.1f), s, order: 12);   // plantas delante de los pies
        foreach (var (n, x) in new[] { ("00", -132f), ("03", -104f), ("05", -76f), ("01", -48f), ("04", -20f) })
            Decor(objects, Piece(n), new Vector2(x, Ceiling + 0.3f), 0.8f, order: -2, hanging: true);

        // La unión con su panorama (x = 0) queda tapada por vegetación de primer plano.
        Decor(objects, Piece("10"), new Vector2(0.5f, Floor - 0.6f), 0.9f, order: 40);
        Decor(objects, Piece("06"), new Vector2(0f, Ceiling + 0.5f), 1f, order: 40, hanging: true);
        Decor(objects, Piece("12"), new Vector2(-2.5f, Floor - 0.3f), 0.8f, order: 40);

        var enemies = new GameObject("Caimanes del camino").transform;
        GameObject warrior = AssetDatabase.LoadAssetAtPath<GameObject>(Level3Prefabs + "Warrior.prefab");
        GameObject spitter = AssetDatabase.LoadAssetAtPath<GameObject>(Level3Prefabs + "Spitter.prefab");
        foreach (var (prefab, x, y) in new[] { (warrior, -112f, Floor), (warrior, -86f, Floor), (spitter, -66f, Floor), (warrior, -45f, Floor), (spitter, -34f, Floor + 2.4f), (warrior, -12f, Floor) })
            ((GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies)).transform.position = new Vector3(x, y + 0.02f, 0f);

        foreach (var (x, i) in new[] { -136f, -92f, -50f, -6f }.Select((x, i) => (x, i)))
            LevelKit.CreateCheckpoint(objects, $"Punto de control {i + 1}", new Vector2(x, Floor));
        foreach (var (x, y, n) in new[] { (-132f, 0.6f, 3), (-99f, 2.2f, 3), (-83f, 3.6f, 3), (-73f, 4.8f, 3), (-60.5f, 2.4f, 5), (-38f, 3f, 4),
                                         (-36f, 6f, 2), (-19f, 3.8f, 3), (31f, 4.3f, 2), (42f, 4.4f, 2) })
            LevelKit.CreateCoinRow(objects, new Vector2(x, y), n);
        CreatePlasmaPickup(objects, new Vector2(-124f, 1.1f), LevelKit.CreateNotice(kit.Canvas,
            "¡Pistola de plasma!\n<size=28>Fin: disparar · 3 cargas y luego recarga (6 s)</size>"));

        Parallax(new[] { (ParallaxLand + "Parallax_Fondo_Bosque_v1.png", 0.92f, -60), (ParallaxLand + "Parallax_Fondo_Bosque_v2.png", 0.8f, -55),
                         (ParallaxLand + "Parallax_Arboles_Lianas.png", 0.6f, -50), (ParallaxLand + "Parallax_Lianas.png", 0.45f, -45) },
                 new Color(0.62f, 0.82f, 0.9f));
        Finish(LandPath, player);
    }

    // ---------- Lago ----------

    public static void BuildLake()
    {
        var kit = Prepare(LakeSource, aquatic: true, bossHealth: 10, out GameObject player, out AdventureBoss boss);
        var objects = new GameObject("Tramo nuevo · lago").transform;

        // Todo el lago es agua: el jugador nada desde que aparece.
        var water = new GameObject("Agua");
        water.transform.SetParent(objects, false);
        var area = water.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.offset = new Vector2((ExtensionStart - 8f + ArenaEnd) * 0.5f, (Floor - 3f + Ceiling) * 0.5f);
        area.size = new Vector2(ArenaEnd - (ExtensionStart - 8f), Ceiling - (Floor - 3f));
        water.AddComponent<WaterVolume>();

        // Fondo, techo (borde inferior en y = 20,4), pared de entrada y columnas que obligan a nadar en zigzag.
        PaintTiles("Roca del lago", "roca", new[]
        {
            new RectInt(-127, -7, 127, 7), new RectInt(-127, 17, 127, 2), new RectInt(-128, -7, 1, 26),
            new RectInt(-96, 0, 4, 8), new RectInt(-79, 7, 4, 10), new RectInt(-63, 0, 5, 9),
            new RectInt(-46, 6, 5, 11), new RectInt(-32, 0, 9, 3), new RectInt(-17, 8, 4, 9),
        });
        GameObject piranha = EnsurePiranhaPrefab();
        var fish = new GameObject("Pirañas").transform;
        foreach (var (x, y) in new[] { (-120f, 10f), (-102f, 4f), (-86f, 14f), (-67f, 13f), (-50f, 3f), (-36f, 12f), (-24f, 6f), (-8f, 12f) })
            ((GameObject)PrefabUtility.InstantiatePrefab(piranha, fish)).transform.position = new Vector3(x, y, 0f);

        Sprite Piece(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Pieces}Lago/lago_{n}.png");
        // Las columnas se visten con rocas y algas en sus extremos (hasta tener un tileset de roca submarina).
        foreach (var (x, y, n, hanging) in new[] { (-113f, Floor, "07", false), (-93f, 8.4f, "06", true), (-72.5f, Floor, "09", false),
                                                   (-52.5f, 7.2f, "06", true), (-18f, 9.6f, "06", true) })
            Decor(objects, Piece(n), new Vector2(x, hanging ? y + 0.6f : y), 0.9f, order: 3, hanging: hanging);
        Decor(objects, Piece("04"), new Vector2(0f, Floor - 0.3f), 0.9f, order: 40);   // tapa la unión con su panorama
        foreach (var (n, x, y, s) in new[] { ("10", -113f, 9.6f, 0.6f), ("05", -72.5f, 10.8f, 0.55f), ("11", -33f, 3.6f, 0.7f), ("09", -130f, Floor, 0.6f),
                                            ("06", -100f, Floor, 0.7f), ("07", -60f, Floor, 0.8f), ("03", -45f, Floor, 0.5f), ("12", -10f, Floor, 0.6f) })
            Decor(objects, Piece(n), new Vector2(x, y - 0.1f), s, order: -3);
        foreach (var (n, x, y) in new[] { ("16", -125f, 12f), ("17", -84f, 4f), ("18", -64f, 15f), ("16", -42f, 13f), ("17", -12f, 6f) })
            Decor(objects, Piece(n), new Vector2(x, y), 0.6f, order: -4);
        foreach (var (n, x) in new[] { ("13", -120f), ("13", -88f), ("13", -48f), ("13", -14f) })
            Decor(objects, Piece(n), new Vector2(x, Ceiling - 0.1f), 1.2f, order: -6, hanging: true);

        foreach (var (x, i) in new[] { -136f, -100f, -62f, -8f }.Select((x, i) => (x, i)))
            LevelKit.CreateCheckpoint(objects, $"Punto de control {i + 1}", new Vector2(x, Floor));
        foreach (var (x, y, n) in new[] { (-125f, 5f, 4), (-105f, 13f, 3), (-93f, 4f, 3), (-73f, 13.5f, 3), (-52f, 4f, 3), (-33f, 7f, 4), (-18f, 5f, 3) })
            LevelKit.CreateCoinRow(objects, new Vector2(x, y), n);

        Parallax(new[] { (ParallaxLand + "Parallax_Fondo_Acuatico_Estructuras.png", 0.88f, -60), (ParallaxLake + "Parallax_Raices_Bajo_Agua.png", 0.7f, -55),
                         (ParallaxLake + "Parallax_Algas_v2.png", 0.55f, -50) },
                 new Color(0.05f, 0.2f, 0.19f));
        Finish(LakePath, player);
    }

    // ---------- Partes comunes ----------

    /// <summary>Abre la escena original, quita lo que reemplazan los sistemas comunes y pone jugador, cámara e interfaz.</summary>
    private static LevelKit.Hud Prepare(string source, bool aquatic, int bossHealth, out GameObject player, out AdventureBoss boss)
    {
        TestRoomBuilder.PrepareAssets(out _, out GameObject playerPrefab, out _);
        LevelKit.PrepareSharedArt();
        ConfigureArt();
        Projectile rock = ConvertRockPrefab();
        ConvertBossPrefab(aquatic ? "CaimanAcuatico" : "CaimanTerrestre", bossHealth);

        var scene = EditorSceneManager.OpenScene(source, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name.StartsWith("Capibara") || root.name == "Main Camera" || root.name == "EventSystem") Object.DestroyImmediate(root);
        Object.DestroyImmediate(GameObject.Find("Límite izquierdo"));   // la selva nueva sigue hacia la izquierda
        int ground = LayerMask.NameToLayer("Ground"), arena = LayerMask.NameToLayer("Enemy");
        foreach (var c in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.gameObject.layer == arena && c.GetComponentInParent<AdventureBoss>() == null) c.gameObject.layer = ground;   // su capa "Arena" era la 8
        TestRoomBuilder.AddGlobalLight();

        player = LevelKit.CreatePlayer(playerPrefab, new Vector2(ExtensionStart + 4f, aquatic ? 6f : Floor));
        boss = Object.FindAnyObjectByType<AdventureBoss>();
        boss.target = player.GetComponent<PlayerLife>();
        boss.rockPrefab = rock;
        EditorUtility.SetDirty(boss);

        var bounds = Rect.MinMaxRect(ExtensionStart - 2f, Floor - 3f, ArenaEnd, Ceiling + 1f);
        TestRoomBuilder.AddCameraRig(player.transform, bounds.center, bounds.size);

        string title = aquatic ? "NIVEL 2 · FASE II\n<size=34>El Lago</size>" : "NIVEL 2\n<size=34>La Amazonía</size>";
        LevelKit.Hud hud = LevelKit.CreateHud(player.GetComponent<PlayerController>(), title,
            "¡Nivel completado!", "Venciste al caimán.\nPero el lago guarda restos de maquinaria...\n\n<size=28>Siguiente: el pantano</size>",
            "CAIMÁN", boss.Health != null ? boss.Health : boss.GetComponent<Health>());
        FeelAssetsBuilder.CreateGameFeel(hud.DamageFlash);
        WireEdwardSystems(hud, player, boss, aquatic);
        return hud;
    }

    /// <summary>Conecta su flujo y su director con el jugador y la interfaz comunes; de su HUD solo quedan el aviso y el fundido.</summary>
    private static void WireEdwardSystems(LevelKit.Hud hud, GameObject player, AdventureBoss boss, bool aquatic)
    {
        var oldHud = GameObject.Find("HUD · Amazonía").transform;
        foreach (Transform child in oldHud.Cast<Transform>().ToArray())
            if (child.name != "Aviso" && child.name != "Fundido") Object.DestroyImmediate(child.gameObject);

        var flow = Object.FindAnyObjectByType<AdventureFlow>();
        flow.player = player.GetComponent<PlayerController>();
        flow.boss = boss;
        flow.aquatic = aquatic;
        flow.status = oldHud.Find("Aviso").GetComponent<UnityEngine.UI.Text>();
        flow.fade = oldHud.Find("Fundido").GetComponent<UnityEngine.UI.Image>();
        flow.completedMessage = hud.CompletedMessage;
        EditorUtility.SetDirty(flow);

        var director = flow.GetComponent<AdventureDirector>();
        director.canvas = hud.Canvas.GetComponent<Canvas>();
        director.bossBar = hud.BossBar;
        EditorUtility.SetDirty(director);
    }

    private static void Finish(string path, GameObject player)
    {
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);   // "guardar como": el original queda intacto
        // Un suelo de Tilemap guardado en la misma sesión queda sin colisión: se recarga y se recrea el compuesto.
        EditorSceneManager.OpenScene(path);
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (map.GetComponent<CompositeCollider2D>() != null) GroundTilemapFactory.RebuildComposite(map.gameObject);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Level2Builder: construido " + path);
    }

    // ---------- Prefabs de Edward adaptados ----------

    /// <summary>El caimán usa la vida común (Game.Health) y la capa Enemy: así le pegan el ataque y el plasma del jugador.</summary>
    private static void ConvertBossPrefab(string name, int maxHealth)
    {
        string path = Prefabs + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        root.layer = LayerMask.NameToLayer("Enemy");
        var health = root.GetComponent<Health>() ?? root.AddComponent<Health>();
        TestRoomBuilder.Set(health, "maxHealth", maxHealth);
        TestRoomBuilder.Set(health, "invulnerabilityTime", 0.3f);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    /// <summary>La piedra del caimán pasa a ser un proyectil común (daña al jugador y se rompe contra el suelo).</summary>
    private static Projectile ConvertRockPrefab()
    {
        string path = Prefabs + "Piedra.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        root.layer = 0;
        foreach (var c in root.GetComponentsInChildren<Collider2D>()) c.isTrigger = true;
        var projectile = root.GetComponent<Projectile>() ?? root.AddComponent<Projectile>();
        TestRoomBuilder.SetMask(projectile, "groundMask", LayerMask.GetMask("Ground"));
        TestRoomBuilder.Set(projectile, "destroyOnGround", true);
        TestRoomBuilder.Set(projectile, "lifetime", 4f);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return prefab.GetComponent<Projectile>();
    }

    // ---------- Piraña (arte hecho con Gemini) ----------

    private static GameObject EnsurePiranhaPrefab()
    {
        string art = Folder + "Art/Pirana/";
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { art.TrimEnd('/') }))
            TestRoomBuilder.ConfigureSprite(AssetDatabase.GUIDToAssetPath(guid), new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        Sprite[] Frames(string name, int from, int to) =>
            Enumerable.Range(from, to - from + 1).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"{art}pirana_{name}_{i}.png")).ToArray();

        var go = new GameObject("Pirana") { layer = LayerMask.NameToLayer("Enemy") };
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var body = go.AddComponent<CapsuleCollider2D>();
        body.direction = CapsuleDirection2D.Horizontal;
        body.size = new Vector2(1.2f, 0.75f);
        var health = go.AddComponent<Health>();
        TestRoomBuilder.Set(health, "maxHealth", 2);
        TestRoomBuilder.Set(health, "invulnerabilityTime", 0.15f);
        var visual = LevelKit.SpriteChild(go.transform, Frames("nada", 0, 0)[0], Vector2.zero, 0.85f, order: 9);
        visual.name = "Visual";

        var swimmer = go.AddComponent<SwimmerEnemy>();
        TestRoomBuilder.Set(swimmer, "sprite", visual);
        TestRoomBuilder.SetArray(swimmer, "swimFrames", Frames("nada", 0, 7));
        TestRoomBuilder.SetArray(swimmer, "windupFrames", Frames("aviso", 0, 3));
        TestRoomBuilder.SetArray(swimmer, "chargeFrames", Frames("ataque", 0, 3));
        TestRoomBuilder.SetArray(swimmer, "hurtFrames", Frames("golpe", 0, 1));
        TestRoomBuilder.SetArray(swimmer, "deadFrames", Frames("golpe", 2, 5));
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "Pirana.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- Terreno y decorado ----------

    private static void ConfigureArt()
    {
        foreach (string folder in new[] { Pieces + "Bosque", Pieces + "Lago" })
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                TestRoomBuilder.ConfigureSprite(AssetDatabase.GUIDToAssetPath(guid), new Vector2(0.5f, 0f), FilterMode.Bilinear, PiecePixelsPerUnit);
        foreach (string folder in new[] { ParallaxLand.TrimEnd('/'), ParallaxLake.TrimEnd('/') })
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                TestRoomBuilder.ConfigureSprite(AssetDatabase.GUIDToAssetPath(guid), new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
    }

    // Losetas del tileset hecho con Gemini (144 px = 1,2 unidades). Claves: TL/T1/T2/TR arriba, L/R lados,
    // F1-F3 relleno, H1-H3 relleno justo bajo la superficie (con hojas o algas), BL/B1/B2/BR abajo.
    private const float TileSize = 1.2f;
    private const int TilePixelsPerUnit = 120;

    private static TileBase LoadTile(string set, string key)
    {
        string folder = Folder + "Art/Tileset/";
        string assetPath = $"{folder}Tiles/{set}_{key}.asset";
        TestRoomBuilder.EnsureFolder(folder + "Tiles");
        TestRoomBuilder.ConfigureSprite($"{folder}{set}_{key}.png", new Vector2(0.5f, 0.5f), FilterMode.Point, TilePixelsPerUnit);   // sin suavizado: no se ven costuras
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, assetPath); }
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}{set}_{key}.png");
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    /// <summary>Pinta los bloques (en celdas) en un Tilemap con colisión, eligiendo cada loseta según sus vecinas.</summary>
    private static void PaintTiles(string name, string set, RectInt[] blocks)
    {
        var grid = new GameObject("Grid " + name).AddComponent<Grid>();
        grid.cellSize = new Vector3(TileSize, TileSize, 0f);
        var map = GroundTilemapFactory.Create(grid.transform, name, LayerMask.NameToLayer("Ground"), sortingOrder: 1);
        TileBase T(string key) => LoadTile(set, key);
        TileBase[] top = { T("T1"), T("T2") }, fill = { T("F1"), T("F2"), T("F3") }, underTop = { T("H1"), T("H2"), T("H3") }, bottom = { T("B1"), T("B2") };
        TileBase tl = T("TL"), tr = T("TR"), l = T("L"), r = T("R"), bl = T("BL"), br = T("BR");

        var cells = new HashSet<Vector2Int>();
        foreach (var b in blocks) foreach (var c in b.allPositionsWithin) cells.Add(c);
        foreach (var c in cells)
        {
            bool hasL = cells.Contains(c + Vector2Int.left), hasR = cells.Contains(c + Vector2Int.right);
            bool hasU = cells.Contains(c + Vector2Int.up), hasD = cells.Contains(c + Vector2Int.down);
            int pick = Mathf.Abs(c.x * 7 + c.y * 13);   // variante fija por celda: el nivel sale igual cada vez
            TileBase tile = !hasU ? (!hasL ? tl : !hasR ? tr : top[pick % 2])
                          : !hasD ? (!hasL ? bl : !hasR ? br : bottom[pick % 2])
                          : !hasL ? l : !hasR ? r
                          : !cells.Contains(c + Vector2Int.up * 2) ? underTop[pick % 3] : fill[pick % 3];
            map.SetTile((Vector3Int)c, tile);
        }
    }

    /// <summary>Tronco caído de Edward como plataforma de un solo sentido (se sube desde abajo).</summary>
    private static void Log(Transform parent, float x, float top)
    {
        var go = new GameObject("Tronco") { layer = LayerMask.NameToLayer("Ground") };
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, top, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(4f * TileSize - 0.2f, 0.35f);
        box.offset = new Vector2(0f, -0.18f);
        box.usedByEffector = true;
        go.AddComponent<PlatformEffector2D>().surfaceArc = 160f;
        // Cuatro piezas del tileset: extremo con rama, dos centrales y extremo hueco.
        for (int i = 0; i < 4; i++)
        {
            string path = $"{Folder}Art/Tileset/tronco_{i}.png";
            TestRoomBuilder.ConfigureSprite(path, new Vector2(0.5f, 0.78f), FilterMode.Bilinear, TilePixelsPerUnit);   // la corteza a la altura del borde
            LevelKit.SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(path), new Vector2((i - 1.5f) * TileSize, 0f), 1f, order: 1);
        }
    }

    private static void Void(Transform parent, float x0, float x1)
    {
        var go = new GameObject("Vacío");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector2((x0 + x1) * 0.5f, Floor - 9f);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(x1 - x0, 4f);
        go.AddComponent<KillZone>();
    }

    /// <summary>Pieza suelta de Edward (pivote abajo al centro). Colgada: se gira para que cuelgue desde "at".</summary>
    private static void Decor(Transform parent, Sprite sprite, Vector2 at, float scale, int order, bool hanging = false)
    {
        if (sprite == null) return;
        var go = new GameObject(sprite.name);
        go.transform.SetParent(parent, true);
        go.transform.position = hanging ? at - new Vector2(0f, sprite.bounds.size.y * scale) : at;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
    }

    /// <summary>Capas de parallax de Edward repetidas a lo ancho de la parte nueva, detrás de su panorama original.</summary>
    private static void Parallax((string path, float follow, int order)[] layers, Color sky)
    {
        var bounds = Rect.MinMaxRect(ExtensionStart - 2f, Floor - 3f, ArenaEnd, Ceiling + 1f);
        Camera.main.backgroundColor = sky;
        foreach (var (path, follow, order) in layers)
        {
            var go = new GameObject("Parallax " + System.IO.Path.GetFileNameWithoutExtension(path));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            sr.sortingLayerName = "Background";   // debajo de su panorama (Background, orden -20)
            sr.sortingOrder = order;
            var layer = go.AddComponent<ParallaxLayer>();
            var so = new SerializedObject(layer);
            so.FindProperty("follow").vector2Value = new Vector2(follow, 0.9f);
            so.FindProperty("anchor").enumValueIndex = (int)ParallaxLayer.Anchor.LevelCenter;
            so.FindProperty("fitToCover").boolValue = true;
            so.FindProperty("levelBounds").rectValue = bounds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreatePlasmaPickup(Transform parent, Vector2 at, HudToast notice)
    {
        var go = new GameObject("Pistola de plasma");
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Fx/circle.png");
        foreach (var (size, color, order) in new[] { (1.3f, new Color(0.3f, 1f, 1f, 0.35f), 5), (0.7f, new Color(0.5f, 1f, 1f, 0.9f), 6), (0.3f, Color.white, 7) })
        {
            var glow = LevelKit.SpriteChild(go.transform, circle, Vector2.zero, size, order);
            glow.color = color;
        }
        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.7f;
        TestRoomBuilder.Set(go.AddComponent<PlasmaPickup>(), "notice", notice);
    }
}
