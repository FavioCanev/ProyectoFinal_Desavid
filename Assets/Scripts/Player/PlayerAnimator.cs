using UnityEngine;

namespace Game
{
    /// <summary>
    /// Elige la animación del jugador según su estado. El Animator solo tiene estados con nombre
    /// (sin transiciones): el código decide cuál reproducir.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerCombat), typeof(Health))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("Segundos que se muestra la animación de daño.")]
        [SerializeField, Min(0f)] private float hurtShowTime = 0.25f;

        private PlayerController controller;
        private PlayerCombat combat;
        private Health health;
        private string current;
        private float hurtUntil;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            combat.Attacked += OnAttacked;
        }

        private void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            combat.Attacked -= OnAttacked;
        }

        private void OnDamaged(Vector2 _) => hurtUntil = Time.time + hurtShowTime;
        private void OnAttacked(AttackDirection _) => Play("Attack", restart: true);

        private void Update()
        {
            string state =
                health.IsDead ? "Dead" :
                Time.time < hurtUntil ? "Hurt" :
                controller.IsDashing ? "Dash" :
                combat.IsAttacking ? "Attack" :
                !controller.IsGrounded ? "Jump" :
                Mathf.Abs(controller.Velocity.x) > 0.1f ? "Run" : "Idle";
            Play(state, restart: false);
        }

        private void Play(string state, bool restart)
        {
            if (animator == null || (state == current && !restart)) return;
            current = state;
            animator.Play(state, 0, 0f);
        }
    }
}
