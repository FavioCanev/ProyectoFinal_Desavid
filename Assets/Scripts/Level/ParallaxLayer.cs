using UnityEngine;

namespace Game
{
    /// <summary>
    /// Capa con parallax. "follow" indica cuánto acompaña a la cámara:
    /// 1 = fija en pantalla; 0 = fija en el mundo; entre 0 y 1 = fondo (se mueve más lento que el nivel);
    /// negativo = primer plano (pasa más rápido que el nivel, como en Hollow Knight).
    /// Corre después de la cámara (Cinemachine) para no temblar.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        public enum Anchor { LevelCenter, OwnPosition }

        [SerializeField] private Vector2 follow = new Vector2(0.85f, 0.9f);
        [Tooltip("LevelCenter para fondos que cubren la pantalla; OwnPosition para piezas sueltas del primer plano.")]
        [SerializeField] private Anchor anchor = Anchor.LevelCenter;
        [Tooltip("Escalar y repetir el sprite para cubrir siempre la pantalla (fondos en mosaico).")]
        [SerializeField] private bool fitToCover;
        [Tooltip("Con fitToCover: repetir en mosaico a lo ancho (solo para imágenes cuyos bordes encajan). Si no, se estira para cubrir.")]
        [SerializeField] private bool tile = true;
        [Tooltip("Rectángulo del nivel por el que se mueve la cámara (unidades del mundo).")]
        [SerializeField] private Rect levelBounds;
        [Tooltip("Desplazamiento fijo respecto al resultado. Con follow.y = 1, deja la pieza pegada a un borde de la pantalla.")]
        [SerializeField] private Vector2 screenOffset;
        [Tooltip("Distancia detrás de la cámara (solo fondos).")]
        [SerializeField] private float depth = 20f;

        private Camera cam;
        private Vector2 anchorPosition;
        private bool initialized;

        private void Start() => Refresh(Camera.main);

        private void LateUpdate()
        {
            if (cam != null) Follow();
        }

        /// <summary>Ajusta la capa para la cámara dada (también se usa al sacar capturas sin jugar).</summary>
        public void Refresh(Camera camera)
        {
            if (!initialized)
            {
                anchorPosition = anchor == Anchor.OwnPosition ? (Vector2)transform.position : levelBounds.center;
                initialized = true;
            }
            cam = camera;
            if (cam == null) return;
            if (fitToCover) FitToCover();
            Follow();
        }

        private void Follow()
        {
            Vector3 c = cam.transform.position;
            Vector2 p = anchorPosition + Vector2.Scale((Vector2)c - anchorPosition, follow) + screenOffset;
            float z = anchor == Anchor.LevelCenter ? c.z + depth : transform.position.z;
            transform.position = new Vector3(p.x, p.y, z);
        }

        // Escala para cubrir la altura de la vista más lo que se desplaza; el ancho se repite en mosaico.
        private void FitToCover()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null) return;
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            float travelY = Mathf.Max(0f, levelBounds.height - viewH) * Mathf.Abs(1f - follow.y);
            float travelX = Mathf.Max(0f, levelBounds.width - viewW) * Mathf.Abs(1f - follow.x);
            Vector2 spriteSize = sr.sprite.bounds.size;
            if (!tile)
            {
                // Sin mosaico: una sola imagen, escalada para cubrir todo lo que recorre la cámara (sin costuras).
                float cover = Mathf.Max((viewH + travelY) / spriteSize.y, (viewW + travelX) / spriteSize.x) * 1.04f;
                transform.localScale = new Vector3(cover, cover, 1f);
                sr.drawMode = SpriteDrawMode.Simple;
                return;
            }
            float scale = (viewH + travelY) / spriteSize.y * 1.04f;
            transform.localScale = new Vector3(scale, scale, 1f);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2((viewW + travelX) / scale + spriteSize.x * 2f, spriteSize.y);
        }
    }
}
