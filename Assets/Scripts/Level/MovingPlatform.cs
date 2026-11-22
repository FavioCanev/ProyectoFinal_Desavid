using UnityEngine;

namespace Game
{
    /// <summary>
    /// Plataforma que va y viene entre dos puntos, con una pausa breve en cada extremo.
    /// El jugador la detecta como suelo y se mueve con ella (ver PlayerController).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector2 pointA;
        [SerializeField] private Vector2 pointB;
        [SerializeField, Min(0f)] private float speed = 2.5f;
        [Tooltip("Pausa en cada extremo, en segundos.")]
        [SerializeField, Min(0f)] private float waitTime = 0.5f;

        private Rigidbody2D rb;
        private Vector2 target;
        private float waitUntil;

        public Vector2 Velocity { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.position = pointA;
            target = pointB;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (Time.time < waitUntil)
            {
                Velocity = Vector2.zero;
                return;
            }
            Vector2 next = Vector2.MoveTowards(rb.position, target, speed * dt);
            Velocity = (next - rb.position) / dt;
            rb.MovePosition(next);
            if ((next - target).sqrMagnitude < 1e-4f)
            {
                target = target == pointB ? pointA : pointB;
                waitUntil = Time.time + waitTime;
            }
        }
    }
}
