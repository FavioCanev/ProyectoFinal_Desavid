using System;
using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Bandera de punto de control (al estilo de Mario): al tocarla, el jugador reaparecerá aquí,
    /// la bandera sube por el mástil y cambia de color.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer flag;
        [SerializeField] private Sprite activeSprite;
        [Tooltip("Altura local de la bandera ya izada.")]
        [SerializeField] private float raisedY = 2f;
        [SerializeField, Min(0.01f)] private float raiseTime = 0.4f;

        /// <summary>Se dispara la primera vez que se activa cualquier punto de control (para el aviso del HUD).</summary>
        public static event Action Activated;

        public bool IsActive { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var life = other.GetComponentInParent<PlayerLife>();
            if (life == null) return;
            life.SetCheckpoint(transform.position);
            if (IsActive) return;
            IsActive = true;
            GameFeel.Play(Sfx.Checkpoint);
            Activated?.Invoke();
            if (flag != null) StartCoroutine(Raise());
        }

        private IEnumerator Raise()
        {
            if (activeSprite != null) flag.sprite = activeSprite;
            Transform t = flag.transform;
            float startY = t.localPosition.y;
            for (float time = 0f; time < raiseTime; time += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - time / raiseTime, 3f);   // sube rápido y frena arriba
                t.localPosition = new Vector3(t.localPosition.x, Mathf.Lerp(startY, raisedY, k), 0f);
                yield return null;
            }
            t.localPosition = new Vector3(t.localPosition.x, raisedY, 0f);
        }
    }
}
