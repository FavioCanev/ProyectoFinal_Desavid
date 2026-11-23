using System.Collections;
using Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Amazonia
{
    /// <summary>
    /// Progreso del nivel 2 (Edward): tras vencer al caimán en tierra se abre el lago y, con E en la orilla,
    /// el capibara se zambulle (pasa a Nivel2_Lago). Vencerlo en el lago completa el nivel.
    /// La vida, las vidas, la pausa y el Game Over son los comunes del juego (Game.*).
    /// </summary>
    public class AdventureFlow : MonoBehaviour
    {
        public static AdventureFlow Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => Instance = null;   // el proyecto entra en Play sin recargar el dominio

        public PlayerController player;
        public AdventureBoss boss;
        [Tooltip("Texto de pista en pantalla (\"[E] SUMERGIRSE\"...).")]
        public Text status;
        public Image fade;
        public ParticleSystem impact;
        [Tooltip("Cartel de nivel completado del HUD común (solo en el lago).")]
        public GameObject completedMessage;
        public bool aquatic;
        [Tooltip("Desde esta X se puede zambullir con E cuando el lago está abierto.")]
        public float shoreX = 58f;
        public Vector2 diveTarget = new Vector2(64f, -1f);

        private static readonly Color FadeColor = new Color(0f, 0.025f, 0.03f);
        private Health playerHealth;
        private AdventureDirector director;

        public bool Finished { get; private set; }
        public bool LakeOpen { get; private set; }
        public bool Transitioning { get; private set; }

        private void Awake()
        {
            Instance = this;
            playerHealth = player.GetComponent<Health>();   // no PlayerLife.Health: puede no haber hecho su Awake aún
            director = GetComponent<AdventureDirector>();
        }

        private void OnEnable() => playerHealth.Died += OnPlayerDied;
        private void OnDisable() => playerHealth.Died -= OnPlayerDied;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => StartCoroutine(Fade(1f, 0f));

        // Morir en plena pelea la reinicia (como en las arenas del resto del juego).
        private void OnPlayerDied()
        {
            if (!Finished && director.IntroDone) boss.ResetFight();
        }

        private void Update()
        {
            if (status == null || Finished || Transitioning || director.Busy) return;
            if (LakeOpen)
            {
                bool atShore = player.transform.position.x > shoreX;
                status.text = atShore ? "[ E ]  SUMERGIRSE · SIGUE AL CAIMÁN" : "EL CAIMÁN ESCAPÓ · VE A LA ORILLA →";
                if (atShore && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryDive();
            }
            else if (!director.IntroDone) status.text = aquatic ? "LAGO AMAZÓNICO · SIGUE AL CAIMÁN →" : "AMAZONÍA PERUANA · EXPLORA LA SELVA →";
            else if (boss.State == AdventureBoss.BossState.Telegraph) status.text = "¡PELIGRO! PREPÁRATE PARA ESQUIVAR";
            else if (boss.State == AdventureBoss.BossState.Recovery) status.text = "AGOTADO · ¡ATACA!";
            else status.text = boss.Enraged ? "¡FURIA!" : "";
        }

        public void LandWon()
        {
            director.LandDefeated();
            ClearProjectiles();
        }

        public void OpenLake() => LakeOpen = true;

        public bool TryDive()
        {
            if (aquatic || !LakeOpen || player.transform.position.x < shoreX || Transitioning || Finished) return false;
            StartCoroutine(Dive());
            return true;
        }

        private IEnumerator Dive()
        {
            Transitioning = true;
            yield return director.PrepareDive();
            player.ControlsEnabled = false;
            status.text = "DESCENDIENDO AL LAGO…";
            Vector2 start = player.transform.position;
            for (float t = 0f; t < 1.1f; t += Time.deltaTime)
            {
                float k = t / 1.1f;
                player.Teleport(Vector2.Lerp(start, diveTarget, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 1.3f);
                yield return null;
            }
            Effect(player.transform.position, Color.cyan);
            yield return Fade(0f, 1f);
            GameFlow.CompleteLevel();   // Nivel2_Tierra -> Nivel2_Lago
        }

        public void BossDefeated()
        {
            if (!Finished) StartCoroutine(Victory());
        }

        private IEnumerator Victory()
        {
            Finished = true;
            ClearProjectiles();
            Effect(boss.transform.position, Color.cyan);
            yield return new WaitForSeconds(1.2f);
            boss.Stopped = true;
            yield return director.Ending();
            player.ControlsEnabled = false;
            if (completedMessage != null) completedMessage.SetActive(true);
            GameFeel.Play(Sfx.Goal);
            yield return new WaitForSecondsRealtime(2.5f);
            GameFlow.CompleteLevel();   // Nivel2_Lago -> Nivel3
        }

        public void Effect(Vector3 position, Color color)
        {
            if (impact == null) return;
            var fx = Instantiate(impact, position, Quaternion.identity);
            var main = fx.main;
            main.startColor = color;
            fx.Play();
            Destroy(fx.gameObject, 1.5f);
        }

        private static void ClearProjectiles()
        {
            foreach (var shot in FindObjectsByType<Projectile>()) Destroy(shot.gameObject);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fade == null) yield break;
            fade.gameObject.SetActive(true);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                fade.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, Mathf.Lerp(from, to, t / 0.6f));
                yield return null;
            }
            fade.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, to);
            fade.gameObject.SetActive(to > 0f);
        }
    }
}
