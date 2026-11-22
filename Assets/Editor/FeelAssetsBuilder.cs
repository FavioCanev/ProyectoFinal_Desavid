using System;
using System.Linq;
using System.Text.RegularExpressions;
using Game;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera los efectos del gamefeel: material y prefabs de partículas, esporas del ambiente y el objeto
/// GameFeel de la escena (sonidos, sacudidas, partículas y destello de daño).
/// </summary>
public static class FeelAssetsBuilder
{
    private const string Circle = "Assets/Art/Fx/circle.png";
    private const string Spore = "Assets/Art/Fx/spore.png";
    private const string SfxFolder = "Assets/Audio/Sfx/";
    private const string MaterialPath = "Assets/Data/FxMaterial.mat";
    private const string SporeMaterialPath = "Assets/Data/SporeMaterial.mat";

    /// <summary>Crea el objeto GameFeel en la escena abierta. El destello de daño lo pone el HUD.</summary>
    public static GameFeel CreateGameFeel(Image damageFlash)
    {
        Material fx = EnsureMaterial(MaterialPath, Circle);
        ParticleSystem[] effects =
        {
            Particles("HitSpark", fx, count: 14, life: (0.15f, 0.3f), speed: (4f, 9f), size: (0.06f, 0.16f),
                colorA: Color.white, colorB: new Color(1f, 0.95f, 0.6f), gravity: 0f, coneAngle: 180f, order: 20),
            Particles("Dust", fx, count: 8, life: (0.25f, 0.45f), speed: (0.6f, 1.8f), size: (0.12f, 0.28f),
                colorA: new Color(0.75f, 0.7f, 0.55f, 0.7f), colorB: new Color(0.6f, 0.55f, 0.45f, 0.5f), gravity: -0.1f, coneAngle: 70f, order: 11),
            Particles("DeathBurst", fx, count: 26, life: (0.4f, 0.75f), speed: (2f, 6f), size: (0.1f, 0.28f),
                colorA: new Color(0.6f, 1f, 0.4f), colorB: new Color(0.3f, 0.75f, 0.2f), gravity: 0.6f, coneAngle: 180f, order: 20),
            Particles("AcidSplash", fx, count: 18, life: (0.35f, 0.6f), speed: (3f, 6f), size: (0.08f, 0.2f),
                colorA: new Color(0.55f, 1f, 0.3f), colorB: new Color(0.3f, 0.85f, 0.2f), gravity: 1.5f, coneAngle: 35f, order: 20),
        };

        var go = new GameObject("GameFeel");
        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        var impulse = go.AddComponent<CinemachineImpulseSource>();
        impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
        impulse.ImpulseDefinition.ImpulseDuration = 0.25f;

        var feel = go.AddComponent<GameFeel>();
        TestRoomBuilder.Set(feel, "impulse", impulse);
        TestRoomBuilder.Set(feel, "audioSource", audio);
        TestRoomBuilder.Set(feel, "damageFlash", damageFlash);
        TestRoomBuilder.SetArray(feel, "clips", Enum.GetNames(typeof(Sfx)).Select(LoadClip).Cast<UnityEngine.Object>().ToArray());
        TestRoomBuilder.SetArray(feel, "effects", effects.Cast<UnityEngine.Object>().ToArray());
        return feel;
    }

    /// <summary>Esporas que flotan alrededor de la cámara: unas detrás del nivel y pocas delante (profundidad).</summary>
    public static void CreateSpores(Transform camera)
    {
        Material spore = EnsureMaterial(SporeMaterialPath, Spore);
        Ambient(camera, spore, "Esporas lejanas", rate: 7f, size: (0.04f, 0.12f), alpha: 0.55f, order: -40);
        Ambient(camera, spore, "Esporas cercanas", rate: 1.5f, size: (0.15f, 0.32f), alpha: 0.3f, order: 40);
    }

    // Snake_case del nombre del enum: BossRoar -> boss_roar.wav
    private static AudioClip LoadClip(string sfxName)
    {
        string file = Regex.Replace(sfxName, "(?<=[a-z])([A-Z])", "_$1").ToLowerInvariant();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}{file}.wav");
        if (clip == null) Debug.LogWarning("FeelAssetsBuilder: falta el sonido " + file);
        return clip;
    }

    private static Material EnsureMaterial(string path, string texturePath)
    {
        TestRoomBuilder.ConfigureSprite(texturePath, new Vector2(0.5f, 0.5f), FilterMode.Bilinear, pixelsPerUnit: 64);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static ParticleSystem Particles(string name, Material material, int count, (float, float) life, (float, float) speed,
        (float, float) size, Color colorA, Color colorB, float gravity, float coneAngle, int order)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.Item1, life.Item2);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.Item1, speed.Item2);
        main.startSize = new ParticleSystem.MinMaxCurve(size.Item1, size.Item2);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;   // se ven aunque el juego esté en pausa de impacto

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = Mathf.Min(coneAngle, 89f);
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // apunta hacia arriba
        if (coneAngle >= 180f) shape.shapeType = ParticleSystemShapeType.Circle;

        FadeAndShrink(ps);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;

        TestRoomBuilder.EnsureFolder("Assets/Prefabs/Fx");
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/Fx/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab.GetComponent<ParticleSystem>();
    }

    private static void Ambient(Transform camera, Material material, string name, float rate, (float, float) size, float alpha, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(camera, false);
        go.transform.localPosition = new Vector3(0f, 0f, 10f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(size.Item1, size.Item2);
        main.startColor = new Color(0.75f, 1f, 0.6f, alpha);
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // se quedan en el mundo: dan sensación de profundidad
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 15f, 1f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.3f;

        FadeAndShrink(ps, fadeIn: true);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
    }

    private static void FadeAndShrink(ParticleSystem ps, bool fadeIn = false)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            fadeIn
                ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        if (fadeIn) return;
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
    }
}
