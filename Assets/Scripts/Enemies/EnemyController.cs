using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Enemigo de suelo con patrones de ataque. Ciclo: patrulla/persigue → aviso en rojo → ataque →
    /// recuperación (momento vulnerable). Los ataques y números salen de su EnemyConfig.
    /// El daño por contacto lo comprueba PlayerLife; los ataques dañan con zonas de golpe o proyectiles.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        private enum State { Dormant, Roam, Windup, Active, Recover, Hurt, Dead }

        [SerializeField] private EnemyConfig config;
        [SerializeField] private Transform visual;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask playerMask;
        [SerializeField] private Projectile spitPrefab;
        [SerializeField] private Projectile shockwavePrefab;
        [Tooltip("Empieza dormido hasta que algo lo active (jefes).")]
        [SerializeField] private bool startsDormant;

        private Rigidbody2D rb;
        private Collider2D body;
        private Health health;
        private Transform player;
        private System.Random rng;
        private State state;
        private EnemyAttack current;
        private float stateUntil, activeStartedAt, nextAttackAt, hitFlashUntil;
        private int facing = 1, attackDir = 1;
        private string animState;
        private Color baseColor = Color.white;
        private Vector2 home;
        private readonly Dictionary<EnemyAttack, float> readyAt = new Dictionary<EnemyAttack, float>();
        private readonly List<AttackOption> options = new List<AttackOption>(4);

        /// <summary>Se dispara cuando empieza el aviso de un ataque.</summary>
        public event Action<EnemyAttack> AttackStarted;

        public Health Health => health;
        public EnemyConfig Config => config;
        public bool IsDormant => state == State.Dormant;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<Collider2D>();
            health = GetComponent<Health>();
            rng = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            if (sprite != null) baseColor = sprite.color;
            home = transform.position;
            state = startsDormant ? State.Dormant : State.Roam;
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

        /// <summary>Despierta a un enemigo dormido (por ejemplo, al entrar en la arena del jefe).</summary>
        public void Activate()
        {
            if (state != State.Dormant) return;
            state = State.Roam;
            nextAttackAt = Time.time + 1f;   // un respiro antes del primer ataque
            GameFeel.Play(Sfx.BossRoar);
            GameFeel.Shake(0.6f);
        }

        /// <summary>Lo devuelve a su posición inicial con la vida completa (si el jugador muere ante el jefe).</summary>
        public void ResetToStart()
        {
            if (state == State.Dead) return;
            health.Restore();
            rb.position = home;
            transform.position = home;
            rb.linearVelocity = Vector2.zero;
            state = startsDormant ? State.Dormant : State.Roam;
            readyAt.Clear();
        }

        private void Update()
        {
            if (sprite == null) return;
            if (state == State.Windup)
                sprite.color = Color.Lerp(baseColor, config.windupColor, 0.5f + 0.5f * Mathf.Sin(Time.time * 40f));
            else
                sprite.color = Time.time < hitFlashUntil ? Color.Lerp(baseColor, Color.red, 0.6f) : baseColor;
        }

        private void FixedUpdate()
        {
            switch (state)
            {
                case State.Dead: return;
                case State.Dormant: Halt(); Play("Idle"); return;
                case State.Hurt: if (Time.time >= stateUntil) state = State.Roam; return;
                case State.Windup:
                    Halt(); Face(attackDir);
                    if (Time.time >= stateUntil) BeginActive();
                    return;
                case State.Active: TickActive(); return;
                case State.Recover:
                    Halt();
                    if (Time.time >= stateUntil) state = State.Roam;
                    return;
                default: Roam(); return;
            }
        }

        // ---------- Movimiento ----------

        private void Roam()
        {
            bool seen = CanSeePlayer(out float dx);
            if (seen && Time.time >= nextAttackAt)
            {
                EnemyAttack attack = Choose(Mathf.Abs(dx));
                if (attack != EnemyAttack.None) { StartWindup(attack, dx >= 0f ? 1 : -1); return; }
            }

            int dir;
            float speed;
            if (seen)
            {
                dir = dx >= 0f ? 1 : -1;
                float dist = Mathf.Abs(dx);
                if (config.preferredDistance > 0f)
                {
                    // A distancia: retrocede si el jugador se acerca demasiado; se acerca si está lejos.
                    if (dist < config.preferredDistance - 1f) { dir = -dir; speed = config.chaseSpeed; }
                    else speed = dist > config.preferredDistance + 1f ? config.chaseSpeed : 0f;
                }
                else speed = dist > 0.6f ? config.chaseSpeed : 0f;
            }
            else { dir = facing; speed = config.patrolSpeed; }

            if (speed > 0f && Blocked(dir))
            {
                if (seen) speed = 0f;                               // no se tira por el borde
                else { dir = -dir; if (Blocked(dir)) speed = 0f; }  // patrulla: da la vuelta
            }

            Face(seen ? (dx >= 0f ? 1 : -1) : dir);
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            Play(speed > 0.05f ? "Walk" : "Idle");
            if (animator != null && animState == "Walk") animator.speed = Mathf.Max(0.5f, speed / Mathf.Max(0.1f, config.patrolSpeed));
        }

        private bool CanSeePlayer(out float dx)
        {
            dx = 0f;
            if (player == null) return false;
            Vector2 d = player.position - transform.position;
            dx = d.x;
            return Mathf.Abs(d.x) <= config.detectRange && Mathf.Abs(d.y) <= config.detectHeight;
        }

        private bool Blocked(int dir)
        {
            float w = config.bodyHalfWidth + 0.1f;
            // Muro a media altura o a la altura de la cabeza (salientes bajos), o falta de suelo delante.
            float head = body != null ? body.bounds.size.y - 0.15f : 1.2f;
            bool wall = Physics2D.Raycast((Vector2)transform.position + new Vector2(0f, 0.5f), Vector2.right * dir, w, groundMask)
                     || Physics2D.Raycast((Vector2)transform.position + new Vector2(0f, head), Vector2.right * dir, w, groundMask);
            bool ground = Physics2D.Raycast((Vector2)transform.position + new Vector2(dir * w, 0.1f), Vector2.down, 0.6f, groundMask);
            return wall || !ground;
        }

        private bool IsGrounded() =>
            Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.1f, Vector2.down, 0.25f, groundMask);

        private void Halt() => rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // ---------- Ataques ----------

        private EnemyAttack Choose(float distance)
        {
            options.Clear();
            Add(EnemyAttack.Swipe, config.swipe);
            Add(EnemyAttack.Charge, config.charge);
            Add(EnemyAttack.Spit, config.spit);
            Add(EnemyAttack.Leap, config.leap);
            return EnemyAttackPicker.Pick(distance, options, rng);

            void Add(EnemyAttack kind, AttackTimings t) =>
                options.Add(new AttackOption(kind, t.enabled, t.minRange, t.maxRange,
                    !readyAt.TryGetValue(kind, out float at) || Time.time >= at));
        }

        private AttackTimings Timings(EnemyAttack attack) => attack switch
        {
            EnemyAttack.Swipe => config.swipe,
            EnemyAttack.Charge => config.charge,
            EnemyAttack.Spit => config.spit,
            _ => config.leap,
        };

        private void StartWindup(EnemyAttack attack, int dir)
        {
            current = attack;
            attackDir = dir;
            AttackTimings t = Timings(attack);
            state = State.Windup;
            stateUntil = Time.time + t.windup;
            readyAt[attack] = Time.time + t.cooldown;
            Play("Windup");
            AttackStarted?.Invoke(attack);
        }

        private void BeginActive()
        {
            state = State.Active;
            activeStartedAt = Time.time;
            switch (current)
            {
                case EnemyAttack.Swipe:
                    Play("Swipe");
                    HitPlayer(config.swipe.hitOffset, config.swipe.hitSize);
                    stateUntil = Time.time + config.swipe.activeTime;
                    break;
                case EnemyAttack.Charge:
                    Play("Charge");
                    stateUntil = Time.time + config.charge.duration;
                    break;
                case EnemyAttack.Spit:
                    Play("Spit");
                    FireSpit();
                    stateUntil = Time.time + 0.25f;
                    break;
                case EnemyAttack.Leap:
                    Play("Leap");
                    LaunchLeap();
                    stateUntil = Time.time + config.leap.flightTime + 1.5f;   // tope por si no aterriza
                    break;
            }
        }

        private void TickActive()
        {
            switch (current)
            {
                case EnemyAttack.Charge:
                    if (Blocked(attackDir) || Time.time >= stateUntil) EnterRecover(config.charge.recover);
                    else rb.linearVelocity = new Vector2(attackDir * config.charge.speed, rb.linearVelocity.y);
                    break;
                case EnemyAttack.Leap:
                    bool landed = Time.time - activeStartedAt > 0.15f && rb.linearVelocity.y <= 0.01f && IsGrounded();
                    if (landed) { SpawnShockwaves(); Play("Slam"); }
                    if (landed || Time.time >= stateUntil) EnterRecover(config.leap.recover);
                    break;
                default:
                    Halt();
                    if (Time.time >= stateUntil) EnterRecover(Timings(current).recover);
                    break;
            }
        }

        private void EnterRecover(float time)
        {
            if (current != EnemyAttack.Leap) Play("Idle");
            Halt();
            state = State.Recover;
            stateUntil = Time.time + time;
            nextAttackAt = stateUntil + config.attackPause;
        }

        private void HitPlayer(Vector2 offset, Vector2 size)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(offset.x * attackDir, offset.y);
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, playerMask);
            if (hit == null) return;
            var target = hit.GetComponentInParent<Health>();
            if (target != null) target.TryDamage(1, transform.position);
        }

        // Proyectiles en arco: se calcula la velocidad para caer donde está el jugador tras "t" segundos.
        private void FireSpit()
        {
            if (player == null || spitPrefab == null) return;
            SpitAttack s = config.spit;
            Vector2 origin = (Vector2)transform.position + new Vector2(s.mouthOffset.x * attackDir, s.mouthOffset.y);
            Vector2 target = (Vector2)player.position + Vector2.up * 0.8f;
            float g = Mathf.Abs(Physics2D.gravity.y);
            for (int i = 0; i < s.count; i++)
            {
                float t = Mathf.Max(0.3f, s.flightTime + (i - (s.count - 1) * 0.5f) * s.spreadTime);
                var v = new Vector2((target.x - origin.x) / t, (target.y - origin.y + 0.5f * g * t * t) / t);
                v.x = Mathf.Clamp(v.x, -12f, 12f);
                Instantiate(spitPrefab, origin, Quaternion.identity).Launch(v, 1f);
            }
            GameFeel.Play(Sfx.Spit, 0.7f);
        }

        private void LaunchLeap()
        {
            if (player == null) return;
            float t = config.leap.flightTime;
            float dx = Mathf.Clamp(player.position.x - transform.position.x, -config.leap.maxRange, config.leap.maxRange);
            float dy = Mathf.Clamp(player.position.y - transform.position.y, 0f, 4f);
            float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
            rb.linearVelocity = new Vector2(dx / t, (dy + 0.5f * g * t * t) / t);
        }

        private void SpawnShockwaves()
        {
            GameFeel.Shake(0.6f);
            GameFeel.Play(Sfx.Shockwave);
            GameFeel.Spawn(Fx.Dust, transform.position);
            if (shockwavePrefab == null) return;
            foreach (int dir in new[] { -1, 1 })
            {
                var wave = Instantiate(shockwavePrefab, (Vector2)transform.position + new Vector2(dir * 0.8f, 0.3f), Quaternion.identity);
                wave.Launch(new Vector2(dir * config.leap.shockwaveSpeed, 0f), 0f);
            }
        }

        // ---------- Golpes y muerte ----------

        private void OnDamaged(Vector2 source)
        {
            if (health.IsDead) return;
            hitFlashUntil = Time.time + 0.12f;
            GameFeel.Play(Sfx.EnemyHurt, 0.8f);
            if (config.superArmor) return;   // el jefe no se interrumpe
            float away = Mathf.Sign(transform.position.x - source.x);
            rb.linearVelocity = new Vector2(away * config.hurtKnockback.x, config.hurtKnockback.y);
            state = State.Hurt;
            stateUntil = Time.time + config.hurtTime;
            Play("Hurt");
        }

        private void OnDied()
        {
            state = State.Dead;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;   // sin colisiones: ya no daña ni recibe golpes
            Play("Dead");
            GameFeel.Play(Sfx.EnemyDie);
            GameFeel.Spawn(Fx.DeathBurst, (Vector2)transform.position + Vector2.up * 0.8f);
            if (config.superArmor)
            {
                // El jefe cae: pausa larga y sacudida fuerte para que se sienta la victoria.
                GameFeel.HitStop(0.35f);
                GameFeel.Shake(1.5f);
            }
            Destroy(gameObject, config.corpseTime);
        }

        private void Face(int dir)
        {
            facing = dir;
            if (visual == null) return;
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
        }

        private void Play(string animation)
        {
            if (animator == null || animation == animState) return;
            animState = animation;
            animator.speed = 1f;
            animator.Play(animation, 0, 0f);
        }
    }
}
