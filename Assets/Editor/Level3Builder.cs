using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Construye la escena Nivel3 completa a partir de Level3Layout: terreno, ácido, plataformas móviles,
/// carteles, enemigos, arena del jefe, fondo en capas con parallax, primer plano, esporas, HUD y gamefeel.
/// Menú: Game > Rebuild Level 3 (sobrescribe Nivel3).
/// </summary>
public static class Level3Builder
{
    public const string ScenePath = "Assets/Niveles/3_Favio/Nivel3.unity";
    private const string TileFolder = "Assets/Niveles/3_Favio/Art/Terrain/terreno-removebg-preview_";
    private const string BackgroundPath = "Assets/Niveles/3_Favio/Art/Background/pantano.jpg";
    private const string ForegroundFolder = "Assets/Niveles/3_Favio/Art/Foreground/";
    private const string LevelTitle = "NIVEL 3\n<size=34>El Pantano Ácido</size>";
    private const float Cell = Level3Layout.Cell;

    // Losetas elegidas del tileset (por número).
    private static readonly int[] SurfaceTiles = { 0, 1, 5 };
    private static readonly int[] FillTiles = { 4, 10, 14, 9, 31 };
    private static readonly int[] MetalTiles = { 34, 35 };
    private static readonly int[] BeamTiles = { 40 };

    // Las losetas tienen bordes transparentes: sin un respaldo, el fondo brillante se cuela entre ellas.
    private const string BackingTilePath = "Assets/Niveles/3_Favio/Data/BackingTile.asset";
    private static readonly Color BackingColor = new Color(0.12f, 0.09f, 0.07f);
    // El fondo algo oscurecido separa el área de juego (como en Hollow Knight).
    private static readonly Color BackgroundTint = new Color(0.58f, 0.68f, 0.63f);
    private static readonly Color AcidColor = new Color(0.45f, 0.95f, 0.25f, 0.7f);
    private static readonly Color AcidDeepColor = new Color(0.07f, 0.2f, 0.06f, 0.93f);
    private static readonly Color ForegroundColor = new Color(0.03f, 0.05f, 0.04f, 0.97f);   // silueta casi negra

    [MenuItem("Game/Rebuild Level 3 (sobrescribe Nivel3)")]
    public static void Build()
    {
        TestRoomBuilder.PrepareAssets(out Sprite white, out GameObject playerPrefab, out PhysicsMaterial2D noFriction);
        EnemyAssetsBuilder.Prefabs enemyPrefabs = EnemyAssetsBuilder.Prepare(noFriction);
        PrepareArt();
        LevelKit.PrepareSharedArt();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TestRoomBuilder.AddGlobalLight();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        grid.cellSize = new Vector3(Cell, Cell, 0f);
        var ground = GroundTilemapFactory.Create(grid.transform, "Ground", LayerMask.NameToLayer("Ground"), sortingOrder: 0);
        var backing = TestRoomBuilder.NewTilemap(grid.transform, "Ground Backing", sortingOrder: -1);
        backing.color = BackingColor;
        TestRoomBuilder.NewTilemap(grid.transform, "Decor", sortingOrder: -5);   // vacía: para pintar adornos a mano
        PaintTerrain(ground, backing, EnsureBackingTile(white));   // los tiles se cargan DESPUÉS de NewScene

        var player = LevelKit.CreatePlayer(playerPrefab, Level3Layout.OnSurface(Level3Layout.PlayerStart));
        var playerHealth = player.GetComponent<Health>();

        var enemies = new GameObject("Enemigos").transform;
        foreach (var spawn in Level3Layout.Enemies)
            Place(enemyPrefabs.For(spawn.Type), enemies, spawn.Cell);
        var boss = Place(enemyPrefabs.Boss, enemies, Level3Layout.BossSpawn);

        var objects = new GameObject("Objetos").transform;
        for (int i = 0; i < Level3Layout.Checkpoints.Length; i++)
            LevelKit.CreateCheckpoint(objects, $"Punto de control {i + 1}", Level3Layout.OnSurface(Level3Layout.Checkpoints[i]));
        foreach (var (cell, count) in Level3Layout.CoinRows)
            LevelKit.CreateCoinRow(objects, Level3Layout.OnSurface(cell) + Vector2.up * 0.6f, count);
        CreateAcid(objects, white);
        Sprite metal = TileSprite(MetalTiles[0]);
        CreateMovers(objects, metal);

        RectInt b = Level3Layout.CameraBounds;
        var boundsCenter = new Vector2((b.xMin + b.width * 0.5f) * Cell, (b.yMin + b.height * 0.5f) * Cell);
        Vector2 boundsSize = new Vector2(b.width, b.height) * Cell;
        Camera cam = TestRoomBuilder.AddCameraRig(player.transform, boundsCenter, boundsSize);
        CreateBackground(new Rect(boundsCenter - boundsSize * 0.5f, boundsSize));
        CreateForeground();
        FeelAssetsBuilder.CreateSpores(cam.transform);

        LevelKit.Hud hud = LevelKit.CreateHud(player.GetComponent<PlayerController>(), LevelTitle,
            "¡Nivel completado!", "Venciste al Gran Cocodrilo.\nLas huellas de la maquinaria\nllevan a la ciudad...\n\n<size=28>Siguiente: Minertown</size>",
            "GRAN COCODRILO", boss.GetComponent<Health>());
        FeelAssetsBuilder.CreateGameFeel(hud.DamageFlash);
        GameObject goal = LevelKit.CreateGoal(objects, Level3Layout.OnSurface(Level3Layout.Goal), hud.CompletedMessage);
        CreateArena(objects, metal, boss.GetComponent<EnemyController>(), playerHealth, goal, hud.BossBar);

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Un suelo guardado en la misma sesión queda sin colisión: se recarga y se recrea el compuesto.
        EditorSceneManager.OpenScene(ScenePath);
        GroundTilemapFactory.RebuildComposite(GameObject.Find("Ground"));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        LevelKit.SetBuildScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Level3Builder: Nivel3 construido en " + ScenePath);
    }

