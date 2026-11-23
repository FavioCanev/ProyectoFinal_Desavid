using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>Meta del nivel: muestra el mensaje y, tras una pausa, pasa al siguiente nivel (o al menú si es el último).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Goal : MonoBehaviour
    {
        [SerializeField] private GameObject completedMessage;
        [SerializeField, Min(0f)] private float delayBeforeNext = 2.5f;

        public bool Reached { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife>();
            if (Reached || life == null) return;
            Reached = true;
            life.GetComponent<PlayerController>().ControlsEnabled = false;
            if (completedMessage != null) completedMessage.SetActive(true);
            GameFeel.Play(Sfx.Goal);
            StartCoroutine(Continue());
        }

        private IEnumerator Continue()
        {
            yield return new WaitForSecondsRealtime(delayBeforeNext);
            GameFlow.CompleteLevel();
        }
    }
}
