using UnityEngine;

namespace Game
{
    /// <summary>Botones del menú principal de Dante: Jugar empieza una partida nueva desde el nivel 1.</summary>
    public class MainMenu : MonoBehaviour
    {
        public void Jugar() => GameFlow.NewGame();

        public void Salir()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
