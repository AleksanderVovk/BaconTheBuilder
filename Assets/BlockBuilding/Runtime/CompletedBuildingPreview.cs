using UnityEngine;
using UnityEngine.Rendering;

// Copies only chunk renderers and shares their existing meshes; no colliders or per-block objects.
public class CompletedBuildingPreview : MonoBehaviour
{
    [Header("Level"), Tooltip("Existing runtime containing the completed chunk meshes.")]
    public BlockLevelRuntime runtime;

    [Header("Image"), Tooltip("UI image receiving a live RenderTexture.")]
    public UnityEngine.UI.RawImage image;

    [Header("Render layer"), Tooltip("Dedicated preview layer; camera sees only this layer."), Range(0, 31)]
    public int previewLayer = 31;

    [Header("Resolution"), Tooltip("Longest texture edge, allocated only while the result is visible."), Range(256, 1024)]
    public int resolution = 640;

    [Header("Rotation"), Tooltip("Degrees per second; continues while gameplay is suspended.")]
    public float rotationSpeed = 20;

    private GameObject root, pivot;
    private Camera previewCamera;
    private RenderTexture texture;
    public float Rotation
    {
        get
        {
            return pivot != null ? pivot.transform.localEulerAngles.y : 0;
        }
    }

    public int MeshCount { get; private set; }

    private void OnEnable()
    {
        if (Application.isPlaying)
            CreatePreview();
    }

    private void CreatePreview()
    {
        Release();
        if (runtime == null || image == null || !runtime.IsComplete)
            return;
        root = new GameObject("Completed building render");
        root.transform.position = new Vector3(0, -10000, 0);
        pivot = new GameObject("Rotating model");
        pivot.transform.SetParent(root.transform, false);
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (var site in runtime.GetComponentsInChildren<BuildSite>())
        {
            foreach (var source in site.builder.GetComponentsInChildren<MeshFilter>())
            {
                if (source.sharedMesh == null || source.sharedMesh.vertexCount == 0)
                    continue;
                var renderer = source.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }
        }

        if (first)
        {
            Release();
            return;
        }

        foreach (var site in runtime.GetComponentsInChildren<BuildSite>())
            foreach (var source in site.builder.GetComponentsInChildren<MeshFilter>())
            {
                if (source.sharedMesh == null || source.sharedMesh.vertexCount == 0)
                    continue;
                var renderer = source.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;
                var chunk = new GameObject("Preview chunk");
                chunk.layer = previewLayer;
                chunk.transform.SetParent(pivot.transform, false);
                chunk.transform.localPosition = source.transform.position - bounds.center;
                chunk.transform.localRotation = source.transform.rotation;
                chunk.transform.localScale = source.transform.lossyScale;
                chunk.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var view = chunk.AddComponent<MeshRenderer>();
                view.sharedMaterial = renderer.sharedMaterial;
                view.shadowCastingMode = ShadowCastingMode.Off;
                view.receiveShadows = false;
                MeshCount++;
            }

        float aspect = Mathf.Max(0.4f, image.rectTransform.rect.width / Mathf.Max(1, image.rectTransform.rect.height));
        int width = aspect >= 1 ? resolution : Mathf.RoundToInt(resolution * aspect);
        int height = aspect >= 1 ? Mathf.RoundToInt(resolution / aspect) : resolution;
        texture = new RenderTexture(Mathf.Max(128, width), Mathf.Max(128, height), 16, RenderTextureFormat.ARGB32)
        {
            name = "Completed building preview",
            antiAliasing = 1
        };
        texture.Create();
        image.texture = texture;
        var cameraObject = new GameObject("Preview camera");
        cameraObject.transform.SetParent(root.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.075f, 0.12f, 0.17f, 1);
        previewCamera.orthographic = true;
        previewCamera.targetTexture = texture;
        previewCamera.aspect = (float)texture.width / texture.height;
        previewCamera.nearClipPlane = 0.1f;
        float radius = Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.z * bounds.extents.z);
        // A spherical frame stays valid through every turn, including a wide house with trees.
        float sphere = Mathf.Sqrt(radius * radius + bounds.extents.y * bounds.extents.y);
        float distance = Mathf.Max(20, sphere * 3);
        previewCamera.farClipPlane = distance + sphere * 3 + 10;
        previewCamera.transform.localPosition = new Vector3(-1, 0.65f, -1).normalized * distance;
        previewCamera.transform.LookAt(root.transform.position);
        float tilt = previewCamera.transform.localPosition.normalized.y;
        float vertical = bounds.extents.y * Mathf.Sqrt(1 - tilt * tilt) + radius * tilt;
        previewCamera.orthographicSize = Mathf.Max(vertical, radius / aspect) * 1.08f;
        previewCamera.Render();
    }

    private void LateUpdate()
    {
        if (pivot == null || previewCamera == null)
            return;
        pivot.transform.Rotate(0, rotationSpeed * Time.unscaledDeltaTime, 0, Space.Self);
        previewCamera.Render();
    }

    private void OnDisable()
    {
        Release();
    }

    private void OnDestroy()
    {
        Release();
    }

    private void Release()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = false;
            previewCamera.targetTexture = null;
        }

        if (image != null)
            image.texture = null;
        if (root != null)
        {
            root.SetActive(false);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
        }

        if (texture != null)
        {
            texture.Release();
            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
        }

        root = null;
        pivot = null;
        previewCamera = null;
        texture = null;
        MeshCount = 0;
    }
}
