using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    /// <summary>
    /// Orden de las escenas del juego y paso de una a otra. La partida (vidas y monedas) vive aquí,
    /// en memoria, mientras el juego está abierto.
    /// </summary>
    public static class GameFlow
    {
        public const string IntroScene = "Intro";
        public const string MenuScene = "Menu";

        /// <summary>Niveles en orden de juego. Nivel2_Lago se alcanza zambulléndose desde Nivel2_Tierra.</summary>
        public static readonly string[] Levels = { "Nivel1", "Nivel2_Tierra", "Nivel2_Lago", "Nivel3" };

        public static RunState Run { get; private set; } = new RunState();

        public static bool IsLastLevel(string scene) => Array.IndexOf(Levels, scene) == Levels.Length - 1;

        /// <summary>Siguiente nivel tras el dado, o null si es el último.</summary>
        public static string NextLevel(string scene)
        {
            int i = Array.IndexOf(Levels, scene);
            return i >= 0 && i < Levels.Length - 1 ? Levels[i + 1] : null;
        }

        public static void NewGame()
        {
            Run.Reset();
            Load(Levels[0]);
        }

        public static void LoadLevel(string scene)
        {
            Load(scene);
            Run.BeginLevel();
        }

        /// <summary>Al terminar un nivel: el siguiente, o el menú si era el último.</summary>
        public static void CompleteLevel()
        {
            string next = NextLevel(SceneManager.GetActiveScene().name);
            if (next != null) LoadLevel(next);
            else { Run.Reset(); Load(MenuScene); }
        }

        public static void RestartLevel()
        {
            Run.RestartLevel();
            Load(SceneManager.GetActiveScene().name);
        }

        public static void GoToMenu() => Load(MenuScene);

        private static void Load(string scene)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(scene);
        }
    }
}
