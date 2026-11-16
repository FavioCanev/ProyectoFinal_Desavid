using UnityEngine;

namespace Game
{
    /// <summary>
    /// Enemigo de suelo: patrulla y se gira ante muros o bordes; si ve al jugador cerca y a su altura, lo persigue
    /// (sin tirarse por los bordes). Al recibir un golpe retrocede; al morir se detiene y desaparece.
    /// El daño al jugador es por contacto (lo comprueba PlayerLife).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Animator animator;
        [SerializeField] private LayerMask groundMask;

        [Header("Movimiento")]
        [SerializeField, Min(0f)] private float patrolSpeed = 1.8f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.6f;
        [Tooltip("Distancia horizontal a la que detecta al jugador.")]
        [SerializeField, Min(0f)] private float detectRange = 6f;
        [Tooltip("Diferencia de altura máxima para perseguir.")]
        [SerializeField, Min(0f)] private float detectHeight = 2f;
        [Tooltip("A esta distancia muestra la animación de ataque.")]
        [SerializeField, Min(0f)] private float attackRange = 1.4f;
        [SerializeField, Min(0f)] private float bodyHalfWidth = 0.45f;

        [Header("Golpes")]
        [SerializeField] private Vector2 hurtKnockback = new Vector2(5f, 3f);
        [SerializeField, Min(0f)] private float hurtTime = 0.3f;
        [Tooltip("Segundos que el cuerpo queda visible tras morir.")]
        [SerializeField, Min(0f)] private float corpseTime = 1.2f;

        private Rigidbody2D rb;
        private Health health;
        private Transform player;
        private int facing = 1;
        private float hurtUntil;
        private string animState;

        public Health Health => health;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void Start()
        {
            var p = FindFirstObjectByType<PlayerController>();
            if (p != null) player = p.transform;
        }

        private void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void FixedUpdate()
        {
            if (health.IsDead || Time.time < hurtUntil) return;

            bool chasing = CanSeePlayer(out float dx);
            int dir = chasing ? (dx >= 0f ? 1 : -1) : facing;
            float speed = chasing ? (Mathf.Abs(dx) > 0.6f ? chaseSpeed : 0f) : patrolSpeed;

            if (speed > 0f && Blocked(dir))
            {
                if (chasing) speed = 0f;                          // no se tira por el borde persiguiendo
                else { dir = -dir; if (Blocked(dir)) speed = 0f; } // patrulla: da la vuelta
            }

            Face(dir);
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            Play(chasing && Mathf.Abs(dx) < attackRange ? "Attack" : speed > 0.05f ? "Walk" : "Idle");
            // Al perseguir corre más rápido: la animación de caminar se acelera para que los pies no patinen.
            if (animator != null && animState == "Walk") animator.speed = Mathf.Max(0.5f, speed / patrolSpeed);
        }

        private bool CanSeePlayer(out float dx)
        {
            dx = 0f;
            if (player == null) return false;
            Vector2 d = player.position - transform.position;
            dx = d.x;
            return Mathf.Abs(d.x) <= detectRange && Mathf.Abs(d.y) <= detectHeight;
        }

        private bool Blocked(int dir) => WallAhead(dir) || !GroundAhead(dir);

        private bool WallAhead(int dir) =>
            Physics2D.Raycast((Vector2)transform.position + new Vector2(0f, 0.5f), Vector2.right * dir, bodyHalfWidth + 0.1f, groundMask);

        private bool GroundAhead(int dir) =>
            Physics2D.Raycast((Vector2)transform.position + new Vector2(dir * (bodyHalfWidth + 0.1f), 0.1f), Vector2.down, 0.6f, groundMask);

        private void OnDamaged(Vector2 source)
        {
            if (health.IsDead) return;
            float away = Mathf.Sign(transform.position.x - source.x);
            rb.linearVelocity = new Vector2(away * hurtKnockback.x, hurtKnockback.y);
            hurtUntil = Time.time + hurtTime;
            Play("Hurt");
        }

        private void OnDied()
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;   // sin colisiones: ya no daña ni recibe golpes
            Play("Dead");
            Destroy(gameObject, corpseTime);
        }

        private void Face(int dir)
        {
            facing = dir;
            if (visual == null) return;
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
        }

        private void Play(string state)
        {
            if (animator == null || state == animState) return;
            animState = state;
            animator.speed = 1f;
            animator.Play(state, 0, 0f);
        }
    }
}
