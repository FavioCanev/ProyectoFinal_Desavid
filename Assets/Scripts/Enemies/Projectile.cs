using UnityEngine;

namespace Game
{
    /// <summary>
    /// Proyectil enemigo (escupitajo de ácido u onda de choque): daña al jugador al tocarlo.
    /// Opcionalmente se destruye al chocar con el suelo. Desaparece solo tras un tiempo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.1f)] private float lifetime = 4f;
        [SerializeField] private bool destroyOnGround = true;
        [SerializeField] private LayerMask groundMask;

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

        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife>();
            if (life != null)
            {
                life.Health.TryDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }
            if (destroyOnGround && (groundMask.value & (1 << other.gameObject.layer)) != 0) Destroy(gameObject);
        }
    }
}
