using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Construye un nivel de losetas (niveles 4 y 5) a partir de su descripción: suelo, plataformas de un solo sentido,
/// enemigos, monedas, banderas, decorado, fondo con parallax y, si tiene, la arena del jefe.
/// Medidas en celdas de 1,2 unidades: la fila -1 tiene su borde superior en y = 0 (el suelo).
/// </summary>
public static class TiledLevelBuilder
{
    public const float Cell = TilePainter.TileSize;

    public class Level
    {
        public string ScenePath, TileArt, TileSet, Title, CompletedTitle, CompletedText;
        public RectInt[] Blocks;
        public (int cell, int row, string[] pieces, float pivotY)[] Platforms = { };
        public (GameObject prefab, int cell, int row)[] Enemies = { };
        public (int cell, int row)[] Checkpoints = { };
        public (int cell, int row, int count)[] Coins = { };
        /// <summary>Decorado: sprite, celda, fila de la superficie (pies), escala, orden, capa de dibujo.</summary>
        public (string sprite, float cell, float row, float scale, int order)[] Decor = { };
        public (string sprite, float cell, float y, float scale)[] Hanging = { };
        public Vector2Int PlayerStart, Goal;
        public RectInt CameraBounds;
        public string Background;
        public Color BackgroundTint = Color.white, Sky = Color.black;
        // Arena del jefe (opcional)
        public GameObject Boss;
        public string BossName;
        public Vector2Int BossSpawn;
        public RectInt ArenaTrigger, ArenaDoor;
        public string DoorPiece;
    }

    public static Vector2 Surface(int cell, int row) => new Vector2((cell + 0.5f) * Cell, row * Cell);

    public static void Build(Level L, GameObject playerPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TestRoomBuilder.AddGlobalLight();
        TilePainter.Paint(L.TileArt, L.TileSet, "Suelo", L.Blocks);

        var player = LevelKit.CreatePlayer(playerPrefab, Surface(L.PlayerStart.x, L.PlayerStart.y));
        var objects = new GameObject("Objetos").transform;
        int p = 0;
        foreach (var (cell, row, pieces, pivotY) in L.Platforms)
            TilePainter.OneWayPlatform(objects, $"Plataforma {++p}", (cell + 0.5f) * Cell, row * Cell, pieces, pivotY);
        RectInt b = L.CameraBounds;
        TilePainter.Void(objects, (b.xMin - 10) * Cell, (b.xMax + 10) * Cell, (b.yMin - 4) * Cell);

        var enemies = new GameObject("Enemigos").transform;
        foreach (var (prefab, cell, row) in L.Enemies)
            ((GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies)).transform.position = Surface(cell, row) + Vector2.up * 0.02f;
        for (int i = 0; i < L.Checkpoints.Length; i++)
            LevelKit.CreateCheckpoint(objects, $"Punto de control {i + 1}", Surface(L.Checkpoints[i].cell, L.Checkpoints[i].row));
        foreach (var (cell, row, count) in L.Coins)
            LevelKit.CreateCoinRow(objects, Surface(cell, row) + Vector2.up * 0.6f, count);

        var decor = new GameObject("Decorado").transform;
        foreach (var (sprite, cell, row, scale, order) in L.Decor)
            Prop(decor, sprite, new Vector2((cell + 0.5f) * Cell, row * Cell), scale, order, hanging: false);
        foreach (var (sprite, cell, y, scale) in L.Hanging)
            Prop(decor, sprite, new Vector2((cell + 0.5f) * Cell, y), scale, -2, hanging: true);

        Rect bounds = Rect.MinMaxRect(b.xMin * Cell, b.yMin * Cell, b.xMax * Cell, b.yMax * Cell);
        Camera cam = TestRoomBuilder.AddCameraRig(player.transform, bounds.center, bounds.size);
        cam.backgroundColor = L.Sky;
        Background(L, bounds);

        EnemyController boss = null;
        if (L.Boss != null)
        {
            boss = ((GameObject)PrefabUtility.InstantiatePrefab(L.Boss, enemies)).GetComponent<EnemyController>();
            boss.transform.position = Surface(L.BossSpawn.x, L.BossSpawn.y) + Vector2.up * 0.02f;
        }
        LevelKit.Hud hud = LevelKit.CreateHud(player.GetComponent<PlayerController>(), L.Title, L.CompletedTitle, L.CompletedText,
            boss != null ? L.BossName : null, boss != null ? boss.GetComponent<Health>() : null);
        FeelAssetsBuilder.CreateGameFeel(hud.DamageFlash);
        GameObject goal = LevelKit.CreateGoal(objects, Surface(L.Goal.x, L.Goal.y), hud.CompletedMessage);
        if (boss != null) Arena(L, objects, boss, player.GetComponent<Health>(), goal, hud.BossBar);

        EditorSceneManager.SaveScene(scene, L.ScenePath);
        // Un suelo de Tilemap guardado en la misma sesión queda sin colisión: se recarga y se recrea el compuesto.
        EditorSceneManager.OpenScene(L.ScenePath);
        foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (map.GetComponent<CompositeCollider2D>() != null) GroundTilemapFactory.RebuildComposite(map.gameObject);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("TiledLevelBuilder: construido " + L.ScenePath);
    }

