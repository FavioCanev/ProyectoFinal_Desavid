using System;

namespace Game
{
    /// <summary>
    /// Lógica pura (sin Unity) del cargador de plasma, como lo diseñó Edward: unos pocos disparos y,
    /// al gastarlos, una recarga larga. El reloj avanza solo con Tick (la pausa lo detiene).
    /// </summary>
    public class PlasmaAmmo
    {
        private readonly float reloadTime;

        public int Capacity { get; }
        public int Ammo { get; private set; }
        public float ReloadLeft { get; private set; }
        public bool Reloading => ReloadLeft > 0f;
        /// <summary>0 al empezar la recarga, 1 con el cargador lleno.</summary>
        public float Progress => Reloading ? 1f - ReloadLeft / reloadTime : 1f;

        public PlasmaAmmo(int capacity, float reloadTime)
        {
            Capacity = Math.Max(1, capacity);
            this.reloadTime = Math.Max(0.01f, reloadTime);
            Ammo = Capacity;
        }

        /// <summary>Gasta un disparo si hay. Al vaciarse empieza la recarga.</summary>
        public bool TryConsume()
        {
            if (Reloading || Ammo <= 0) return false;
            Ammo--;
            if (Ammo == 0) ReloadLeft = reloadTime;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!Reloading) return;
            ReloadLeft = Math.Max(0f, ReloadLeft - deltaTime);
            if (!Reloading) Ammo = Capacity;
        }
    }
}
