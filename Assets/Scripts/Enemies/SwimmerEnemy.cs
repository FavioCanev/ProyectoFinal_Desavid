using UnityEngine;

namespace Game
{
    /// <summary>
    /// Enemigo que nada (piraña): patrulla ondulando alrededor de su punto de inicio; al ver al jugador se detiene
    /// y brilla en rojo (aviso), embiste hacia donde estaba y queda agotado un momento. Daña por contacto
    /// (capa Enemy + Health, como el resto de enemigos). Las animaciones son listas de cuadros.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class SwimmerEnemy : MonoBehaviour
    {
        private enum State { Patrol, Windup, Charge, Recover, Hurt, Dead }

        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Sprite[] swimFrames, windupFrames, chargeFrames, hurtFrames, deadFrames;
        [SerializeField] private float patrolRange = 4f, patrolSpeed = 1.6f;
        [SerializeField] private float detectRange = 7f, chargeSpeed = 9f;
        [SerializeField] private float windupTime = 0.55f, chargeTime = 0.55f, recoverTime = 0.8f, hurtTime = 0.25f;
        [SerializeField] private Color windupColor = new Color(1f, 0.45f, 0.4f);

        private Rigidbody2D rb;
        private Health health;
        private Transform player;
        private Vector2 home, chargeDirection;
        private State state;
        private float timer, frameClock;
        private int patrolDirection = 1;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            health = GetComponent<Health>();
            home = rb.position;
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Start()
        {
            var life = FindAnyObjectByType<PlayerLife>();
            if (life != null) player = life.transform;
        }

        private void OnDamaged(Vector2 source)
        {
            if (health.IsDead) return;
            Enter(State.Hurt);
            rb.linearVelocity = ((Vector2)transform.position - source).normalized * 4f;
            GameFeel.Play(Sfx.EnemyHurt);
        }

        private void OnDied()
        {
            Enter(State.Dead);
            GetComponent<Collider2D>().enabled = false;
            GameFeel.Play(Sfx.EnemyDie);
            GameFeel.Spawn(Fx.DeathBurst, transform.position);
            Destroy(gameObject, 2f);
        }

        private void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;
            Vector2 toPlayer = player != null ? (Vector2)player.position + Vector2.up * 0.8f - rb.position : Vector2.positiveInfinity;
            switch (state)
            {
                case State.Patrol:
                    // Ida y vuelta horizontal con una ondulación suave.
                    if (Mathf.Abs(rb.position.x - home.x) > patrolRange) patrolDirection = rb.position.x > home.x ? -1 : 1;
                    rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, Mathf.Sin(timer * 2.2f) * 0.6f + (home.y - rb.position.y));
                    Face(patrolDirection);
                    if (toPlayer.magnitude < detectRange) Enter(State.Windup);
                    break;
                case State.Windup:
                    rb.linearVelocity = Vector2.zero;
                    Face(toPlayer.x);
                    if (timer > windupTime) { chargeDirection = toPlayer.normalized; Enter(State.Charge); }
                    break;
                case State.Charge:
                    rb.linearVelocity = chargeDirection * chargeSpeed;
                    if (timer > chargeTime) Enter(State.Recover);
                    break;
                case State.Recover:
                    rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, (home - rb.position).normalized * patrolSpeed * 0.5f, 10f * Time.fixedDeltaTime);
                    if (timer > recoverTime) Enter(State.Patrol);
                    break;
                case State.Hurt:
                    rb.linearVelocity *= 0.9f;
                    if (timer > hurtTime) Enter(State.Recover);
                    break;
                case State.Dead:
                    rb.linearVelocity = new Vector2(0f, 0.8f);   // flota panza arriba
                    break;
            }
        }

        private void Update()
        {
            frameClock += Time.deltaTime;
            Sprite[] frames = state switch
            {
                State.Windup => windupFrames, State.Charge => chargeFrames, State.Hurt => hurtFrames,
                State.Dead => deadFrames, _ => swimFrames,
            };
            bool loops = state != State.Dead;
            int index = Mathf.FloorToInt(frameClock * 10f);
            if (frames != null && frames.Length > 0) sprite.sprite = frames[loops ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
            sprite.color = state == State.Windup && Mathf.FloorToInt(Time.time * 16f) % 2 == 0 ? windupColor
                         : state == State.Dead ? new Color(1f, 1f, 1f, Mathf.Clamp01(2f - timer)) : Color.white;
        }

        private void Enter(State next)
        {
            state = next;
            timer = 0f;
            frameClock = 0f;
        }

        private void Face(float x)
        {
            if (Mathf.Abs(x) > 0.01f) sprite.flipX = x < 0f;   // el dibujo mira a la derecha
        }
    }
}
