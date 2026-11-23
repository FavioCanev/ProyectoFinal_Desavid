using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Cartel que aparece, se queda un momento y se desvanece. Sirve para el nombre del nivel al entrar
    /// (showOnStart) y para avisar de los puntos de control (showOnCheckpoint).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HudToast : MonoBehaviour
    {
        [SerializeField] private bool showOnStart;
        [SerializeField] private bool showOnCheckpoint;
        [SerializeField, Min(0.1f)] private float holdTime = 2f;
        [SerializeField, Min(0.01f)] private float fadeTime = 0.4f;

        private CanvasGroup group;
        private float shownAt = float.NegativeInfinity;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        private void OnEnable() { if (showOnCheckpoint) Checkpoint.Activated += Show; }
        private void OnDisable() { if (showOnCheckpoint) Checkpoint.Activated -= Show; }

        private void Start() { if (showOnStart) Show(); }

        public void Show() => shownAt = Time.unscaledTime;

        private void Update()
        {
            float t = Time.unscaledTime - shownAt;
            if (t < fadeTime) group.alpha = t / fadeTime;
            else if (t < fadeTime + holdTime) group.alpha = 1f;
            else group.alpha = Mathf.Clamp01(1f - (t - fadeTime - holdTime) / fadeTime);
        }
    }
}
