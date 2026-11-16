using UnityEngine;

namespace Game
{
    /// <summary>Zona bajo los fosos: caer aquí cuesta una máscara y devuelve al último punto de reaparición.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife>();
            if (life != null) life.HazardHit();
        }
    }
}
