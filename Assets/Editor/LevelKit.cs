using System.Linq;
using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Piezas comunes a todos los niveles: jugador, cámara, interfaz (vida, dash, vidas, monedas, pausa, Game Over),
/// gamefeel, banderas, monedas, carteles y meta. Así los niveles se arman igual y se ven iguales.
/// </summary>
public static class LevelKit
{
    private const string CheckpointFolder = "Assets/Art/Checkpoint/";
    private const string UiFolder = "Assets/Art/UI/";   // madera y corazones recortados del arte de Dante (nivel 1)
    private const string CoinSheet = "Assets/Art/Coin/moneda-Photoroom.png";
    private const string CoinAnimator = "Assets/Art/Coin/moneda-Photoroom_0.controller";
    private const string PlayerSheet = "Assets/Art/Player/spriteCapi.png";

    private static readonly Color Cream = new Color(1f, 0.95f, 0.82f);
    private static readonly Color DarkWood = new Color(0.22f, 0.12f, 0.05f);

    public struct Hud
    {
        public GameObject CompletedMessage, BossBar;
        public Image DamageFlash;
        public Transform Canvas;
    }

    public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ---------- Arte compartido ----------

    // Medidas de la bandera en unidades del mundo (los sprites miden en píxeles a FlagPixelsPerUnit).
    private const int FlagPixelsPerUnit = 30;
    private static readonly Vector2 FlagLowered = new Vector2(0.03f, 0.8f);   // esquina superior izquierda, junto a la base
    private const float FlagRaisedY = 2.1f;                                   // junto a la bola del mástil

