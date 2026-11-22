using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Menú de pausa con Esc: Continuar, Reiniciar nivel y Menú principal (si existe esa escena).</summary>
    public class PauseMenu : MonoBehaviour
    {
        public const string MainMenuScene = "Menu";

        [Tooltip("Se desactiva mientras dura la pausa para que no lea las teclas.")]
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            panel.SetActive(false);
            resumeButton.onClick.AddListener(() => SetPaused(false));
            restartButton.onClick.AddListener(Restart);
            menuButton.onClick.AddListener(() => Load(MainMenuScene));
            menuButton.gameObject.SetActive(Application.CanStreamedLevelBeLoaded(MainMenuScene));
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetPaused(!IsPaused);
            // La pausa de impacto (GameFeel) devuelve el tiempo a 1 al terminar: aquí manda la pausa.
            if (IsPaused) Time.timeScale = 0f;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            panel.SetActive(paused);
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (player != null) player.enabled = !paused;
            if (paused && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }

        private void Restart() => Load(SceneManager.GetActiveScene().name);

        private void Load(string scene)
        {
            SetPaused(false);
            SceneManager.LoadScene(scene);
        }

        private void OnDestroy()
        {
            if (IsPaused) { Time.timeScale = 1f; AudioListener.pause = false; }
        }
    }
}
