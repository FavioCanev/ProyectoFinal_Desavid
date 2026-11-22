using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Regresión: si la física 2D queda en modo "Script", en Play nada cae ni se mueve.
    /// (Pasó tras unas pruebas de diagnóstico que cambiaron el modo y no lo restauraron.)
    /// </summary>
    public class ProjectSettingsTests
    {
        [Test]
        public void Physics2D_RunsAutomaticallyInFixedUpdate()
        {
            Assert.AreEqual(SimulationMode2D.FixedUpdate, Physics2D.simulationMode,
                "La física 2D no se simula sola: revisa Project Settings > Physics 2D > Simulation Mode.");
        }

        [Test]
        public void PlayerAndEnemies_PassThroughEachOther()
        {
            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(player, enemy),
                "El jugador y los enemigos chocan físicamente; el daño debe ser solo por contacto.");
        }

        [Test]
        public void Enemies_PassThroughEachOther()
        {
            int enemy = LayerMask.NameToLayer("Enemy");
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(enemy, enemy),
                "Los enemigos chocan entre sí y se bloquean al patrullar.");
        }
    }
}
