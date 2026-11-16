using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Muestra la vida del jugador como máscaras (llenas o vacías).</summary>
    public class HealthHUD : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Image[] masks;
        [SerializeField] private Color fullColor = new Color(0.95f, 0.95f, 1f);
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);

        private void OnEnable()
        {
            if (health == null) return;
            health.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (health != null) health.Changed -= Refresh;
        }

        private void Refresh()
        {
            for (int i = 0; i < masks.Length; i++)
                masks[i].color = i < health.Current ? fullColor : emptyColor;
        }
    }
}
