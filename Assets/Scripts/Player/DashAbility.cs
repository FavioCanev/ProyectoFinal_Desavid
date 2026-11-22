using System;

namespace Game
{
    /// <summary>
    /// Lógica pura (sin Unity) del dash: duración, tiempo de espera y usos en el aire.
    /// El tiempo de espera empieza a contar cuando el dash termina.
    /// </summary>
    public class DashAbility
    {
        private readonly float duration;
        private readonly float cooldown;
        private readonly int airDashes;
        private float timeLeft;
        private float cooldownLeft;
        private int airDashesLeft;

        public int Direction { get; private set; } = 1;
        public bool IsDashing => timeLeft > 0f;

        public DashAbility(float duration, float cooldown, int airDashes)
        {
            this.duration = Math.Max(0.01f, duration);
            this.cooldown = Math.Max(0f, cooldown);
            this.airDashes = Math.Max(0, airDashes);
            airDashesLeft = this.airDashes;
        }

        /// <summary>Avanza los tiempos. Llamar una vez por paso de física.</summary>
        public void Tick(float deltaTime, bool grounded)
        {
            if (IsDashing) timeLeft = Math.Max(0f, timeLeft - deltaTime);
            else cooldownLeft = Math.Max(0f, cooldownLeft - deltaTime);
            if (grounded && !IsDashing) airDashesLeft = airDashes;
        }

        /// <summary>Empieza un dash si se puede. Devuelve true si empezó.</summary>
        public bool TryStart(int direction, bool grounded)
        {
            if (IsDashing || cooldownLeft > 0f) return false;
            if (!grounded)
            {
                if (airDashesLeft <= 0) return false;
                airDashesLeft--;
            }
            Direction = direction >= 0 ? 1 : -1;
            timeLeft = duration;
            cooldownLeft = cooldown;
            return true;
        }

        /// <summary>Corta el dash en seco (por ejemplo, al recibir un golpe).</summary>
        public void Cancel() => timeLeft = 0f;
    }
}
