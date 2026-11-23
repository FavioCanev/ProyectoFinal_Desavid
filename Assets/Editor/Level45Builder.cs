using System.Linq;
using Game;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Niveles 4 (Minertown, la calle) y 5 (La Mansión, con el jefe final), descritos en celdas de 1,2 unidades
/// (la fila -1 tiene su borde superior en y = 0). Dificultad y reparto como los otros niveles: ~200 unidades,
/// 4 banderas, enemigos de cuerpo a cuerpo y de distancia, fosos de salto y de dash, alturas con monedas.
/// Menú: Game > Rebuild Levels 4 y 5.
/// </summary>
public static class Level45Builder
{
    private const string L4 = "Assets/Niveles/4_Minertown/";
    private const string L5 = "Assets/Niveles/5_Mansion/";
    public const string Level4Path = L4 + "Nivel4.unity";
    public const string Level5Path = L5 + "Nivel5.unity";

    private static RectInt R(int x0, int x1, int y0, int y1) => new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);

    [MenuItem("Game/Rebuild Levels 4 y 5 (sobrescribe Nivel4 y Nivel5)")]
    public static void BuildAll()
    {
        TestRoomBuilder.PrepareAssets(out _, out GameObject playerPrefab, out PhysicsMaterial2D noFriction);
        LevelKit.PrepareSharedArt();
        MinerAssetsBuilder.Prefabs enemies = MinerAssetsBuilder.Prepare(noFriction);
        TiledLevelBuilder.Build(Level4(enemies), playerPrefab);
        TiledLevelBuilder.Build(Level5(enemies), playerPrefab);
        LevelKit.SetBuildScenes();
        AssetDatabase.SaveAssets();
    }

    // ---------- Nivel 4: Minertown ----------

    private static TiledLevelBuilder.Level Level4(MinerAssetsBuilder.Prefabs e)
    {
        string art = L4 + "Art/Tileset/";
        string[] balcony = { art + "balcon_1.png", art + "balcon_2.png" };
        string P(string n) => art + n + ".png";
        GameObject pick = e.Pickaxe, dyn = e.Dynamite;
        return new TiledLevelBuilder.Level
        {
            ScenePath = Level4Path, TileArt = art, TileSet = "calle",
            Title = "NIVEL 4\n<size=34>Minertown</size>",
            CompletedTitle = "¡Nivel completado!", CompletedText = "Llegaste a la mansión\nde la minera.\n\n<size=28>Siguiente: La Mansión</size>",
            Blocks = new[]
            {
                R(0, 45, -7, -1), R(50, 92, -7, -1), R(100, 166, -7, -1),            // vereda: alcantarilla (4) y hueco (7)
                R(-3, -1, -7, 22), R(167, 169, -7, 22),                              // paredes de los extremos
                R(14, 18, 0, 1), R(26, 31, 0, 1), R(60, 66, 0, 3), R(80, 84, 0, 1),  // muros para subir
                R(118, 126, 0, 2), R(140, 144, 0, 1),
            },
            Platforms = new[]
            {
                (29, 4, balcony, 0.25f), (56, 2, balcony, 0.25f), (70, 6, balcony, 0.25f), (76, 8, balcony, 0.25f),
                (122, 5, balcony, 0.25f), (133, 3, balcony, 0.25f), (150, 4, balcony, 0.25f),
            },
            Enemies = new[]
            {
                (pick, 22, 0), (dyn, 30, 2), (pick, 40, 0), (dyn, 63, 4), (pick, 74, 0),
                (dyn, 122, 3), (pick, 110, 0), (pick, 135, 0), (dyn, 142, 2),
            },
            Checkpoints = new[] { (2, 0), (51, 0), (101, 0), (146, 0) },
            Coins = new[]
            {
                (8, 0, 4), (15, 2, 3), (27, 2, 3), (28, 4, 2), (46, 2, 3), (55, 2, 2), (61, 4, 4), (69, 6, 2), (75, 8, 3),
                (86, 0, 4), (94, 3, 5), (104, 0, 3), (119, 3, 4), (121, 5, 2), (132, 3, 2), (141, 2, 3), (149, 4, 2), (156, 0, 3),
            },
            Decor = new[]
            {
                (P("farol"), 6f, 0f, 1.3f, -3), (P("banca"), 11f, 0f, 1f, -3), (P("basura"), 21f, 0f, 0.9f, -3), (P("farol"), 36f, 0f, 1.3f, -3),
                (P("cajas"), 43f, 0f, 1f, -3), (P("farol"), 53f, 0f, 1.3f, -3), (P("basura"), 77f, 0f, 0.9f, -3), (P("banca"), 88f, 0f, 1f, -3),
                (P("farol"), 103f, 0f, 1.3f, -3), (P("cajas"), 113f, 0f, 1f, -3), (P("farol"), 130f, 0f, 1.3f, -3), (P("basura"), 138f, 0f, 0.9f, -3),
                (P("reja_izq"), 151f, 0f, 1f, -4), (P("porton"), 153.5f, 0f, 1f, -4), (P("reja_der"), 156f, 0f, 1f, -4),
                (L4 + "Art/Fondos/minertown_fachada.png", 159.5f, -1.3f, 3.4f, -6),
            },
            PlayerStart = new Vector2Int(4, 0), Goal = new Vector2Int(161, 0),
            CameraBounds = new RectInt(-3, -3, 173, 20),
            Background = L4 + "Art/Fondos/calle_espejo.png", BackgroundTint = new Color(0.8f, 0.82f, 0.9f), Sky = new Color(0.04f, 0.05f, 0.1f),
        };
    }

    // ---------- Nivel 5: La Mansión ----------

    private static TiledLevelBuilder.Level Level5(MinerAssetsBuilder.Prefabs e)
    {
        string art = L5 + "Art/Tileset/";
        string[] beam = Enumerable.Range(0, 4).Select(i => $"{art}viga_{i}.png").ToArray();
        string[] balcony = { art + "balcon_0.png", art + "balcon_1.png", art + "balcon_2.png" };
        string P(string n) => art + n + ".png";
        GameObject pick = e.Pickaxe, dyn = e.Dynamite;
        return new TiledLevelBuilder.Level
        {
            ScenePath = Level5Path, TileArt = art, TileSet = "mansion",
            Title = "NIVEL 5\n<size=34>La Mansión</size>",
            CompletedTitle = "¡Juego completado!",
            CompletedText = "Detuviste al jefe de la minera\nque contaminaba la selva.\n\n<size=28>Volviendo al menú...</size>",
            Blocks = new[]
            {
                R(0, 58, -7, -1), R(64, 104, -7, -1), R(112, 170, -7, -1),          // piso roto: huecos de 5 y 7
                R(-3, -1, -7, 24), R(171, 173, -7, 24),
                R(10, 13, 0, 1), R(14, 17, 0, 3), R(18, 21, 0, 5),                   // gran escalera
                R(70, 76, 0, 2), R(90, 95, 0, 1),
                R(131, 135, 0, 1), R(164, 170, 0, 1),                                // repisas de la arena
            },
            Platforms = new[]
            {
                (25, 8, balcony, 0.2f), (31, 8, balcony, 0.2f), (37, 8, balcony, 0.2f),   // balcón alto tras la escalera
                (61, 2, beam, 0.97f), (80, 6, balcony, 0.2f), (108, 3, beam, 0.97f),
                (142, 5, beam, 0.97f), (150, 8, beam, 0.97f), (158, 5, beam, 0.97f),      // vigas de la arena (sobre la altura del jefe)
            },
            Enemies = new[]
            {
                (pick, 30, 0), (dyn, 20, 6), (dyn, 37, 8), (pick, 45, 0), (pick, 68, 0),
                (dyn, 73, 3), (pick, 98, 0), (dyn, 118, 0), (pick, 122, 0),
            },
            Checkpoints = new[] { (2, 0), (65, 0), (113, 0), (126, 0) },
            Coins = new[]
            {
                (6, 0, 3), (11, 2, 2), (15, 4, 2), (19, 6, 3), (24, 8, 3), (30, 8, 3), (36, 8, 3), (50, 0, 4), (60, 2, 3),
                (72, 3, 4), (79, 6, 3), (92, 2, 3), (100, 0, 3), (107, 3, 3), (116, 0, 3), (141, 5, 3), (149, 8, 4), (157, 5, 3),
            },
            Decor = new[]
            {
                (P("estatua"), 4f, 0f, 1.4f, -3), (P("armadura"), 26f, 0f, 1.1f, -3), (P("cortina"), 33f, 0f, 1.6f, -5), (P("armadura"), 40f, 0f, 1.1f, -3),
                (P("estatua"), 55f, 0f, 1.4f, -3), (P("cortina"), 67f, 0f, 1.6f, -5), (P("armadura"), 84f, 0f, 1.1f, -3), (P("estatua"), 101f, 0f, 1.4f, -3),
                (P("cortina"), 115f, 0f, 1.6f, -5), (P("armadura"), 129f, 0f, 1.1f, -3), (P("estatua"), 145f, 0f, 1.4f, -3), (P("estatua"), 155f, 0f, 1.4f, -3),
            },
            Hanging = new[]
            {
                (P("candelabro"), 15f, 16f, 1.5f), (P("candelabro"), 48f, 16f, 1.5f), (P("candelabro"), 86f, 16f, 1.5f),
                (P("candelabro"), 120f, 16f, 1.5f), (P("candelabro"), 145f, 18f, 1.8f), (P("candelabro"), 156f, 18f, 1.8f),
            },
            PlayerStart = new Vector2Int(3, 0), Goal = new Vector2Int(167, 2),
            CameraBounds = new RectInt(-3, -3, 177, 22),
            Background = L5 + "Art/Fondos/mansion_espejo.png", BackgroundTint = new Color(0.78f, 0.78f, 0.82f), Sky = new Color(0.05f, 0.05f, 0.08f),
            Boss = e.Boss, BossName = "JEFE MINERO", BossSpawn = new Vector2Int(152, 0),
            ArenaTrigger = R(133, 169, 0, 14), ArenaDoor = R(128, 129, 0, 9), DoorPiece = art + "mansion_F1.png",
        };
    }
}
