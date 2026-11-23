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
            if (TryGetComponent(out plasma)) plasma.Fired += OnFired;
        }

        private PlayerPlasma plasma;
        private void OnFired() => Play(controller.Swimming ? "SwimAttack" : "Attack", restart: true);   // el ataque del capibara ya es su cañón de plasma

        private void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            combat.Attacked -= OnAttacked;
            if (plasma != null) plasma.Fired -= OnFired;
        }

        private void OnDamaged(Vector2 _) => hurtUntil = Time.time + hurtShowTime;
        private void OnAttacked(AttackDirection _) => Play(controller.Swimming ? "SwimAttack" : "Attack", restart: true);

        private void Update() => Play(controller.Swimming ? SwimState() : LandState(), restart: false);

        private string LandState() =>
            health.IsDead ? "Dead" :
            Time.time < hurtUntil ? "Hurt" :
            controller.IsDashing ? "Dash" :
            combat.IsAttacking ? "Attack" :
            !controller.IsGrounded ? "Jump" :
            Mathf.Abs(controller.Velocity.x) > 0.1f ? "Run" : "Idle";

        // Bajo el agua: nado horizontal o vertical según hacia dónde va más rápido.
        private string SwimState()
        {
            Vector2 v = controller.Velocity;
            return health.IsDead ? "SwimDead" :
                   Time.time < hurtUntil ? "SwimHurt" :
                   controller.IsDashing ? "SwimDash" :
                   combat.IsAttacking ? "SwimAttack" :
                   v.sqrMagnitude < 0.6f ? "SwimIdle" :
                   Mathf.Abs(v.y) > Mathf.Abs(v.x) ? "SwimUp" : "Swim";
        }

        private void Play(string state, bool restart)
        {
            if (animator == null || (state == current && !restart)) return;
            current = state;
            animator.Play(state, 0, 0f);
        }
    }
}
