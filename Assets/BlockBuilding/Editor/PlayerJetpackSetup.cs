using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlayerJetpackSetup
{
    private const string Folder = "Assets/BlockBuilding/Models/Jetpack";
    private static ParticleSystem Exhaust(Transform parent, Vector3 position, Material material)
    {
        var go = new GameObject("JetExhaust");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var p = go.AddComponent<ParticleSystem>();
        p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = p.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1;
        main.startLifetime = .17f;
        main.startSpeed = 4;
        main.startSize = new ParticleSystem.MinMaxCurve(.075f, .14f);
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(.12f, .95f, 1, .8f);
        main.gravityModifier = 0;
        var emission = p.emission;
        emission.rateOverTime = 65;
        var shape = p.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7;
        shape.radius = .055f;
        var color = p.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0, .8f, 1), .25f), new GradientColorKey(new Color(.1f, .2f, 1), 1) },
            new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(.6f, .5f), new GradientAlphaKey(0, 1) }
        );
        color.color = new ParticleSystem.MinMaxGradient(gradient);
        var size = p.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .05f));
        var renderer = p.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return p;
    }

    public static string CapturePreview()
    {
        var jet = Object.FindFirstObjectByType<PlayerJetpack>();
        var go = new GameObject("Jetpack preview");
        go.hideFlags = HideFlags.HideAndDontSave;
        var cam = go.AddComponent<Camera>();
        cam.CopyFrom(Camera.main);
        cam.enabled = false;
        Vector3 center = jet.visual.transform.position;
        cam.transform.position = center + new Vector3(1.45f, .65f, -2.65f);
        cam.transform.LookAt(center + Vector3.up * .03f);
        cam.fieldOfView = 38;
        var rt = new RenderTexture(720, 720, 24);
        var old = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(720, 720, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, 720, 720), 0, 0);
            t.Apply();
            System.IO.File.WriteAllBytes("F:/ClearVovkCore/BlockBuildingBackup/JetpackPreview.png", t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }
        finally
        {
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }

        EditorSceneManager.MarkSceneDirty(jet.gameObject.scene);
        EditorSceneManager.SaveScene(jet.gameObject.scene);
        return "Visual preview saved; scene saved.";
    }

    [MenuItem("Tools/Block Building/Install Player Jetpack")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var session = Object.FindFirstObjectByType<BuildLevelSession>();
        var player = session.runtime.carrier.player;
        var jet = player.GetComponent<PlayerJetpack>();
        if (jet != null)
            return "Jetpack already installed.";
        string modelPath = Folder + "/ObbyJetpack.fbx";
        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.addCollider = false;
        importer.SaveAndReimport();
        var material = new Material(Shader.Find("BlockBuilding/Jetpack Vertex"));
        AssetDatabase.CreateAsset(material, Folder + "/Jetpack.mat");
        var animator = player.GetComponent<Player>().animator;
        var bone = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
        if (bone == null)
            bone = animator.transform;
        var mount = new GameObject("JetpackMount").transform;
        mount.SetParent(bone, false);
        mount.position = bone.position - animator.transform.forward * .24f + Vector3.up * .02f;
        mount.rotation = Quaternion.LookRotation(animator.transform.forward, Vector3.up);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mount);
        model.name = "ObbyJetpack";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = prefab.transform.localRotation;
        model.transform.localScale = prefab.transform.localScale;
        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        jet = player.gameObject.AddComponent<PlayerJetpack>();
        jet.controller = player.GetComponent<PlayerController>();
        jet.character = player.GetComponent<CharacterController>();
        jet.state = player.GetComponent<PlayerState>();
        jet.carrier = session.runtime.carrier;
        jet.session = session;
        jet.visual = mount.gameObject;
        jet.settings = Object.FindFirstObjectByType<GameSettingsStore>();
        var exhaustMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        exhaustMaterial.SetFloat("_Surface", 1);
        exhaustMaterial.SetFloat("_Blend", 1);
        exhaustMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        exhaustMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        exhaustMaterial.SetFloat("_ZWrite", 0);
        exhaustMaterial.SetFloat("_Cull", 0);
        exhaustMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        exhaustMaterial.renderQueue = 3000;
        AssetDatabase.CreateAsset(exhaustMaterial, Folder + "/Exhaust.mat");
        jet.exhaust = new[]
        {
            Exhaust(mount, new Vector3(-.255f, -.52f, -.08f), exhaustMaterial),
            Exhaust(mount, new Vector3(.255f, -.52f, -.08f), exhaustMaterial)
        };
        string soundPath = "Assets/BlockBuilding/Audio/Jetpack/RocketEngine.wav";
        AssetDatabase.ImportAsset(soundPath, ImportAssetOptions.ForceSynchronousImport);
        var audioImporter = (AudioImporter)AssetImporter.GetAtPath(soundPath);
        audioImporter.forceToMono = true;
        var sample = audioImporter.defaultSampleSettings;
        sample.loadType = AudioClipLoadType.DecompressOnLoad;
        sample.compressionFormat = AudioCompressionFormat.Vorbis;
        sample.quality = .65f;
        sample.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        sample.sampleRateOverride = 22050;
        audioImporter.defaultSampleSettings = sample;
        audioImporter.SaveAndReimport();
        jet.engine = player.gameObject.AddComponent<AudioSource>();
        jet.engine.playOnAwake = false;
        jet.engine.loop = true;
        jet.engine.spatialBlend = 0;
        jet.engine.volume = jet.engineVolume;
        jet.engine.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(soundPath);
        var settings = jet.settings;
        settings.soundSources = settings.soundSources.Concat(new[] { jet.engine }).ToArray();
        settings.baseVolumes = settings.baseVolumes.Concat(new[] { jet.engineVolume }).ToArray();
        EditorUtility.SetDirty(settings);
        var jump = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().First(b => b.gameObject.scene == player.gameObject.scene
            && b.name == "Jump");
        var hold = jump.gameObject.AddComponent<JetpackJumpHold>();
        hold.jetpack = jet;
        hold.button = jump;
        session.gameplayBehaviours = session.gameplayBehaviours.Concat(new Behaviour[] { jet }).ToArray();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(session.onPlayerReset, jet.ResetFlight);
        foreach (var obj in new Object[]
        {
            jet,
            jet.engine,
            session,
            hold,
            settings
        }

        )
            EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        return "Blender jetpack attached to "
            + bone.name
            + "; mesh "
            + mesh.vertexCount
            + " vertices, one material. Hold existing Jump/Space for flight. Downloaded engine and paired exhaust wired. Scene saved.";
    }
}
