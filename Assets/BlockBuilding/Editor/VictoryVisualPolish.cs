using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class VictoryVisualPolish
{
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit play mode");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/Sprites/Currency/Golden Trophy 1st Outline 256px.png");
        if (sprite == null)
            throw new System.Exception("Trophy sprite missing");
        foreach (var old in UnityEngine.Object.FindObjectsByType<VictoryCupGraphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = old.gameObject;
            UnityEngine.Object.DestroyImmediate(old);
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        foreach (var price in UnityEngine.Object.FindObjectsByType<GymVictoryPrice>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var cup = price.cup.GetComponent<UnityEngine.UI.Image>();
            if (cup != null)
            {
                cup.sprite = sprite;
                cup.preserveAspect = true;
            }

            price.caption.transform.localPosition = new Vector3(-.3f, -1.65f, -.02f);
            price.caption.rectTransform.sizeDelta = new Vector2(4.5f, .65f);
            price.caption.fontSize = 3.5f;
            price.caption.fontStyle = TMPro.FontStyles.Bold;
            price.caption.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            price.cup.transform.localPosition = new Vector3(2.05f, -1.65f, -.02f);
            ((RectTransform)price.cup.transform).sizeDelta = new Vector2(.65f, .65f);
            var tr = price.transform.Find("PricePlate");
            if (tr == null)
            {
                var go = new GameObject("PricePlate", typeof(RectTransform), typeof(Canvas));
                tr = go.transform;
                tr.SetParent(price.transform, false);
                go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            }

            tr.localPosition = new Vector3(0, -1.65f, .05f);
            ((RectTransform)tr).sizeDelta = new Vector2(5.4f, .9f);
            var panel = tr.GetComponent<UnityEngine.UI.Image>();
            if (panel == null)
                panel = tr.gameObject.AddComponent<UnityEngine.UI.Image>();
            panel.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BlockBuilding/UI/Panel.asset");
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            panel.color = new Color(.22f, .25f, .3f, .96f);
            panel.raycastTarget = false;
            price.pricePlate = tr.gameObject;
            EditorUtility.SetDirty(price);
            EditorUtility.SetDirty(price.caption);
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