    public static void PrepareSharedArt()
    {
        TestRoomBuilder.ConfigureSprite(CheckpointFolder + "flag_pole.png", new Vector2(0.5f, 0f), FilterMode.Point, FlagPixelsPerUnit);
        TestRoomBuilder.ConfigureSprite(CheckpointFolder + "flag_off.png", new Vector2(0f, 1f), FilterMode.Point, FlagPixelsPerUnit);
        TestRoomBuilder.ConfigureSprite(CheckpointFolder + "flag_on.png", new Vector2(0f, 1f), FilterMode.Point, FlagPixelsPerUnit);

        foreach (string name in new[] { "ui_panel", "ui_banner", "ui_plank_gold", "ui_ring", "ui_button", "icon_play", "icon_restart", "icon_home", "icon_pause" })
            TestRoomBuilder.ConfigureSprite(UiFolder + name + ".png", new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        TestRoomBuilder.ConfigureSprite(UiFolder + "heart_full.png", new Vector2(0.5f, 0.5f), FilterMode.Point);
        TestRoomBuilder.ConfigureSprite(UiFolder + "heart_empty.png", new Vector2(0.5f, 0.5f), FilterMode.Point);
        // La tabla se estira a lo ancho (barra del jefe) sin deformar sus extremos.
        TestRoomBuilder.ConfigureSprite(UiFolder + "ui_plank.png", new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        var plank = (TextureImporter)AssetImporter.GetAtPath(UiFolder + "ui_plank.png");
        plank.spriteBorder = new Vector4(24f, 18f, 24f, 18f);
        plank.SaveAndReimport();
    }

    public static Sprite Ui(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UiFolder + name + ".png");

    private static Sprite SubSprite(string sheet, string name) =>
        AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().First(s => s.name == name);

    // ---------- Jugador y objetos del nivel ----------

    public static GameObject CreatePlayer(GameObject playerPrefab, Vector2 feet)
    {
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = feet + Vector2.up * 0.02f;
        return player;
    }

    public static void CreateCheckpoint(Transform parent, string name, Vector2 feet)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 2.4f);
        trigger.offset = new Vector2(0f, 1.2f);

        SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_pole.png"), Vector2.zero, 1f, order: 2);
        var flag = SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_off.png"), FlagLowered, 1f, order: 3);
        flag.name = "Bandera";

        var checkpoint = go.AddComponent<Checkpoint>();
        TestRoomBuilder.Set(checkpoint, "flag", flag);
        TestRoomBuilder.Set(checkpoint, "activeSprite", AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_on.png"));
        var so = new SerializedObject(checkpoint);
        so.FindProperty("raisedY").floatValue = FlagRaisedY;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Moneda giratoria (arte de Dante) con su centro en "center".</summary>
    public static void CreateCoin(Transform parent, Vector2 center)
    {
        var go = new GameObject("Moneda");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SubSprite(CoinSheet, "moneda-Photoroom_0");
        sr.sortingOrder = 4;
        go.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CoinAnimator);
        var trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;   // en escala local: unas 0,36 unidades del mundo
        go.AddComponent<Coin>();
    }

    /// <summary>Fila de monedas sobre el suelo, separadas "spacing", empezando en "first" (centro de la primera).</summary>
    public static void CreateCoinRow(Transform parent, Vector2 first, int count, float spacing = 0.8f)
    {
        for (int i = 0; i < count; i++) CreateCoin(parent, first + Vector2.right * (i * spacing));
    }

    /// <summary>Cartel de madera con texto (tutorial).</summary>
    public static void CreateSign(Transform parent, Vector2 feet, string text)
    {
        var go = new GameObject("Cartel");
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var board = SpriteChild(go.transform, Ui("ui_plank"), new Vector2(0f, 1.5f), 1f, order: 3);
        board.drawMode = SpriteDrawMode.Sliced;
        board.size = new Vector2(4.2f, 1.25f);
        var post = SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Debug/white.png"), new Vector2(0f, 0.45f), 1f, order: 2);
        post.transform.localScale = new Vector3(0.12f, 0.9f, 1f);
        post.color = new Color(0.32f, 0.2f, 0.1f);

        var textGo = new GameObject("Texto");
        textGo.transform.SetParent(go.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var mesh = textGo.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = Font;
        mesh.fontSize = 64;
        mesh.characterSize = 0.04f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = Cream;
        var renderer = textGo.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = Font.material;
        renderer.sortingOrder = 4;
    }

    public static GameObject CreateGoal(Transform parent, Vector2 feet, GameObject completedMessage)
    {
        var go = new GameObject("Meta");
        go.transform.SetParent(parent, false);
        go.transform.position = feet;
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.6f, 3f);
        trigger.offset = new Vector2(0f, 1.5f);

        // Bandera grande y dorada: el final del nivel.
        var pole = SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_pole.png"), Vector2.zero, 1.5f, order: 2);
        var flag = SpriteChild(go.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CheckpointFolder + "flag_on.png"), new Vector2(0.05f, FlagRaisedY * 1.5f), 1.5f, order: 3);
        flag.color = new Color(1f, 0.85f, 0.3f);
        pole.name = "Mástil"; flag.name = "Bandera dorada";
        TestRoomBuilder.Set(go.AddComponent<Goal>(), "completedMessage", completedMessage);
        return go;
    }

    // ---------- Interfaz ----------

