using System;

namespace Game
{
    /// <summary>
    /// Lógica pura de vida e invulnerabilidad (sin Unity), para poder probarla.
    /// </summary>
    public class HealthPool
    {
        private readonly float invulnerabilityTime;
        private float invulnerableUntil;

        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        public HealthPool(int max, float invulnerabilityTime)
        {
            Max = Math.Max(1, max);
            Current = Max;
            this.invulnerabilityTime = Math.Max(0f, invulnerabilityTime);
        }

        public bool IsInvulnerable(float now) => now < invulnerableUntil;

        /// <summary>Resta vida si se puede. Devuelve true si el golpe contó.</summary>
        public bool TryDamage(int amount, float now, bool ignoreInvulnerability = false)
        {
            if (IsDead || amount <= 0) return false;
            if (!ignoreInvulnerability && IsInvulnerable(now)) return false;
            Current = Math.Max(0, Current - amount);
            invulnerableUntil = now + invulnerabilityTime;
            return true;
        }

        public void Restore()
        {
            Current = Max;
            invulnerableUntil = 0f;
        }
    }
}
