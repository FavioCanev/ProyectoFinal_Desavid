using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    /// <summary>
    /// Crea el Tilemap del suelo con su collider fusionado (sin costuras entre losetas).
    /// </summary>
    public static class GroundTilemapFactory
    {
        public static Tilemap Create(Transform parent, string name, int layer, int sortingOrder)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);

            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            go.AddComponent<TilemapCollider2D>();
            AddMergedComposite(go);
            return tilemap;
        }

        /// <summary>
        /// Vuelve a crear el collider compuesto del suelo.
        /// Hay que llamarlo sobre una escena que acaba de guardarse y recargarse: un suelo creado por código
        /// y guardado en la misma sesión queda con el compuesto SIN geometría (el jugador atraviesa el suelo),
        /// y recrearlo sobre la escena ya cargada lo arregla. Observado con Unity 6000.6.8f1.
        /// </summary>
        public static void RebuildComposite(GameObject ground)
        {
            var tileCollider = ground.GetComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.None;
            Object.DestroyImmediate(ground.GetComponent<CompositeCollider2D>());
            Object.DestroyImmediate(ground.GetComponent<Rigidbody2D>());
            AddMergedComposite(ground);
        }

        private static void AddMergedComposite(GameObject ground)
        {
            var composite = ground.AddComponent<CompositeCollider2D>();   // añade su propio Rigidbody2D
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            ground.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            ground.GetComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        }
    }
}
