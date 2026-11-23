using Game;
using UnityEngine;

namespace Amazonia
{
    /// <summary>
    /// El caimán de Edward (tierra y agua). Conserva su IA original: persigue, avisa en naranja y ataca
    /// con zarpazo, empujón o piedra (tierra) o embestida (agua, con furia a mitad de vida).
    /// Ahora usa la vida y el jugador comunes del juego (Game.Health, Game.PlayerLife).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class AdventureBoss : MonoBehaviour
    {
        public enum BossState { Patrol, Chase, Positioning, Telegraph, Claw, Push, Rock, Charge, Recovery, Stagger, Retreat, Defeated }

        public bool aquatic;
        public AdventureBalance balance;
        public PlayerLife target;
        public AdventureVisual visual;
        public Projectile rockPrefab;
        [Tooltip("En tierra, al vencerlo huye hasta esta X y se zambulle en el lago.")]
        public float lakeX = 63f;

        private const float LandGravity = 2.8f;
        private Rigidbody2D body;
        private Vector2 spawn;
        private float timer;
        private bool hit, executed, diving;
        private float diveTime;
        private BossState planned;
        private int cycle;

        public BossState State { get; private set; }
        public Health Health { get; private set; }
        public Vector2 ChargeDirection { get; private set; }
        public bool Enraged => aquatic && Health.Current <= Health.Max / 2;
        /// <summary>Detenido (cinemáticas): no se mueve ni ataca.</summary>
        public bool Stopped;
        /// <summary>No recibe daño (antes de presentarse y durante cinemáticas).</summary>
        public bool Protected;
        public Rigidbody2D Body => body;

        private static int GroundMask => LayerMask.GetMask("Ground");

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            Health = GetComponent<Health>();
            Health.Shielded = () => Protected;
            body.gravityScale = aquatic ? 0f : LandGravity;
            spawn = body.position;
            State = BossState.Patrol;
        }

        private void OnEnable()
        {
            Health.Died += OnDefeated;
            Health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            Health.Died -= OnDefeated;
            Health.Damaged -= OnDamaged;
        }

        private void OnDamaged(Vector2 _) => visual.Flash();

        private void OnDefeated()
        {
            Protected = true;
            visual.Warning = false;
            if (aquatic)
            {
                Enter(BossState.Defeated);
                body.linearVelocity = Vector2.zero;
                visual.Play("Death");
                AdventureFlow.Instance.BossDefeated();
            }
            else
            {
                Enter(BossState.Stagger);
                visual.Play("Hurt");
                AdventureFlow.Instance.LandWon();
            }
        }

        /// <summary>Si el jugador muere durante la pelea, el caimán vuelve a su sitio con la vida llena.</summary>
        public void ResetFight()
        {
            if (Health.IsDead) return;
            Health.Restore();
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            Enter(BossState.Patrol);
        }

        private void FixedUpdate()
        {
            if (Stopped || target == null || target.Health.IsDead) { body.linearVelocity = Vector2.zero; return; }
            timer += Time.fixedDeltaTime;
            Vector2 delta = (Vector2)target.transform.position - body.position;
            float sign = delta.x >= 0f ? 1f : -1f;
            visual.Enraged = Enraged;

            switch (State)
            {
                case BossState.Defeated: body.linearVelocity = new Vector2(0f, -0.4f); return;
                case BossState.Stagger:
                    body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                    if (timer > 1f) Enter(BossState.Retreat);
                    return;
                case BossState.Retreat: Retreat(); return;
            }

            visual.Warning = State == BossState.Telegraph;
            switch (State)
            {
                case BossState.Patrol:
                    visual.Play("Idle");
                    StopMoving();
                    if (delta.magnitude < (aquatic ? 19f : 17f)) Enter(BossState.Chase);
                    break;
                case BossState.Chase: Chase(delta, sign); break;
                case BossState.Positioning:
                    visual.Face(delta.x);
                    visual.Play("Swim");
                    body.linearVelocity = delta.magnitude < 5f ? Steer(-delta.normalized) * 2f : Vector2.zero;
                    if (timer > 0.45f) { planned = BossState.Charge; Enter(BossState.Telegraph); }
                    break;
                case BossState.Telegraph:
                    StopMoving();
                    visual.Face(delta.x);
                    visual.Play(planned == BossState.Rock ? "RockPrepare" : "Telegraph");
                    if (timer > balance.preparation * (Enraged ? 0.78f : 1f))
                    {
                        ChargeDirection = aquatic ? delta.normalized : new Vector2(sign, 0f);
                        Enter(planned);
                    }
                    break;
                case BossState.Claw: Claw(); break;
                case BossState.Push:
                    visual.Play("Push");
                    body.linearVelocity = new Vector2(ChargeDirection.x * 8f, body.linearVelocity.y);
                    if (timer > 0.45f) Enter(BossState.Recovery);
                    break;
                case BossState.Rock: ThrowRock(sign); break;
                case BossState.Charge:
                    visual.Play(Enraged ? "ChargeAlt" : "Charge");
                    body.linearVelocity = ChargeDirection * (balance.chargeSpeed + (Enraged ? 1f : 0f));
                    if (timer > 0.7f) Enter(BossState.Recovery);
                    break;
                case BossState.Recovery:
                    StopMoving();
                    visual.Play("Recovery");
                    if (timer > balance.recovery * (Enraged ? 0.82f : 1f)) Enter(BossState.Chase);
                    break;
            }
        }

