using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game
{
    /// <summary>Meta del nivel: muestra el mensaje de nivel completado; con R se vuelve a empezar.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Goal : MonoBehaviour
    {
        [SerializeField] private GameObject completedMessage;

        public bool Reached { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Reached || other.GetComponentInParent<PlayerLife>() == null) return;
            Reached = true;
            if (completedMessage != null) completedMessage.SetActive(true);
            GameFeel.Play(Sfx.Goal);
        }

        private void Update()
        {
            if (Reached && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
