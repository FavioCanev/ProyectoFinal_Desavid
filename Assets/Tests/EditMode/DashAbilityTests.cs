using NUnit.Framework;

namespace Game.Tests
{
    public class DashAbilityTests
    {
        private const float Step = 0.02f;

        private static DashAbility Create() => new DashAbility(duration: 0.18f, cooldown: 0.4f, airDashes: 1);

        private static void Advance(DashAbility dash, float seconds, bool grounded)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Step) dash.Tick(Step, grounded);
        }

        [Test]
        public void StartsOnGround_AndLastsItsDuration()
        {
            var dash = Create();
            Assert.IsTrue(dash.TryStart(1, grounded: true));
            Assert.IsTrue(dash.IsDashing);
            Advance(dash, 0.2f, grounded: true);
            Assert.IsFalse(dash.IsDashing);
        }

        [Test]
        public void CannotStartAgain_DuringDashOrCooldown()
        {
            var dash = Create();
            dash.TryStart(1, grounded: true);
            Assert.IsFalse(dash.TryStart(1, grounded: true), "Durante el dash.");
            Advance(dash, 0.2f, grounded: true);
            Assert.IsFalse(dash.TryStart(1, grounded: true), "Durante el tiempo de espera.");
            Advance(dash, 0.42f, grounded: true);
            Assert.IsTrue(dash.TryStart(1, grounded: true), "Tras el tiempo de espera.");
        }

        [Test]
        public void OnlyOneAirDash_UntilLanding()
        {
            var dash = Create();
            Assert.IsTrue(dash.TryStart(1, grounded: false));
            Advance(dash, 0.7f, grounded: false);
            Assert.IsFalse(dash.TryStart(1, grounded: false), "Segundo dash en el aire.");
            Advance(dash, Step, grounded: true);
            Assert.IsTrue(dash.TryStart(1, grounded: false), "Tras tocar el suelo se recarga.");
        }

        [Test]
        public void IsReady_MatchesWhetherADashCouldStart()
        {
            var dash = Create();
            Assert.IsTrue(dash.IsReady(grounded: true));
            dash.TryStart(1, grounded: false);
            Assert.IsFalse(dash.IsReady(grounded: false), "Durante el dash.");
            Advance(dash, 0.7f, grounded: false);
            Assert.IsFalse(dash.IsReady(grounded: false), "Ya gastó el dash aéreo.");
            Assert.IsTrue(dash.IsReady(grounded: true), "En el suelo vuelve a estar listo.");
        }

        [Test]
        public void Direction_FollowsSign()
        {
            var dash = Create();
            dash.TryStart(-1, grounded: true);
            Assert.AreEqual(-1, dash.Direction);
        }

        [Test]
        public void Cancel_StopsDashImmediately()
        {
            var dash = Create();
            dash.TryStart(1, grounded: true);
            dash.Cancel();
            Assert.IsFalse(dash.IsDashing);
        }
    }
}
