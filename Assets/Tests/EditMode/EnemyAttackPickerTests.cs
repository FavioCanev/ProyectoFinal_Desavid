using System;
using NUnit.Framework;

namespace Game.Tests
{
    public class EnemyAttackPickerTests
    {
        private static readonly Random Rng = new Random(1);

        private static AttackOption Swipe(bool ready = true) => new AttackOption(EnemyAttack.Swipe, true, 0f, 1.6f, ready);
        private static AttackOption Charge(bool ready = true) => new AttackOption(EnemyAttack.Charge, true, 2.5f, 8f, ready);

        [Test]
        public void PicksTheAttackWhoseRangeFits()
        {
            Assert.AreEqual(EnemyAttack.Swipe, EnemyAttackPicker.Pick(1f, new[] { Swipe(), Charge() }, Rng));
            Assert.AreEqual(EnemyAttack.Charge, EnemyAttackPicker.Pick(5f, new[] { Swipe(), Charge() }, Rng));
        }

        [Test]
        public void ReturnsNone_WhenNothingIsInRange()
        {
            Assert.AreEqual(EnemyAttack.None, EnemyAttackPicker.Pick(2f, new[] { Swipe(), Charge() }, Rng));
        }

        [Test]
        public void SkipsAttacksStillOnCooldown()
        {
            Assert.AreEqual(EnemyAttack.None, EnemyAttackPicker.Pick(1f, new[] { Swipe(ready: false) }, Rng));
        }

        [Test]
        public void SkipsDisabledAttacks()
        {
            var disabled = new AttackOption(EnemyAttack.Spit, false, 0f, 10f, true);
            Assert.AreEqual(EnemyAttack.None, EnemyAttackPicker.Pick(3f, new[] { disabled }, Rng));
        }

        [Test]
        public void WithSeveralValid_EventuallyUsesEach()
        {
            var options = new[]
            {
                new AttackOption(EnemyAttack.Charge, true, 2f, 8f, true),
                new AttackOption(EnemyAttack.Spit, true, 2f, 10f, true),
                new AttackOption(EnemyAttack.Leap, true, 3f, 9f, true),
            };
            var rng = new Random(42);
            bool charge = false, spit = false, leap = false;
            for (int i = 0; i < 60; i++)
            {
                var a = EnemyAttackPicker.Pick(5f, options, rng);
                charge |= a == EnemyAttack.Charge; spit |= a == EnemyAttack.Spit; leap |= a == EnemyAttack.Leap;
            }
            Assert.IsTrue(charge && spit && leap, "El selector no varía entre los ataques válidos.");
        }
    }
}
