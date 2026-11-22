using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Barra de vida del jefe (una imagen de relleno horizontal).</summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Image fill;

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

        private void Refresh() => fill.fillAmount = (float)health.Current / health.Max;
    }
}
