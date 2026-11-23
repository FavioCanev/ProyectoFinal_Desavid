using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Herramientas de revisión sin abrir el editor: vuelca la jerarquía de una escena a texto y saca capturas
/// a lo largo del nivel. Uso en batch: -executeMethod SceneInspector.Run -scene Ruta.unity
/// </summary>
public static class SceneInspector
{
    public static void Run()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string scene = args[System.Array.IndexOf(args, "-scene") + 1];
        EditorSceneManager.OpenScene(scene);
        string name = Path.GetFileNameWithoutExtension(scene);
        Dump("Logs/" + name + "_jerarquia.txt");
        Shots("Logs/Capturas_" + name);
    }

    /// <summary>Hoja de muestras: cada loseta de las carpetas dadas con su nombre. -folders "A;B" -out archivo.png</summary>
    public static void TileSheet()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string[] folders = args[System.Array.IndexOf(args, "-folders") + 1].Split(';');
        string output = args[System.Array.IndexOf(args, "-out") + 1];
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var tiles = AssetDatabase.FindAssets("t:Tile", folders).Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>).Where(t => t != null && t.sprite != null).OrderBy(t => t.name).ToArray();
        const int columns = 12;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < tiles.Length; i++)
        {
            var go = new GameObject(tiles[i].name);
            go.transform.position = new Vector3((i % columns) * 1.6f, -(i / columns) * 1.6f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tiles[i].sprite;
            float s = 1f / Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            go.transform.localScale = new Vector3(s, s, 1f);
            var label = new GameObject("t").AddComponent<TextMesh>();
            label.transform.position = go.transform.position + new Vector3(0f, -0.65f, 0f);
            label.text = tiles[i].name.Replace("Tilesheet - ", "").Replace("Blocks_Platforms_", "BP");
            label.font = font; label.fontSize = 40; label.characterSize = 0.05f; label.anchor = TextAnchor.MiddleCenter;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        var cam = new GameObject("cam").AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = new Color(0.25f, 0.35f, 0.5f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        int rows = (tiles.Length + columns - 1) / columns;
        cam.orthographicSize = Mathf.Max(rows * 0.8f + 0.3f, columns * 1.6f / (2f * 16f / 9f));
        Shot(cam, output, new Vector2((columns - 1) * 0.8f, -(rows - 1) * 0.8f));
    }

    /// <summary>
    /// Recorre el camino del jugador como lo vería la cámara del juego: cada "paso" unidades busca el suelo
    /// (de arriba abajo, dentro de los límites de la cámara) y saca una captura 16:9 a esa altura.
    /// Junta las capturas de a 4 en hojas de revisión. Uso: -executeMethod SceneInspector.PathShots -scene Ruta.unity
    /// </summary>
    public static void PathShots()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string scenePath = args[System.Array.IndexOf(args, "-scene") + 1];
        EditorSceneManager.OpenScene(scenePath);
        string name = Path.GetFileNameWithoutExtension(scenePath);
        string folder = "Logs/Recorrido_" + name;
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        Directory.CreateDirectory(folder);

        var cam = Camera.main;
        foreach (var b in cam.GetComponents<Behaviour>()) if (b != null && b.GetType().Name.Contains("Brain")) b.enabled = false;
        var confiner = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).FirstOrDefault(c => c.name == "Camera Bounds");
        Bounds limits = confiner != null ? confiner.bounds : new Bounds(Vector3.zero, Vector3.one * 400f);
        float h = cam.orthographicSize, w = h * 16f / 9f;
        int ground = LayerMask.GetMask("Ground");
        var shots = new System.Collections.Generic.List<string>();
        for (float x = limits.min.x + w; x <= limits.max.x - w + 0.01f; x += w * 1.5f)
        {
            // El suelo más bajo con aire encima (no el techo): donde camina el jugador; la cámara lo enfoca 1,5 más arriba.
            var hits = Physics2D.RaycastAll(new Vector2(x, limits.max.y), Vector2.down, limits.size.y, ground)
                .Where(hh => !hh.collider.isTrigger && hh.normal.y > 0.7f && !Physics2D.OverlapPoint(hh.point + Vector2.up * 0.6f, ground)).ToArray();
            float y = hits.Length > 0 ? hits.Min(hh => hh.point.y) + 1.5f : limits.center.y;
            y = Mathf.Clamp(y, limits.min.y + h, limits.max.y - h);
            string file = $"{folder}/{shots.Count:00}_x{Mathf.RoundToInt(x)}.png";
            Shot(cam, file, new Vector2(Mathf.Clamp(x, limits.min.x + w, limits.max.x - w), y));
            shots.Add(file);
        }
        // Hojas de revisión: 4 capturas (640x360) por hoja, con su posición escrita.
        for (int s = 0; s < shots.Count; s += 4)
        {
            var sheet = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            for (int k = 0; k < 4 && s + k < shots.Count; k++)
            {
                var src = new Texture2D(2, 2);
                src.LoadImage(File.ReadAllBytes(shots[s + k]));
                int ox = (k % 2) * 640, oy = (1 - k / 2) * 360;
                for (int py = 0; py < 360; py++)
                    for (int px = 0; px < 640; px++)
                        sheet.SetPixel(ox + px, oy + py, src.GetPixel(px * 2, py * 2));
                Object.DestroyImmediate(src);
            }
            sheet.Apply();
            File.WriteAllBytes($"{folder}/hoja_{s / 4:00}.png", sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }
        Debug.Log($"SceneInspector: {shots.Count} capturas del recorrido en {folder}");
    }

    private static void Dump(string file)
    {
        var sb = new StringBuilder();
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects()) Write(sb, root.transform, 0);
        foreach (var map in Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None))
        {
            map.CompressBounds();
            sb.AppendLine($"\nTilemap {map.name}: celdas {map.cellBounds}, tamaño de celda {map.layoutGrid.cellSize}, escala {map.transform.lossyScale}");
            var counts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var p in map.cellBounds.allPositionsWithin)
            {
                var t = map.GetTile(p);
                if (t == null) continue;
                string key = $"{t.name} ({t.GetType().Name}) {AssetDatabase.GetAssetPath(t)}";
                counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
            }
            foreach (var kv in counts.OrderByDescending(k => k.Value)) sb.AppendLine($"  {kv.Value} x {kv.Key}");
        }
        File.WriteAllText(file, sb.ToString());
    }

    private static void Write(StringBuilder sb, Transform t, int depth)
    {
        var comps = t.GetComponents<Component>().Select(c => c == null ? "¡SCRIPT PERDIDO!" : c.GetType().Name).Where(n => n != "Transform");
        sb.Append(' ', depth * 2).Append(t.name).Append(" @").Append(((Vector2)t.position).ToString("F1"))
          .Append(" s").Append(((Vector2)t.lossyScale).ToString("F2")).Append(" L").Append(LayerMask.LayerToName(t.gameObject.layer))
          .Append(t.gameObject.activeSelf ? "" : " (inactivo)").Append(" [").Append(string.Join(", ", comps)).AppendLine("]");
        foreach (Transform child in t) Write(sb, child, depth + 1);
    }

    /// <summary>Una vista general y capturas de 1280x720 cada cierta distancia, a la altura del suelo.</summary>
    private static void Shots(string folder)
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        Directory.CreateDirectory(folder);
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        var cam = Camera.main != null ? Camera.main : new GameObject("Cámara temporal").AddComponent<Camera>();
        foreach (var b in cam.GetComponents<Behaviour>()) if (b != null && b.GetType().Name.Contains("Brain")) b.enabled = false;
        var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        if (colliders.Length == 0 && renderers.Length == 0) return;   // escena solo de interfaz (menú, intro)
        Bounds level = colliders.Length > 0 ? colliders[0].bounds : renderers[0].bounds;
        foreach (var c in colliders) level.Encapsulate(c.bounds);

        cam.orthographic = true;
        float size = cam.orthographicSize;
        cam.orthographicSize = level.size.x / (2f * 16f / 9f) + 1f;
        Shot(cam, folder + "/00_general.png", level.center);
        cam.orthographicSize = size;
        int i = 1;
        for (float x = level.min.x + size * 1.6f; x < level.max.x; x += size * 3.2f)
            Shot(cam, $"{folder}/{i++:00}.png", new Vector2(x, level.center.y));
    }

    private static void Shot(Camera cam, string file, Vector2 at)
    {
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(at.x, at.y, -10f);
        foreach (var layer in Object.FindObjectsByType<Game.ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(cam);
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        File.WriteAllBytes(file, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
    }
}
