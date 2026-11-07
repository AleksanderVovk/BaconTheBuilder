using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NaturalFeedbackSetup
{
    [MenuItem("Tools/Block Building/Install Natural Feedback")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        StockAudioSetup.Install();
        var p = UnityEngine.Object.FindFirstObjectByType<TrainingPresentation>();
        foreach (var line in p.streaks)
            line.gameObject.SetActive(false);
        var effect = p.GetComponent<TrainingEffortVfx>();
        if (effect == null)
            effect = p.gameObject.AddComponent<TrainingEffortVfx>();
        effect.presentation = p;
        Directory.CreateDirectory("Assets/BlockBuilding/Generated/TrainingVfx");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/Generated/TrainingVfx/Effort.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", 5);
            material.SetFloat("_DstBlend", 10);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, "Assets/BlockBuilding/Generated/TrainingVfx/Effort.mat");
        }

        effect.trails = new TrailRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            var tr = p.transform.Find("WeightRibbon" + i);
            if (tr == null)
            {
                tr = new GameObject("WeightRibbon" + i).transform;
                tr.SetParent(p.transform, false);
            }

            var trail = tr.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = tr.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = .16f;
            trail.emitting = false;
            trail.minVertexDistance = .015f;
            trail.numCapVertices = 3;
            trail.widthMultiplier = .04f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1, .95f, .65f), 0), new GradientColorKey(new Color(1, .55f, .12f), 1) },
                new[] { new GradientAlphaKey(.85f, 0), new GradientAlphaKey(0, 1) }
            );
            trail.colorGradient = gradient;
            effect.trails[i] = trail;
        }

        var node = p.transform.Find("EffortBurst");
        if (node == null)
        {
            node = new GameObject("EffortBurst").transform;
            node.SetParent(p.transform, false);
        }

        var ps = node.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = node.gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;
        main.startLifetime = .25f;
        main.startSize = .055f;
        main.startSpeed = .5f;
        main.gravityModifier = .1f;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1, .6f, .15f), 1) },
            new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) }
        );
        color.color = fade;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        effect.particles = ps;
        var hud = p.GetComponent<GymEquipmentHub>().session.gameplayHUD.transform;
        var label = hud.Find("EffortTempo");
        if (label == null)
        {
            label = new GameObject("EffortTempo", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            label.SetParent(hud, false);
        }

        var text = label.GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 280);
        rect.sizeDelta = new Vector2(280, 44);
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        text.fontSize = 30;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            new Color(1, 1, .5f),
            new Color(1, 1, .5f),
            new Color(1, .5f, .05f),
            new Color(1, .5f, .05f)
        );
        text.text = "ТЕМП x1.0";
        label.gameObject.SetActive(false);
        effect.tempo = text;
        EditorUtility.SetDirty(effect);
        EditorUtility.SetDirty(p);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        EditorSceneManager.SaveScene(p.gameObject.scene);
    }
}
