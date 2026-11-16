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

        [Header("Detección de suelo")]
        public LayerMask groundMask;
        [Tooltip("Grosor de la zona bajo los pies donde se busca suelo.")]
        [Min(0.01f)] public float groundCheckDepth = 0.1f;

        /// <summary>Velocidad inicial necesaria para alcanzar jumpHeight con la gravedad de subida.</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * riseGravityScale * jumpHeight);
    }
}
