using System.Collections.Generic;
using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Suelos de losetas de los tilesets hechos con Gemini (niveles 2, 4 y 5) y plataformas de un solo sentido.
/// Cada loseta mide 144 px = 1,2 unidades. Claves de las losetas ("{set}_{clave}.png" en la carpeta del arte):
/// TL/T1/T2/TR arriba, L/R lados, F1-F3 relleno, H1-H3 relleno justo bajo la superficie, BL/B1/B2/BR abajo.
/// </summary>
public static class TilePainter
{
    public const float TileSize = 1.2f;
    public const int PixelsPerUnit = 120;

    private static TileBase LoadTile(string artFolder, string set, string key)
    {
        string assetPath = $"{artFolder}Tiles/{set}_{key}.asset";
        TestRoomBuilder.EnsureFolder(artFolder + "Tiles");
        TestRoomBuilder.ConfigureSprite($"{artFolder}{set}_{key}.png", new Vector2(0.5f, 0.5f), FilterMode.Point, PixelsPerUnit);   // sin suavizado: no se ven costuras
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, assetPath); }
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{artFolder}{set}_{key}.png");
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    /// <summary>Pinta los bloques (en celdas) en un Tilemap con colisión, eligiendo cada loseta según sus vecinas.</summary>
    public static Tilemap Paint(string artFolder, string set, string name, RectInt[] blocks)
    {
        var grid = new GameObject("Grid " + name).AddComponent<Grid>();
        grid.cellSize = new Vector3(TileSize, TileSize, 0f);
        var map = GroundTilemapFactory.Create(grid.transform, name, LayerMask.NameToLayer("Ground"), sortingOrder: 1);
        TileBase T(string key) => LoadTile(artFolder, set, key);
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
        return map;
    }

    /// <summary>
    /// Plataforma de un solo sentido (se sube desde abajo) hecha con piezas en fila (extremo, centros, extremo).
    /// "top" es la altura donde se apoya el jugador; "pivotY" es la altura del borde de apoyo dentro de cada pieza (0-1).
    /// </summary>
    public static GameObject OneWayPlatform(Transform parent, string name, float centerX, float top, string[] piecePaths, float pivotY)
    {
        var go = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(centerX, top, 0f);
        float width = piecePaths.Length * TileSize;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(width - 0.2f, 0.35f);
        box.offset = new Vector2(0f, -0.18f);
        box.usedByEffector = true;
        go.AddComponent<PlatformEffector2D>().surfaceArc = 160f;
        for (int i = 0; i < piecePaths.Length; i++)
        {
            TestRoomBuilder.ConfigureSprite(piecePaths[i], new Vector2(0.5f, pivotY), FilterMode.Bilinear, PixelsPerUnit);
            LevelKit.SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(piecePaths[i]),
                new Vector2((i - (piecePaths.Length - 1) * 0.5f) * TileSize, 0f), 1f, order: 1);
        }
        return go;
    }

    /// <summary>Zona invisible bajo los fosos: caer cuesta un corazón y devuelve a la última bandera.</summary>
    public static void Void(Transform parent, float x0, float x1, float y)
    {
        var go = new GameObject("Vacío");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector2((x0 + x1) * 0.5f, y);
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(x1 - x0, 4f);
        go.AddComponent<KillZone>();
    }
}
