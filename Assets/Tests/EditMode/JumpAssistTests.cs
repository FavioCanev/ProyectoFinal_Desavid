using NUnit.Framework;

namespace Game.Tests
{
    public class JumpAssistTests
    {
        private const float Step = 0.02f;
        private const float Coyote = 0.1f;
        private const float Buffer = 0.12f;

        private static JumpAssist Create() => new JumpAssist(Coyote, Buffer);

        [Test]
        public void NoJump_WhenButtonNeverPressed()
        {
            var jump = Create();
            jump.Tick(Step, grounded: true, jumpPressed: false);
            Assert.IsFalse(jump.TryConsumeJump());
        }

        [Test]
        public void Jumps_WhenPressedOnGround()
        {
            var jump = Create();
            jump.Tick(Step, grounded: true, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeJump());
        }

        [Test]
        public void NoJump_WhenPressedInAirWithoutCoyote()
        {
            var jump = Create();
            for (int i = 0; i < 20; i++) jump.Tick(Step, grounded: false, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsFalse(jump.TryConsumeJump());
        }

        [Test]
        public void CoyoteTime_AllowsJumpShortlyAfterLeavingGround()
        {
            var jump = Create();
            jump.Tick(Step, grounded: true, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeJump());
        }

        [Test]
        public void CoyoteTime_ExpiresAfterItsDuration()
        {
            var jump = Create();
            jump.Tick(Step, grounded: true, jumpPressed: false);
            for (int i = 0; i < 10; i++) jump.Tick(Step, grounded: false, jumpPressed: false); // 0.2 s > 0.1 s
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsFalse(jump.TryConsumeJump());
        }

        [Test]
        public void JumpBuffer_JumpsOnLandingIfPressedJustBefore()
        {
            var jump = Create();
            jump.Tick(Step, grounded: false, jumpPressed: true);
            jump.Tick(Step, grounded: false, jumpPressed: false);
            jump.Tick(Step, grounded: true, jumpPressed: false);
            Assert.IsTrue(jump.TryConsumeJump());
        }

        [Test]
        public void JumpBuffer_ExpiresIfLandingIsTooLate()
        {
            var jump = Create();
            jump.Tick(Step, grounded: false, jumpPressed: true);
            for (int i = 0; i < 8; i++) jump.Tick(Step, grounded: false, jumpPressed: false); // 0.16 s > 0.12 s
            jump.Tick(Step, grounded: true, jumpPressed: false);
            Assert.IsFalse(jump.TryConsumeJump());
        }

        [Test]
        public void CannotDoubleJump_ConsumingTwiceInARowFails()
        {
            var jump = Create();
            jump.Tick(Step, grounded: true, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeJump());
            Assert.IsFalse(jump.TryConsumeJump());
        }

        [Test]
        public void CannotJumpAgain_WithoutANewPress_EvenIfStillGrounded()
        {
            var jump = Create();
            jump.Tick(Step, grounded: true, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeJump());
            jump.Tick(Step, grounded: true, jumpPressed: false);
            Assert.IsFalse(jump.TryConsumeJump());
        }

        [Test]
        public void DoubleJump_OnceInTheAir_UntilLanding()
        {
            var jump = new JumpAssist(Coyote, Buffer, airJumps: 1);
            jump.Tick(Step, grounded: true, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeJump(), "Salto desde el suelo.");
            for (int i = 0; i < 10; i++) jump.Tick(Step, grounded: false, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsFalse(jump.TryConsumeJump(), "En el aire no cuenta como salto desde el suelo.");
            Assert.IsTrue(jump.TryConsumeAirJump(), "Doble salto.");
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsFalse(jump.TryConsumeAirJump(), "Solo uno en el aire.");
            jump.Tick(Step, grounded: true, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeJump() || jump.TryConsumeAirJump(), "Al tocar el suelo se recarga.");
        }

        [Test]
        public void AirJump_WaitsForCoyoteToEnd_SoLedgeJumpsStayNormal()
        {
            var jump = new JumpAssist(Coyote, Buffer, airJumps: 1);
            jump.Tick(Step, grounded: true, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: true);   // recién salido del borde: aún hay coyote
            Assert.IsFalse(jump.TryConsumeAirJump());
            Assert.IsTrue(jump.TryConsumeJump(), "Dentro del coyote es un salto normal.");
        }

        [Test]
        public void Bounce_RefillsTheAirJump()
        {
            var jump = new JumpAssist(Coyote, Buffer, airJumps: 1);
            jump.Tick(Step, grounded: true, jumpPressed: false);
            for (int i = 0; i < 10; i++) jump.Tick(Step, grounded: false, jumpPressed: false);
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeAirJump());
            jump.RefillAirJumps();   // rebote con el ataque hacia abajo
            jump.Tick(Step, grounded: false, jumpPressed: true);
            Assert.IsTrue(jump.TryConsumeAirJump(), "Tras rebotar en un enemigo se puede volver a saltar en el aire.");
        }
    }
}
