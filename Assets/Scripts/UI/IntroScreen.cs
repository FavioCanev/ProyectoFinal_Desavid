using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace Game
{
    /// <summary>Video de introducción de Dante: al terminar (o al pulsar cualquier tecla) pasa al menú.</summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class IntroScreen : MonoBehaviour
    {
        private VideoPlayer video;
        private bool leaving;

        private void Awake()
        {
            video = GetComponent<VideoPlayer>();
            video.isLooping = false;
            video.loopPointReached += _ => GoToMenu();
            video.errorReceived += (_, message) => { Debug.LogWarning("Intro sin video: " + message); GoToMenu(); };
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) GoToMenu();
        }

        private void GoToMenu()
        {
            if (leaving) return;
            leaving = true;
            GameFlow.GoToMenu();
        }
    }
}
