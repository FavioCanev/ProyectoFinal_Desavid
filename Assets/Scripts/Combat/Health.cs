using System;
using UnityEngine;

namespace Game
{
    /// <summary>Vida de cualquier personaje (jugador o enemigo).</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [Tooltip("Segundos sin recibir daño después de un golpe.")]
        [SerializeField, Min(0f)] private float invulnerabilityTime = 0.2f;

        private HealthPool pool;

        /// <summary>Se dispara al recibir daño; el parámetro es la posición de quien golpeó.</summary>
        public event Action<Vector2> Damaged;
        public event Action Died;
        public event Action Changed;

        private HealthPool Pool => pool ??= new HealthPool(maxHealth, invulnerabilityTime);

        public int Current => Pool.Current;
        public int Max => Pool.Max;
        public bool IsDead => Pool.IsDead;
        public bool IsInvulnerable => Pool.IsInvulnerable(Time.time);

        public bool TryDamage(int amount, Vector2 source, bool ignoreInvulnerability = false)
        {
            if (!Pool.TryDamage(amount, Time.time, ignoreInvulnerability)) return false;
            Changed?.Invoke();
            Damaged?.Invoke(source);
            if (Pool.IsDead) Died?.Invoke();
            return true;
        }

        public void Restore()
        {
            Pool.Restore();
            Changed?.Invoke();
        }
    }
}
