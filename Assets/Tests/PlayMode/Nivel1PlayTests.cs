using System.Collections;
using System.Collections.Generic;
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
            Time.timeScale = 1f;
            SceneManager.LoadScene("Nivel1");
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            life = player.GetComponent<PlayerLife>();
            combat = player.GetComponent<PlayerCombat>();
            yield return new WaitForSeconds(1f);   // deja que caiga y se asiente
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1f;   // por si una pausa de impacto quedó a medias
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_LandsOnGround()
        {
            yield return null;
            Assert.IsTrue(player.IsGrounded, $"El jugador no está en el suelo (y = {player.transform.position.y:F2}).");
            Assert.Greater(player.transform.position.y, 0f, "El jugador atravesó el suelo.");
        }

        [UnityTest]
        public IEnumerator Dash_MovesPlayerAboutThreeUnits()
        {
            float startX = player.transform.position.x;
            player.Face(1);
            player.RequestDash();
            yield return new WaitForSeconds(0.4f);
            float moved = player.transform.position.x - startX;
            Assert.That(moved, Is.InRange(2.6f, 4.6f), $"El dash recorrió {moved:F2} unidades.");
        }

        [UnityTest]
        public IEnumerator Attack_DefeatsNearestEnemy()
        {
            EnemyController enemy = Nearest(e => !e.Config.superArmor);
            Health enemyHealth = enemy.Health;

            for (int i = 0; i < 10 && enemyHealth != null && !enemyHealth.IsDead; i++)
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
            EnemyController enemy = Nearest(e => !e.Config.superArmor);
            player.Teleport((Vector2)enemy.transform.position + new Vector2(0f, 1.8f));
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(combat.TryAttack(AttackDirection.Down));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Greater(player.Velocity.y, 5f, "El ataque hacia abajo no hizo rebotar al jugador.");
        }

        [UnityTest]
        public IEnumerator FallingIntoAcid_CostsOneMask_AndRespawnsAtCheckpoint()
        {
            int before = life.Health.Current;
            Vector2 checkpoint = life.Checkpoint;

            player.Teleport(Level1Layout.OnSurface(Level1Layout.PitProbe));
            yield return new WaitForSeconds(2f);

            Assert.AreEqual(before - 1, life.Health.Current, "Caer al ácido debería costar una máscara.");
            Assert.Less(Vector2.Distance(player.transform.position, checkpoint), 1.5f, "No reapareció en el punto de reaparición.");
        }

        [UnityTest]
        public IEnumerator Enemies_PatrolOnTheirOwn()
        {
            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Where(e => !e.IsDormant).ToArray();
            float[] startX = enemies.Select(e => e.transform.position.x).ToArray();
            yield return new WaitForSeconds(1.5f);

            for (int i = 0; i < enemies.Length; i++)
            {
                Vector3 p = enemies[i].transform.position;
                Assert.Greater(Mathf.Abs(p.x - startX[i]), 0.5f,
                    $"{enemies[i].name} no se movió: empezó en x={startX[i]:F2} (columna {startX[i] / Level1Layout.Cell:F0}) y quedó en ({p.x:F2}, {p.y:F2}).");
                Assert.Greater(enemies[i].transform.position.y, 0f, $"{enemies[i].name} cayó del nivel.");
            }
        }

        [UnityTest]
        public IEnumerator Warrior_AttacksAndHurtsPlayer_WhenClose()
        {
            EnemyController warrior = Nearest(e => e.Config.charge.enabled && !e.Config.superArmor);
            var attacks = new List<EnemyAttack>();
            warrior.AttackStarted += attacks.Add;

            player.Teleport((Vector2)warrior.transform.position + new Vector2(-1.2f, 0f));
            yield return new WaitForSeconds(2f);

            Assert.IsNotEmpty(attacks, "El guerrero no atacó al tener al jugador cerca.");
            Assert.Less(life.Health.Current, life.Health.Max, "El jugador no recibió daño.");
        }

        [UnityTest]
        public IEnumerator Spitter_HitsPlayerWithAcid()
        {
            EnemyController spitter = Nearest(e => e.Config.spit.enabled && !e.Config.superArmor);
            var attacks = new List<EnemyAttack>();
            spitter.AttackStarted += attacks.Add;

            player.Teleport((Vector2)spitter.transform.position + new Vector2(-6f, 0.05f));
            yield return new WaitForSeconds(3f);

            CollectionAssert.Contains(attacks, EnemyAttack.Spit, "El escupidor no escupió.");
            Assert.Less(life.Health.Current, life.Health.Max, "El ácido no alcanzó al jugador.");
        }

        [UnityTest]
        public IEnumerator BossArena_StartsOnEntry_AndGoalAppearsWhenBossDies()
        {
            var arena = Object.FindFirstObjectByType<BossArena>();
            EnemyController boss = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).First(e => e.Config.superArmor);
            Assert.IsTrue(boss.IsDormant, "El jefe debería esperar dormido.");
            Assert.IsNull(Object.FindFirstObjectByType<Goal>(), "La meta no debería verse antes de vencer al jefe.");

            player.Teleport(Level1Layout.OnSurface(new Vector2Int(370, 0)));
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(arena.Started, "Entrar en la arena no empezó la pelea.");
            Assert.IsFalse(boss.IsDormant, "El jefe no despertó.");

            for (int i = 0; i < 40 && !boss.Health.IsDead; i++)
                boss.Health.TryDamage(1, player.transform.position, ignoreInvulnerability: true);
            yield return new WaitForSeconds(0.6f);

            Assert.IsTrue(arena.Cleared, "Vencer al jefe no cerró la pelea.");
            var goal = Object.FindFirstObjectByType<Goal>();
            Assert.IsNotNull(goal, "La meta no apareció tras vencer al jefe.");
            player.Teleport((Vector2)goal.transform.position + Vector2.up * 0.1f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(goal.Reached, "Tocar la meta no completó el nivel.");
        }

        [UnityTest]
        public IEnumerator MovingPlatform_CarriesPlayer()
        {
            var platform = Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).OrderBy(p => p.transform.position.x).First();
            player.Teleport((Vector2)platform.transform.position + Vector2.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            float playerStart = player.transform.position.x, platformStart = platform.transform.position.x;

            yield return new WaitForSeconds(1.2f);
            float platformMoved = platform.transform.position.x - platformStart;
            float playerMoved = player.transform.position.x - playerStart;

            Assert.Greater(Mathf.Abs(platformMoved), 1f, "La plataforma no se movió.");
            Assert.IsTrue(player.IsGrounded, "El jugador se cayó de la plataforma.");
            Assert.Less(Mathf.Abs(playerMoved - platformMoved), 0.5f, "El jugador no se movió con la plataforma.");
        }

        [UnityTest]
        public IEnumerator Dash_BlocksEnemyDamage_ButNotAcid()
        {
            player.Face(1);
            player.RequestDash();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(player.IsDashing, "El dash no empezó.");

            int before = life.Health.Current;
            Assert.IsFalse(life.Health.TryDamage(1, player.transform.position), "Un golpe hizo daño durante el dash.");
            Assert.AreEqual(before, life.Health.Current);

            life.HazardHit();
            Assert.AreEqual(before - 1, life.Health.Current, "El ácido debe dañar aunque se esté haciendo dash.");
        }

        [UnityTest]
        public IEnumerator PauseMenu_FreezesGame_AndResumes()
        {
            var pause = Object.FindFirstObjectByType<PauseMenu>();
            pause.SetPaused(true);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "La pausa no detuvo el tiempo.");
            Assert.IsFalse(player.enabled, "El jugador sigue leyendo teclas en pausa.");

            pause.SetPaused(false);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(player.enabled);
        }

        private EnemyController Nearest(System.Func<EnemyController, bool> filter) =>
            Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .Where(filter)
                .OrderBy(e => Mathf.Abs(e.transform.position.x - player.transform.position.x))
                .First();
    }
}
