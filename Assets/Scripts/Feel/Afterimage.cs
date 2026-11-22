using UnityEngine;

namespace Game
{
    /// <summary>Copia del sprite que se desvanece (estela del dash).</summary>
    public class Afterimage : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float lifetime;
        private float age;
        private Color startColor;

        public static void Spawn(SpriteRenderer source, Color color, float lifetime)
        {
            var go = new GameObject("Estela");
            go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            go.transform.localScale = source.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingOrder = source.sortingOrder - 1;
            sr.color = color;
            var a = go.AddComponent<Afterimage>();
            a.sr = sr;
            a.lifetime = lifetime;
            a.startColor = color;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / lifetime;
            if (t >= 1f) { Destroy(gameObject); return; }
            sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
        }
    }
}
