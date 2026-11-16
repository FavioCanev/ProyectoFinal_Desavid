using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum AttackDirection { Forward, Up, Down }

    /// <summary>
    /// Ataque en 3 direcciones. Hacia abajo solo en el aire; si golpea algo, el jugador rebota (pogo).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        private PlayerController controller;
        private float cooldownUntil;
        private float attackingUntil;
        private readonly HashSet<Health> alreadyHit = new HashSet<Health>();

        public event Action<AttackDirection> Attacked;
        public bool IsAttacking => Time.time < attackingUntil;
        public AttackDirection LastDirection { get; private set; }

        private PlayerConfig Config => controller.Config;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            controller.AttackPressed += OnAttackPressed;
        }

        private void OnDestroy() => controller.AttackPressed -= OnAttackPressed;

        private void OnAttackPressed() => TryAttack(ChooseDirection());

        public AttackDirection ChooseDirection()
        {
            if (controller.VerticalInput > 0.5f) return AttackDirection.Up;
            if (!controller.IsGrounded && controller.VerticalInput < -0.5f) return AttackDirection.Down;
            return AttackDirection.Forward;
        }

        /// <summary>Ataca si ya pasó el tiempo de espera. Devuelve true si atacó.</summary>
        public bool TryAttack(AttackDirection direction)
        {
            if (Time.time < cooldownUntil) return false;
            cooldownUntil = Time.time + Config.attackCooldown;
            attackingUntil = Time.time + Config.attackDuration;
            LastDirection = direction;
            Attacked?.Invoke(direction);

            bool hitSomething = HitEnemies(direction);
            if (direction == AttackDirection.Down && hitSomething) controller.Bounce(Config.pogoVelocity);
            return true;
        }

        private bool HitEnemies(AttackDirection direction)
        {
            GetHitBox(direction, out Vector2 center, out Vector2 size);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, Config.enemyMask);

            alreadyHit.Clear();
            bool hitSomething = false;
            foreach (Collider2D hit in hits)
            {
                var target = hit.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !alreadyHit.Add(target)) continue;
                if (target.TryDamage(Config.attackDamage, transform.position)) hitSomething = true;
            }
            return hitSomething;
        }

        private void GetHitBox(AttackDirection direction, out Vector2 center, out Vector2 size)
        {
            Vector2 origin = transform.position;
            switch (direction)
            {
                case AttackDirection.Up:
                    center = origin + Config.upHitOffset; size = Config.upHitSize; break;
                case AttackDirection.Down:
                    center = origin + Config.downHitOffset; size = Config.downHitSize; break;
                default:
                    center = origin + new Vector2(Config.forwardHitOffset.x * controller.Facing, Config.forwardHitOffset.y);
                    size = Config.forwardHitSize; break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (controller == null || controller.Config == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            foreach (AttackDirection d in Enum.GetValues(typeof(AttackDirection)))
            {
                GetHitBox(d, out Vector2 c, out Vector2 s);
                Gizmos.DrawWireCube(c, s);
            }
        }
    }
}
