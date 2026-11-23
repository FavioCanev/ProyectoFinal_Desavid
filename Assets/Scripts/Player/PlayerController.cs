using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Movimiento del jugador: correr, salto de altura variable, coyote time y jump buffer; y nado en 4 direcciones
    /// cuando está dentro de un WaterVolume (bajo el agua se puede atacar y hacer dash igual que en tierra).
    /// También aplica empujones (al recibir daño) y rebotes (al golpear hacia abajo).
    /// La entrada se lee en Update; la física se aplica en FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [Tooltip("Hijo con el SpriteRenderer; se voltea según la dirección.")]
        [SerializeField] private Transform visual;

        private Rigidbody2D rb;
        private CapsuleCollider2D body;
        private PlayerControls controls;
        private JumpAssist jumpAssist;
        private DashAbility dash;

        private UnityEngine.InputSystem.InputAction shoot;
        private float moveInput;
        private bool jumpPressedSinceLastStep;
        private bool dashPressedSinceLastStep;
        private Vector2 groundVelocity;
        private bool jumpHeld;
        private int facing = 1;
        private float knockbackLeft;
        private float? pendingVerticalVelocity;
        private bool bouncing;

        /// <summary>Se dispara cuando se pulsa atacar (y hay control).</summary>
        public event Action AttackPressed;
        public event Action Jumped;
        /// <summary>Al tocar suelo tras caer; el parámetro es la velocidad de caída.</summary>
        public event Action<float> Landed;
        public event Action Dashed;
        /// <summary>Se pulsó disparar plasma (Fin) y hay control.</summary>
        public event Action ShootPressed;

        private bool wasGrounded = true;
        private bool wasDashing;
        private float lastVerticalVelocity;

        public bool ControlsEnabled { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public bool IsDashing => dash.IsDashing;
        public bool DashReady => dash.IsReady(IsGrounded || Swimming);
        /// <summary>Dentro del agua: nado en 4 direcciones, sin gravedad.</summary>
        public bool Swimming { get; set; }
        /// <summary>Hacia dónde dispara: adelante; bajo el agua también en diagonal con arriba/abajo.</summary>
        public Vector2 Aim => Swimming && Mathf.Abs(VerticalInput) > 0.5f
            ? new Vector2(moveInput, Mathf.Sign(VerticalInput)).normalized
            : new Vector2(facing, 0f);
        /// <summary>1 = mira a la derecha, -1 = a la izquierda.</summary>
        public int Facing => facing;
        /// <summary>Entrada vertical (-1 abajo, 1 arriba), usada para elegir la dirección del ataque.</summary>
        public float VerticalInput { get; private set; }
        public Vector2 Velocity => rb.linearVelocity;
        public Bounds BodyBounds => body.bounds;
        public PlayerConfig Config => config;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<CapsuleCollider2D>();
            controls = new PlayerControls();
            jumpAssist = new JumpAssist(config.coyoteTime, config.jumpBufferTime, config.airJumps);
            dash = new DashAbility(config.dashDuration, config.dashCooldown, config.airDashes);
            shoot = controls.asset.FindAction("Gameplay/Shoot", throwIfNotFound: true);
        }

        private void OnEnable() => controls.Gameplay.Enable();
        private void OnDisable() => controls.Gameplay.Disable();
        private void OnDestroy() => controls.Dispose();

        private void Update()
        {
            Vector2 move = controls.Gameplay.Move.ReadValue<Vector2>();
            VerticalInput = ControlsEnabled ? move.y : 0f;
            moveInput = ControlsEnabled && Mathf.Abs(move.x) > 0.2f ? Mathf.Sign(move.x) : 0f;

            if (ControlsEnabled && controls.Gameplay.Jump.WasPressedThisFrame()) jumpPressedSinceLastStep = true;
            jumpHeld = controls.Gameplay.Jump.IsPressed() || jumpHeldByCode;
            if (ControlsEnabled && controls.Gameplay.Attack.WasPressedThisFrame()) AttackPressed?.Invoke();
            if (ControlsEnabled && controls.Gameplay.Dash.WasPressedThisFrame()) dashPressedSinceLastStep = true;
            if (ControlsEnabled && shoot.WasPressedThisFrame()) ShootPressed?.Invoke();

            if (moveInput != 0f && knockbackLeft <= 0f) Face((int)moveInput);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGround(out groundVelocity);
            if (IsGrounded && !wasGrounded && lastVerticalVelocity < -1f) Landed?.Invoke(-lastVerticalVelocity);
            wasGrounded = IsGrounded;
            lastVerticalVelocity = rb.linearVelocity.y;

            jumpAssist.Tick(dt, IsGrounded, jumpPressedSinceLastStep);
            jumpPressedSinceLastStep = false;

            bool dashRecharges = IsGrounded || Swimming;   // bajo el agua el dash se recarga como en el suelo
            dash.Tick(dt, dashRecharges);
            if (dashPressedSinceLastStep && knockbackLeft <= 0f && dash.TryStart(moveInput != 0f ? (int)moveInput : facing, dashRecharges))
                Dashed?.Invoke();
            dashPressedSinceLastStep = false;

            if (dash.IsDashing)
            {
                // Dash: velocidad horizontal fija y sin gravedad mientras dura.
                Face(dash.Direction);
                rb.gravityScale = 0f;
                rb.linearVelocity = new Vector2(dash.Direction * config.dashSpeed, 0f);
                wasDashing = true;
                return;
            }

            Vector2 v = rb.linearVelocity;
            if (wasDashing)
            {
                // Al terminar, el dash corta en seco (como en Hollow Knight): nada de deslizarse.
                v.x = Mathf.Clamp(v.x, -config.runSpeed, config.runSpeed);
                wasDashing = false;
            }

            if (Swimming)
            {
                Swim(v, dt);
                return;
            }

            if (knockbackLeft > 0f)
            {
                knockbackLeft -= dt;   // durante el empujón no se controla la dirección
            }
            else
            {
                // Sobre una plataforma móvil, "quieto" significa moverse con ella.
                float targetX = moveInput * config.runSpeed + groundVelocity.x;
                float rate = moveInput != 0f ? config.acceleration : config.deceleration;
                v.x = Mathf.MoveTowards(v.x, targetX, rate * dt);
            }

            if (pendingVerticalVelocity.HasValue)
            {
                v.y = pendingVerticalVelocity.Value;
                pendingVerticalVelocity = null;
            }
            else if (knockbackLeft <= 0f && jumpAssist.TryConsumeJump())
            {
                v.y = config.JumpVelocity;
                bouncing = false;
                Jumped?.Invoke();
            }
            else if (knockbackLeft <= 0f && jumpAssist.TryConsumeAirJump())
            {
                v.y = config.AirJumpVelocity;   // doble salto
                bouncing = false;
                Jumped?.Invoke();
            }

            if (v.y <= 0f) bouncing = false;
            rb.gravityScale = GravityScaleFor(v.y);
            v.y = Mathf.Max(v.y, -config.maxFallSpeed);

            rb.linearVelocity = v;
        }

        /// <summary>Nado: acelera hacia la dirección pulsada; sin pulsar arriba/abajo se hunde despacio.</summary>
        private void Swim(Vector2 v, float dt)
        {
            rb.gravityScale = 0f;
            if (knockbackLeft > 0f) { knockbackLeft -= dt; rb.linearVelocity = v; return; }
            float vertical = ControlsEnabled && Mathf.Abs(VerticalInput) > 0.2f ? Mathf.Sign(VerticalInput) : 0f;
            Vector2 target = new Vector2(moveInput, vertical);
            target = target.sqrMagnitude > 0f ? target.normalized * config.swimSpeed : new Vector2(0f, -config.sinkSpeed);
            if (pendingVerticalVelocity.HasValue) { v.y = pendingVerticalVelocity.Value; pendingVerticalVelocity = null; }
            rb.linearVelocity = Vector2.MoveTowards(v, target, config.swimAcceleration * dt);
        }

        private bool jumpHeldByCode;

        /// <summary>Pulsa saltar como si se apretara Espacio (lo usan las pruebas). "held": mantenerlo apretado.</summary>
        public void RequestJump(bool held)
        {
            jumpPressedSinceLastStep = true;
            jumpHeldByCode = held;
        }

        public void ReleaseJump() => jumpHeldByCode = false;

        /// <summary>Pide un dash como si se pulsara Shift (lo usan las pruebas).</summary>
        public void RequestDash() => dashPressedSinceLastStep = true;

        /// <summary>Lanza al jugador hacia arriba (rebote al golpear algo con el ataque hacia abajo).</summary>
        public void Bounce(float verticalVelocity)
        {
            pendingVerticalVelocity = verticalVelocity;
            bouncing = true;
            jumpAssist.RefillAirJumps();
        }

        /// <summary>Empuja al jugador y le quita el control horizontal durante un momento.</summary>
        public void Knockback(Vector2 velocity, float duration)
        {
            rb.linearVelocity = velocity;
            knockbackLeft = duration;
            bouncing = false;
            dash.Cancel();
        }

        /// <summary>Mueve al jugador al instante (reaparición) y lo detiene.</summary>
        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            knockbackLeft = 0f;
            pendingVerticalVelocity = null;
            bouncing = false;
            dash.Cancel();
        }

        public void Face(int direction)
        {
            if (direction == 0) return;
            facing = direction > 0 ? 1 : -1;
            if (visual == null) return;
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * facing, s.y, s.z);
        }

        // Subiendo con el botón pulsado (o por rebote/empujón): gravedad normal de subida.
        // Subiendo sin pulsar: se corta el salto. Cayendo: gravedad de caída.
        private float GravityScaleFor(float verticalVelocity)
        {
            if (verticalVelocity > 0.01f)
            {
                bool keepRising = jumpHeld || bouncing || knockbackLeft > 0f;
                return keepRising ? config.riseGravityScale : config.riseGravityScale * config.jumpCutGravityMultiplier;
            }
            return config.fallGravityScale;
        }

        /// <summary>¿Hay suelo bajo los pies? Si es una plataforma móvil, devuelve también su velocidad.</summary>
        private bool CheckGround(out Vector2 platformVelocity)
        {
            Bounds b = body.bounds;
            float depth = config.groundCheckDepth;
            // La caja se apoya justo bajo los pies; un poco más estrecha que el cuerpo para no rozar paredes.
            Vector2 center = new Vector2(b.center.x, b.min.y - depth * 0.5f + 0.02f);
            Vector2 size = new Vector2(b.size.x * 0.9f, depth);
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, config.groundMask);
            var platform = hit != null ? hit.GetComponentInParent<MovingPlatform>() : null;
            platformVelocity = platform != null ? platform.Velocity : Vector2.zero;
            return hit != null;
        }
    }
}
