using Game;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta la intro y el menú de Dante con el flujo común: la intro pasa al menú; "Inicio" empieza una partida
/// nueva y "Exit" sale. Su arte y su disposición no se tocan. Menú: Game > Conectar intro y menú.
/// </summary>
public static class MenuScenesBuilder
{
    private const string IntroPath = "Assets/Scenes/Intro.unity";
    private const string MenuPath = "Assets/Scenes/Menu.unity";

    [MenuItem("Game/Conectar intro y menú")]
    public static void Build()
    {
        var intro = EditorSceneManager.OpenScene(IntroPath);
        var video = Object.FindAnyObjectByType<UnityEngine.Video.VideoPlayer>();
        if (video.GetComponent<IntroScreen>() == null) video.gameObject.AddComponent<IntroScreen>();
        EditorSceneManager.SaveScene(intro);

        var menu = EditorSceneManager.OpenScene(MenuPath);
        var root = GameObject.Find("Menu");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);   // su MenuSystem: lo reemplaza MainMenu
        var main = root.GetComponent<MainMenu>() ?? root.AddComponent<MainMenu>();
        Rewire(root.transform.Find("InIcio").GetComponent<Button>(), main.Jugar);
        Rewire(root.transform.Find("Exit").GetComponent<Button>(), main.Salir);
        EditorSceneManager.SaveScene(menu);
        LevelKit.SetBuildScenes();
        Debug.Log("MenuScenesBuilder: intro y menú conectados");
    }

    private static void Rewire(Button button, UnityEngine.Events.UnityAction action)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }
}
