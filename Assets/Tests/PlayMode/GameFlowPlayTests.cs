using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>El recorrido entre escenas: el menú empieza una partida nueva en el nivel 1.</summary>
    public class GameFlowPlayTests
    {
        [UnityTest]
        public IEnumerator Menu_Play_StartsANewGameAtLevel1()
        {
            GameFlow.Run.AddCoins(7);
            GameFlow.Run.TryUseLife();
            SceneManager.LoadScene(GameFlow.MenuScene);
            yield return null;
            Object.FindFirstObjectByType<MainMenu>().Jugar();
            yield return null;
            yield return null;
            Assert.AreEqual("Nivel1", SceneManager.GetActiveScene().name);
            Assert.AreEqual(0, GameFlow.Run.Coins, "Una partida nueva empieza sin monedas.");
            Assert.AreEqual(RunState.StartingLives, GameFlow.Run.Lives, "Una partida nueva empieza con todas las vidas.");
        }
    }
}
