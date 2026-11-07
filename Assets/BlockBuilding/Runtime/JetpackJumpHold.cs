using UnityEngine;
using UnityEngine.EventSystems;

// Added to the existing jump button; leaves its existing listeners intact.
public class JetpackJumpHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Jetpack"), Tooltip("Existing player's flight component.")]
    public PlayerJetpack jetpack;

    [Header("Jump button"), Tooltip("Existing mobile jump button.")]
    public UnityEngine.UI.Button button;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (jetpack != null && (button == null || button.IsInteractable()))
            jetpack.SetJumpHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (jetpack != null)
            jetpack.SetJumpHeld(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (jetpack != null)
            jetpack.SetJumpHeld(false);
    }

    private void OnDisable()
    {
        if (jetpack != null)
            jetpack.SetJumpHeld(false);
    }
}
