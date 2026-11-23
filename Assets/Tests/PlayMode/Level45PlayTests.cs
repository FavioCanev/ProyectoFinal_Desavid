using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>Pruebas en modo Play de los niveles 4 (Minertown) y 5 (La Mansión, jefe final).</summary>
    public class Level45PlayTests
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

        [UnityTest]
        public IEnumerator Level4_PlayerLands_AndAMinerCanBeDefeated()
        {
            yield return Load("Nivel4");
            Assert.IsTrue(player.IsGrounded, "El jugador no está en la vereda.");
            var miner = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .OrderBy(e => Mathf.Abs(e.transform.position.x - player.transform.position.x)).First();
            Health health = miner.Health;
            var combat = player.GetComponent<PlayerCombat>();
            for (int i = 0; i < 12 && !health.IsDead; i++)
            {
                player.Teleport((Vector2)miner.transform.position + new Vector2(-1.1f, 0.05f));
                player.Face(1);
                yield return new WaitForFixedUpdate();
                combat.TryAttack(AttackDirection.Forward);
                yield return new WaitForSeconds(0.4f);
            }
            Assert.IsTrue(health.IsDead, "El minero sigue vivo tras varios ataques.");
        }

        [UnityTest]
        public IEnumerator Level4_DynamiteThrowerHitsThePlayer()
        {
            yield return Load("Nivel4");
            var thrower = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .Where(e => e.Config.spit.enabled).OrderBy(e => e.transform.position.x).First();
            var attacks = new System.Collections.Generic.List<EnemyAttack>();
            thrower.AttackStarted += attacks.Add;
            var life = player.GetComponent<PlayerLife>();
            player.Teleport((Vector2)thrower.transform.position + new Vector2(-6f, 0.05f));
            for (float t = 0f; t < 5f && life.Health.Current == life.Health.Max; t += Time.deltaTime) yield return null;
            CollectionAssert.Contains(attacks, EnemyAttack.Spit, "El dinamitero no lanzó dinamita.");
            Assert.Less(life.Health.Current, life.Health.Max, "La dinamita no dañó al jugador.");
        }

        [UnityTest]
        public IEnumerator Level4_GoalLoadsLevel5()
        {
            yield return Load("Nivel4");
            var goal = Object.FindFirstObjectByType<Goal>();
            player.Teleport((Vector2)goal.transform.position + Vector2.up * 0.1f);
            yield return new WaitForSecondsRealtime(3.5f);
            Assert.AreEqual("Nivel5", SceneManager.GetActiveScene().name, "La meta del nivel 4 no llevó al nivel 5.");
        }

        [UnityTest]
        public IEnumerator Level5_BossWakesInTheArena_AndBeatingItEndsTheGame()
        {
            yield return Load("Nivel5");
            var arena = Object.FindFirstObjectByType<BossArena>();
            var boss = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).First(e => e.Config.superArmor);
            Assert.IsTrue(boss.IsDormant, "El jefe debería esperar dormido.");
            Assert.IsNull(Object.FindFirstObjectByType<Goal>(), "La meta no debería verse antes de vencer al jefe.");

            player.Teleport(arena.transform.position);
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(arena.Started, "Entrar en la arena no empezó la pelea.");
            for (int i = 0; i < 40 && !boss.Health.IsDead; i++)
                boss.Health.TryDamage(1, player.transform.position, ignoreInvulnerability: true);
            yield return new WaitForSeconds(0.6f);
            var goal = Object.FindFirstObjectByType<Goal>();
            Assert.IsNotNull(goal, "La meta no apareció tras vencer al jefe.");
            player.Teleport((Vector2)goal.transform.position + Vector2.up * 0.1f);
            yield return new WaitForSecondsRealtime(3.5f);
            Assert.AreEqual(GameFlow.MenuScene, SceneManager.GetActiveScene().name, "Vencer al jefe final no terminó el juego.");
        }
    }
}
