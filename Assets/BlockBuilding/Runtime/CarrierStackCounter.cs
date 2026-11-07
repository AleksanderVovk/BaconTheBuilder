using TMPro;
using UnityEngine;

public class CarrierStackCounter : MonoBehaviour
{
    [Header("Inventory"), Tooltip("Existing carrier.")]
    public PlayerBlockCarrier carrier;

    [Header("Text"), Tooltip("World-space TMP count above the compact stack.")]
    public TMP_Text label;

    [Header("Camera"), Tooltip("Cached gameplay camera; no Camera.main lookup in Update.")]
    public Transform viewCamera;
    private void OnEnable()
    {
        if (carrier != null)
            carrier.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (carrier != null)
            carrier.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (label == null || carrier == null)
            return;
        label.gameObject.SetActive(carrier.Count > 0);
        label.text = carrier.InfiniteBlocksActive ? "<size=55%>БЕЗЛИМИТ</size>" : "x " + carrier.Count;
        if (carrier.carryAnchor != null)
            label.transform.position = carrier.carryAnchor.position + Vector3.up * (carrier.VisualStackHeight + 0.45f);
    }

    private void LateUpdate()
    {
        if (carrier == null || label == null || carrier.Count == 0 || carrier.carryAnchor == null)
            return;
        label.transform.position = carrier.carryAnchor.position + Vector3.up * (carrier.VisualStackHeight + 0.45f);
        if (viewCamera != null)
            label.transform.rotation = viewCamera.rotation;
    }
}
