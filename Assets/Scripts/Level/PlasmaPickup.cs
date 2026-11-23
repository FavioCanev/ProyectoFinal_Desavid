using UnityEngine;

namespace Game
{
    /// <summary>La pistola de plasma tirada en el camino: al tocarla, la partida la obtiene para siempre.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlasmaPickup : MonoBehaviour
    {
        [Tooltip("Cartel que se muestra al recogerla (opcional).")]
        [SerializeField] private HudToast notice;

        private void Start()
        {
            if (GameFlow.Run.HasPlasma) gameObject.SetActive(false);   // ya la tenía (por ejemplo, al reiniciar)
        }

        private void Update() => transform.localPosition += Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.002f);   // flota

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerLife>() == null) return;
            GameFlow.Run.UnlockPlasma();
            GameFeel.Play(Sfx.Goal);
            GameFeel.Spawn(Fx.HitSpark, transform.position);
            if (notice != null) notice.Show();
            gameObject.SetActive(false);
        }
    }
}
