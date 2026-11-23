using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>Pantalla de Game Over: aparece al morir sin vidas y, con Enter o tras unos segundos, empieza una partida nueva desde el nivel 1.</summary>
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private PlayerLife player;
        [SerializeField] private GameObject panel;
        [SerializeField, Min(1f)] private float autoRestartAfter = 4f;

        private float shownAt = -1f;

        public bool IsShowing => shownAt >= 0f;

        private void Awake() => panel.SetActive(false);
        private void OnEnable() { if (player != null) player.GameOver += Show; }
        private void OnDisable() { if (player != null) player.GameOver -= Show; }

        private void Show()
        {
            shownAt = Time.unscaledTime;
            panel.SetActive(true);
            GameFeel.Play(Sfx.GameOver, pitchJitter: 0f);
        }

        private void Update()
        {
            if (!IsShowing) return;
            bool skip = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
            if (skip || Time.unscaledTime - shownAt >= autoRestartAfter) GameFlow.NewGame();
        }
    }
}
