using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Construye la escena Nivel1 completa a partir de Level1Layout: terreno, ácido, plataformas móviles,
/// carteles, enemigos, arena del jefe, fondo en capas con parallax, primer plano, esporas, HUD y gamefeel.
/// Menú: Game > Rebuild Level 1 (sobrescribe Nivel1).
/// </summary>
public static class Level1Builder
{
    public const string ScenePath = "Assets/Scenes/Nivel1.unity";
    private const string TileFolder = "Assets/Art/Terrain/terreno-removebg-preview_";
    private const string BackgroundPath = "Assets/Art/Background/pantano.jpg";
    private const string ForegroundFolder = "Assets/Art/Foreground/";
    private const string CheckpointFolder = "Assets/Art/Checkpoint/";
    private const string UiFolder = "Assets/Art/UI/";   // madera y corazones recortados del arte de Dante (nivel 1)
    private const string LevelTitle = "NIVEL 3\n<size=34>El Pantano Ácido</size>";
    private const float Cell = Level1Layout.Cell;

    // Losetas elegidas del tileset (por número).
    private static readonly int[] SurfaceTiles = { 0, 1, 5 };
    private static readonly int[] FillTiles = { 4, 10, 14, 9, 31 };
    private static readonly int[] MetalTiles = { 34, 35 };
    private static readonly int[] BeamTiles = { 40 };

    // Las losetas tienen bordes transparentes: sin un respaldo, el fondo brillante se cuela entre ellas.
    private const string BackingTilePath = "Assets/Data/BackingTile.asset";
    private static readonly Color BackingColor = new Color(0.12f, 0.09f, 0.07f);
    // El fondo algo oscurecido separa el área de juego (como en Hollow Knight).
    private static readonly Color BackgroundTint = new Color(0.58f, 0.68f, 0.63f);
    private static readonly Color AcidColor = new Color(0.45f, 0.95f, 0.25f, 0.7f);
    private static readonly Color AcidDeepColor = new Color(0.07f, 0.2f, 0.06f, 0.93f);
    private static readonly Color ForegroundColor = new Color(0.03f, 0.05f, 0.04f, 0.97f);   // silueta casi negra

    private struct Hud
    {
        public GameObject CompletedMessage, BossBar;
        public Image DamageFlash;
    }

    [MenuItem("Game/Rebuild Level 1 (sobrescribe Nivel1)")]
    public static void Build()
    {
        TestRoomBuilder.PrepareAssets(out Sprite white, out GameObject playerPrefab, out PhysicsMaterial2D noFriction);
        EnemyAssetsBuilder.Prefabs enemyPrefabs = EnemyAssetsBuilder.Prepare(noFriction);
        PrepareArt();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TestRoomBuilder.AddGlobalLight();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        grid.cellSize = new Vector3(Cell, Cell, 0f);
        var ground = GroundTilemapFactory.Create(grid.transform, "Ground", LayerMask.NameToLayer("Ground"), sortingOrder: 0);
        var backing = TestRoomBuilder.NewTilemap(grid.transform, "Ground Backing", sortingOrder: -1);
        backing.color = BackingColor;
        TestRoomBuilder.NewTilemap(grid.transform, "Decor", sortingOrder: -5);   // vacía: para pintar adornos a mano
        PaintTerrain(ground, backing, EnsureBackingTile(white));   // los tiles se cargan DESPUÉS de NewScene

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = Level1Layout.OnSurface(Level1Layout.PlayerStart) + Vector2.up * 0.02f;
        var playerHealth = player.GetComponent<Health>();

        var enemies = new GameObject("Enemigos").transform;
        foreach (var spawn in Level1Layout.Enemies)
            Place(enemyPrefabs.For(spawn.Type), enemies, spawn.Cell);
        var boss = Place(enemyPrefabs.Boss, enemies, Level1Layout.BossSpawn);

        var objects = new GameObject("Objetos").transform;
        for (int i = 0; i < Level1Layout.Checkpoints.Length; i++)
            CreateCheckpoint(objects, $"Punto de control {i + 1}", Level1Layout.Checkpoints[i]);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateAcid(objects, white);
        Sprite metal = TileSprite(MetalTiles[0]);
        CreateMovers(objects, metal);

        RectInt b = Level1Layout.CameraBounds;
        var boundsCenter = new Vector2((b.xMin + b.width * 0.5f) * Cell, (b.yMin + b.height * 0.5f) * Cell);
        Vector2 boundsSize = new Vector2(b.width, b.height) * Cell;
        Camera cam = TestRoomBuilder.AddCameraRig(player.transform, boundsCenter, boundsSize);
        CreateBackground(new Rect(boundsCenter - boundsSize * 0.5f, boundsSize));
        CreateForeground();
        FeelAssetsBuilder.CreateSpores(cam.transform);

        Hud hud = CreateHud(white, font, player.GetComponent<PlayerController>(), boss.GetComponent<Health>());
        FeelAssetsBuilder.CreateGameFeel(hud.DamageFlash);
        GameObject goal = CreateGoal(objects, white, hud.CompletedMessage);
        CreateArena(objects, metal, boss.GetComponent<EnemyController>(), playerHealth, goal, hud.BossBar);

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Un suelo guardado en la misma sesión queda sin colisión: se recarga y se recrea el compuesto.
        EditorSceneManager.OpenScene(ScenePath);
        GroundTilemapFactory.RebuildComposite(GameObject.Find("Ground"));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(TestRoomBuilder.TestRoomPath, true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Level1Builder: Nivel1 construido en " + ScenePath);
    }

    public static void Open() => EditorSceneManager.OpenScene(ScenePath);

    private static GameObject Place(GameObject prefab, Transform parent, Vector2Int cell)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = Level1Layout.OnSurface(cell) + Vector2.up * 0.02f;
        return go;
    }

