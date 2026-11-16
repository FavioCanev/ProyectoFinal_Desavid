using System.IO;
using Game;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Saca capturas de Nivel1 en varios puntos (inicio, cada enemigo, vigas, meta) a Logs/Capturas,
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
        Directory.CreateDirectory(OutputFolder);

        var cam = Camera.main;
        var brain = cam.GetComponent<CinemachineBrain>();
        if (brain != null) brain.enabled = false;   // la cámara se coloca a mano para cada captura
        var background = Object.FindFirstObjectByType<ParallaxBackground>();

        Shot(cam, background, "1_inicio", Level1Layout.OnSurface(Level1Layout.PlayerStart));
        for (int i = 0; i < Level1Layout.Enemies.Length; i++)
            Shot(cam, background, $"{i + 2}_enemigo{i + 1}", Level1Layout.OnSurface(Level1Layout.Enemies[i]));
        Shot(cam, background, "5_vigas", Level1Layout.OnSurface(new Vector2Int(155, 13)));
        Shot(cam, background, "6_meta", Level1Layout.OnSurface(Level1Layout.Goal));
        Debug.Log("LevelScreenshots: capturas guardadas en " + Path.GetFullPath(OutputFolder));
    }

    private static void Shot(Camera cam, ParallaxBackground background, string name, Vector2 focus)
    {
        var rt = new RenderTexture(Width, Height, 24);
        cam.targetTexture = rt;   // antes de Refresh: así el fondo usa el formato 16:9 de la captura
        cam.transform.position = new Vector3(focus.x, focus.y + 1.5f, -10f);
        if (background != null) background.Refresh(cam);
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