    /// <param name="bossName">Nombre para la barra del jefe; null si el nivel no usa nuestra barra.</param>
    public static Hud CreateHud(PlayerController player, string levelTitle, string completedTitle, string completedDetail,
        string bossName = null, Health bossHealth = null)
    {
        Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Debug/white.png");
        Font font = Font;
        var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGo.transform;

        // Destello de daño: imagen roja a pantalla completa, transparente hasta que te golpean.
        var flash = Stretch(NewImage("Destello de daño", canvas, white, Vector2.zero, Vector2.zero, Vector2.zero));
        flash.color = new Color(0.8f, 0f, 0f, 0f);

        // Vida: corazones arriba a la izquierda.
        var health = player.GetComponent<Health>();
        var hearts = new Image[health.Max];
        for (int i = 0; i < hearts.Length; i++)
            hearts[i] = NewImage($"Corazón {i + 1}", canvas, Ui("heart_full"), new Vector2(0f, 1f), new Vector2(48f + i * 84f, -40f), new Vector2(76f, 72f));
        var healthHud = canvasGo.AddComponent<HealthHUD>();
        TestRoomBuilder.Set(healthHud, "health", health);
        TestRoomBuilder.SetArray(healthHud, "hearts", hearts);
        TestRoomBuilder.Set(healthHud, "fullSprite", Ui("heart_full"));
        TestRoomBuilder.Set(healthHud, "emptySprite", Ui("heart_empty"));

        // Dash: botón de madera con flecha bajo los corazones (apagado mientras se recarga).
        var dashIcon = NewImage("Dash", canvas, Ui("icon_play"), new Vector2(0f, 1f), new Vector2(52f, -128f), new Vector2(70f, 70f));
        var dash = dashIcon.gameObject.AddComponent<DashIndicator>();
        TestRoomBuilder.Set(dash, "player", player);
        TestRoomBuilder.Set(dash, "icon", dashIcon);
        Label(NewText("Tecla", dashIcon.transform, font, 30, TextAnchor.MiddleLeft, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(150f, 0f), new Vector2(200f, 44f), "DASH"));

        CreatePlasmaCounter(canvas, font, player.GetComponent<PlayerPlasma>());
        CreateRunCounters(canvas, font);

        // Nombre del nivel al empezar.
        var title = NewImage("Título del nivel", canvas, Ui("ui_banner"), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(640f, 300f));
        Label(NewText("Texto", title.transform, font, 60, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(560f, 200f), levelTitle));
        Toast(title.gameObject, onStart: true, hold: 2.5f);

        // Aviso al tocar una bandera.
        var notice = NewImage("Aviso de punto de control", canvas, Ui("ui_plank_gold"), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(520f, 150f));
        Label(NewText("Texto", notice.transform, font, 38, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 80f), "Punto de control"), outline: false);
        Toast(notice.gameObject, onStart: false, hold: 1.4f);

        GameObject message = Board(canvas, font, "Nivel completado", completedTitle, completedDetail);
        message.SetActive(false);

