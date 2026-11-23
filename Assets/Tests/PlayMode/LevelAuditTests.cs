using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    /// <summary>
    /// Auditoría de cada nivel jugable: busca errores de construcción que un jugador notaría
    /// (losetas visibles sin colisión, monedas inalcanzables, decorado flotando, objetos en el aire).
    /// Cada problema se informa con su posición para poder encontrarlo.
    /// </summary>
    public class LevelAuditTests
    {
        private const float JumpReach = 5f;        // altura útil de salto + doble salto (5,6 unidades) con margen
        private const float PitReach = 6f;         // distancia horizontal a un borde para monedas en el arco de un salto
        private static readonly string[] Scenes = GameFlow.Levels;

        private int ground;
        private readonly List<string> problems = new List<string>();

        private IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            GameFlow.Run.Reset();
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            yield return new WaitForFixedUpdate();
            ground = LayerMask.GetMask("Ground");
            problems.Clear();
        }

        private void Report(string scene)
        {
            string list = string.Join("\n", problems.Take(40));
            Assert.IsEmpty(problems, $"{scene}: {problems.Count} problema(s):\n{list}");
        }

        private static bool Swimming(string scene) => scene == "Nivel2_Lago";

        [UnityTest]
        public IEnumerator VisibleTiles_HaveCollision([ValueSource(nameof(Scenes))] string scene)
        {
            yield return Load(scene);
            foreach (var map in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (map.GetComponent<TilemapRenderer>() == null || map.name.Contains("Backing") || map.name.Contains("Decor")) continue;
                var collider = map.GetComponent<TilemapCollider2D>();
                if (collider == null) { problems.Add($"El mapa '{map.name}' se ve pero no tiene colisión."); continue; }
                map.CompressBounds();
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(cell) && map.GetColliderType(cell) == Tile.ColliderType.None)
                        problems.Add($"Loseta sin colisión en '{map.name}' celda {cell} (mundo {map.GetCellCenterWorld(cell):F1}).");
            }
            Report(scene);
        }

        // Alcance con margen de comodidad usando salto + doble salto (3 + 2,6 de alto) y dash.
        private const float StepUp = 4.8f;        // subir a una superficie más alta
        private const float StepUpReach = 6f;     // distancia horizontal al subir
        private const float LongReach = 10f;      // al mismo nivel o bajando, con doble salto y dash
        private const float Sample = 0.5f;

        /// <summary>
        /// Mapa de superficies (rayos hacia abajo cada 0,5) y recorrido desde donde aparece el jugador:
        /// cada moneda, bandera y meta debe quedar al alcance de un salto (o salto con dash) desde una superficie alcanzable.
        /// También comprueba que el jugador se sostiene de verdad sobre las superficies con monedas.
        /// </summary>
        [UnityTest]
        public IEnumerator Collectibles_AreReachableFromTheStart([ValueSource(nameof(Scenes))] string scene)
        {
            yield return Load(scene);
            if (Swimming(scene)) { Assert.Pass("En el lago se nada en todas direcciones."); yield break; }
            var player = Object.FindFirstObjectByType<PlayerController>();
            player.GetComponent<PlayerLife>().Protected = true;   // que ningún enemigo lo empuje durante la prueba
            Vector2 start = player.transform.position;

            Bounds level = new Bounds(start, Vector2.zero);
            foreach (var c in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                if (!c.isTrigger && (ground & (1 << c.gameObject.layer)) != 0) level.Encapsulate(c.bounds);

            // Superficie = sólido debajo, aire encima y sitio para el jugador (1,6 de alto). Se recorre cada columna
            // de arriba abajo con puntos: un rayo solo ve la primera cara de un colisionador (el suelo de losetas es uno solo).
            bool Solid(Vector2 p) => Physics2D.OverlapPoint(p, ground) is Collider2D c && !c.isTrigger;
            var surfaces = new List<Vector2>();
            for (float x = level.min.x; x <= level.max.x; x += Sample)
                for (float y = level.max.y; y >= level.min.y; y -= 0.1f)
                    if (Solid(new Vector2(x, y)) && !Solid(new Vector2(x, y + 0.1f)) && !Solid(new Vector2(x, y + 1.5f)))
                        surfaces.Add(new Vector2(x, y + 0.05f));

            // Recorrido en anchura desde la superficie donde está el jugador.
            var reached = new bool[surfaces.Count];
            var queue = new Queue<int>();
            for (int i = 0; i < surfaces.Count; i++)
                if (Vector2.Distance(surfaces[i], start) < 1.5f) { reached[i] = true; queue.Enqueue(i); }
            while (queue.Count > 0)
            {
                Vector2 a = surfaces[queue.Dequeue()];
                for (int j = 0; j < surfaces.Count; j++)
                {
                    if (reached[j]) continue;
                    Vector2 b = surfaces[j];
                    float dy = b.y - a.y, dx = Mathf.Abs(b.x - a.x);
                    bool canGo = dy > 0.5f ? dy <= StepUp && dx <= StepUpReach : dx <= LongReach;
                    if (canGo) { reached[j] = true; queue.Enqueue(j); }
                }
            }
            var reachable = surfaces.Where((p, i) => reached[i]).ToList();
            bool Reachable(Vector2 target, float height) =>
                reachable.Any(p => target.y - p.y <= height && target.y - p.y >= -LongReach && Mathf.Abs(target.x - p.x) <= (target.y - p.y > 0.5f ? StepUpReach + 1f : LongReach));

            foreach (var coin in Object.FindObjectsByType<Coin>(FindObjectsSortMode.None))
                if (!Reachable(coin.transform.position, JumpReach + 0.3f)) problems.Add($"Moneda inalcanzable en {(Vector2)coin.transform.position:F1}.");
            foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                if (!Reachable((Vector2)cp.transform.position + Vector2.up * 0.5f, 1f)) problems.Add($"Bandera inalcanzable en {(Vector2)cp.transform.position:F1}.");
            foreach (var goal in Object.FindObjectsByType<Goal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!Reachable((Vector2)goal.transform.position + Vector2.up * 0.5f, 1f)) problems.Add($"Meta inalcanzable en {(Vector2)goal.transform.position:F1}.");

            // ¿Se sostiene de verdad sobre la superficie de cada moneda? (detecta lo que se ve sólido pero no sostiene)
            var coinPositions = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None).Select(c => (Vector2)c.transform.position).ToList();
            foreach (Vector2 c in coinPositions)
            {
                RaycastHit2D below = Physics2D.Raycast(c, Vector2.down, JumpReach + 0.6f, ground);
                if (!below) continue;
                player.Teleport(below.point + Vector2.up * 0.05f);
                for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
                if (!player.IsGrounded || Mathf.Abs(player.transform.position.y - below.point.y) > 0.3f)
                    problems.Add($"Moneda en {c:F1}: la superficie de abajo ({below.collider.name}) no sostiene al jugador.");
            }
            Report(scene);
        }

        /// <summary>Lo que debe apoyarse en el suelo (enemigos de tierra, banderas, carteles, meta, decorado con pivote abajo) no flota.</summary>
        [UnityTest]
        public IEnumerator GroundedThings_AreNotFloating([ValueSource(nameof(Scenes))] string scene)
        {
            yield return Load(scene);
            void Check(Vector2 feet, string what)
            {
                RaycastHit2D hit = Physics2D.Raycast(feet + Vector2.up * 0.3f, Vector2.down, 1f, ground);
                if (!hit) problems.Add($"{what} en {feet:F1} está en el aire.");
            }
            foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None)) Check(cp.transform.position, "Bandera");
            foreach (var goal in Object.FindObjectsByType<Goal>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Check(goal.transform.position, "Meta");
            foreach (var e in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                if (!e.IsDormant) Check(e.transform.position, "Enemigo " + e.name);
            foreach (var sign in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                Check((Vector2)sign.transform.parent.position, "Cartel");

            if (!Swimming(scene))
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                {
                    // Decorado suelto con el pivote abajo (plantas, rocas, tocones): debe tocar el suelo o colgar de algo.
                    if (sr.sprite == null || sr.sprite.pivot.y > sr.sprite.rect.height * 0.1f) continue;
                    if (sr.GetComponentInParent<Rigidbody2D>() != null || sr.GetComponentInParent<ParallaxLayer>() != null) continue;
                    if (sr.GetComponentInParent<Checkpoint>() != null || sr.GetComponentInParent<Goal>() != null || sr.GetComponentInParent<Collider2D>() != null) continue;
                    if (sr.sortingLayerName != "Default") continue;
                    Bounds b = sr.bounds;
                    bool resting = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.4f), Vector2.down, 1.2f, ground);
                    bool hangingOrHigh = b.max.y > 19f;   // colgando del borde superior del nivel
                    if (!resting && !hangingOrHigh) problems.Add($"Decorado '{sr.name}' flota en {new Vector2(b.center.x, b.min.y):F1}.");
                }
            Report(scene);
        }
    }
}
