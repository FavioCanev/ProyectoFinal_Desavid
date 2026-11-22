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
    private static readonly Color ForegroundColor = new Color(1f, 1f, 1f, 0.9f);

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
            CreateCheckpoint(objects, white, $"Punto de reaparición {i + 1}", Level1Layout.Checkpoints[i]);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var sign in Level1Layout.Signs) CreateSign(objects, white, font, sign);
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

        Hud hud = CreateHud(white, font, playerHealth, boss.GetComponent<Health>());
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

    private static void CreateAcid(Transform parent, Sprite white)
    {
        RectInt b = Level1Layout.CameraBounds;
        float left = (b.xMin - 10) * Cell, right = (b.xMax + 10) * Cell;
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

    private static void CreateCheckpoint(Transform parent, Sprite white, string name, Vector2Int cell)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = Level1Layout.OnSurface(cell);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 2.4f);
        trigger.offset = new Vector2(0f, 1.2f);

        var marker = Marker(go.transform, white, new Vector2(0.18f, 1.3f), new Color(0.5f, 0.55f, 0.6f));
        TestRoomBuilder.Set(go.AddComponent<Checkpoint>(), "marker", marker);
    }

    private static void CreateSign(Transform parent, Sprite white, Font font, Level1Layout.Sign sign)
    {
        var go = new GameObject("Cartel");
        go.transform.SetParent(parent, false);
        go.transform.position = Level1Layout.OnSurface(sign.Cell);
        SpriteBox(go.transform, white, "Poste", (Vector2)go.transform.position + new Vector2(0f, 0.45f), new Vector2(0.08f, 0.9f), new Color(0.35f, 0.25f, 0.18f), order: 2);
        SpriteBox(go.transform, white, "Tabla", (Vector2)go.transform.position + new Vector2(0f, 1.35f), new Vector2(3.6f, 0.85f), new Color(0.08f, 0.1f, 0.08f, 0.8f), order: 3);

        var textGo = new GameObject("Texto");
        textGo.transform.SetParent(go.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        var text = textGo.AddComponent<TextMesh>();
        text.text = sign.Text;
        text.font = font;
        text.fontSize = 64;
        text.characterSize = 0.045f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(0.85f, 1f, 0.75f);
        var renderer = textGo.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = 4;
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

    /// <summary>Siluetas oscuras delante del jugador que pasan más rápido que el nivel (profundidad).</summary>
    private static void CreateForeground()
    {
        var root = new GameObject("Primer plano").transform;
        Sprite moss = AssetDatabase.LoadAssetAtPath<Sprite>(ForegroundFolder + "fg_moss.png");
        Sprite reeds = AssetDatabase.LoadAssetAtPath<Sprite>(ForegroundFolder + "fg_reeds.png");
        Sprite branch = AssetDatabase.LoadAssetAtPath<Sprite>(ForegroundFolder + "fg_branch.png");

        // (columna, altura en el mundo). El musgo cuelga desde lo alto de la vista de cada tramo.
        var mossSpots = new[] { (5, 9.6f), (60, 9.6f), (120, 9.6f), (190, 16.8f), (240, 16.8f), (300, 13.5f), (330, 11.4f), (380, 9.6f) };
        var reedSpots = new[] { (15, 0.15f), (90, 0.15f), (145, 0.15f), (200, 9.15f), (255, 9.15f), (340, 1.95f), (365, 0.15f) };
        var branchSpots = new[] { (30, 9.8f), (175, 17f), (395, 9.8f) };

        foreach (var (col, y) in mossSpots) Foreground(root, moss, new Vector2(col * Cell, y), 0.7f);
        foreach (var (col, y) in reedSpots) Foreground(root, reeds, new Vector2(col * Cell, y), 0.5f);
        foreach (var (col, y) in branchSpots) Foreground(root, branch, new Vector2(col * Cell, y), 0.8f);
    }

    private static void Foreground(Transform parent, Sprite sprite, Vector2 position, float scale)
    {
        var go = new GameObject(sprite.name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = ForegroundColor;
        sr.sortingOrder = 50;
        var layer = go.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(layer);
        so.FindProperty("follow").vector2Value = new Vector2(-0.3f, -0.1f);
        so.FindProperty("anchor").enumValueIndex = (int)ParallaxLayer.Anchor.OwnPosition;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- HUD ----------

    private static Hud CreateHud(Sprite white, Font font, Health playerHealth, Health bossHealth)
    {
        var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Destello de daño: imagen roja a pantalla completa, transparente hasta que te golpean.
        var flashRt = NewUi("Destello de daño", canvasGo.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        flashRt.anchorMin = Vector2.zero; flashRt.anchorMax = Vector2.one; flashRt.sizeDelta = Vector2.zero;
        var flash = flashRt.gameObject.AddComponent<Image>();
        flash.color = new Color(0.8f, 0f, 0f, 0f);
        flash.raycastTarget = false;

        var masks = new Image[playerHealth.Max];
        for (int i = 0; i < masks.Length; i++)
        {
            var rt = NewUi($"Máscara {i + 1}", canvasGo.transform, new Vector2(0f, 1f), new Vector2(40f + i * 56f, -36f), new Vector2(44f, 44f));
            masks[i] = rt.gameObject.AddComponent<Image>();
            masks[i].sprite = white;
        }
        var hud = canvasGo.AddComponent<HealthHUD>();
        TestRoomBuilder.Set(hud, "health", playerHealth);
        TestRoomBuilder.SetArray(hud, "masks", masks);

        var help = NewText("Controles", canvasGo.transform, font, 24, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.75f),
            new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(1800f, 40f),
            "Mover: A/D o ←/→    Saltar: Espacio    Dash: Shift    Atacar: } o Supr    (↑ + ataque: arriba · en el aire ↓ + ataque: abajo)");
        help.rectTransform.pivot = Vector2.zero;

        var message = NewText("Nivel completado", canvasGo.transform, font, 64, TextAnchor.MiddleCenter, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 240f),
            "¡Nivel completado!\n<size=36>Pulsa R para volver a empezar</size>");
        message.gameObject.SetActive(false);

        return new Hud { DamageFlash = flash, CompletedMessage = message.gameObject, BossBar = CreateBossBar(canvasGo.transform, white, font, bossHealth) };
    }

    private static GameObject CreateBossBar(Transform canvas, Sprite white, Font font, Health bossHealth)
    {
        var root = NewUi("Barra del jefe", canvas, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(720f, 26f));
        var back = root.gameObject.AddComponent<Image>();
        back.sprite = white;
        back.color = new Color(0f, 0f, 0f, 0.65f);

        var fillRt = NewUi("Relleno", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(712f, 18f));
        var fill = fillRt.gameObject.AddComponent<Image>();
        fill.sprite = white;
        fill.color = new Color(0.9f, 0.35f, 0.2f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;

        NewText("Nombre", root, font, 28, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.8f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(720f, 40f), "GRAN COCODRILO");

        var bar = root.gameObject.AddComponent<BossHealthBar>();
        TestRoomBuilder.Set(bar, "health", bossHealth);
        TestRoomBuilder.Set(bar, "fill", fill);
        return root.gameObject;
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
