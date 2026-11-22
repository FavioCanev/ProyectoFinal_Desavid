using System.IO;
using Game;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Saca capturas de Nivel1 en varios puntos a Logs/Capturas, más una vista general alejada,
/// para revisar el aspecto sin jugar. Menú: Game > Capturas del Nivel 1.
/// </summary>
public static class LevelScreenshots
{
    private const string OutputFolder = "Logs/Capturas";
    private const int Width = 1280, Height = 720;

    [MenuItem("Game/Capturas del Nivel 1")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene(Level1Builder.ScenePath);
        if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
        Directory.CreateDirectory(OutputFolder);

        var cam = Camera.main;
        var brain = cam.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = false;   // la cámara se coloca a mano para cada captura

        Shot(cam, "01_inicio", Level1Layout.OnSurface(Level1Layout.PlayerStart));
        Shot(cam, "02_hueco_dash", Level1Layout.OnSurface(new Vector2Int(38, 0)));
        Shot(cam, "03_guerreros", Level1Layout.OnSurface(new Vector2Int(80, 0)));
        Shot(cam, "04_torre", Level1Layout.OnSurface(new Vector2Int(160, 8)));
        Shot(cam, "05_vigas", Level1Layout.OnSurface(new Vector2Int(215, 21)));
        Shot(cam, "06_saliente", Level1Layout.OnSurface(new Vector2Int(250, 20)));
        Shot(cam, "07_plataformas", Level1Layout.OnSurface(new Vector2Int(298, 6)));
        Shot(cam, "08_arena", Level1Layout.OnSurface(new Vector2Int(395, 0)));

        // Interfaz: el HUD se dibuja sobre la cámara solo para estas capturas (no se guarda la escena).
        var hud = GameObject.Find("HUD").GetComponent<Canvas>();
        var pausePanel = Object.FindAnyObjectByType<PauseMenu>().transform.GetChild(0).gameObject;
        hud.renderMode = RenderMode.ScreenSpaceCamera;
        hud.worldCamera = cam;
        hud.planeDistance = 1f;
        hud.sortingOrder = 1000;   // por encima de los sprites del nivel, como en el juego
        pausePanel.SetActive(false);
        Shot(cam, "09_interfaz", Level1Layout.OnSurface(new Vector2Int(380, 0)));
        pausePanel.SetActive(true);
        Shot(cam, "10_pausa", Level1Layout.OnSurface(new Vector2Int(380, 0)));
        hud.renderMode = RenderMode.ScreenSpaceOverlay;
        pausePanel.SetActive(false);

        // Vista general: cámara muy alejada para ver el trazado completo.
        float size = cam.orthographicSize;
        RectInt b = Level1Layout.CameraBounds;
        cam.orthographicSize = b.width * Level1Layout.Cell / (2f * Width / Height);
        Shot(cam, "00_vista_general", new Vector2((b.xMin + b.width * 0.5f) * Level1Layout.Cell, (b.yMin + b.height * 0.5f) * Level1Layout.Cell - 1.5f));
        cam.orthographicSize = size;
        Debug.Log("LevelScreenshots: capturas guardadas en " + Path.GetFullPath(OutputFolder));
    }

    // Igual que en el juego: la cámara no muestra nada fuera de los límites del nivel.
    private static Vector3 Confine(Camera cam, Vector2 p)
    {
        RectInt b = Level1Layout.CameraBounds;
        float c = Level1Layout.Cell, h = cam.orthographicSize, w = h * Width / Height;
        float minX = b.xMin * c + w, maxX = b.xMax * c - w, minY = b.yMin * c + h, maxY = b.yMax * c - h;
        float x = minX <= maxX ? Mathf.Clamp(p.x, minX, maxX) : (b.xMin + b.width * 0.5f) * c;
        float y = minY <= maxY ? Mathf.Clamp(p.y, minY, maxY) : (b.yMin + b.height * 0.5f) * c;
        return new Vector3(x, y, -10f);
    }

    private static void Shot(Camera cam, string name, Vector2 focus)
    {
        var rt = new RenderTexture(Width, Height, 24);
        cam.targetTexture = rt;   // antes de Refresh: así las capas usan el formato 16:9 de la captura
        cam.transform.position = Confine(cam, new Vector2(focus.x, focus.y + 1.5f));
        foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(cam);
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(OutputFolder, name + ".png"), tex.EncodeToPNG());

        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
    }
}
