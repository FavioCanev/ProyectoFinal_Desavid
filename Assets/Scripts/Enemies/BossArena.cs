using UnityEngine;

namespace Game
{
    /// <summary>
    /// Arena del jefe: al entrar se cierra la puerta, aparece la barra de vida y el jefe despierta.
    /// Al vencerlo se abre la puerta y aparece la meta. Si el jugador muere, la pelea se reinicia.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossArena : MonoBehaviour
    {
        [SerializeField] private EnemyController boss;
        [SerializeField] private Health playerHealth;
        [SerializeField] private GameObject door;
        [SerializeField] private GameObject goal;
        [SerializeField] private GameObject healthBar;

        public bool Started { get; private set; }
        public bool Cleared { get; private set; }

        private void Awake()
        {
            SetFightVisible(false);
            if (goal != null) goal.SetActive(false);
        }

        private void OnEnable()
        {
            if (boss != null) boss.Health.Died += OnBossDied;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (boss != null) boss.Health.Died -= OnBossDied;
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Started || Cleared || other.GetComponentInParent<PlayerLife>() == null) return;
            Started = true;
            SetFightVisible(true);
            boss.Activate();
        }

        private void OnBossDied()
        {
            Cleared = true;
            SetFightVisible(false);
            if (goal != null) goal.SetActive(true);
        }

        private void OnPlayerDied()
        {
            if (!Started || Cleared) return;
            Started = false;
            SetFightVisible(false);
            boss.ResetToStart();
        }

        private void SetFightVisible(bool visible)
        {
            if (door != null) door.SetActive(visible);
            if (healthBar != null) healthBar.SetActive(visible);
        }
    }
}
