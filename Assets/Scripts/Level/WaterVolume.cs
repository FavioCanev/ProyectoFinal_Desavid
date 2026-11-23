using UnityEngine;

namespace Game
{
    /// <summary>Zona de agua: mientras el jugador está dentro, nada en 4 direcciones.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterVolume : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other) => SetSwimming(other, true);
        private void OnTriggerExit2D(Collider2D other) => SetSwimming(other, false);

        private static void SetSwimming(Collider2D other, bool swimming)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.Swimming = swimming;
        }
    }
}
