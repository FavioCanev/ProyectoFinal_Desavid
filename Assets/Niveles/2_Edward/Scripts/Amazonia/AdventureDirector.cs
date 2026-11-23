using System.Collections;
using Game;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Amazonia
{
    /// <summary>
    /// Secuencias narrativas de Edward: presentación del caimán, diálogos con retratos, videos, huida al lago,
    /// zambullida y final. Durante una secuencia el jugador no se controla ni recibe daño, y la cámara enfoca
    /// a quien corresponda. Esc omite. No gobierna las físicas del combate.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AdventureDirector : MonoBehaviour
    {
        [System.Serializable]
        public class Line
        {
            public string speaker;
            [TextArea] public string text;
            public bool caiman;
            public Line(string s, string t, bool c = false) { speaker = s; text = t; caiman = c; }
        }

        public Line[] landIntro =
        {
            new Line("Narrador", "El agua estaba más turbia de lo normal. La selva parecía contener la respiración."),
            new Line("Caimán", "¡Alto ahí! Nadie cruza mi orilla sin permiso.", true),
            new Line("Capibara", "Solo busco saber quién está ensuciando el lago."),
            new Line("Caimán", "Eso dicen todos los que llegan. ¡Fuera de mi territorio!", true),
        };
        public Line[] escape =
        {
            new Line("Capibara", "¿Eso cuenta como ganar?"),
            new Line("Caimán", "No cantes victoria... aquí abajo mando yo.", true),
        };
        public Line[] immersion = { new Line("Capibara", "No puedo dejarlo escapar... y todavía necesito respuestas.") };
        public Line[] waterIntro =
        {
            new Line("Narrador", "Bajo las raíces, el lago ocultaba cicatrices que la superficie no mostraba."),
            new Line("Caimán", "Entraste a mis aguas. Aquí tus saltos no servirán.", true),
            new Line("Capibara", "Entonces tendré que aprender a nadar más rápido."),
        };
        public Line[] ending =
        {
            new Line("Narrador", "Entre los sedimentos yacían restos de metal y marcas de maquinaria. El verdadero peligro no tenía escamas."),
            new Line("Capibara", "Esto no terminó aquí... alguien está dañando la selva."),
        };

        public Sprite capyPortrait, caimanPortrait, dialogFrame;
        public VideoClip escapeClip, immersionClip;
        public string escape_video_path = "", immersion_video_path = "";
        [Tooltip("Canvas del HUD común: aquí se dibujan el diálogo, el título y el video.")]
        public Canvas canvas;
        [Tooltip("Barra del jefe del HUD común: aparece tras la presentación.")]
        public GameObject bossBar;
        [Tooltip("La pelea empieza al pasar esta X.")]
        public float introX = 21f;

        private AdventureFlow flow;
        private AdventureAudio audioRig;
        private PlayerLife life;
        private CinemachineCamera vcam;
        private GameObject dialog, videoPanel;
        private Text dialogText, title;
        private Image portrait;
        private VideoPlayer video;
        private RawImage screen;
        private RenderTexture videoTexture;
        private bool advanceDialogue;

        public bool Busy { get; private set; }
        public bool IntroDone { get; private set; }
        public bool VideoPrepared { get; private set; }
        public bool SkipRequested { get; private set; }

        public void AdvanceDialogue() => advanceDialogue = true;
        public void RequestSkip() => SkipRequested = true;

        private void Awake()
        {
            flow = GetComponent<AdventureFlow>();
            audioRig = GetComponent<AdventureAudio>();
        }

        private void Start()
        {
            life = flow.player.GetComponent<PlayerLife>();
            vcam = FindAnyObjectByType<CinemachineCamera>();
            BuildUI();
            flow.boss.Stopped = true;
            flow.boss.Protected = true;
            flow.boss.visual.sprite.enabled = false;
            if (bossBar != null) bossBar.SetActive(false);
            audioRig.Music(flow.aquatic ? audioRig.waterExploration : audioRig.exploration);
        }

        private void Update()
        {
            if (Busy && Keyboard.current?.escapeKey.wasPressedThisFrame == true) RequestSkip();
            if (!IntroDone && !Busy && !flow.Finished && flow.player.transform.position.x > introX) StartCoroutine(Intro());
        }

        // ---------- Secuencias ----------

        private IEnumerator Intro()
        {
            Lock();
            IntroDone = true;
            Focus(flow.boss.transform);
            flow.boss.visual.sprite.enabled = true;
            flow.boss.visual.Face(-1f);
            flow.boss.visual.Play("Appear");
            yield return Wait(1.5f);
            yield return Talk(flow.aquatic ? waterIntro : landIntro);
            if (bossBar != null) bossBar.SetActive(true);
            title.text = flow.aquatic ? "CAIMÁN\nFASE II: AGUAS PROFUNDAS" : "CAIMÁN\nGUARDIÁN DEL LAGO";
            flow.boss.visual.Play("Telegraph");
            yield return Wait(1.2f);
            flow.boss.Protected = false;
            Unlock();
            flow.boss.Stopped = false;
            audioRig.Music(flow.aquatic ? audioRig.waterBattle : audioRig.landBattle);
        }

        public void LandDefeated() => StartCoroutine(EscapeSequence());

        private IEnumerator EscapeSequence()
        {
            Lock();
            if (bossBar != null) bossBar.SetActive(false);
            Focus(flow.boss.transform);
            flow.boss.visual.Play("Hurt");
            yield return Wait(1.9f);
            yield return Talk(escape);
            if (!SkipRequested) yield return PlayVideo(escapeClip, escape_video_path);
            if (!VideoPrepared && !SkipRequested)
            {
                // Sin video: el caimán lanza una piedra de despedida y huye.
                flow.Effect(flow.boss.transform.position, Color.gray);
                yield return Wait(0.6f);
                flow.boss.visual.Play("Rock");
                yield return Wait(0.7f);
            }
            Unlock();
            Focus(flow.boss.transform);
            flow.boss.BeginRetreat();
            audioRig.Music(audioRig.transition);
        }

        public IEnumerator PrepareDive()
        {
            Lock();
            yield return Talk(immersion);
            yield return Wait(0.8f);
            if (!SkipRequested) yield return PlayVideo(immersionClip, immersion_video_path);
            yield return Wait(0.6f);
            Unlock();
            flow.player.ControlsEnabled = false;
        }

        public IEnumerator Ending()
        {
            Lock();
            if (bossBar != null) bossBar.SetActive(false);
            yield return Wait(0.8f);
            yield return Talk(ending);
            Unlock();
        }

        // ---------- Control del jugador y la cámara ----------

        private void Lock()
        {
            Busy = true;
            SkipRequested = false;
            flow.player.ControlsEnabled = false;
            life.Protected = true;
            flow.boss.Stopped = true;
            foreach (var p in FindObjectsByType<Projectile>()) Destroy(p.gameObject);
        }

        private void Unlock()
        {
            dialog.SetActive(false);
            title.text = "";
            Focus(flow.player.transform);
            Busy = false;
            SkipRequested = false;
            life.Protected = false;
            if (!flow.Finished) flow.player.ControlsEnabled = true;
        }

        private void Focus(Transform target)
        {
            if (vcam != null) vcam.Follow = target;
        }

        private IEnumerator Wait(float duration)
        {
            for (float t = 0f; t < duration && !SkipRequested; t += Time.deltaTime) yield return null;
        }

        public IEnumerator Talk(Line[] lines)
        {
            dialog.SetActive(true);
            foreach (var line in lines)
            {
                if (SkipRequested) break;
                portrait.sprite = line.caiman ? caimanPortrait : capyPortrait;
                dialogText.text = line.speaker + "\n" + line.text;
                advanceDialogue = false;
                yield return null;
                while (!SkipRequested)
                {
                    var k = Keyboard.current;
                    if (advanceDialogue || k != null && k.enterKey.wasPressedThisFrame || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break;
                    yield return null;
                }
            }
            dialog.SetActive(false);
        }

        public IEnumerator PlayVideo(VideoClip clip, string path)
        {
            VideoPrepared = false;
            if (!clip && string.IsNullOrWhiteSpace(path)) yield break;
            bool failed = false;
            VideoPlayer.ErrorEventHandler error = (v, m) => { failed = true; Debug.LogWarning("Video no disponible; sigue la animación alternativa: " + m); };
            video.errorReceived += error;
            video.source = string.IsNullOrWhiteSpace(path) ? VideoSource.VideoClip : VideoSource.Url;
            if (video.source == VideoSource.VideoClip) video.clip = clip; else video.url = path;
            video.Prepare();
            float timeout = Time.realtimeSinceStartup + 10f;
            while (!video.isPrepared && !failed && !SkipRequested && Time.realtimeSinceStartup < timeout) yield return null;
            if (video.isPrepared && !failed && !SkipRequested)
            {
                VideoPrepared = true;
                Rect area = ((RectTransform)canvas.transform).rect;
                float ratio = (float)video.width / Mathf.Max(1, video.height);
                float h = area.height, w = h * ratio;
                if (w > area.width) { w = area.width; h = w / ratio; }
                screen.rectTransform.sizeDelta = new Vector2(w, h);
                videoTexture = new RenderTexture((int)video.width, (int)video.height, 0);
                videoTexture.Create();
                video.targetTexture = videoTexture;
                screen.texture = videoTexture;
                videoPanel.SetActive(true);
                video.Play();
                yield return null;
                timeout = Time.realtimeSinceStartup + (float)video.length + 5f;
                while (!SkipRequested && !failed && Time.realtimeSinceStartup < timeout && (video.isPlaying || video.frame < 1)) yield return null;
            }
            video.Stop();
            videoPanel.SetActive(false);
            video.targetTexture = null;
            screen.texture = null;
            ReleaseVideoTexture();
            video.errorReceived -= error;
        }

        private void OnDestroy()
        {
            if (video) video.Stop();
            ReleaseVideoTexture();
        }

        private void ReleaseVideoTexture()
        {
            if (!videoTexture) return;
            videoTexture.Release();
            Destroy(videoTexture);
            videoTexture = null;
        }

        // ---------- Interfaz (se crea por código sobre el HUD común) ----------

        private void BuildUI()
        {
            Transform root = canvas.transform;
            dialog = Rect("Diálogo narrativo", root, new Vector2(0f, -370f), new Vector2(1400f, 260f)).gameObject;
            var bg = dialog.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.06f, 0.045f, 0.95f);
            if (dialogFrame) { bg.sprite = dialogFrame; bg.color = Color.white; }
            portrait = Rect("Retrato", dialog.transform, new Vector2(-560f, 0f), new Vector2(200f, 200f)).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            dialogText = Label("Texto", dialog.transform, new Vector2(100f, 15f), new Vector2(1050f, 150f), "", 32);
            Label("Continuar", dialog.transform, new Vector2(0f, -100f), new Vector2(900f, 40f), "ENTER / CLIC: continuar · ESC: omitir", 22);
            dialog.SetActive(false);

            title = Label("Presentación", root, new Vector2(0f, 250f), new Vector2(1400f, 160f), "", 60);

            videoPanel = Rect("Video con bandas", root, Vector2.zero, new Vector2(4000f, 3000f)).gameObject;
            videoPanel.AddComponent<Image>().color = Color.black;
            screen = Rect("Video", videoPanel.transform, Vector2.zero, new Vector2(1600f, 900f)).gameObject.AddComponent<RawImage>();
            Label("Omitir video", videoPanel.transform, new Vector2(0f, -500f), new Vector2(900f, 40f), "ESC: OMITIR VIDEO", 26);
            videoPanel.SetActive(false);

            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.SetTargetAudioSource(0, audioRig.effects);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = Vector2.one * 0.5f;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        private static Text Label(string name, Transform parent, Vector2 pos, Vector2 size, string value, int font)
        {
            var t = Rect(name, parent, pos, size).gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = font;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = value;
            t.raycastTarget = false;
            return t;
        }
    }
}