        GameObject bossBar = bossName != null ? CreateBossBar(canvas, white, font, bossName, bossHealth) : null;
        CreatePauseMenu(canvas, white, font, player);
        CreateGameOver(canvas, white, font, player.GetComponent<PlayerLife>());
        return new Hud { DamageFlash = flash, CompletedMessage = message, BossBar = bossBar, Canvas = canvas };
    }

    /// <summary>Cargas de plasma bajo el dash (ocultas hasta conseguir la pistola en el nivel 2).</summary>
    private static void CreatePlasmaCounter(Transform canvas, Font font, PlayerPlasma plasma)
    {
        Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Fx/circle.png");
        var root = NewUi("Plasma", canvas, new Vector2(0f, 1f), new Vector2(48f, -186f), new Vector2(330f, 60f));
        var pips = new Image[3];
        for (int i = 0; i < pips.Length; i++)
            pips[i] = NewImage($"Carga {i + 1}", root, circle, new Vector2(0f, 0.5f), new Vector2(8f + i * 46f, 0f), new Vector2(36f, 36f));
        var bar = NewImage("Recarga", root, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Debug/white.png"),
            new Vector2(0f, 0f), new Vector2(8f, -6f), new Vector2(128f, 6f));
        bar.color = new Color(0.4f, 1f, 1f);
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        Label(NewText("Tecla", root, font, 26, TextAnchor.MiddleLeft, Cream, new Vector2(0f, 0.5f), new Vector2(158f, 0f), new Vector2(170f, 40f), "FIN"));

        var hud = canvas.gameObject.AddComponent<PlasmaHUD>();
        TestRoomBuilder.Set(hud, "plasma", plasma);
        TestRoomBuilder.Set(hud, "root", root.gameObject);
        TestRoomBuilder.SetArray(hud, "pips", pips);
        TestRoomBuilder.Set(hud, "reload", bar);
    }

    /// <summary>Cartel que anuncia algo una vez (por ejemplo, al recoger la pistola de plasma).</summary>
    public static HudToast CreateNotice(Transform canvas, string text)
    {
        var notice = NewImage("Aviso", canvas, Ui("ui_plank_gold"), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 190f));
        Label(NewText("Texto", notice.transform, Font, 34, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 140f), text), outline: false);
        Toast(notice.gameObject, onStart: false, hold: 3f, onCheckpoint: false);
        return notice.GetComponent<HudToast>();
    }

    /// <summary>Vidas de reserva (cabeza del capibara) y monedas, arriba a la derecha.</summary>
    private static void CreateRunCounters(Transform canvas, Font font)
    {
        var livesIcon = NewImage("Vidas", canvas, SubSprite(PlayerSheet, "spriteCapi_0"), new Vector2(1f, 1f), new Vector2(-170f, -30f), new Vector2(84f, 84f));
        livesIcon.preserveAspect = true;
        Text lives = NewText("Cantidad", livesIcon.transform, font, 44, TextAnchor.MiddleLeft, Cream,
            new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(120f, 60f), "x3");
        Label(lives);

        var coinIcon = NewImage("Monedas", canvas, SubSprite(CoinSheet, "moneda-Photoroom_0"), new Vector2(1f, 1f), new Vector2(-176f, -124f), new Vector2(64f, 64f));
        coinIcon.preserveAspect = true;
        Text coins = NewText("Cantidad", coinIcon.transform, font, 44, TextAnchor.MiddleLeft, Cream,
            new Vector2(0f, 0.5f), new Vector2(102f, 0f), new Vector2(120f, 60f), "0");
        Label(coins);

        var run = canvas.gameObject.AddComponent<RunHUD>();
        TestRoomBuilder.Set(run, "lives", lives);
        TestRoomBuilder.Set(run, "coins", coins);
    }

    private static GameObject CreateBossBar(Transform canvas, Sprite white, Font font, string bossName, Health bossHealth)
    {
        var frame = NewImage("Barra del jefe", canvas, Ui("ui_plank"), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(980f, 70f));
        frame.type = Image.Type.Sliced;

        var back = NewImage("Fondo", frame.transform, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 26f));
        back.color = new Color(0.12f, 0.05f, 0.03f);
        var fill = NewImage("Relleno", frame.transform, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 26f));
        fill.color = new Color(0.85f, 0.18f, 0.12f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;

        var plate = NewImage("Placa", frame.transform, Ui("ui_banner"), new Vector2(0.5f, 1f), new Vector2(0f, 70f), new Vector2(400f, 120f));
        Label(NewText("Nombre", plate.transform, font, 30, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(360f, 60f), bossName));

        var bar = frame.gameObject.AddComponent<BossHealthBar>();
        TestRoomBuilder.Set(bar, "health", bossHealth);
        TestRoomBuilder.Set(bar, "fill", fill);
        return frame.gameObject;
    }

    private static void CreatePauseMenu(Transform canvas, Sprite white, Font font, PlayerController player)
    {
        var root = FullScreen("Pausa", canvas);
        var panel = Stretch(NewImage("Panel de pausa", root, white, Vector2.zero, Vector2.zero, Vector2.zero));
        panel.color = new Color(0f, 0f, 0f, 0.6f);
        panel.raycastTarget = true;   // tapa el juego mientras está en pausa

        var board = NewImage("Tablero", panel.transform, Ui("ui_panel"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 670f));
        Label(NewText("Título", board.transform, font, 50, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(480f, 90f), "PAUSA"), outline: false);
        Button resume = PauseButton(board.transform, font, "Continuar", "icon_play", 0);
        Button restart = PauseButton(board.transform, font, "Reiniciar nivel", "icon_restart", 1);
        Button menu = PauseButton(board.transform, font, "Menú principal", "icon_home", 2);

        var pause = root.gameObject.AddComponent<PauseMenu>();
        TestRoomBuilder.Set(pause, "player", player);
        TestRoomBuilder.Set(pause, "panel", panel.gameObject);
        TestRoomBuilder.Set(pause, "resumeButton", resume);
        TestRoomBuilder.Set(pause, "restartButton", restart);
        TestRoomBuilder.Set(pause, "menuButton", menu);

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }

    private static void CreateGameOver(Transform canvas, Sprite white, Font font, PlayerLife life)
    {
        var root = FullScreen("Game Over", canvas);
        var panel = Stretch(NewImage("Panel de Game Over", root, white, Vector2.zero, Vector2.zero, Vector2.zero));
        panel.color = new Color(0f, 0f, 0f, 0.75f);
        panel.raycastTarget = true;
        Board(panel.transform, font, "Tablero", "GAME OVER",
            "Te quedaste sin vidas.\nVuelves al nivel 1.\n\n<size=28>Pulsa Enter para continuar</size>");

        var screen = root.gameObject.AddComponent<GameOverScreen>();
        TestRoomBuilder.Set(screen, "player", life);
        TestRoomBuilder.Set(screen, "panel", panel.gameObject);
    }

    /// <summary>Tablero de madera con título en la placa dorada y un texto debajo.</summary>
    private static GameObject Board(Transform parent, Font font, string name, string title, string detail)
    {
        var board = NewImage(name, parent, Ui("ui_panel"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 670f));
        Label(NewText("Título", board.transform, font, 46, TextAnchor.MiddleCenter, DarkWood,
            new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(480f, 90f), title), outline: false);
        Label(NewText("Detalle", board.transform, font, 34, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(480f, 300f), detail));
        return board.gameObject;
    }

    private static Button PauseButton(Transform board, Font font, string text, string icon, int index)
    {
        var plank = NewImage(text, board, Ui("ui_button"), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f - index * 130f), new Vector2(440f, 116f));
        plank.raycastTarget = true;
        var button = plank.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 0.85f, 0.55f);
        colors.pressedColor = new Color(0.8f, 0.65f, 0.4f);
        button.colors = colors;
        NewImage("Ícono", plank.transform, Ui(icon), new Vector2(0f, 0.5f), new Vector2(-14f, 0f), new Vector2(96f, 96f));
        Label(NewText("Texto", plank.transform, font, 34, TextAnchor.MiddleCenter, Cream,
            new Vector2(0.5f, 0.5f), new Vector2(36f, 0f), new Vector2(330f, 60f), text));
        return button;
    }

    private static void Toast(GameObject go, bool onStart, float hold, bool onCheckpoint = true)
    {
        go.AddComponent<CanvasGroup>();
        var toast = go.AddComponent<HudToast>();
        var so = new SerializedObject(toast);
        if (onStart) so.FindProperty("showOnStart").boolValue = true;
        else if (onCheckpoint) so.FindProperty("showOnCheckpoint").boolValue = true;
        so.FindProperty("holdTime").floatValue = hold;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Utilidades ----------

    private static Transform FullScreen(string name, Transform canvas)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(canvas, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return rt;
    }

    public static Image NewImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var image = NewUi(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Image Stretch(Image image)
    {
        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return image;
    }

    /// <summary>Texto con borde oscuro para que se lea sobre la madera y sobre el fondo.</summary>
    private static void Label(Text text, bool outline = true)
    {
        text.raycastTarget = false;
        if (!outline) return;
        var o = text.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.15f, 0.07f, 0.02f, 0.9f);
        o.effectDistance = new Vector2(2f, -2f);
    }

    public static SpriteRenderer SpriteChild(Transform parent, Sprite sprite, Vector2 localPosition, float scale, int order)
    {
        var go = new GameObject("Pieza");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    public static RectTransform NewUi(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private static Text NewText(string name, Transform parent, Font font, int size, TextAnchor align, Color color,
        Vector2 anchor, Vector2 position, Vector2 boxSize, string content)
    {
        var rt = NewUi(name, parent, anchor, position, boxSize);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.supportRichText = true;
        text.text = content;
        return text;
    }

    // ---------- Escenas del juego ----------

    /// <summary>Escenas en el orden de GameFlow (más la sala de pruebas al final) para el build.</summary>
    [MenuItem("Game/Actualizar escenas del build")]
    public static void SetBuildScenes()
    {
        string[] paths =
        {
            "Assets/Scenes/Intro.unity", "Assets/Scenes/Menu.unity",
            "Assets/Niveles/1_Dante/Nivel1.unity",
            "Assets/Niveles/2_Edward/Nivel2_Tierra.unity", "Assets/Niveles/2_Edward/Nivel2_Lago.unity",
            Level3Builder.ScenePath, TestRoomBuilder.TestRoomPath,
        };
        EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
    }
}
