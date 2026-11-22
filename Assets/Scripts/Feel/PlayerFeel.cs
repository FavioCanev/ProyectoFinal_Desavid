using UnityEngine;

namespace Game
{
    /// <summary>
    /// Sensaciones del jugador: estirar al saltar, aplastar al aterrizar, estela del dash,
    /// y la reacción (pausa, sacudida, sonido, partículas) a golpear y a ser golpeado.
    /// Solo escucha eventos: la lógica de juego no depende de este componente.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerCombat), typeof(PlayerLife))]
    public class PlayerFeel : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer sprite;

        [Header("Estirar y aplastar")]
        [SerializeField] private Vector2 jumpStretch = new Vector2(0.82f, 1.2f);
        [SerializeField] private Vector2 landSquash = new Vector2(1.25f, 0.78f);
        [Tooltip("Qué tan rápido vuelve a su forma normal.")]
        [SerializeField, Min(0f)] private float squashRecovery = 14f;

        [Header("Estela del dash")]
        [SerializeField] private Color afterimageColor = new Color(0.55f, 1f, 0.75f, 0.55f);
        [SerializeField, Min(0.01f)] private float afterimageInterval = 0.035f;

        [Header("Impacto")]
        [SerializeField, Min(0f)] private float hitStopOnHit = 0.055f;
        [SerializeField, Min(0f)] private float hitStopOnHurt = 0.12f;

        private PlayerController controller;
        private PlayerCombat combat;
        private PlayerLife life;
        private Vector3 baseScale;
        private Vector2 squash = Vector2.one;
        private float nextAfterimage;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            life = GetComponent<PlayerLife>();
            baseScale = new Vector3(Mathf.Abs(visual.localScale.x), visual.localScale.y, visual.localScale.z);

            controller.Jumped += OnJumped;
            controller.Landed += OnLanded;
            controller.Dashed += OnDashed;
            combat.Attacked += OnAttacked;
            combat.HitLanded += OnHitLanded;
            life.Health.Damaged += OnHurt;
            life.HazardTouched += OnHazard;
        }

        private void OnDestroy()
        {
            controller.Jumped -= OnJumped;
            controller.Landed -= OnLanded;
            controller.Dashed -= OnDashed;
            combat.Attacked -= OnAttacked;
            combat.HitLanded -= OnHitLanded;
            life.Health.Damaged -= OnHurt;
            life.HazardTouched -= OnHazard;
        }

        private void Update()
        {
            if (controller.IsDashing && Time.time >= nextAfterimage && sprite != null)
            {
                Afterimage.Spawn(sprite, afterimageColor, 0.22f);
                nextAfterimage = Time.time + afterimageInterval;
            }
        }

        private void LateUpdate()
        {
            // Vuelve suavemente a la forma normal; respeta hacia dónde mira el jugador.
            squash = Vector2.Lerp(squash, Vector2.one, 1f - Mathf.Exp(-squashRecovery * Time.deltaTime));
            visual.localScale = new Vector3(baseScale.x * squash.x * controller.Facing, baseScale.y * squash.y, baseScale.z);
        }

        private void OnJumped()
        {
            squash = jumpStretch;
            GameFeel.Play(Sfx.Jump, 0.6f);
            GameFeel.Spawn(Fx.Dust, transform.position);
        }

        private void OnLanded(float impactSpeed)
        {
            float strength = Mathf.InverseLerp(4f, 20f, impactSpeed);
            squash = Vector2.Lerp(Vector2.one, landSquash, 0.5f + 0.5f * strength);
            GameFeel.Play(Sfx.Land, 0.4f + 0.6f * strength);
            GameFeel.Spawn(Fx.Dust, transform.position);
            if (strength > 0.7f) GameFeel.Shake(0.15f);
        }

        private void OnDashed()
        {
            squash = new Vector2(1.25f, 0.85f);
            GameFeel.Play(Sfx.Dash, 0.8f);
            GameFeel.Spawn(Fx.Dust, transform.position, controller.Facing);
            nextAfterimage = 0f;
        }

        private void OnAttacked(AttackDirection _) => GameFeel.Play(Sfx.Swing, 1f, 0.1f);

        private void OnHitLanded(Vector2 point, bool pogo)
        {
            GameFeel.HitStop(hitStopOnHit);
            GameFeel.Shake(pogo ? 0.35f : 0.25f);
            GameFeel.Play(pogo ? Sfx.Pogo : Sfx.Hit, pogo ? 0.8f : 0.55f);   // el golpe es el más fuerte de la librería
            GameFeel.Spawn(Fx.HitSpark, point);
            if (pogo) squash = jumpStretch;
        }

        private void OnHurt(Vector2 _)
        {
            GameFeel.HitStop(hitStopOnHurt);
            GameFeel.Shake(0.8f);
            GameFeel.DamageFlash();
            GameFeel.Play(Sfx.PlayerHurt);
        }

        private void OnHazard()
        {
            GameFeel.Play(Sfx.Acid);
            GameFeel.Spawn(Fx.AcidSplash, transform.position);
        }
    }
}
