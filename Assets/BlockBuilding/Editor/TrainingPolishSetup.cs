using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TrainingPolishSetup
{
    [MenuItem("Tools/Block Building/Install Training Presentation")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var t = session.training;
        var hub = t.GetComponent<GymEquipmentHub>();
        const string path = "Assets/BlockBuilding/Models/PremiumGyms/Standard.fbx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var model = t.transform.Find("StandardModel");
        if (model == null)
        {
            model = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path))).transform;
            model.name = "StandardModel";
            model.SetParent(t.transform, false);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var source = mats[i];
                    string p = "Assets/BlockBuilding/Models/PremiumGyms/Materials/" + source.name + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (mat == null)
                    {
                        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.color = source.color;
                        mat.SetFloat("_Metallic", .35f);
                        mat.SetFloat("_Smoothness", .4f);
                        AssetDatabase.CreateAsset(mat, p);
                    }

                    mats[i] = mat;
                }

                renderer.sharedMaterials = mats;
            }
        }

        foreach (string name in new[]
        {
            "Seat",
            "LegA",
            "LegB",
            "Barbell"
        }

        )
        {
            var old = t.transform.Find(name);
            if (old != null)
                old.gameObject.SetActive(false);
        }

        var standard = hub.equipment[0];
        foreach (var tr in model.GetComponentsInChildren<Transform>())
            if (tr.name.StartsWith("Barbell", StringComparison.Ordinal))
                standard.bar = tr;
        t.barbell = standard.bar;
        t.poseAnchor = standard.pose;
        Vector3 gymOrigin = new Vector3(12, 0, -3);
        t.transform.position = gymOrigin;
        foreach (var entry in session.catalog.levels)
        {
            entry.data.level.trainingPoint = gymOrigin;
            EditorUtility.SetDirty(entry.data);
        }

        var podium = GameObject.Find("Podium");
        if (podium != null)
            podium.transform.localScale = new Vector3(60, .25f, 30);
        CreateFence(session.runtime.transform);
        var presentation = t.GetComponent<TrainingPresentation>();
        if (presentation == null)
            presentation = t.gameObject.AddComponent<TrainingPresentation>();
        presentation.training = t;
        presentation.view = GameObject.Find("Main Camera").GetComponent<Camera>();
        foreach (var b in presentation.view.GetComponents<Behaviour>())
            if (b.GetType().Name == "CinemachineBrain")
                presentation.cameraDriver = b;
        var hud = session.gameplayHUD.transform;
        var hint = Rect("TrainingTapHint", hud);
        hint.anchorMin = hint.anchorMax = new Vector2(.5f, 0);
        hint.pivot = new Vector2(.5f, 0);
        hint.anchoredPosition = new Vector2(0, 210);
        hint.sizeDelta = new Vector2(650, 56);
        var bg = hint.GetComponent<UnityEngine.UI.Image>();
        if (bg == null)
            bg = hint.gameObject.AddComponent<UnityEngine.UI.Image>();
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BlockBuilding/UI/Panel.asset");
        bg.type = UnityEngine.UI.Image.Type.Sliced;
        bg.raycastTarget = false;
        var cg = hint.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = hint.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        presentation.hint = cg;
        var label = Rect("Text", hint);
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = new Vector2(12, 4);
        label.offsetMax = new Vector2(-12, -4);
        var text = label.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = label.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        text.text = "Нажимай, чтобы качаться быстрее";
        text.fontSize = 26;
        text.enableAutoSizing = true;
        text.fontSizeMax = 26;
        text.fontSizeMin = 16;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        hint.gameObject.SetActive(false);
        var effects = Rect("TrainingSpeedLines", hud);
        effects.anchorMin = Vector2.zero;
        effects.anchorMax = Vector2.one;
        effects.offsetMin = effects.offsetMax = Vector2.zero;
        effects.SetAsFirstSibling();
        presentation.streaks = new RectTransform[16];
        for (int i = 0; i < 16; i++)
        {
            var line = Rect("Line" + i, effects);
            line.anchorMin = line.anchorMax = line.pivot = new Vector2(.5f, .5f);
            var image = line.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = line.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.75f, .95f, 1, .65f);
            image.raycastTarget = false;
            line.gameObject.SetActive(false);
            presentation.streaks[i] = line;
        }

        EditorUtility.SetDirty(presentation);
        EditorUtility.SetDirty(standard);
        EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        EditorSceneManager.SaveScene(t.gameObject.scene);
        return "Standard Blender gym, side yard, fence, training camera and tap acceleration installed.";
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var r = parent.Find(name);
        if (r != null)
            return (RectTransform)r;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void CreateFence(Transform parent)
    {
        var found = parent.Find("GymFence");
        if (found != null)
            return;
        var prototype = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cube = prototype.GetComponent<MeshFilter>().sharedMesh;
        var parts = new List<CombineInstance>();
        Action<Vector3, Vector3> add = (p, s) => parts.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(p, Quaternion.identity, s) });
        for (int x = 8; x <= 26; x += 3)
        {
            add(new Vector3(x, .7f, -11), new Vector3(.18f, 1.4f, .18f));
            add(new Vector3(x, .7f, 1), new Vector3(.18f, 1.4f, .18f));
        }

        for (int z = -8; z <= -2; z += 3)
            add(new Vector3(26, .7f, z), new Vector3(.18f, 1.4f, .18f));
        foreach (float y in new[]
        {
            .45f,
            1.05f
        }

        )
        {
            add(new Vector3(17, y, -11), new Vector3(18, .12f, .12f));
            add(new Vector3(17, y, 1), new Vector3(18, .12f, .12f));
            add(new Vector3(26, y, -5), new Vector3(.12f, .12f, 12));
        }

        var mesh = new Mesh
        {
            name = "Gym perimeter"
        };
        mesh.CombineMeshes(parts.ToArray());
        UnityEngine.Object.DestroyImmediate(prototype);
        Directory.CreateDirectory("Assets/BlockBuilding/Generated/Gym");
        AssetDatabase.CreateAsset(mesh, "Assets/BlockBuilding/Generated/Gym/Fence.asset");
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(.13f, .22f, .29f);
        mat.SetFloat("_Metallic", .4f);
        AssetDatabase.CreateAsset(mat, "Assets/BlockBuilding/Generated/Gym/Fence.mat");
        var go = new GameObject("GymFence");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
}
