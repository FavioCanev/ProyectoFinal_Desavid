using UnityEngine;

namespace Game
{
    /// <summary>
    /// Proyectil enemigo (escupitajo de ácido u onda de choque): daña al jugador al tocarlo.
    /// Opcionalmente se destruye al chocar con el suelo, y puede explotar (dinamita): daña en un radio.
    /// Desaparece solo tras un tiempo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.1f)] private float lifetime = 4f;
        [SerializeField] private bool destroyOnGround = true;
        [SerializeField] private LayerMask groundMask;
        [Tooltip("0 = no explota. Si es mayor, al chocar daña al jugador dentro de este radio.")]
        [SerializeField, Min(0f)] private float explosionRadius;
        [Tooltip("Gira mientras vuela (grados por segundo), como un cartucho de dinamita.")]
        [SerializeField] private float spin;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector2 velocity, float gravityScale)
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
            rb.linearVelocity = velocity;
        }

        private void Update()
        {
            if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Las plataformas de un solo sentido se atraviesan al subir (como hace el jugador).
            if (other.usedByEffector && rb != null && rb.linearVelocity.y > 0f) return;
            var life = other.GetComponentInParent<PlayerLife>();
            if (life != null)
            {
                if (explosionRadius > 0f) Explode();
                else life.Health.TryDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }
            if (destroyOnGround && (groundMask.value & (1 << other.gameObject.layer)) != 0)
            {
                if (explosionRadius > 0f) Explode();
                Destroy(gameObject);
            }
        }

        private void Explode()
        {
            Vector2 at = transform.position;
            foreach (var hit in Physics2D.OverlapCircleAll(at, explosionRadius))
            {
                var life = hit.GetComponentInParent<PlayerLife>();
                if (life != null) { life.Health.TryDamage(damage, at); break; }
            }
            GameFeel.Spawn(Fx.DeathBurst, at);
            GameFeel.Play(Sfx.Shockwave);
            GameFeel.Shake(0.6f);
        }
    }
}
