using TMPro;
using UnityEngine;

public class GymFloatingTitle : MonoBehaviour
{
    [Header("Camera"), Tooltip("Cached player camera.")]
    public Transform view;

    [Header("Label"), Tooltip("Gradient station caption.")]
    public TMP_Text label;

    [Header("Scale"), Tooltip("Distance at which label reaches full world size."), Min(1)]
    public float fullSizeDistance = 15;
    private void LateUpdate()
    {
        if (view == null)
            return;
        transform.rotation = view.rotation;
        float distance = Vector3.Distance(view.position, transform.position);
        transform.localScale = Vector3.one * Mathf.Clamp(distance / fullSizeDistance, 0.24f, 1);
    }
}
