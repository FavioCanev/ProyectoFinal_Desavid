using UnityEngine;

namespace Game
{
    /// <summary>
    /// Trazado del Nivel 1 (bosque de Dante) en celdas de SU mapa de losetas (cada celda mide 1,11 unidades).
    /// Las columnas -38 a 36 son el tramo original de Dante (se conserva tal cual); de la 40 en adelante, la ampliación.
    /// Es el nivel tutorial: enseña cada control con un cartel justo antes de necesitarlo.
    /// Medidas del jugador en celdas de este mapa: salta unas 2,7 de alto y 5,7 de largo; con dash en el aire, unas 8,5.
    /// </summary>
    public static class Level1Layout
    {
        public const float CellWorld = 1.11f;

        /// <summary>Bloque de tierra (columnas X0-X1, filas Y0-Y1). La fila Y1 es la superficie.</summary>
        public readonly struct Block
        {
            public readonly int X0, X1, Y0, Y1;
            public Block(int x0, int x1, int y0, int y1) { X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; }
        }

        public const int Bottom = -20;   // fondo de los bloques de tierra; más abajo está el vacío

        public static readonly Block[] Blocks =
        {
            // Tramo 2: escuela de dash (el hueco 48-54 mide 7 celdas = 7,8 unidades: imposible sin dash)
            new Block(40, 47, Bottom, -5),
            new Block(55, 72, Bottom, -5),
            // Tramo 3: escalera de plataformas sobre el vacío hasta la meseta
            new Block(75, 78, -3, -3),
            new Block(80, 83, -1, -1),
            new Block(85, 88, 1, 1),
            new Block(91, 104, Bottom, 1),
            // Tramo 4: bajada al claro de los leñadores (rebote sobre ellos) y final
            new Block(107, 148, Bottom, -6),
            new Block(114, 117, -2, -2),          // plataforma con monedas (se pasa por debajo)
            new Block(149, 152, Bottom, 8),       // pared final
        };

        /// <summary>Leñadores (columna, fila de la superficie donde se apoyan).</summary>
        public static readonly Vector2Int[] Enemies =
        {
            new Vector2Int(10, -8), new Vector2Int(64, -5), new Vector2Int(99, 1),
            new Vector2Int(112, -6), new Vector2Int(121, -6), new Vector2Int(133, -6),
        };

        public static readonly Vector2Int PlayerStart = new Vector2Int(-23, -11);
        public static readonly Vector2Int[] Checkpoints =
        {
            new Vector2Int(-3, -9), new Vector2Int(41, -5), new Vector2Int(92, 1), new Vector2Int(108, -6),
        };

        public static readonly (Vector2Int Cell, int Count)[] CoinRows =
        {
            (new Vector2Int(-30, -11), 3), (new Vector2Int(-11, -10), 2), (new Vector2Int(3, -9), 3), (new Vector2Int(4, -2), 3),
            (new Vector2Int(31, -5), 3), (new Vector2Int(49, -3), 5), (new Vector2Int(58, -5), 4), (new Vector2Int(75, -3), 3),
            (new Vector2Int(80, -1), 3), (new Vector2Int(85, 1), 3), (new Vector2Int(95, 1), 4), (new Vector2Int(114, -2), 4),
            (new Vector2Int(125, -6), 4), (new Vector2Int(138, -6), 3),
        };

        /// <summary>Carteles del tutorial: cada control justo antes de necesitarlo.</summary>
        public static readonly (Vector2Int Cell, string Text)[] Signs =
        {
            (new Vector2Int(-33, -11), "Mover: A/D o ←/→\nSaltar: Espacio"),
            (new Vector2Int(-19, -11), "Junta monedas: valen\nen todo el juego"),
            (new Vector2Int(0, -9), "Las banderas guardan\ntu avance (3 vidas)"),
            (new Vector2Int(44, -5), "Shift: dash\n(también en el aire)"),
            (new Vector2Int(57, -5), "} o Supr: atacar\n↑ + ataque: hacia arriba"),
            (new Vector2Int(101, 1), "En el aire: ↓ + ataque\nsobre un enemigo = rebote"),
            (new Vector2Int(127, -6), "Fin: plasma (desde el nivel 2)\nE: zambullirse · Esc: pausa"),
        };

        public static readonly Vector2Int Goal = new Vector2Int(145, -6);
        /// <summary>Robot gigante de Dante: decorado junto a la meta.</summary>
        public static readonly Vector2Int RobotDecor = new Vector2Int(140, -6);

        /// <summary>Rectángulo que la cámara no puede abandonar (en celdas).</summary>
        public static readonly RectInt CameraBounds = new RectInt(-38, -17, 191, 30);
        /// <summary>Caer por debajo de esta fila cuesta un corazón y devuelve a la última bandera.</summary>
        public const int VoidRow = -19;
    }
}
