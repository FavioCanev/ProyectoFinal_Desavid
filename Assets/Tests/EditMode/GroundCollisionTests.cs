using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Tests
{
    /// <summary>
    /// Regresión: el suelo creado por código debe colisionar de verdad.
    /// Un suelo guardado en la misma sesión que lo creó quedaba con el collider compuesto vacío y el
    /// jugador lo atravesaba. La solución es recargar la escena y llamar a RebuildComposite.
    /// Esta prueba sigue ese mismo camino: crear, guardar, recargar, reconstruir, guardar, recargar, simular.
    /// </summary>
    public class GroundCollisionTests
    {
        private const string ScenePath = "Assets/Tests/Temp_GroundTest.unity";
        private const string TilePath = "Assets/Niveles/3_Favio/Art/Terrain/terreno-removebg-preview_0.asset";

        private SimulationMode2D previousMode;

        [SetUp]
        public void SetUp()
        {
            // OpenScene/NewScene descartan los cambios sin guardar: se pide guardar antes de empezar.
            Assume.That(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(), "Hay cambios sin guardar.");
            previousMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        [TearDown]
        public void TearDown()
        {
            Physics2D.simulationMode = previousMode;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(ScenePath);
        }

        [Test]
        public void FallingBody_LandsOnGroundTilemap_AfterSaveAndReload()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Se carga DESPUÉS de NewScene: NewScene descarga de memoria los assets sin referencia.
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(TilePath);
            Assert.IsNotNull(tile, "No se pudo cargar el tile de prueba.");

            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.cellSize = new Vector3(0.45f, 0.45f, 0f);
            var ground = GroundTilemapFactory.Create(grid.transform, "Ground", layer: 0, sortingOrder: 0);
            for (int x = -5; x <= 5; x++) ground.SetTile(new Vector3Int(x, -2, 0), tile);   // borde superior en y = -0.45

            var body = new GameObject("Body");
            body.transform.position = new Vector3(0f, 1f, 0f);
            body.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.6f);
            body.AddComponent<Rigidbody2D>().freezeRotation = true;

            EditorSceneManager.SaveScene(scene, ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
            GroundTilemapFactory.RebuildComposite(GameObject.Find("Ground"));
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(ScenePath);

            var rb = GameObject.Find("Body").GetComponent<Rigidbody2D>();
            Physics2D.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics2D.Simulate(0.02f);

            // El cuerpo mide 1.6 de alto y su origen está en el centro: apoyado sobre el suelo queda en y = 0.35.
            Assert.Greater(rb.position.y, 0f, "El cuerpo atravesó el suelo.");
            Assert.Less(rb.position.y, 0.6f, "El cuerpo no llegó a caer.");
        }
    }
}