    public static void Open() => EditorSceneManager.OpenScene(ScenePath);

    private static GameObject Place(GameObject prefab, Transform parent, Vector2Int cell)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = Level3Layout.OnSurface(cell) + Vector2.up * 0.02f;
        return go;
    }

    // ---------- Terreno ----------

    private static void PaintTerrain(Tilemap ground, Tilemap backing, TileBase backingTile)
    {
        var rng = new System.Random(7);   // semilla fija: el nivel sale igual cada vez
        TileBase[] surface = LoadTiles(SurfaceTiles), fill = LoadTiles(FillTiles), metal = LoadTiles(MetalTiles),
                   beam = LoadTiles(BeamTiles);

        var kinds = new Dictionary<Vector2Int, Level3Layout.Kind>();
        foreach (var block in Level3Layout.Blocks)
            for (int x = block.X0; x <= block.X1; x++)
                for (int y = block.Y0; y <= block.Y1; y++)
                    kinds[new Vector2Int(x, y)] = block.Kind;

        foreach (var pair in kinds)
        {
            Vector2Int c = pair.Key;
            bool covered = kinds.TryGetValue(c + Vector2Int.up, out var above) && above == Level3Layout.Kind.Earth;
            TileBase tile = pair.Value switch
            {
                Level3Layout.Kind.Metal => metal[Mathf.Abs(c.x) % metal.Length],
                Level3Layout.Kind.Beam => beam[0],
                _ => covered ? Pick(fill, rng) : Pick(surface, rng),
            };
            ground.SetTile((Vector3Int)c, tile);
            if (pair.Value == Level3Layout.Kind.Earth) backing.SetTile((Vector3Int)c, backingTile);
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
        RectInt b = Level3Layout.CameraBounds;
        int row = Level3Layout.AcidTopRow;
        bool Solid(int x) => Level3Layout.Blocks.Any(k => k.X0 <= x && x <= k.X1 && k.Y0 <= row && row <= k.Y1);

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
        float top = (Level3Layout.AcidTopRow + 1) * Cell, bottom = Level3Layout.AcidBottomRow * Cell;
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
        foreach (var m in Level3Layout.Movers)
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
                LevelKit.SpriteChild(go.transform, metal, new Vector2((k - (m.Width - 1) * 0.5f) * Cell, -Cell * 0.5f), Cell / metal.bounds.size.x, order: 1);

            var platform = go.AddComponent<MovingPlatform>();
            var so = new SerializedObject(platform);
            so.FindProperty("pointA").vector2Value = from;
            so.FindProperty("pointB").vector2Value = to;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------- Objetos del nivel ----------

    private static void CreateArena(Transform parent, Sprite metal, EnemyController boss, Health playerHealth, GameObject goal, GameObject bossBar)
    {
        Vector2 doorCenter = Level3Layout.ToWorld(Level3Layout.ArenaDoor, out Vector2 doorSize);
        var door = new GameObject("Puerta de la arena") { layer = LayerMask.NameToLayer("Ground") };
        door.transform.SetParent(parent, false);
        door.transform.position = doorCenter;
        door.AddComponent<BoxCollider2D>().size = doorSize;
        RectInt d = Level3Layout.ArenaDoor;
        for (int x = 0; x < d.width; x++)
            for (int y = 0; y < d.height; y++)
            {
                var piece = LevelKit.SpriteChild(door.transform, metal, new Vector2((x - (d.width - 1) * 0.5f) * Cell, (y - (d.height - 1) * 0.5f) * Cell),
                    Cell / metal.bounds.size.x, order: 1);
                piece.color = new Color(0.7f, 0.65f, 0.6f);
            }

        Vector2 triggerCenter = Level3Layout.ToWorld(Level3Layout.ArenaTrigger, out Vector2 triggerSize);
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

    }

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

    // ---------- Utilidades ----------

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


}
