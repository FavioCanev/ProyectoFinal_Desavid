using UnityEngine;

namespace Game
{
    /// <summary>
    /// Fondo que acompaña a la cámara pero se desplaza un poco más lento que el nivel (parallax),
    /// lo que da sensación de profundidad. Se escala solo para cubrir siempre la pantalla.
    /// Corre después de la cámara (Cinemachine) para no temblar.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        [Tooltip("1 = fijo en pantalla. Menos de 1 = se desplaza un poco al moverse.")]
        [SerializeField, Range(0f, 1f)] private float follow = 0.95f;
        [Tooltip("Rectángulo del nivel por el que se mueve la cámara (unidades del mundo).")]
        [SerializeField] private Rect levelBounds;
        [Tooltip("Distancia detrás de la cámara.")]
        [SerializeField] private float depth = 20f;

        private Camera cam;

        private void Start() => Refresh(Camera.main);

        private void LateUpdate()
        {
            if (cam != null) Follow();
        }

        /// <summary>Ajusta tamaño y posición para la cámara dada (también se usa al sacar capturas).</summary>
        public void Refresh(Camera camera)
        {
            cam = camera;
            if (cam == null) return;
            FitToCover();
            Follow();
        }

        private void Follow()
        {
            Vector3 c = cam.transform.position;
            Vector2 lag = ((Vector2)c - levelBounds.center) * (1f - follow);
            transform.position = new Vector3(c.x - lag.x, c.y - lag.y, c.z + depth);
        }

        // Tamaño mínimo para que el borde nunca se vea: la vista más lo que se desplaza el fondo.
        private void FitToCover()
        {
            var sprite = GetComponent<SpriteRenderer>().sprite;
            if (sprite == null) return;
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            float travelX = Mathf.Max(0f, levelBounds.width - viewW) * (1f - follow);
            float travelY = Mathf.Max(0f, levelBounds.height - viewH) * (1f - follow);
            Vector2 size = sprite.bounds.size;
            float scale = Mathf.Max((viewW + travelX) / size.x, (viewH + travelY) / size.y) * 1.02f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
