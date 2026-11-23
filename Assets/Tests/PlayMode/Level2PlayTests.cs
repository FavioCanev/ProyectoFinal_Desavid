using System.Collections;
using System.Linq;
using Amazonia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>Pruebas en modo Play del Nivel 2 (Amazonía de Edward con el jugador común).</summary>
    public class Level2PlayTests
    {
        private PlayerController player;

        private IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            GameFlow.Run.Reset();
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            yield return new WaitForSeconds(1f);
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        /// <summary>Omite diálogos y videos hasta que el director quede libre.</summary>
        private static IEnumerator SkipCinematics(AdventureDirector director, float timeout = 15f)
        {
            for (float t = 0f; t < timeout && director.Busy; t += Time.unscaledDeltaTime)
            {
                director.RequestSkip();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Land_PlayerLands_AndPlasmaPickupEnablesShooting()
        {
            yield return Load("Nivel2_Tierra");
            Assert.IsTrue(player.IsGrounded, "El jugador no está en el suelo de la selva nueva.");
            Assert.IsFalse(GameFlow.Run.HasPlasma);

            var pickup = Object.FindFirstObjectByType<PlasmaPickup>();
            player.Teleport(pickup.transform.position + Vector3.down * 0.5f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(GameFlow.Run.HasPlasma, "Recoger la pistola no la desbloqueó.");

            var plasma = player.GetComponent<PlayerPlasma>();
            Assert.IsTrue(plasma.TryShoot(), "Con la pistola, Fin debería disparar.");
            Assert.AreEqual(2, plasma.Ammo.Ammo);
        }

        [UnityTest]
        public IEnumerator Land_PlasmaShotHurtsACrocodile()
        {
            yield return Load("Nivel2_Tierra");
            GameFlow.Run.UnlockPlasma();
            var enemy = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .OrderBy(e => Mathf.Abs(e.transform.position.x - player.transform.position.x)).First();
            int before = enemy.Health.Current;
            player.Teleport((Vector2)enemy.transform.position + new Vector2(-4f, 0.05f));
            player.Face(1);
            yield return new WaitForFixedUpdate();
            player.GetComponent<PlayerPlasma>().TryShoot();
            yield return new WaitForSeconds(0.5f);
            Assert.Less(enemy.Health.Current, before, "El disparo de plasma no dañó al caimán.");
        }

        [UnityTest]
        public IEnumerator Land_BeatingTheCaiman_OpensTheLake_AndDivingLoadsIt()
        {
            yield return Load("Nivel2_Tierra");
            var flow = Object.FindFirstObjectByType<AdventureFlow>();
            var director = flow.GetComponent<AdventureDirector>();
            var boss = flow.boss;

            player.Teleport(new Vector2(25f, 0.1f));   // pasa x = 21: empieza la presentación
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(director.IntroDone, "Entrar en la arena no presentó al caimán.");
            yield return SkipCinematics(director);

            for (int i = 0; i < 40 && !boss.Health.IsDead; i++)
                boss.Health.TryDamage(1, player.transform.position, ignoreInvulnerability: true);
            yield return SkipCinematics(director);
            for (float t = 0f; t < 12f && !flow.LakeOpen; t += Time.deltaTime) { director.RequestSkip(); yield return null; }
            Assert.IsTrue(flow.LakeOpen, "Vencer al caimán no abrió el lago.");

            player.Teleport(new Vector2(60f, 0.2f));
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(flow.TryDive(), "En la orilla debería poder zambullirse.");
            for (float t = 0f; t < 20f && SceneManager.GetActiveScene().name != "Nivel2_Lago"; t += Time.unscaledDeltaTime)
            {
                director.RequestSkip();
                yield return null;
            }
            Assert.AreEqual("Nivel2_Lago", SceneManager.GetActiveScene().name, "Zambullirse no llevó al lago.");
        }

        [UnityTest]
        public IEnumerator Lake_PlayerSwimsUp()
        {
            yield return Load("Nivel2_Lago");
            Assert.IsTrue(player.Swimming, "En el lago el jugador debería nadar.");
            float y = player.transform.position.y;
            Assert.Less(Mathf.Abs(player.Velocity.y), 1f, "Sin pulsar nada no debería salir disparado.");
            player.Bounce(4f);   // un empujón hacia arriba se frena en el agua en vez de caer como en tierra
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(player.transform.position.y, y, "Bajo el agua no subió.");
        }

        [UnityTest]
        public IEnumerator Lake_PiranhaCanBeDefeatedUnderwater()
        {
            yield return Load("Nivel2_Lago");
            var piranha = Object.FindObjectsByType<SwimmerEnemy>(FindObjectsSortMode.None)
                .OrderBy(p => Vector2.Distance(p.transform.position, player.transform.position)).First();
            var health = piranha.GetComponent<Health>();
            var combat = player.GetComponent<PlayerCombat>();
            for (int i = 0; i < 10 && !health.IsDead; i++)
            {
                player.Teleport((Vector2)piranha.transform.position + new Vector2(-1.1f, -0.85f));
                player.Face(1);
                yield return new WaitForFixedUpdate();
                combat.TryAttack(AttackDirection.Forward);
                yield return new WaitForSeconds(0.4f);
            }
            Assert.IsTrue(health.IsDead, "La piraña sigue viva tras varios ataques bajo el agua.");
        }

        [UnityTest]
        public IEnumerator Lake_BeatingTheCaiman_CompletesTheLevel()
        {
            yield return Load("Nivel2_Lago");
            var flow = Object.FindFirstObjectByType<AdventureFlow>();
            var director = flow.GetComponent<AdventureDirector>();
            player.Teleport(new Vector2(25f, 8f));
            yield return new WaitForSeconds(0.3f);
            yield return SkipCinematics(director);
            for (int i = 0; i < 40 && !flow.boss.Health.IsDead; i++)
                flow.boss.Health.TryDamage(1, player.transform.position, ignoreInvulnerability: true);
            for (float t = 0f; t < 20f && SceneManager.GetActiveScene().name != "Nivel3"; t += Time.unscaledDeltaTime)
            {
                director.RequestSkip();
                yield return null;
            }
            Assert.AreEqual("Nivel3", SceneManager.GetActiveScene().name, "Vencer al caimán del lago no llevó al nivel 3.");
        }
    }
}
