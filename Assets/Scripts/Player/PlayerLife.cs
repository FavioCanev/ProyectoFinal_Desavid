using UnityEngine;

namespace Game
{
    /// <summary>
    /// Daño por contacto con enemigos, empujón, parpadeo de invulnerabilidad,
    /// caídas al vacío, puntos de reaparición y muerte con reaparición.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health))]
    public class PlayerLife : MonoBehaviour
    {
        [Tooltip("Sprite que parpadea mientras el jugador es invulnerable.")]
        [SerializeField] private SpriteRenderer sprite;

        private PlayerController controller;
        private Health health;
        private float respawnAt = -1f;

        /// <summary>Al caer en el ácido o al vacío.</summary>
        public event System.Action HazardTouched;

        public Vector2 Checkpoint { get; private set; }
        public Health Health => health;

        private PlayerConfig Config => controller.Config;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<Health>();
            Checkpoint = transform.position;
            health.Shielded = () => controller.IsDashing;   // el dash atraviesa enemigos y proyectiles; el ácido daña igual
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void FixedUpdate()
        {
            if (!health.IsDead) CheckEnemyContact();
        }

        private void Update()
        {
            if (sprite != null)
            {
                bool blinking = health.IsInvulnerable && !health.IsDead;
                sprite.enabled = !blinking || Mathf.FloorToInt(Time.time * 15f) % 2 == 0;
            }
            if (respawnAt >= 0f && Time.time >= respawnAt) Respawn();
        }

        public void SetCheckpoint(Vector2 position) => Checkpoint = position;

        /// <summary>Caída al vacío: cuesta una máscara y devuelve al último punto de reaparición.</summary>
        public void HazardHit()
        {
            if (health.IsDead) return;
            HazardTouched?.Invoke();
            health.TryDamage(1, transform.position, ignoreInvulnerability: true);
            controller.Teleport(Checkpoint);
        }

        private void CheckEnemyContact()
        {
            Bounds b = controller.BodyBounds;
            Collider2D hit = Physics2D.OverlapBox(b.center, b.size * 0.9f, 0f, Config.enemyMask);
            if (hit == null) return;
            var enemy = hit.GetComponentInParent<Health>();
            if (enemy != null && !enemy.IsDead) health.TryDamage(1, hit.bounds.center);
        }

        private void OnDamaged(Vector2 source)
        {
            if (health.IsDead) return;
            float away = Mathf.Sign(transform.position.x - source.x);
            if (Mathf.Approximately(transform.position.x, source.x)) away = -controller.Facing;
            controller.Knockback(new Vector2(away * Config.knockbackVelocity.x, Config.knockbackVelocity.y), Config.knockbackTime);
        }

        private void OnDied()
        {
            controller.ControlsEnabled = false;
            respawnAt = Time.time + Config.respawnDelay;
        }

        private void Respawn()
        {
            respawnAt = -1f;
            controller.Teleport(Checkpoint);
            health.Restore();
            controller.ControlsEnabled = true;
        }
    }
}
