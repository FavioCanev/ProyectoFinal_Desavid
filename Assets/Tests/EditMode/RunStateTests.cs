using NUnit.Framework;

namespace Game.Tests
{
    public class RunStateTests
    {
        [Test]
        public void ThreeRespawns_ThenGameOver()
        {
            var run = new RunState();
            for (int i = 0; i < RunState.StartingLives; i++)
                Assert.IsTrue(run.TryUseLife(), $"Reaparición {i + 1}.");
            Assert.IsFalse(run.TryUseLife(), "La cuarta muerte debería ser Game Over.");
            Assert.AreEqual(0, run.Lives);
        }

        [Test]
        public void Reset_RestoresLivesAndClearsCoins()
        {
            var run = new RunState();
            run.TryUseLife();
            run.AddCoins(5);
            run.Reset();
            Assert.AreEqual(RunState.StartingLives, run.Lives);
            Assert.AreEqual(0, run.Coins);
        }

        [Test]
        public void RestartLevel_UndoesCoinsTakenInThatLevel()
        {
            var run = new RunState();
            run.AddCoins(3);
            run.BeginLevel();
            run.AddCoins(4);
            run.RestartLevel();
            Assert.AreEqual(3, run.Coins, "Reiniciar el nivel no debe regalar monedas.");
        }

        [Test]
        public void NextLevel_FollowsTheGameOrder()
        {
            Assert.AreEqual("Nivel2_Tierra", GameFlow.NextLevel("Nivel1"));
            Assert.AreEqual("Nivel3", GameFlow.NextLevel("Nivel2_Lago"));
            Assert.IsNull(GameFlow.NextLevel("Nivel3"));
            Assert.IsTrue(GameFlow.IsLastLevel("Nivel3"));
        }
    }
}