    // ---------- Terreno ----------

    private static void PaintTerrain(Tilemap ground, Tilemap backing, TileBase backingTile)
    {
        var rng = new System.Random(7);   // semilla fija: el nivel sale igual cada vez
        TileBase[] surface = LoadTiles(SurfaceTiles), fill = LoadTiles(FillTiles), metal = LoadTiles(MetalTiles),
                   beam = LoadTiles(BeamTiles);

        var kinds = new Dictionary<Vector2Int, Level1Layout.Kind>();
        foreach (var block in Level1Layout.Blocks)
            for (int x = block.X0; x <= block.X1; x++)
                for (int y = block.Y0; y <= block.Y1; y++)
                    kinds[new Vector2Int(x, y)] = block.Kind;

        foreach (var pair in kinds)
        {
            Vector2Int c = pair.Key;
            bool covered = kinds.TryGetValue(c + Vector2Int.up, out var above) && above == Level1Layout.Kind.Earth;
            TileBase tile = pair.Value switch
            {
                Level1Layout.Kind.Metal => metal[Mathf.Abs(c.x) % metal.Length],
                Level1Layout.Kind.Beam => beam[0],
                _ => covered ? Pick(fill, rng) : Pick(surface, rng),
            };
            ground.SetTile((Vector3Int)c, tile);
            if (pair.Value == Level1Layout.Kind.Earth) backing.SetTile((Vector3Int)c, backingTile);
        }
    }

