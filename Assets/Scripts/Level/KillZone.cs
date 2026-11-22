using UnityEngine;

namespace Game
{
    /// <summary>
    /// Ácido o vacío: al jugador le cuesta una máscara y lo devuelve al último punto de reaparición;
    /// a un enemigo (por ejemplo, empujado de un golpe) lo mata.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife>();
            if (life != null) { life.HazardHit(); return; }

            var health = other.GetComponentInParent<Health>();
            if (health != null && !health.IsDead) health.TryDamage(health.Current, transform.position, ignoreInvulnerability: true);
        }
    }
}
