using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public enum Sfx { Jump, Land, Dash, Swing, Hit, EnemyHurt, EnemyDie, PlayerHurt, Spit, Shockwave, BossRoar, Checkpoint, Goal, Acid, Pogo }
    public enum Fx { HitSpark, Dust, DeathBurst, AcidSplash }

    /// <summary>
    /// Punto central del "gamefeel": pausa de impacto, sacudida de cámara, sonidos, partículas y
    /// destello de daño. Se usa con llamadas estáticas (GameFeel.Shake(...)); si no hay uno en la escena,
    /// no hace nada (así las pruebas y escenas sin efectos funcionan igual).
    /// </summary>
    public class GameFeel : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulse;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Un clip por cada valor de Sfx, en el mismo orden.")]
        [SerializeField] private AudioClip[] clips;
        [Tooltip("Un sistema de partículas por cada valor de Fx, en el mismo orden.")]
        [SerializeField] private ParticleSystem[] effects;
        [SerializeField] private Image damageFlash;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        private static GameFeel instance;
        private float hitStopUntil;
        private bool hitStopRunning;
        private float flashAlpha;

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (damageFlash == null) return;
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 2.5f);
            Color c = damageFlash.color;
            damageFlash.color = new Color(c.r, c.g, c.b, flashAlpha);
        }

        // ---------- API ----------

        /// <summary>Congela el juego un instante (como Hollow Knight al golpear). Las llamadas seguidas se alargan, no se apilan.</summary>
        public static void HitStop(float seconds)
        {
            if (instance == null || seconds <= 0f) return;
            instance.hitStopUntil = Mathf.Max(instance.hitStopUntil, Time.unscaledTime + seconds);
            if (!instance.hitStopRunning) instance.StartCoroutine(instance.HitStopRoutine());
        }

        /// <summary>Sacude la cámara; 1 = golpe fuerte.</summary>
        public static void Shake(float force)
        {
            // Dirección al azar: en 2D una sacudida en todas direcciones se siente más natural que siempre hacia abajo.
            if (instance != null && instance.impulse != null)
                instance.impulse.GenerateImpulseWithVelocity((Vector3)(Random.insideUnitCircle.normalized * force * 0.35f));
        }

        public static void Play(Sfx sound, float volumeScale = 1f, float pitchJitter = 0.06f)
        {
            if (instance == null || instance.audioSource == null) return;
            int i = (int)sound;
            if (instance.clips == null || i >= instance.clips.Length || instance.clips[i] == null) return;
            instance.audioSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);   // variación para que no suene repetido
            instance.audioSource.PlayOneShot(instance.clips[i], instance.volume * volumeScale);
        }

        public static void Spawn(Fx effect, Vector2 position, int facing = 1)
        {
            if (instance == null) return;
            int i = (int)effect;
            if (instance.effects == null || i >= instance.effects.Length || instance.effects[i] == null) return;
            var fx = Instantiate(instance.effects[i], position, Quaternion.identity);
            if (facing < 0) fx.transform.localScale = new Vector3(-1f, 1f, 1f);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.2f);
        }

        /// <summary>Destello rojo en pantalla al recibir daño.</summary>
        public static void DamageFlash(float strength = 0.35f)
        {
            if (instance != null) instance.flashAlpha = Mathf.Max(instance.flashAlpha, strength);
        }

        private IEnumerator HitStopRoutine()
        {
            hitStopRunning = true;
            Time.timeScale = 0f;
            while (Time.unscaledTime < hitStopUntil) yield return null;
            Time.timeScale = 1f;
            hitStopRunning = false;
        }
    }
}
