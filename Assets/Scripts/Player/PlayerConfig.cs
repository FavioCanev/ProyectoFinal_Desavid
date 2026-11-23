using UnityEngine;

namespace Game
{
    /// <summary>
    /// Todos los números que definen la "sensación" del jugador, en un solo lugar.
    /// Se ajustan desde el Inspector sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Player Config", fileName = "PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movimiento horizontal")]
        [Tooltip("Velocidad máxima al correr (unidades por segundo).")]
        [Min(0f)] public float runSpeed = 8f;
        [Tooltip("Qué tan rápido alcanza la velocidad máxima. Alto = casi instantáneo.")]
        [Min(0f)] public float acceleration = 90f;
        [Tooltip("Qué tan rápido se detiene al soltar la tecla. Alto = frena en seco.")]
        [Min(0f)] public float deceleration = 110f;

        [Header("Salto")]
        [Tooltip("Altura máxima del salto manteniendo el botón (unidades).")]
        [Min(0f)] public float jumpHeight = 3f;
        [Tooltip("Gravedad al subir. Mayor = salto más rápido y firme.")]
        [Min(0.1f)] public float riseGravityScale = 3f;
        [Tooltip("Gravedad al caer. Mayor que la de subida = caída más pesada.")]
        [Min(0.1f)] public float fallGravityScale = 5f;
        [Tooltip("Si sueltas el botón mientras subes, la gravedad se multiplica por esto y el salto se corta.")]
        [Min(1f)] public float jumpCutGravityMultiplier = 3f;
        [Tooltip("Velocidad máxima de caída (unidades por segundo).")]
        [Min(0f)] public float maxFallSpeed = 22f;

        [Header("Comodidad")]
        [Tooltip("Segundos después de salir de una plataforma en los que aún se puede saltar.")]
        [Min(0f)] public float coyoteTime = 0.1f;
        [Tooltip("Segundos que se recuerda un salto pulsado justo antes de aterrizar.")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("Dash")]
        [Tooltip("Velocidad horizontal durante el dash (unidades por segundo).")]
        [Min(0f)] public float dashSpeed = 18f;
        [Tooltip("Cuánto dura el dash. Distancia = velocidad × duración.")]
        [Min(0.01f)] public float dashDuration = 0.18f;
        [Tooltip("Segundos de espera tras terminar un dash.")]
        [Min(0f)] public float dashCooldown = 0.4f;
        [Tooltip("Dashes permitidos en el aire antes de volver a tocar el suelo.")]
        [Min(0)] public int airDashes = 1;

        [Header("Detección de suelo")]
        public LayerMask groundMask;
        [Tooltip("Grosor de la zona bajo los pies donde se busca suelo.")]
        [Min(0.01f)] public float groundCheckDepth = 0.1f;

        [Header("Combate")]
        [Tooltip("Segundos mínimos entre dos ataques.")]
        [Min(0.05f)] public float attackCooldown = 0.35f;
        [Tooltip("Cuánto dura la animación de ataque.")]
        [Min(0.05f)] public float attackDuration = 0.25f;
        [Min(1)] public int attackDamage = 1;
        [Tooltip("Velocidad hacia arriba al golpear algo con el ataque hacia abajo (rebote).")]
        [Min(0f)] public float pogoVelocity = 12f;
        [Tooltip("Zonas de golpe, relativas a los pies del jugador. El ataque frontal se refleja según la dirección.")]
        public Vector2 forwardHitOffset = new Vector2(0.95f, 0.85f);
        public Vector2 forwardHitSize = new Vector2(1.6f, 1.0f);
        public Vector2 upHitOffset = new Vector2(0f, 2.0f);
        public Vector2 upHitSize = new Vector2(1.2f, 1.4f);
        public Vector2 downHitOffset = new Vector2(0f, -0.4f);
        public Vector2 downHitSize = new Vector2(1.2f, 1.2f);
        public LayerMask enemyMask;

        [Header("Nado (dentro de un WaterVolume)")]
        [Min(0f)] public float swimSpeed = 5.3f;
        [Min(0f)] public float swimAcceleration = 24f;
        [Tooltip("Velocidad a la que se hunde sin pulsar arriba/abajo.")]
        [Min(0f)] public float sinkSpeed = 0.6f;

        [Header("Plasma (tecla Fin; se consigue en el nivel 2)")]
        [Min(1)] public int plasmaCapacity = 3;
        [Min(0f)] public float plasmaReloadTime = 6f;
        [Min(0f)] public float plasmaCooldown = 0.42f;
        [Min(0f)] public float plasmaSpeed = 16f;
        [Min(1)] public int plasmaDamage = 1;

        [Header("Daño recibido")]
        [Tooltip("Empujón al recibir un golpe: X se aleja del enemigo, Y hacia arriba.")]
        public Vector2 knockbackVelocity = new Vector2(6f, 7f);
        [Tooltip("Segundos sin control tras el empujón.")]
        [Min(0f)] public float knockbackTime = 0.2f;
        [Tooltip("Segundos entre morir y reaparecer.")]
        [Min(0f)] public float respawnDelay = 1.2f;

        /// <summary>Velocidad inicial necesaria para alcanzar jumpHeight con la gravedad de subida.</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * riseGravityScale * jumpHeight);
    }
}
