using NUnit.Framework;

namespace Game.Tests
{
    public class HealthPoolTests
    {
        [Test]
        public void StartsFull()
        {
            var h = new HealthPool(5, 1f);
            Assert.AreEqual(5, h.Current);
            Assert.IsFalse(h.IsDead);
        }

        [Test]
        public void Damage_ReducesHealth()
        {
            var h = new HealthPool(5, 1f);
            Assert.IsTrue(h.TryDamage(1, now: 0f));
            Assert.AreEqual(4, h.Current);
        }

        [Test]
        public void Invulnerability_BlocksHitsUntilItExpires()
        {
            var h = new HealthPool(5, 1f);
            h.TryDamage(1, now: 0f);
            Assert.IsFalse(h.TryDamage(1, now: 0.5f), "Golpe durante la invulnerabilidad.");
            Assert.IsTrue(h.TryDamage(1, now: 1.1f), "Golpe después de la invulnerabilidad.");
            Assert.AreEqual(3, h.Current);
        }

        [Test]
        public void IgnoreInvulnerability_AlwaysHits()
        {
            var h = new HealthPool(5, 1f);
            h.TryDamage(1, now: 0f);
            Assert.IsTrue(h.TryDamage(1, now: 0.1f, ignoreInvulnerability: true));
            Assert.AreEqual(3, h.Current);
        }

        [Test]
        public void ReachingZero_Dies_AndNoMoreDamage()
        {
            var h = new HealthPool(2, 0f);
            h.TryDamage(5, now: 0f);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(0, h.Current);
            Assert.IsFalse(h.TryDamage(1, now: 1f));
        }

        [Test]
        public void Restore_RefillsAndClearsInvulnerability()
        {
            var h = new HealthPool(3, 10f);
            h.TryDamage(3, now: 0f);
            h.Restore();
            Assert.AreEqual(3, h.Current);
            Assert.IsFalse(h.IsInvulnerable(now: 0.1f));
        }
    }
}
