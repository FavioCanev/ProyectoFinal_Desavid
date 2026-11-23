using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Reglas del trazado del Nivel 3 que se pueden comprobar sin jugar.</summary>
    public class Level3LayoutTests
    {
        // El jugador mide 1,6 de alto (su cápsula); con un pequeño margen para no rozar la cabeza.
        private const float PlayerHeight = 1.7f;

        [Test]
        public void OverheadPlatforms_LeaveRoomToWalkUnder()
        {
            foreach (var top in Level3Layout.Blocks)
            foreach (var floor in Level3Layout.Blocks)
            {
                bool overlapsX = top.X0 <= floor.X1 && floor.X0 <= top.X1;
                if (!overlapsX || floor.Y1 >= top.Y0 || floor.Kind != Level3Layout.Kind.Earth) continue;

                float clearance = (top.Y0 - (floor.Y1 + 1)) * Level3Layout.Cell;
                // 0 = apoyada en el suelo (obstáculo sólido). Entre 0 y la altura del jugador = trampa de cabeza.
                Assert.IsFalse(clearance > 0f && clearance < PlayerHeight,
                    $"El bloque en columnas {top.X0}-{top.X1}, fila {top.Y0}, deja solo {clearance:F2} de alto " +
                    $"sobre el suelo (fila {floor.Y1}): el jugador no cabe debajo.");
            }
        }

        [Test]
        public void DashGaps_NeedDashButAreReachableWithIt()
        {
            // Medidas del jugador con la configuración por defecto (ver PlayerConfig).
            var config = UnityEngine.ScriptableObject.CreateInstance<PlayerConfig>();
            float g = UnityEngine.Mathf.Abs(UnityEngine.Physics2D.gravity.y);
            float rise = config.JumpVelocity / (g * config.riseGravityScale);
            float fall = UnityEngine.Mathf.Sqrt(2f * config.jumpHeight / (g * config.fallGravityScale));
            float jump = config.runSpeed * (rise + fall);
            float jumpAndDash = jump + config.dashSpeed * config.dashDuration;

            // Los dos huecos de dash del nivel miden 17 columnas.
            float gap = 17 * Level3Layout.Cell;
            Assert.Greater(gap, jump, "El hueco de dash se puede cruzar sin dash.");
            Assert.Less(gap, jumpAndDash - 0.5f, "El hueco de dash es demasiado justo incluso con dash.");
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