    /// <summary>Puerta que se cierra al entrar y zona que despierta al jefe (el mismo sistema que la arena del nivel 3).</summary>
    private static void Arena(Level L, Transform parent, EnemyController boss, Health playerHealth, GameObject goal, GameObject bossBar)
    {
        RectInt d = L.ArenaDoor;
        var door = new GameObject("Puerta de la arena") { layer = LayerMask.NameToLayer("Ground") };
        door.transform.SetParent(parent, false);
        door.transform.position = new Vector2((d.x + d.width * 0.5f) * Cell, (d.y + d.height * 0.5f) * Cell);
        door.AddComponent<BoxCollider2D>().size = new Vector2(d.width, d.height) * Cell;
        Sprite piece = AssetDatabase.LoadAssetAtPath<Sprite>(L.DoorPiece);
        for (int x = 0; x < d.width; x++)
            for (int y = 0; y < d.height; y++)
                LevelKit.SpriteChild(door.transform, piece, new Vector2((x - (d.width - 1) * 0.5f) * Cell, (y - (d.height - 1) * 0.5f) * Cell), 1f, order: 2)
                    .color = new Color(0.75f, 0.7f, 0.68f);

        RectInt t = L.ArenaTrigger;
        var arenaGo = new GameObject("Arena del jefe");
        arenaGo.transform.SetParent(parent, false);
        arenaGo.transform.position = new Vector2((t.x + t.width * 0.5f) * Cell, (t.y + t.height * 0.5f) * Cell);
        var trigger = arenaGo.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(t.width, t.height) * Cell;
        var arena = arenaGo.AddComponent<BossArena>();
        TestRoomBuilder.Set(arena, "boss", boss);
        TestRoomBuilder.Set(arena, "playerHealth", playerHealth);
        TestRoomBuilder.Set(arena, "door", door);
        TestRoomBuilder.Set(arena, "goal", goal);
        TestRoomBuilder.Set(arena, "healthBar", bossBar);
    }

    /// <summary>Fondo pintado repetido a lo ancho (la imagen ya viene pegada a su espejo: no hay costuras).</summary>
    private static void Background(Level L, Rect levelRect)
    {
        TestRoomBuilder.ConfigureSprite(L.Background, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        var go = new GameObject("Fondo");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(L.Background);
        sr.color = L.BackgroundTint;
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -100;
        var layer = go.AddComponent<ParallaxLayer>();
        var so = new SerializedObject(layer);
        so.FindProperty("follow").vector2Value = new Vector2(0.85f, 0.9f);
        so.FindProperty("anchor").enumValueIndex = (int)ParallaxLayer.Anchor.LevelCenter;
        so.FindProperty("fitToCover").boolValue = true;
        so.FindProperty("levelBounds").rectValue = levelRect;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Pieza de decorado con el pivote abajo (apoyada) o arriba (colgada). Sin colisión.</summary>
    public static SpriteRenderer Prop(Transform parent, string sprite, Vector2 at, float scale, int order, bool hanging)
    {
        TestRoomBuilder.ConfigureSprite(sprite, new Vector2(0.5f, hanging ? 1f : 0f), FilterMode.Bilinear, TilePainter.PixelsPerUnit);
        var sr = LevelKit.SpriteChild(parent, AssetDatabase.LoadAssetAtPath<Sprite>(sprite), Vector2.zero, scale, order);
        sr.transform.position = at;
        sr.name = System.IO.Path.GetFileNameWithoutExtension(sprite);
        return sr;
    }
}
