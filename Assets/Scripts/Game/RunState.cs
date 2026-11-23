using System;

namespace Game
{
    /// <summary>
    /// Lógica pura (sin Unity) de la partida: vidas y monedas, que se conservan entre niveles.
    /// Cada vida es una reaparición: con 3 vidas, la cuarta muerte es Game Over.
    /// </summary>
    public class RunState
    {
        public const int StartingLives = 3;

        private int coinsAtLevelStart;

        public int Lives { get; private set; } = StartingLives;
        public int Coins { get; private set; }
        /// <summary>La pistola de plasma se consigue en el nivel 2 y se conserva el resto de la partida.</summary>
        public bool HasPlasma { get; private set; }

        public event Action Changed;

        /// <summary>Gasta una vida para reaparecer. Devuelve false si ya no quedan (Game Over).</summary>
        public bool TryUseLife()
        {
            if (Lives <= 0) return false;
            Lives--;
            Changed?.Invoke();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Changed?.Invoke();
        }

        public void UnlockPlasma()
        {
            if (HasPlasma) return;
            HasPlasma = true;
            Changed?.Invoke();
        }

        /// <summary>Al empezar un nivel: recuerda las monedas para poder deshacer las de un reinicio.</summary>
        public void BeginLevel() => coinsAtLevelStart = Coins;

        /// <summary>Reiniciar el nivel desde la pausa devuelve las monedas que había al empezarlo (no se pueden "farmear").</summary>
        public void RestartLevel()
        {
            Coins = coinsAtLevelStart;
            Changed?.Invoke();
        }

        /// <summary>Partida nueva (menú o Game Over): vidas llenas, sin monedas y sin plasma.</summary>
        public void Reset()
        {
            Lives = StartingLives;
            Coins = 0;
            coinsAtLevelStart = 0;
            HasPlasma = false;
            Changed?.Invoke();
        }
    }
}
