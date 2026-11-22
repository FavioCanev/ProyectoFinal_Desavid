using System;
using System.Collections.Generic;

namespace Game
{
    public enum EnemyAttack { None, Swipe, Charge, Spit, Leap }

    /// <summary>Un ataque posible: su tipo, el rango de distancias en que sirve y si ya terminó su espera.</summary>
    public readonly struct AttackOption
    {
        public readonly EnemyAttack Kind;
        public readonly bool Enabled;
        public readonly float MinRange, MaxRange;
        public readonly bool Ready;

        public AttackOption(EnemyAttack kind, bool enabled, float minRange, float maxRange, bool ready)
        {
            Kind = kind; Enabled = enabled; MinRange = minRange; MaxRange = maxRange; Ready = ready;
        }
    }

    /// <summary>
    /// Lógica pura: elige un ataque entre los activos, listos y cuyo rango incluye la distancia.
    /// Si hay varios, elige al azar (así los jefes varían sus patrones).
    /// </summary>
    public static class EnemyAttackPicker
    {
        public static EnemyAttack Pick(float distance, IReadOnlyList<AttackOption> options, Random rng)
        {
            var valid = new List<EnemyAttack>();
            foreach (var o in options)
                if (o.Enabled && o.Ready && distance >= o.MinRange && distance <= o.MaxRange)
                    valid.Add(o.Kind);
            return valid.Count == 0 ? EnemyAttack.None : valid[rng.Next(valid.Count)];
        }
    }
}
