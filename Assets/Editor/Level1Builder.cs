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
/// Construye la escena Nivel1 completa a partir de Level1Layout: terreno con tiles, jugador, enemigos,
/// puntos de reaparición, zona de caída, meta, cámara y HUD. Menú: Game.
/// </summary>
public static class Level1Builder
{
    public const string ScenePath = "Assets/Scenes/Nivel1.unity";
    private const string TileFolder = "Assets/Art/Terrain/terreno-removebg-preview_";

    // Losetas elegidas del tileset (por número).
    private static readonly int[] SurfaceTiles = { 0, 1, 5 };
    private static readonly int[] FillTiles = { 4, 10, 14, 9, 31 };
    private static readonly int[] MetalTiles = { 34, 35 };
    private static readonly int[] BeamTiles = { 40 };

    // Las losetas tienen bordes transparentes: sin un respaldo, el fondo brillante se cuela entre ellas.
    private const string BackingTilePath = "Assets/Data/BackingTile.asset";
    private static readonly Color BackingColor = new Color(0.12f, 0.09f, 0.07f);

    [MenuItem("Game/Rebuild Level 1 (sobrescribe Nivel1)")]
    public static void Build()
    {
        TestRoomBuilder.PrepareAssets(out Sprite white, out GameObject playerPrefab, out GameObject enemyPrefab);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TestRoomBuilder.AddGlobalLight();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        grid.cellSize = new Vector3(Level1Layout.Cell, Level1Layout.Cell, 0f);
        var ground = GroundTilemapFactory.Create(grid.transform, "Ground", LayerMask.NameToLayer("Ground"), sortingOrder: 0);
        var backing = TestRoomBuilder.NewTilemap(grid.transform, "Ground Backing", sortingOrder: -1);
        backing.color = BackingColor;
        TestRoomBuilder.NewTilemap(grid.transform, "Decor", sortingOrder: -5);   // vacía: para pintar adornos a mano
        PaintTerrain(ground, backing, EnsureBackingTile(white));   // los tiles se cargan DESPUÉS de NewScene

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = Level1Layout.OnSurface(Level1Layout.PlayerStart) + Vector2.up * 0.02f;

        var enemies = new GameObject("Enemigos").transform;
        foreach (var cell in Level1Layout.Enemies)
        {
            var e = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, enemies);
            e.transform.position = Level1Layout.OnSurface(cell) + Vector2.up * 0.02f;
        }

        var objects = new GameObject("Objetos").transform;
        for (int i = 0; i < Level1Layout.Checkpoints.Length; i++)
            CreateCheckpoint(objects, white, $"Punto de reaparición {i + 1}", Level1Layout.Checkpoints[i]);
        CreateKillZone(objects);

        RectInt b = Level1Layout.CameraBounds;
        var boundsCenter = new Vector2((b.xMin + b.width * 0.5f) * Level1Layout.Cell, (b.yMin + b.height * 0.5f) * Level1Layout.Cell);
        Vector2 boundsSize = new Vector2(b.width, b.height) * Level1Layout.Cell;
        TestRoomBuilder.AddCameraRig(player.transform, boundsCenter, boundsSize);
        CreateBackground(new Rect(boundsCenter - boundsSize * 0.5f, boundsSize));

        GameObject completedMessage = CreateHud(white, player.GetComponent<Health>());
        CreateGoal(objects, white, completedMessage);

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
        tile.transform = Matrix4x4.Scale(new Vector3(Level1Layout.Cell, Level1Layout.Cell, 1f));
        tile.flags = TileFlags.LockTransform;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static TileBase[] LoadTiles(int[] numbers) =>
        numbers.Select(n => AssetDatabase.LoadAssetAtPath<TileBase>($"{TileFolder}{n}.asset"))
               .Where(t => t != null).ToArray();

    private static TileBase Pick(TileBase[] tiles, System.Random rng) => tiles[rng.Next(tiles.Length)];

    // ---------- Fondo ----------

    private const string BackgroundPath = "Assets/Art/Background/pantano.jpg";

    private static void CreateBackground(Rect levelBounds)
    {
        TestRoomBuilder.ConfigureSprite(BackgroundPath, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        var go = new GameObject("Fondo");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        sr.sortingOrder = -100;
        var parallax = go.AddComponent<ParallaxBackground>();
        var so = new SerializedObject(parallax);
        so.FindProperty("levelBounds").rectValue = levelBounds;
        so.ApplyModifiedPropertiesWithoutUndo();
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

    private static void CreateGoal(Transform parent, Sprite white, GameObject completedMessage)
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
    }

    private static void CreateKillZone(Transform parent)
    {
        RectInt b = Level1Layout.CameraBounds;
        float cell = Level1Layout.Cell;
        var go = new GameObject("Zona de caída");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector2((b.xMin + b.width * 0.5f) * cell, (Level1Layout.KillZoneTopRow - 2) * cell);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(b.width * cell + 10f, 4f * cell);
        go.AddComponent<KillZone>();
    }

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

    // ---------- HUD ----------

    /// <summary>Crea el HUD (vida y controles) y devuelve el mensaje de nivel completado (oculto).</summary>
    private static GameObject CreateHud(Sprite white, Health playerHealth)
    {
        var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var masks = new Image[playerHealth.Max];
        for (int i = 0; i < masks.Length; i++)
        {
            var rt = NewUi($"Máscara {i + 1}", canvasGo.transform, new Vector2(0f, 1f), new Vector2(40f + i * 56f, -36f), new Vector2(44f, 44f));
            masks[i] = rt.gameObject.AddComponent<Image>();
            masks[i].sprite = white;
        }
        var hud = canvasGo.AddComponent<HealthHUD>();
        TestRoomBuilder.Set(hud, "health", playerHealth);
        var so = new SerializedObject(hud);
        var arr = so.FindProperty("masks");
        arr.arraySize = masks.Length;
        for (int i = 0; i < masks.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = masks[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var help = NewText("Controles", canvasGo.transform, font, 24, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.75f),
            new Vector2(0f, 0f), new Vector2(40f, 30f), new Vector2(1500f, 40f),
            "Mover: A/D o ←/→    Saltar: Espacio (mantén para saltar más)    Atacar: Z    (↑ + Z arriba, en el aire ↓ + Z abajo)");
        help.rectTransform.pivot = Vector2.zero;

        var message = NewText("Nivel completado", canvasGo.transform, font, 64, TextAnchor.MiddleCenter, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 240f),
            "¡Nivel completado!\n<size=36>Pulsa R para volver a empezar</size>");
        message.gameObject.SetActive(false);
        return message.gameObject;
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
