using System;

namespace Game
{
    /// <summary>
    /// Lógica pura (sin Unity) de "coyote time" y "jump buffer".
    /// Se separa para poder probarla con tests automáticos.
    /// </summary>
    public class JumpAssist
    {
        private readonly float coyoteTime;
        private readonly float jumpBufferTime;
        private float coyoteLeft;
        private float bufferLeft;

        public JumpAssist(float coyoteTime, float jumpBufferTime)
        {
            this.coyoteTime = Math.Max(0f, coyoteTime);
            this.jumpBufferTime = Math.Max(0f, jumpBufferTime);
        }

        /// <summary>Avanza los temporizadores. Llamar una vez por paso de física.</summary>
        public void Tick(float deltaTime, bool grounded, bool jumpPressed)
        {
            coyoteLeft = grounded ? coyoteTime : Math.Max(0f, coyoteLeft - deltaTime);
            bufferLeft = jumpPressed ? jumpBufferTime : Math.Max(0f, bufferLeft - deltaTime);
        }

        /// <summary>
        /// Devuelve true si toca saltar ahora (hay un salto recordado y aún queda margen de suelo).
        /// Consume ambos márgenes, así que no permite un doble salto.
        /// </summary>
        public bool TryConsumeJump()
        {
            if (bufferLeft <= 0f || coyoteLeft <= 0f) return false;
            bufferLeft = 0f;
            coyoteLeft = 0f;
            return true;
        }
    }
}
