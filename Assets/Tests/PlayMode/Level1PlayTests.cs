using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>Pruebas en modo Play sobre el Nivel 1 real (bosque de Dante, ampliado).</summary>
    public class Level1PlayTests
    {
        private PlayerController player;
        private PlayerCombat combat;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            Time.timeScale = 1f;
            GameFlow.Run.Reset();
            SceneManager.LoadScene("Nivel1");
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            combat = player.GetComponent<PlayerCombat>();
            yield return new WaitForSeconds(1f);
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_LandsOnDantesGround()
        {
            yield return null;
            Assert.IsTrue(player.IsGrounded, $"El jugador no está en el suelo (y = {player.transform.position.y:F2}).");
        }

        [UnityTest]
        public IEnumerator Lumberjack_CanBeDefeated()
        {
            EnemyController enemy = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .OrderBy(e => Mathf.Abs(e.transform.position.x - player.transform.position.x)).First();
            Health health = enemy.Health;
            for (int i = 0; i < 10 && health != null && !health.IsDead; i++)
            {
                player.Teleport((Vector2)enemy.transform.position + new Vector2(-1.1f, 0.05f));
                player.Face(1);
                yield return new WaitForFixedUpdate();
                combat.TryAttack(AttackDirection.Forward);
                yield return new WaitForSeconds(0.4f);
            }
            Assert.IsTrue(health == null || health.IsDead, "El leñador sigue vivo tras varios ataques.");
        }

        [UnityTest]
        public IEnumerator Goal_LoadsLevel2()
        {
            var goal = Object.FindFirstObjectByType<Goal>();
            player.Teleport((Vector2)goal.transform.position + Vector2.up * 0.1f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(goal.Reached, "Tocar la meta no completó el nivel.");
            yield return new WaitForSecondsRealtime(3f);
            Assert.AreEqual("Nivel2_Tierra", SceneManager.GetActiveScene().name, "La meta no llevó al nivel 2.");
        }
    }
}
