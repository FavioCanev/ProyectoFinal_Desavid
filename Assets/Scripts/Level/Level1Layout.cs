using UnityEngine;

namespace Game
{
    /// <summary>
    /// Trazado del Nivel 1 en celdas de la cuadrícula. Lo usan el constructor del nivel y las pruebas,
    /// así que hay una sola fuente de verdad. Fila y = 0 es el suelo inicial; su borde superior está en y = 0,45.
    /// </summary>
    public static class Level1Layout
    {
        public const float Cell = 0.45f;

        public enum Kind { Earth, Metal, Beam }

        public readonly struct Block
        {
            public readonly int X0, X1, Y0, Y1;
            public readonly Kind Kind;
            public Block(int x0, int x1, int y0, int y1, Kind kind) { X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; Kind = kind; }
        }

        public static readonly Block[] Blocks =
        {
            new Block(-14, -11, -6, 30, Kind.Earth),   // pared izquierda
            new Block(-10, 40, -6, 0, Kind.Earth),     // suelo inicial
            new Block(22, 30, 1, 2, Kind.Earth),       // escalón
            new Block(44, 47, 1, 1, Kind.Metal),       // plataforma sobre el primer foso
            new Block(51, 100, -6, 0, Kind.Earth),     // suelo del primer enemigo
            new Block(86, 90, 4, 4, Kind.Metal),       // subida 1
            new Block(93, 97, 8, 8, Kind.Metal),       // subida 2
            new Block(101, 140, -6, 12, Kind.Earth),   // zona alta
            new Block(145, 148, 13, 13, Kind.Beam),    // vigas sobre el vacío
            new Block(153, 156, 14, 14, Kind.Beam),
            new Block(161, 164, 13, 13, Kind.Beam),
            new Block(171, 200, -6, 12, Kind.Earth),   // zona final
            new Block(201, 204, -6, 30, Kind.Earth),   // pared derecha
        };

        // Posiciones: (columna, fila de la superficie sobre la que se apoya).
        public static readonly Vector2Int PlayerStart = new Vector2Int(-6, 0);
        public static readonly Vector2Int[] Enemies = { new Vector2Int(72, 0), new Vector2Int(125, 12), new Vector2Int(186, 12) };
        public static readonly Vector2Int[] Checkpoints = { new Vector2Int(55, 0), new Vector2Int(105, 12), new Vector2Int(174, 12) };
        public static readonly Vector2Int Goal = new Vector2Int(196, 12);

        /// <summary>Un punto en el aire sobre el primer foso (sin plataforma debajo).</summary>
        public static readonly Vector2Int PitProbe = new Vector2Int(42, 4);

        /// <summary>Por debajo de esta fila está la zona de caída mortal.</summary>
        public const int KillZoneTopRow = -8;

        /// <summary>Rectángulo que la cámara no puede abandonar (en celdas).</summary>
        public static readonly RectInt CameraBounds = new RectInt(-14, -6, 219, 37);

        /// <summary>Punto apoyado sobre la superficie de la celda (para colocar personajes y objetos).</summary>
        public static Vector2 OnSurface(Vector2Int c) => new Vector2((c.x + 0.5f) * Cell, (c.y + 1) * Cell);
    }
}
