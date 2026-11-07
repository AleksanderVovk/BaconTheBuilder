using UnityEngine;
using UnityEngine.Rendering;

// Applied once at startup; no scene object or per-frame checks are required.
public static class MobileFrameRate
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
#if UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = 60;
        OnDemandRendering.renderFrameInterval = 1;
#endif
    }
}
