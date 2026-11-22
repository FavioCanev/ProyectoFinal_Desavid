using UnityEngine;

namespace Game
{
    /// <summary>Al tocarlo, el jugador reaparecerá aquí. El marcador cambia de color al activarse.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer marker;
        [SerializeField] private Color activeColor = new Color(0.4f, 0.95f, 1f);

        public bool IsActive { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife>();
            if (life == null) return;
            life.SetCheckpoint(transform.position);
            if (!IsActive) GameFeel.Play(Sfx.Checkpoint);
            IsActive = true;
            if (marker != null) marker.color = activeColor;
        }
    }
}
