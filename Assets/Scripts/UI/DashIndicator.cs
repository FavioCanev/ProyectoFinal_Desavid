using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Ícono del dash: apagado mientras no se puede usar; al recargarse se enciende con un pulso.</summary>
    public class DashIndicator : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Image icon;
        [SerializeField] private Color readyColor = Color.white;
        [SerializeField] private Color waitingColor = new Color(0.35f, 0.3f, 0.28f, 0.6f);

        private bool wasReady = true;
        private float pulseTime = -1f;
        private const float PulseDuration = 0.2f;

        private void Update()
        {
            if (player == null) return;
            bool ready = player.DashReady;
            if (ready && !wasReady) pulseTime = Time.unscaledTime;
            wasReady = ready;
            icon.color = ready ? readyColor : waitingColor;

            float t = (Time.unscaledTime - pulseTime) / PulseDuration;
            float scale = pulseTime >= 0f && t < 1f ? 1f + 0.25f * Mathf.Sin(t * Mathf.PI) : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
