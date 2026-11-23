using System;

namespace Game
{
    /// <summary>
    /// Lógica pura (sin Unity) de "coyote time", "jump buffer" y saltos en el aire (doble salto).
    /// Se separa para poder probarla con tests automáticos.
    /// </summary>
    public class JumpAssist
    {
        private readonly float coyoteTime;
        private readonly float jumpBufferTime;
        private float coyoteLeft;
        private float bufferLeft;
        private readonly int airJumps;
        private int airJumpsLeft;

        public JumpAssist(float coyoteTime, float jumpBufferTime, int airJumps = 0)
        {
            this.coyoteTime = Math.Max(0f, coyoteTime);
            this.jumpBufferTime = Math.Max(0f, jumpBufferTime);
            this.airJumps = Math.Max(0, airJumps);
        }

        /// <summary>Avanza los temporizadores. Llamar una vez por paso de física.</summary>
        public void Tick(float deltaTime, bool grounded, bool jumpPressed)
        {
            coyoteLeft = grounded ? coyoteTime : Math.Max(0f, coyoteLeft - deltaTime);
            if (grounded) airJumpsLeft = airJumps;
            bufferLeft = jumpPressed ? jumpBufferTime : Math.Max(0f, bufferLeft - deltaTime);
        }

        /// <summary>
        /// Devuelve true si toca saltar ahora (hay un salto recordado y aún queda margen de suelo).
        /// Consume ambos márgenes: el siguiente salto ya cuenta como salto en el aire.
        /// </summary>
        public bool TryConsumeJump()
        {
            if (bufferLeft <= 0f || coyoteLeft <= 0f) return false;
            bufferLeft = 0f;
            coyoteLeft = 0f;
            return true;
        }

        /// <summary>Salto en el aire (doble salto): sin margen de suelo, con un salto recordado y usos restantes.</summary>
        public bool TryConsumeAirJump()
        {
            if (bufferLeft <= 0f || coyoteLeft > 0f || airJumpsLeft <= 0) return false;
            bufferLeft = 0f;
            airJumpsLeft--;
            return true;
        }

        /// <summary>Recarga los saltos en el aire (por ejemplo, al rebotar sobre un enemigo, como en Hollow Knight).</summary>
        public void RefillAirJumps() => airJumpsLeft = airJumps;
    }
}