    /// <summary>Loseta lisa del tamaño exacto de una celda, sin collider (el color lo pone el Tilemap).</summary>
    private static TileBase EnsureBackingTile(Sprite white)
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(BackingTilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, BackingTilePath);
        }
        tile.sprite = white;   // mide 1x1: se escala al tamaño de la celda
        tile.transform = Matrix4x4.Scale(new Vector3(Cell, Cell, 1f));
        tile.flags = TileFlags.LockTransform;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static TileBase[] LoadTiles(int[] numbers) =>
        numbers.Select(n => AssetDatabase.LoadAssetAtPath<TileBase>($"{TileFolder}{n}.asset"))
               .Where(t => t != null).ToArray();

    private static Sprite TileSprite(int number) => AssetDatabase.LoadAssetAtPath<Tile>($"{TileFolder}{number}.asset").sprite;

    private static TileBase Pick(TileBase[] tiles, System.Random rng) => tiles[rng.Next(tiles.Length)];

    // ---------- Ácido y plataformas móviles ----------

    /// <summary>Ácido solo en los fosos (columnas sin suelo a la altura del ácido): bajo el suelo no se ve.</summary>
    private static void CreateAcid(Transform parent, Sprite white)
    {
        RectInt b = Level1Layout.CameraBounds;
        int row = Level1Layout.AcidTopRow;
        bool Solid(int x) => Level1Layout.Blocks.Any(k => k.X0 <= x && x <= k.X1 && k.Y0 <= row && row <= k.Y1);

        int start = b.xMin - 10;
        for (int x = start; x <= b.xMax + 10; x++)
        {
            bool end = x == b.xMax + 10 || Solid(x);
            if (!end) continue;
            if (x > start) AcidPool(parent, white, start * Cell, x * Cell);
            start = x + 1;
        }
    }

    private static void AcidPool(Transform parent, Sprite white, float left, float right)
    {
        float top = (Level1Layout.AcidTopRow + 1) * Cell, bottom = Level1Layout.AcidBottomRow * Cell;
        float centerX = (left + right) * 0.5f, width = right - left;

        // Dos capas: una superficie verde translúcida y, debajo, líquido oscuro y turbio.
        float surface = top - Cell;
        var acid = SpriteBox(parent, white, "Ácido", new Vector2(centerX, (surface + bottom) * 0.5f),
            new Vector2(width, surface - bottom), AcidDeepColor, order: 6);
        acid.AddComponent<BoxCollider2D>().isTrigger = true;   // el sprite mide 1x1: la escala da el tamaño
        acid.AddComponent<KillZone>();
        var skin = SpriteBox(parent, white, "Superficie del ácido", new Vector2(centerX, (top + surface) * 0.5f),
            new Vector2(width, top - surface), AcidColor, order: 6);
        skin.AddComponent<BoxCollider2D>().isTrigger = true;
        skin.AddComponent<KillZone>();

        SpriteBox(parent, white, "Brillo del ácido", new Vector2(centerX, top - 0.025f),
            new Vector2(width, 0.05f), new Color(0.75f, 1f, 0.45f, 0.9f), order: 7);
    }

    private static void CreateMovers(Transform parent, Sprite metal)
    {
        int index = 0;
        foreach (var m in Level1Layout.Movers)
        {
            var go = new GameObject($"Plataforma móvil {++index}") { layer = LayerMask.NameToLayer("Ground") };
            go.transform.SetParent(parent, false);
            Vector2 from = new Vector2((m.FromX + 0.5f) * Cell, (m.FromRow + 1) * Cell);
            Vector2 to = new Vector2((m.ToX + 0.5f) * Cell, (m.ToRow + 1) * Cell);
            go.transform.position = from;   // el origen es el borde superior, donde se apoya el jugador

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(m.Width * Cell, 0.4f);
            box.offset = new Vector2(0f, -0.2f);
            for (int k = 0; k < m.Width; k++)
                SpriteChild(go.transform, metal, new Vector2((k - (m.Width - 1) * 0.5f) * Cell, -Cell * 0.5f), Cell / metal.bounds.size.x, order: 1);

            var platform = go.AddComponent<MovingPlatform>();
            var so = new SerializedObject(platform);
            so.FindProperty("pointA").vector2Value = from;
            so.FindProperty("pointB").vector2Value = to;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------- Objetos del nivel ----------

    // Medidas de la bandera en unidades del mundo (los sprites miden en píxeles a FlagPixelsPerUnit).
    private const int FlagPixelsPerUnit = 30;
    private static readonly Vector2 FlagLowered = new Vector2(0.03f, 0.8f);   // esquina superior izquierda, junto a la base
    private const float FlagRaisedY = 2.1f;                                   // junto a la bola del mástil

    private static void CreateCheckpoint(Transform parent, string name, Vector2Int cell)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = Level1Layout.OnSurface(cell);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 2.4f);
        trigger.offset = new Vector2(0f, 1.2f);

        SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_pole.png"), Vector2.zero, 1f, order: 2);
        var flag = SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_off.png"), FlagLowered, 1f, order: 3);
        flag.name = "Bandera";

        var checkpoint = go.AddComponent<Checkpoint>();
        TestRoomBuilder.Set(checkpoint, "flag", flag);
        TestRoomBuilder.Set(checkpoint, "activeSprite", AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_on.png"));
        var so = new SerializedObject(checkpoint);
        so.FindProperty("raisedY").floatValue = FlagRaisedY;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateGoal(Transform parent, Sprite white, GameObject completedMessage)
    {
        var go = new GameObject("Meta");
        go.transform.SetParent(parent, false);
        go.transform.position = Level1Layout.OnSurface(Level1Layout.Goal);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.6f, 3f);
        trigger.offset = new Vector2(0f, 1.5f);

        Marker(go.transform, white, new Vector2(0.35f, 2.6f), new Color(1f, 0.82f, 0.3f));
        TestRoomBuilder.Set(go.AddComponent<Goal>(), "completedMessage", completedMessage);
        return go;
    }

    private static void CreateArena(Transform parent, Sprite metal, EnemyController boss, Health playerHealth, GameObject goal, GameObject bossBar)
    {
        Vector2 doorCenter = Level1Layout.ToWorld(Level1Layout.ArenaDoor, out Vector2 doorSize);
        var door = new GameObject("Puerta de la arena") { layer = LayerMask.NameToLayer("Ground") };
        door.transform.SetParent(parent, false);
        door.transform.position = doorCenter;
        door.AddComponent<BoxCollider2D>().size = doorSize;
        RectInt d = Level1Layout.ArenaDoor;
        for (int x = 0; x < d.width; x++)
            for (int y = 0; y < d.height; y++)
            {
                var piece = SpriteChild(door.transform, metal, new Vector2((x - (d.width - 1) * 0.5f) * Cell, (y - (d.height - 1) * 0.5f) * Cell),
                    Cell / metal.bounds.size.x, order: 1);
                piece.color = new Color(0.7f, 0.65f, 0.6f);
            }

        Vector2 triggerCenter = Level1Layout.ToWorld(Level1Layout.ArenaTrigger, out Vector2 triggerSize);
        var arenaGo = new GameObject("Arena del jefe");
        arenaGo.transform.SetParent(parent, false);
        arenaGo.transform.position = triggerCenter;
        var trigger = arenaGo.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = triggerSize;

        var arena = arenaGo.AddComponent<BossArena>();
        TestRoomBuilder.Set(arena, "boss", boss);
        TestRoomBuilder.Set(arena, "playerHealth", playerHealth);
        TestRoomBuilder.Set(arena, "door", door);
        TestRoomBuilder.Set(arena, "goal", goal);
        TestRoomBuilder.Set(arena, "healthBar", bossBar);
    }

    // ---------- Fondo y primer plano ----------

    private static void PrepareArt()
    {
        TestRoomBuilder.ConfigureSprite(BackgroundPath, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        TestRoomBuilder.ConfigureSprite(ForegroundFolder + "fg_moss.png", new Vector2(0.5f, 1f), FilterMode.Bilinear);
        TestRoomBuilder.ConfigureSprite(ForegroundFolder + "fg_reeds.png", new Vector2(0.5f, 0f), FilterMode.Bilinear);
        TestRoomBuilder.ConfigureSprite(ForegroundFolder + "fg_branch.png", new Vector2(0f, 1f), FilterMode.Bilinear);

        TestRoomBuilder.ConfigureSprite(CheckpointFolder + "flag_pole.png", new Vector2(0.5f, 0f), FilterMode.Point, FlagPixelsPerUnit);
        TestRoomBuilder.ConfigureSprite(CheckpointFolder + "flag_off.png", new Vector2(0f, 1f), FilterMode.Point, FlagPixelsPerUnit);
        TestRoomBuilder.ConfigureSprite(CheckpointFolder + "flag_on.png", new Vector2(0f, 1f), FilterMode.Point, FlagPixelsPerUnit);

        foreach (string name in new[] { "ui_panel", "ui_banner", "ui_plank_gold", "ui_ring", "ui_button", "icon_play", "icon_restart", "icon_home", "icon_pause" })
            TestRoomBuilder.ConfigureSprite(UiFolder + name + ".png", new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        TestRoomBuilder.ConfigureSprite(UiFolder + "heart_full.png", new Vector2(0.5f, 0.5f), FilterMode.Point);
        TestRoomBuilder.ConfigureSprite(UiFolder + "heart_empty.png", new Vector2(0.5f, 0.5f), FilterMode.Point);
        // La tabla se estira a lo ancho (barra del jefe) sin deformar sus extremos.
        TestRoomBuilder.ConfigureSprite(UiFolder + "ui_plank.png", new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        var plank = (TextureImporter)AssetImporter.GetAtPath(UiFolder + "ui_plank.png");
        plank.spriteBorder = new Vector4(24f, 18f, 24f, 18f);
        plank.SaveAndReimport();
    }

    private static Sprite Ui(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + name + ".png");

    private static void CreateBackground(Rect levelRect)
    {
        var go = new GameObject("Fondo");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        sr.color = BackgroundTint;
        sr.sortingOrder = -100;
        var layer = go.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(layer);
        so.FindProperty("follow").vector2Value = new Vector2(0.85f, 0.9f);
        so.FindProperty("anchor").enumValueIndex = (int)ParallaxLayer.Anchor.LevelCenter;
        so.FindProperty("fitToCover").boolValue = true;
        so.FindProperty("levelBounds").rectValue = levelRect;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Primer plano al estilo de Hollow Knight / Silksong: siluetas casi negras que enmarcan la vista desde
    // los bordes. En vertical van pegadas a la pantalla (follow.y = 1): los juncos nacen por debajo del borde
    // inferior y el musgo cuelga desde encima del superior, a cualquier altura del nivel. En horizontal pasan
    // más rápido que el nivel (parallax). Así nunca parecen "apoyadas" sobre el suelo del juego.
    private const float EdgeOverlap = 0.3f;   // cuánto se esconden tras el borde de la pantalla

    /// <summary>Siluetas oscuras delante del jugador que pasan más rápido que el nivel (profundidad).</summary>
    private static void CreateForeground()
    {
        var root = new GameObject("Primer plano").transform;
        Sprite moss = AssetDatabase.LoadAssetAtPath<Sprite>(ForegroundFolder + "fg_moss.png");
        Sprite reeds = AssetDatabase.LoadAssetAtPath<Sprite>(ForegroundFolder + "fg_reeds.png");
        Sprite branch = AssetDatabase.LoadAssetAtPath<Sprite>(ForegroundFolder + "fg_branch.png");

        // Columnas donde aparece cada pieza (escala entre paréntesis para variar).
        var reedSpots = new[] { (8, 0.8f), (42, 1f), (72, 0.7f), (104, 0.9f), (140, 0.8f), (178, 1f), (226, 0.8f), (252, 0.9f), (290, 1f), (332, 0.8f), (368, 0.9f), (408, 0.8f) };
        var mossSpots = new[] { (2, 0.7f), (60, 0.8f), (120, 0.7f), (190, 0.8f), (240, 0.7f), (300, 0.8f), (330, 0.7f), (390, 0.8f) };
        var branchSpots = new[] { (28, 0.8f), (160, 0.9f), (270, 0.8f), (356, 0.9f) };

        float bottom = -TestRoomBuilder.CameraSize - EdgeOverlap, top = TestRoomBuilder.CameraSize + EdgeOverlap;
        foreach (var (col, scale) in reedSpots) Foreground(root, reeds, col, bottom, scale);
        foreach (var (col, scale) in mossSpots) Foreground(root, moss, col, top, scale);
        foreach (var (col, scale) in branchSpots) Foreground(root, branch, col, top, scale);
    }

    private static void Foreground(Transform parent, Sprite sprite, int column, float screenY, float scale)
    {
        var go = new GameObject(sprite.name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector2(column * Cell, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = ForegroundColor;
        sr.sortingOrder = 50;
        var layer = go.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(layer);
        so.FindProperty("follow").vector2Value = new Vector2(-0.3f, 1f);
        so.FindProperty("screenOffset").vector2Value = new Vector2(0f, screenY);
        so.FindProperty("anchor").enumValueIndex = (int)ParallaxLayer.Anchor.OwnPosition;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- HUD ----------

    private static readonly Color Cream = new Color(1f, 0.95f, 0.82f);
    private static readonly Color DarkWood = new Color(0.22f, 0.12f, 0.05f);

    private static Hud CreateHud(Sprite white, Font font, PlayerController player, Health bossHealth)
    {
        var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGo.transform;

        // Destello de daño: imagen roja a pantalla completa, transparente hasta que te golpean.
        var flash = Stretch(NewImage("Destello de daño", canvas, white, Vector2.zero, Vector2.zero, Vector2.zero));
        flash.color = new Color(0.8f, 0f, 0f, 0f);

        // Vida: corazones arriba a la izquierda.
        var health = player.GetComponent<Health>();
        var hearts = new Image[health.Max];
        for (int i = 0; i < hearts.Length; i++)
            hearts[i] = NewImage($"Corazón {i + 1}", canvas, Ui("heart_full"), new Vector2(0f, 1f), new Vector2(48f + i * 84f, -40f), new Vector2(76f, 72f));
        var hud = canvasGo.AddComponent<HealthHUD>();
        TestRoomBuilder.Set(hud, "health", health);
        TestRoomBuilder.SetArray(hud, "hearts", hearts);
        TestRoomBuilder.Set(hud, "fullSprite", Ui("heart_full"));
        TestRoomBuilder.Set(hud, "emptySprite", Ui("heart_empty"));

        // Dash: botón de madera con flecha bajo los corazones (apagado mientras se recarga).
        var dashIcon = NewImage("Dash", canvas, Ui("icon_play"), new Vector2(0f, 1f), new Vector2(52f, -128f), new Vector2(70f, 70f));
        var dash = dashIcon.gameObject.AddComponent<DashIndicator>();
        TestRoomBuilder.Set(dash, "player", player);
        TestRoomBuilder.Set(dash, "icon", dashIcon);
        Label(NewText("Tecla", dashIcon.transform, font, 30, TextAnchor.MiddleLeft, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(150f, 0f), new Vector2(200f, 44f), "DASH"));

        // Nombre del nivel al empezar.
        var title = NewImage("Título del nivel", canvas, Ui("ui_banner"), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(640f, 300f));
        Label(NewText("Texto", title.transform, font, 60, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(560f, 200f), LevelTitle));
        Toast(title.gameObject, onStart: true, hold: 2.5f);

        // Aviso al tocar una bandera.
        var notice = NewImage("Aviso de punto de control", canvas, Ui("ui_plank_gold"), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(520f, 150f));
        Label(NewText("Texto", notice.transform, font, 38, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 80f), "Punto de control"), outline: false);
        Toast(notice.gameObject, onStart: false, hold: 1.4f);

        var message = NewImage("Nivel completado", canvas, Ui("ui_panel"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 670f));
        Label(NewText("Título", message.transform, font, 46, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(480f, 90f), "¡Nivel completado!"), outline: false);
        Label(NewText("Detalle", message.transform, font, 34, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(480f, 260f), "Venciste al Gran Cocodrilo\n\n<size=28>Pulsa R para volver a empezar</size>"));
        message.gameObject.SetActive(false);

        GameObject bossBar = CreateBossBar(canvas, white, font, bossHealth);
        CreatePauseMenu(canvas, white, font, player);
        return new Hud { DamageFlash = flash, CompletedMessage = message.gameObject, BossBar = bossBar };
    }

    private static GameObject CreateBossBar(Transform canvas, Sprite white, Font font, Health bossHealth)
    {
        var frame = NewImage("Barra del jefe", canvas, Ui("ui_plank"), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(980f, 70f));
        frame.type = Image.Type.Sliced;

        var back = NewImage("Fondo", frame.transform, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 26f));
        back.color = new Color(0.12f, 0.05f, 0.03f);
        var fill = NewImage("Relleno", frame.transform, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 26f));
        fill.color = new Color(0.85f, 0.18f, 0.12f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;

        var name = NewImage("Placa", frame.transform, Ui("ui_banner"), new Vector2(0.5f, 1f), new Vector2(0f, 70f), new Vector2(400f, 120f));
        Label(NewText("Nombre", name.transform, font, 30, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(360f, 60f), "GRAN COCODRILO"));

        var bar = frame.gameObject.AddComponent<BossHealthBar>();
        TestRoomBuilder.Set(bar, "health", bossHealth);
        TestRoomBuilder.Set(bar, "fill", fill);
        return frame.gameObject;
    }

    private static void CreatePauseMenu(Transform canvas, Sprite white, Font font, PlayerController player)
    {
        var root = new GameObject("Pausa", typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one; rootRt.sizeDelta = Vector2.zero;   // pantalla completa
        var panel = Stretch(NewImage("Panel de pausa", root.transform, white, Vector2.zero, Vector2.zero, Vector2.zero));
        panel.color = new Color(0f, 0f, 0f, 0.6f);
        panel.raycastTarget = true;   // tapa el juego mientras está en pausa

        var board = NewImage("Tablero", panel.transform, Ui("ui_panel"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 670f));
        Label(NewText("Título", board.transform, font, 50, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(480f, 90f), "PAUSA"), outline: false);
        Button resume = PauseButton(board.transform, font, "Continuar", "icon_play", 0);
        Button restart = PauseButton(board.transform, font, "Reiniciar nivel", "icon_restart", 1);
        Button menu = PauseButton(board.transform, font, "Menú principal", "icon_home", 2);

        var pause = root.AddComponent<PauseMenu>();
        TestRoomBuilder.Set(pause, "player", player);
        TestRoomBuilder.Set(pause, "panel", panel.gameObject);
        TestRoomBuilder.Set(pause, "resumeButton", resume);
        TestRoomBuilder.Set(pause, "restartButton", restart);
        TestRoomBuilder.Set(pause, "menuButton", menu);

        var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        events.transform.SetParent(null);
    }

    private static Button PauseButton(Transform board, Font font, string text, string icon, int index)
    {
        var plank = NewImage(text, board, Ui("ui_button"), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f - index * 130f), new Vector2(440f, 116f));
        plank.raycastTarget = true;
        var button = plank.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 0.85f, 0.55f);
        colors.pressedColor = new Color(0.8f, 0.65f, 0.4f);
        button.colors = colors;
        NewImage("Ícono", plank.transform, Ui(icon), new Vector2(0f, 0.5f), new Vector2(-14f, 0f), new Vector2(96f, 96f));
        Label(NewText("Texto", plank.transform, font, 34, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(36f, 0f), new Vector2(330f, 60f), text));
        return button;
    }

    private static void Toast(GameObject go, bool onStart, float hold)
    {
        go.AddComponent<CanvasGroup>();
        var toast = go.AddComponent<HudToast>();
        var so = new SerializedObject(toast);
        so.FindProperty(onStart ? "showOnStart" : "showOnCheckpoint").boolValue = true;
        so.FindProperty("holdTime").floatValue = hold;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var image = NewUi(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Image Stretch(Image image)
    {
        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return image;
    }

    /// <summary>Texto con borde oscuro para que se lea sobre la madera y sobre el fondo.</summary>
    private static void Label(Text text, bool outline = true)
    {
        text.raycastTarget = false;
        if (!outline) return;
        var o = text.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.15f, 0.07f, 0.02f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);
    }

    // ---------- Utilidades ----------

    private static SpriteRenderer Marker(Transform parent, Sprite white, Vector2 size, Color color)
    {
        var marker = new GameObject("Marcador");
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
        marker.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = marker.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = color;
        sr.sortingOrder = 5;
        return sr;
    }

    private static GameObject SpriteBox(Transform parent, Sprite white, string name, Vector2 worldCenter, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldCenter;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    private static SpriteRenderer SpriteChild(Transform parent, Sprite sprite, Vector2 localPosition, float scale, int order)
    {
        var go = new GameObject("Pieza");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private static RectTransform NewUi(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private static Text NewText(string name, Transform parent, Font font, int size, TextAnchor align, Color color,
        Vector2 anchor, Vector2 position, Vector2 boxSize, string content)
    {
        var rt = NewUi(name, parent, anchor, position, boxSize);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.supportRichText = true;
        text.text = content;
        return text;
    }
}
