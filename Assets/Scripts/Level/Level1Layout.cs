using UnityEngine;

namespace Game
{
    /// <summary>
    /// Trazado del Nivel 1 en celdas de la cuadrícula (0,45 unidades). Lo usan el constructor y las pruebas.
    /// Fila y = 0 es el suelo inicial (borde superior en y = 0,45).
    /// Medidas del jugador (ver PlayerConfig): salta unas 6,7 filas de alto y unas 14 columnas de largo;
    /// con dash en el aire, unas 21 columnas. Los huecos "de dash" miden 17: imposibles sin dash, con margen.
    /// </summary>
    public static class Level1Layout
    {
        public const float Cell = 0.45f;

        public enum Kind { Earth, Metal, Beam }
        public enum EnemyType { Warrior, Spitter, Boss }

        public readonly struct Block
        {
            public readonly int X0, X1, Y0, Y1;
            public readonly Kind Kind;
            public Block(int x0, int x1, int y0, int y1, Kind kind) { X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; Kind = kind; }
        }

        public readonly struct Spawn
        {
            public readonly EnemyType Type;
            public readonly Vector2Int Cell;
            public Spawn(EnemyType type, int x, int y) { Type = type; Cell = new Vector2Int(x, y); }
        }

        /// <summary>Plataforma móvil: va de (FromX, FromRow) a (ToX, ToRow); X es la columna central.</summary>
        public readonly struct Mover
        {
            public readonly int FromX, FromRow, ToX, ToRow, Width;
            public Mover(int fromX, int fromRow, int toX, int toRow, int width = 4) { FromX = fromX; FromRow = fromRow; ToX = toX; ToRow = toRow; Width = width; }
        }

        public static readonly Block[] Blocks =
        {
            new Block(-14, -11, -8, 34, Kind.Earth),   // pared izquierda
            // Tramo 1: inicio y primer hueco de dash (31-47, 17 columnas)
            new Block(-10, 30, -8, 0, Kind.Earth),
            // Tramo 2: guerreros
            new Block(48, 100, -8, 0, Kind.Earth),
            new Block(74, 79, 5, 5, Kind.Metal),       // refugio para esquivar embestidas (se puede pasar por debajo)
            new Block(110, 165, -8, 0, Kind.Earth),    // tras un foso de ácido (101-109)
            new Block(136, 138, 1, 3, Kind.Earth),     // obstáculo
            // Tramo 3: torre en zigzag hasta el acantilado (escupidor arriba)
            // Las plataformas más bajas van en la fila 5: el jugador (unas 4,6 filas de alto) cabe por debajo.
            new Block(152, 156, 5, 5, Kind.Metal),
            new Block(159, 163, 9, 9, Kind.Metal),
            new Block(152, 156, 13, 13, Kind.Metal),
            new Block(159, 163, 17, 17, Kind.Metal),
            new Block(166, 200, -8, 20, Kind.Earth),   // acantilado
            // Tramo 4: vigas sobre el ácido y segundo hueco de dash (218-234)
            new Block(205, 208, 21, 21, Kind.Beam),
            new Block(214, 217, 21, 21, Kind.Beam),
            new Block(235, 262, 14, 20, Kind.Earth),   // saliente flotante
            // Tramo 5: plataformas móviles (ver Movers) hasta otra saliente
            new Block(318, 345, -1, 4, Kind.Earth),
            // Tramo 6: arena del jefe (como en Hollow Knight: suelo amplio para esquivar, con alturas para escapar).
            // Repisas de 3 filas en los costados; el jefe (3,2 de alto) cabe bajo las plataformas de la fila 9,
            // que se alcanzan saltando desde las repisas. La plataforma central (fila 13) se alcanza desde ellas.
            new Block(346, 420, -8, 0, Kind.Earth),
            new Block(354, 359, 1, 3, Kind.Earth),      // repisa izquierda
            new Block(412, 420, 1, 3, Kind.Earth),      // repisa derecha (la meta aparece aquí)
            new Block(363, 368, 9, 9, Kind.Metal),
            new Block(381, 392, 13, 13, Kind.Metal),
            new Block(405, 410, 9, 9, Kind.Metal),
            new Block(421, 424, -8, 34, Kind.Earth),   // pared derecha
        };

        public static readonly Mover[] Movers =
        {
            new Mover(268, 18, 282, 18, 5),   // horizontal
            new Mover(289, 18, 289, 4, 5),    // vertical (baja hasta cerca del ácido)
            new Mover(297, 4, 312, 4, 5),     // horizontal, justo sobre el ácido
        };

        public static readonly Spawn[] Enemies =
        {
            new Spawn(EnemyType.Warrior, 68, 0),
            new Spawn(EnemyType.Warrior, 88, 0),
            new Spawn(EnemyType.Warrior, 128, 0),
            new Spawn(EnemyType.Spitter, 182, 20),
            new Spawn(EnemyType.Warrior, 244, 20),
            new Spawn(EnemyType.Spitter, 258, 20),
            new Spawn(EnemyType.Warrior, 335, 4),
        };

        public static readonly Vector2Int PlayerStart = new Vector2Int(-6, 0);
        public static readonly Vector2Int[] Checkpoints =
        {
            new Vector2Int(51, 0), new Vector2Int(113, 0), new Vector2Int(168, 20),
            new Vector2Int(237, 20), new Vector2Int(320, 4), new Vector2Int(347, 0),
        };

        // Arena del jefe
        public static readonly Vector2Int BossSpawn = new Vector2Int(396, 0);
        public static readonly Vector2Int Goal = new Vector2Int(416, 3);
        /// <summary>Al entrar en estas columnas empieza la pelea.</summary>
        public static readonly RectInt ArenaTrigger = new RectInt(356, 1, 62, 14);
        /// <summary>Puerta que se cierra detrás del jugador durante la pelea.</summary>
        public static readonly RectInt ArenaDoor = new RectInt(350, 1, 2, 12);

        /// <summary>Superficie del ácido: caer por debajo cuesta una máscara.</summary>
        public const int AcidTopRow = -2;
        public const int AcidBottomRow = -12;

        /// <summary>Un punto en el aire sobre el primer hueco de ácido (para las pruebas).</summary>
        public static readonly Vector2Int PitProbe = new Vector2Int(38, 3);

        /// <summary>Rectángulo que la cámara no puede abandonar (en celdas).</summary>
        public static readonly RectInt CameraBounds = new RectInt(-14, -4, 439, 38);

        /// <summary>Punto apoyado sobre la superficie de la celda (para colocar personajes y objetos).</summary>
        public static Vector2 OnSurface(Vector2Int c) => new Vector2((c.x + 0.5f) * Cell, (c.y + 1) * Cell);

        public static Vector2 ToWorld(RectInt cells, out Vector2 size)
        {
            size = new Vector2(cells.width, cells.height) * Cell;
            return new Vector2(cells.x * Cell, cells.y * Cell) + size * 0.5f;
        }
    }
}
