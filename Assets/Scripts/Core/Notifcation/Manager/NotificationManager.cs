using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum NotificationType
{
    Error = 1,   // красный
    Success = 2, // зелёный
    Info = 3     // белый
}

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private RectTransform container;
    [SerializeField] private NotificationView prefab;

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 5;
    [SerializeField] private int maxPoolSize = 20;
    [SerializeField] private bool autoExpand = true;

    [Header("Layout")]
    [SerializeField] private bool newestOnTop = true;
    [SerializeField] private int maxVisibleNotifications = 5;

    private readonly List<NotificationView> pool = new List<NotificationView>();
    private readonly List<NotificationView> activeNotifications = new List<NotificationView>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitPool();
    }

    private void InitPool()
    {
        if (prefab == null || container == null)
        {
            Debug.LogError("[NotificationManager] Prefab or container is not set.");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
            CreatePooled();
    }

    private NotificationView CreatePooled()
    {
        if (pool.Count >= maxPoolSize && !autoExpand)
            return null;

        var view = Instantiate(prefab, container);
        view.gameObject.SetActive(false);
        pool.Add(view);
        return view;
    }

    private NotificationView GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }

        return autoExpand ? CreatePooled() : null;
    }

    public static void ShowNotification(string text, NotificationType type = NotificationType.Info) => Instance._ShowNotification(text, type);
    public void _ShowNotification(string text, NotificationType type = NotificationType.Info)
    {
        ShowNotification(null, text, type);
    }

    public static void ShowNotification(Sprite icon, string text, NotificationType type = NotificationType.Info) => Instance._ShowNotification(icon, text, type);
    private void _ShowNotification(Sprite icon, string text, NotificationType type = NotificationType.Info)
    {
        var view = GetFromPool();
        if (view == null)
        {
            Debug.LogWarning("[NotificationManager] Pool exhausted.");
            return;
        }

        view.gameObject.SetActive(true);

        if (newestOnTop)
        {
            view.transform.SetAsFirstSibling();
            activeNotifications.Insert(0, view);
        }
        else
        {
            view.transform.SetAsLastSibling();
            activeNotifications.Add(view);
        }

        // форсим лейаут, чтобы не залипали в одной точке
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        TrimActiveList();

        view.Setup(icon, text, type);
    }

    private void TrimActiveList()
    {
        if (maxVisibleNotifications <= 0)
            return;

        while (activeNotifications.Count > maxVisibleNotifications)
        {
            int index = newestOnTop ? activeNotifications.Count - 1 : 0;
            var toHide = activeNotifications[index];
            activeNotifications.RemoveAt(index);
            toHide.StartHide();
        }
    }

    public void RegisterHidden(NotificationView view)
    {
        activeNotifications.Remove(view);
    }
}
