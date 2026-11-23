using UnityEngine;

namespace Game
{
    /// <summary>Moneda: al tocarla suma al contador de la partida (compartido por todos los niveles) y desaparece.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        [SerializeField, Min(1)] private int value = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerLife>() == null) return;
            GameFlow.Run.AddCoins(value);
            GameFeel.Play(Sfx.Coin);
            GameFeel.Spawn(Fx.HitSpark, transform.position);
            Destroy(gameObject);
        }
    }
}
