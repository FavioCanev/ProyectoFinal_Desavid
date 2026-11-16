using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Movimiento del jugador: correr, salto de altura variable, coyote time y jump buffer.
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

        private float moveInput;
        private bool jumpPressedSinceLastStep;
        private bool jumpHeld;
        private int facing = 1;
        private float knockbackLeft;
        private float? pendingVerticalVelocity;
        private bool bouncing;

        /// <summary>Se dispara cuando se pulsa atacar (y hay control).</summary>
        public event Action AttackPressed;

        public bool ControlsEnabled { get; set; } = true;
        public bool IsGrounded { get; private set; }
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
            jumpAssist = new JumpAssist(config.coyoteTime, config.jumpBufferTime);
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
            jumpHeld = controls.Gameplay.Jump.IsPressed();
            if (ControlsEnabled && controls.Gameplay.Attack.WasPressedThisFrame()) AttackPressed?.Invoke();

            if (moveInput != 0f && knockbackLeft <= 0f) Face((int)moveInput);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGround();

            jumpAssist.Tick(dt, IsGrounded, jumpPressedSinceLastStep);
            jumpPressedSinceLastStep = false;

            Vector2 v = rb.linearVelocity;

            if (knockbackLeft > 0f)
            {
                knockbackLeft -= dt;   // durante el empujón no se controla la dirección
            }
            else
            {
                float targetX = moveInput * config.runSpeed;
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
            }

            if (v.y <= 0f) bouncing = false;
            rb.gravityScale = GravityScaleFor(v.y);
            v.y = Mathf.Max(v.y, -config.maxFallSpeed);

            rb.linearVelocity = v;
        }

        /// <summary>Lanza al jugador hacia arriba (rebote al golpear algo con el ataque hacia abajo).</summary>
        public void Bounce(float verticalVelocity)
        {
            pendingVerticalVelocity = verticalVelocity;
            bouncing = true;
        }

        /// <summary>Empuja al jugador y le quita el control horizontal durante un momento.</summary>
        public void Knockback(Vector2 velocity, float duration)
        {
            rb.linearVelocity = velocity;
            knockbackLeft = duration;
            bouncing = false;
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

        private bool CheckGround()
        {
            Bounds b = body.bounds;
            float depth = config.groundCheckDepth;
            // La caja se apoya justo bajo los pies; un poco más estrecha que el cuerpo para no rozar paredes.
            Vector2 center = new Vector2(b.center.x, b.min.y - depth * 0.5f + 0.02f);
            Vector2 size = new Vector2(b.size.x * 0.9f, depth);
            return Physics2D.OverlapBox(center, size, 0f, config.groundMask) != null;
        }
    }
}
