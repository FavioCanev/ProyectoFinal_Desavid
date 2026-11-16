using UnityEngine;

namespace Game
{
    /// <summary>
    /// Movimiento del jugador: correr, salto de altura variable, coyote time y jump buffer.
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

        public bool IsGrounded { get; private set; }
        /// <summary>1 = mira a la derecha, -1 = a la izquierda.</summary>
        public int Facing => facing;
        public Vector2 Velocity => rb.linearVelocity;

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
            float x = controls.Gameplay.Move.ReadValue<Vector2>().x;
            moveInput = Mathf.Abs(x) > 0.2f ? Mathf.Sign(x) : 0f;

            if (controls.Gameplay.Jump.WasPressedThisFrame()) jumpPressedSinceLastStep = true;
            jumpHeld = controls.Gameplay.Jump.IsPressed();

            if (moveInput != 0f) SetFacing((int)moveInput);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGround();

            jumpAssist.Tick(dt, IsGrounded, jumpPressedSinceLastStep);
            jumpPressedSinceLastStep = false;

            Vector2 v = rb.linearVelocity;

            float targetX = moveInput * config.runSpeed;
            float rate = moveInput != 0f ? config.acceleration : config.deceleration;
            v.x = Mathf.MoveTowards(v.x, targetX, rate * dt);

            if (jumpAssist.TryConsumeJump()) v.y = config.JumpVelocity;

            rb.gravityScale = GravityScaleFor(v.y);
            v.y = Mathf.Max(v.y, -config.maxFallSpeed);

            rb.linearVelocity = v;
        }

        // Subiendo con el botón pulsado: gravedad normal de subida.
        // Subiendo sin pulsar: se corta el salto. Cayendo: gravedad de caída.
        private float GravityScaleFor(float verticalVelocity)
        {
            if (verticalVelocity > 0.01f)
                return jumpHeld ? config.riseGravityScale : config.riseGravityScale * config.jumpCutGravityMultiplier;
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

        private void SetFacing(int direction)
        {
            facing = direction;
            if (visual == null) return;
            Vector3 s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * direction, s.y, s.z);
        }
    }
}
