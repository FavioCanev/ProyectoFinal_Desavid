using NUnit.Framework;

namespace Game.Tests
{
    public class PlasmaAmmoTests
    {
        [Test]
        public void ThreeShots_ThenReload_ThenFullAgain()
        {
            var ammo = new PlasmaAmmo(3, 6f);
            for (int i = 0; i < 3; i++) Assert.IsTrue(ammo.TryConsume(), $"Disparo {i + 1}.");
            Assert.IsTrue(ammo.Reloading, "Al vaciarse debería recargar.");
            Assert.IsFalse(ammo.TryConsume(), "No se dispara durante la recarga.");
            ammo.Tick(5.9f);
            Assert.IsFalse(ammo.TryConsume(), "La recarga dura 6 segundos.");
            ammo.Tick(0.2f);
            Assert.AreEqual(3, ammo.Ammo);
            Assert.IsTrue(ammo.TryConsume());
        }

        [Test]
        public void Progress_GoesFromZeroToOne()
        {
            var ammo = new PlasmaAmmo(1, 4f);
            ammo.TryConsume();
            Assert.AreEqual(0f, ammo.Progress, 1e-4f);
            ammo.Tick(2f);
            Assert.AreEqual(0.5f, ammo.Progress, 1e-4f);
        }
    }
}
