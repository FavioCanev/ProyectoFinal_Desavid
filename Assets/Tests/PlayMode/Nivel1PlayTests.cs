using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Pruebas en modo Play sobre el Nivel 1 real: lo que el jugador haría al jugar.
    /// </summary>
    public class Nivel1PlayTests
    {
        private PlayerController player;
        private PlayerLife life;
        private PlayerCombat combat;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            SceneManager.LoadScene("Nivel1");
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            life = player.GetComponent<PlayerLife>();
            combat = player.GetComponent<PlayerCombat>();
            yield return new WaitForSeconds(1f);   // deja que caiga y se asiente
        }

        [UnityTest]
        public IEnumerator Player_LandsOnGround()
        {
            yield return null;
            Assert.IsTrue(player.IsGrounded, $"El jugador no está en el suelo (y = {player.transform.position.y:F2}).");
            Assert.Greater(player.transform.position.y, 0f, "El jugador atravesó el suelo.");
        }

        [UnityTest]
        public IEnumerator Attack_DefeatsNearestEnemy()
        {
            EnemyController enemy = NearestEnemy();
            Health enemyHealth = enemy.Health;

            for (int i = 0; i < 8 && enemyHealth != null && !enemyHealth.IsDead; i++)
            {
                player.Teleport((Vector2)enemy.transform.position + new Vector2(-1f, 0f));
                player.Face(1);
                yield return new WaitForFixedUpdate();
                combat.TryAttack(AttackDirection.Forward);
                yield return new WaitForSeconds(0.4f);
            }

            Assert.IsTrue(enemyHealth == null || enemyHealth.IsDead, "El enemigo sigue vivo tras varios ataques.");
        }

        [UnityTest]
        public IEnumerator DownAttackOnEnemy_BouncesPlayerUp()
        {
            EnemyController enemy = NearestEnemy();
            player.Teleport((Vector2)enemy.transform.position + new Vector2(0f, 1.8f));
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(combat.TryAttack(AttackDirection.Down));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Greater(player.Velocity.y, 5f, "El ataque hacia abajo no hizo rebotar al jugador.");
        }

        [UnityTest]
        public IEnumerator FallingIntoPit_CostsOneMask_AndRespawnsAtCheckpoint()
        {
            int before = life.Health.Current;
            Vector2 checkpoint = life.Checkpoint;

            player.Teleport(Level1Layout.OnSurface(Level1Layout.PitProbe));
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(before - 1, life.Health.Current, "Caer al foso debería costar una máscara.");
            Assert.Less(Vector2.Distance(player.transform.position, checkpoint), 1.5f, "No reapareció en el punto de reaparición.");
        }

        [UnityTest]
        public IEnumerator ReachingGoal_CompletesLevel()
        {
            var goal = Object.FindFirstObjectByType<Goal>();
            player.Teleport((Vector2)goal.transform.position + Vector2.up * 0.1f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(goal.Reached, "Tocar la meta no completó el nivel.");
        }

        [UnityTest]
        public IEnumerator Enemies_PatrolOnTheirOwn()
        {
            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            float[] startX = enemies.Select(e => e.transform.position.x).ToArray();
            yield return new WaitForSeconds(1.5f);

            for (int i = 0; i < enemies.Length; i++)
            {
                Assert.Greater(Mathf.Abs(enemies[i].transform.position.x - startX[i]), 0.5f, $"El enemigo {i} no se movió.");
                Assert.Greater(enemies[i].transform.position.y, -1f, $"El enemigo {i} cayó del nivel.");
            }
        }

        private EnemyController NearestEnemy() =>
            Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .OrderBy(e => Mathf.Abs(e.transform.position.x - player.transform.position.x))
                .First();
    }
}
