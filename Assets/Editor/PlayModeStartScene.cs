using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Al darle a Play con una escena vacía o sin guardar ("Untitled"), el juego arranca desde la intro.
/// Con un nivel abierto, Play prueba ese nivel directamente (como siempre).
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartScene
{
    public const string FirstScene = "Assets/Scenes/Intro.unity";

    static PlayModeStartScene()
    {
        EditorSceneManager.activeSceneChangedInEditMode += (_, _) => Refresh();
        EditorSceneManager.sceneOpened += (_, _) => Refresh();
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingEditMode) Refresh(); };
        EditorApplication.delayCall += Refresh;
    }

    private static void Refresh()
    {
        bool emptyScene = string.IsNullOrEmpty(SceneManager.GetActiveScene().path);
        EditorSceneManager.playModeStartScene = emptyScene ? AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstScene) : null;
    }

    [MenuItem("Game/Abrir la intro (inicio del juego)")]
    public static void OpenIntro() => EditorSceneManager.OpenScene(FirstScene);
}
