using UnityEngine;

namespace Game
{
    /// <summary>Disparo de plasma del jugador: avanza en línea recta, daña al primer enemigo y se rompe contra el suelo.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;
        [SerializeField, Min(0.01f)] private float radius = 0.2f;

        private Rigidbody2D rb;
        private int damage;
        private LayerMask enemyMask, groundMask;

        public void Launch(Vector2 velocity, int amount, LayerMask enemies, LayerMask ground)
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = velocity;
            damage = amount;
            enemyMask = enemies;
            groundMask = ground;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            Destroy(gameObject, lifetime);
        }

        // Barrido por paso: a esta velocidad un disparo podría atravesar un enemigo delgado entre dos pasos.
        private void FixedUpdate()
        {
            if (rb == null) return;
            Vector2 step = rb.linearVelocity * Time.fixedDeltaTime;
            RaycastHit2D hit = Physics2D.CircleCast(rb.position, radius, step.normalized, step.magnitude, enemyMask | groundMask);
            if (!hit) return;

            var target = hit.collider.GetComponentInParent<Health>();
            if (target != null && !target.IsDead && (enemyMask.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                target.TryDamage(damage, rb.position);
                GameFeel.HitStop(0.03f);
            }
            GameFeel.Spawn(Fx.HitSpark, hit.point);
            Destroy(gameObject);
        }
    }
}
