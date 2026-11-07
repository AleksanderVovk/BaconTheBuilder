using UnityEngine;

public static class Environment
{
    public static bool IsMobile { get; private set; }
    public static bool IsDesktop { get; private set; }
    public static bool IsWebGL { get; private set; }
    public static bool IsEditor { get; private set; }
    public static bool IsAndroid { get; private set; }
    public static bool IsIOS { get; private set; }
    public static bool IsWindows { get; private set; }
    public static bool IsMac { get; private set; }
    public static bool IsLinux { get; private set; }

    public static string DeviceModel { get; private set; }
    public static string OperatingSystem { get; private set; }
    public static int ProcessorCount { get; private set; }
    public static int MemorySize { get; private set; }

    static Environment()
    {
        Initialize();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        IsEditor = Application.isEditor;

        IsWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
        IsMobile = Application.isMobilePlatform;
        IsDesktop = !IsMobile && !IsWebGL;

        IsAndroid = Application.platform == RuntimePlatform.Android;
        IsIOS = Application.platform == RuntimePlatform.IPhonePlayer;

        IsWindows = Application.platform == RuntimePlatform.WindowsPlayer ||
                    Application.platform == RuntimePlatform.WindowsEditor;
        IsMac = Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.OSXEditor;
        IsLinux = Application.platform == RuntimePlatform.LinuxPlayer ||
                  Application.platform == RuntimePlatform.LinuxEditor;

        DeviceModel = SystemInfo.deviceModel;
        OperatingSystem = SystemInfo.operatingSystem;
        ProcessorCount = SystemInfo.processorCount;
        MemorySize = SystemInfo.systemMemorySize;
    }
}