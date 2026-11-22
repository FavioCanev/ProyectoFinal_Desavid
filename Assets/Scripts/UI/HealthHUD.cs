using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Muestra la vida del jugador como corazones llenos o vacíos; el que se pierde da un saltito.</summary>
    public class HealthHUD : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Image[] hearts;
        [SerializeField] private Sprite fullSprite;
        [SerializeField] private Sprite emptySprite;

        private const float PopDuration = 0.25f;
        private int shown = -1;
        private int popIndex = -1;
        private float popTime;

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
            if (shown > health.Current) { popIndex = health.Current; popTime = Time.unscaledTime; }
            shown = health.Current;
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].sprite = i < health.Current ? fullSprite : emptySprite;
        }

        private void Update()
        {
            if (popIndex < 0 || popIndex >= hearts.Length) return;
            float t = (Time.unscaledTime - popTime) / PopDuration;
            float scale = t < 1f ? 1f + 0.35f * Mathf.Sin(t * Mathf.PI) : 1f;
            hearts[popIndex].rectTransform.localScale = new Vector3(scale, scale, 1f);
            if (t >= 1f) popIndex = -1;
        }
    }
}
