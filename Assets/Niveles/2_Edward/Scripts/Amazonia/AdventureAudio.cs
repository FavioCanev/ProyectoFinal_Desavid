using UnityEngine;

namespace Amazonia
{
    /// <summary>Música del nivel 2 con fundido entre pistas, y fuentes para efectos y voces del video.
    /// La pausa común del juego (AudioListener.pause) la silencia sola.</summary>
    public class AdventureAudio : MonoBehaviour
    {
        [Range(0, 1)] public float master = 1f, music = 0.5f, sfx = 0.8f;
        public AudioClip exploration, landBattle, transition, waterExploration, waterBattle;
        public AudioSource effects, voice;

        private AudioSource a, b;
        private float blend = 1f;
        private bool useA = true;

        private void Awake()
        {
            a = gameObject.AddComponent<AudioSource>();
            b = gameObject.AddComponent<AudioSource>();
            a.loop = b.loop = true;
            a.playOnAwake = b.playOnAwake = false;
            effects = gameObject.AddComponent<AudioSource>();
            voice = gameObject.AddComponent<AudioSource>();
        }

        public void Music(AudioClip clip)
        {
            if ((useA ? a : b).clip == clip) return;
            useA = !useA;
            AudioSource current = useA ? a : b;
            current.clip = clip;
            blend = 0f;
            if (clip) current.Play();
        }

        private void Update()
        {
            blend = Mathf.MoveTowards(blend, 1f, Time.unscaledDeltaTime);
            (useA ? a : b).volume = blend * master * music;
            (useA ? b : a).volume = (1f - blend) * master * music;
            effects.volume = voice.volume = master * sfx;
        }
    }
}