        private void Chase(Vector2 delta, float sign)
        {
            visual.Face(delta.x);
            if (aquatic)
            {
                visual.Play(Enraged ? "Enraged" : Mathf.Abs(delta.y) > 3f ? (delta.y > 0f ? "Up" : "Down") : "Swim");
                body.linearVelocity = Steer(delta.normalized) * (balance.chaseWater + (Enraged ? 0.7f : 0f));
                if (delta.magnitude < 8f && timer > 0.6f || timer > 3f) Enter(BossState.Positioning);
                return;
            }
            visual.Play("Run");
            body.linearVelocity = new Vector2(sign * balance.chaseLand, body.linearVelocity.y);
            if (Mathf.Abs(delta.x) < 3.3f || timer > 1.7f)
            {
                // Lejos o por encima: piedra. Cerca: alterna zarpazo y empujón.
                planned = Mathf.Abs(delta.x) > 6f || delta.y > 1.7f ? BossState.Rock : cycle++ % 2 == 0 ? BossState.Claw : BossState.Push;
                Enter(BossState.Telegraph);
            }
        }

        private void Claw()
        {
            visual.Play("Claw");
            if (timer > 0.18f && !executed)
            {
                executed = true;
                Vector2 center = body.position + ChargeDirection * 1.6f;
                foreach (var c in Physics2D.OverlapBoxAll(center, new Vector2(2.3f, 1.8f), 0f))
                    if (c.GetComponentInParent<PlayerLife>() == target) { DamagePlayer(); break; }
            }
            if (timer > 0.5f) Enter(BossState.Recovery);
        }

        private void ThrowRock(float sign)
        {
            visual.Play("Rock");
            if (timer > 0.25f && !executed)
            {
                executed = true;
                const float gravity = 1.3f;
                Vector2 origin = body.position + new Vector2(sign * 1.9f, 1.1f);
                Vector2 travel = (Vector2)target.transform.position + Vector2.up * 0.8f - origin;
                float flight = Mathf.Clamp(Mathf.Abs(travel.x) / 8f, 0.55f, 1.8f);
                var velocity = new Vector2(travel.x / flight, travel.y / flight + 0.5f * 9.81f * gravity * flight);
                Instantiate(rockPrefab, origin, Quaternion.identity).Launch(velocity, gravity);
            }
            if (timer > 0.7f) Enter(BossState.Recovery);
        }

        private void Retreat()
        {
            visual.Face(1f);
            if (!diving)
            {
                visual.Play("Escape");
                body.linearVelocity = new Vector2(7f, body.linearVelocity.y);
                if (body.position.x > lakeX)
                {
                    diving = true;
                    diveTime = 0f;
                    GetComponent<Collider2D>().enabled = false;
                    AdventureFlow.Instance.Effect(transform.position, Color.cyan);
                }
                return;
            }
            diveTime += Time.fixedDeltaTime;
            visual.Play("Death");
            body.linearVelocity = new Vector2(1.5f, -2f);
            if (diveTime > 0.8f)
            {
                AdventureFlow.Instance.OpenLake();
                gameObject.SetActive(false);
            }
        }

        public void BeginRetreat()
        {
            Stopped = false;
            Enter(BossState.Retreat);
        }

        /// <summary>Bajo el agua esquiva el terreno probando direcciones cercanas a la deseada.</summary>
        private Vector2 Steer(Vector2 direction)
        {
            if (!Physics2D.CircleCast(body.position, 0.8f, direction, 2f, GroundMask)) return direction;
            Vector2 best = direction;
            float bestScore = float.MinValue;
            for (int i = -3; i <= 3; i++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, i * 40f) * direction;
                RaycastHit2D h = Physics2D.CircleCast(body.position, 0.8f, d, 3f, GroundMask);
                float score = (h ? h.distance : 3f) + Vector2.Dot(direction, d) * 0.4f;
                if (score > bestScore) { bestScore = score; best = d; }
            }
            return best;
        }

        private void StopMoving() => body.linearVelocity = new Vector2(0f, aquatic ? 0f : body.linearVelocity.y);

        private void Enter(BossState state)
        {
            State = state;
            timer = 0f;
            hit = false;
            executed = false;
            StopMoving();
        }

        // Embestida o empujón contra una pared: se detiene y queda agotado.
        private void OnCollisionEnter2D(Collision2D c)
        {
            if (Stopped || Health.IsDead || State != BossState.Charge && State != BossState.Push) return;
            if ((GroundMask & (1 << c.gameObject.layer)) != 0 && (aquatic || Mathf.Abs(c.GetContact(0).normal.x) > 0.5f))
                Enter(BossState.Recovery);
        }

        private void DamagePlayer()
        {
            if (hit) return;
            hit = true;
            target.Health.TryDamage(1, body.position);
        }
    }
}
