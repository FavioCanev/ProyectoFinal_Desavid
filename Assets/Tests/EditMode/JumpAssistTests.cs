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
    }
}
