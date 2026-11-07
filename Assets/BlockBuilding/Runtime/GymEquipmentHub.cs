using UnityEngine;
using YG;

public class GymEquipmentHub : MonoBehaviour
{
    [Header("Stations"), Tooltip("Standard bench followed by five premium models.")]
    public GymEquipment[] equipment = new GymEquipment[0];

    [Header("Training"), Tooltip("Existing single training controller reused for all models.")]
    public StrengthTrainingZone training;

    [Header("Session"), Tooltip("Existing gameplay/menu state.")]
    public BuildLevelSession session;

    [Header("Donate activation"), Tooltip("Enable only after creating matching nonrepeatable unlock products in the platform catalog.")]
    public bool paymentsConfigured;

    [Header("Free access"), Tooltip("Allow every station without buying; does not change permanent purchase saves.")]
    public bool freeAccess = true;
    public event System.Action PurchaseSucceeded;
    public event System.Action InteractionRejected;
    public GymEquipment Selected { get; private set; }
    public bool PurchasePending { get; private set; }

    private string pendingId;
    private bool strengthDirty;
    private float nextSave;
    private void Start()
    {
        YG2.onPurchaseSuccess += Success;
        YG2.onPurchaseFailed += Failure;
        YG2.onGetSDKData += Load;
        training.strength.Changed += StrengthChanged;
        if (YG2.isSDKEnabled)
            Load();
        if (equipment.Length > 0)
            Select(equipment[0]);
    }

    public bool Owns(GymEquipment station)
    {
        return station != null
            && (station.multiplier == 1
                || VictoryEconomy.Owns(station.equipmentId)
                || YG2.saves != null
                && YG2.saves.ownedGyms != null
                && YG2.saves.ownedGyms.Contains(station.equipmentId));
    }

    public bool CanBuy(GymEquipment station)
    {
        return station != null && !Owns(station) && VictoryEconomy.Balance >= station.victoryPrice;
    }

    public string Price(GymEquipment station)
    {
        return station.victoryPrice.ToString();
    }

    private void Load()
    {
        VictoryEconomy.SyncCloud();
        if (YG2.saves.ownedGyms == null)
            YG2.saves.ownedGyms = new System.Collections.Generic.List<string>();
        System.Numerics.BigInteger remote;
        if (StrengthNumber.TryParse(YG2.saves.gymStrengthBig, out remote))
            training.strength.ApplySavedStrength(remote);
        else if (YG2.saves.gymStrength >= 0)
            training.strength.ApplySavedStrength(YG2.saves.gymStrength);
        training.strength.WritePlatformSave(YG2.saves);
        strengthDirty = true;
    }

    private void StrengthChanged()
    {
        if (!training.strength.IsTesting && YG2.isSDKEnabled)
        {
            training.strength.WritePlatformSave(YG2.saves);
            strengthDirty = true;
        }
    }

    private void Update()
    {
        if (strengthDirty && YG2.isSDKEnabled && Time.unscaledTime >= nextSave)
            Save();
        if (training.IsTraining || session.MenuOpen || session.Completed || PurchasePending)
            return;
        Vector3 player = training.carrier.player.position;
        GymEquipment nearest = null;
        float best = training.radius * training.radius;
        foreach (var station in equipment)
        {
            if (station == null)
                continue;
            Vector3 d = station.transform.position - player;
            if (Mathf.Abs(d.y) > training.heightTolerance)
                continue;
            float distance = d.x * d.x + d.z * d.z;
            if (distance <= best)
            {
                best = distance;
                nearest = station;
            }
        }

        if (nearest != null && nearest != Selected)
            Select(nearest);
    }

    private void Select(GymEquipment station)
    {
        Selected = station;
        training.SelectEquipment(station);
    }

    public void Interact()
    {
        if (training.IsTraining)
        {
            training.StopTraining();
            return;
        }

        if (Selected == null || !training.IsInRange || training.carrier.Count > 0 || PurchasePending)
        {
            InteractionRejected?.Invoke();
            return;
        }

        if (Owns(Selected))
        {
            if (!training.TryStartTraining())
                InteractionRejected?.Invoke();
            return;
        }

        if (!CanBuy(Selected))
        {
            InteractionRejected?.Invoke();
            return;
        }

        if (VictoryEconomy.Unlock(Selected.equipmentId, Selected.victoryPrice))
        {
            PurchaseSucceeded?.Invoke();
            training.TryStartTraining();
        }
        else
            InteractionRejected?.Invoke();
    }

    private void Success(string id)
    {
        foreach (var station in equipment)
        {
            if (station.multiplier == 1 || string.IsNullOrEmpty(station.productId) || station.productId != id)
                continue;
            if (YG2.saves.ownedGyms == null)
                YG2.saves.ownedGyms = new System.Collections.Generic.List<string>();
            if (!YG2.saves.ownedGyms.Contains(station.equipmentId))
                YG2.saves.ownedGyms.Add(station.equipmentId);
            Save();
            PurchaseSucceeded?.Invoke();
            break;
        }

        if (id == pendingId)
        {
            PurchasePending = false;
            pendingId = null;
        }
    }

    private void Failure(string id)
    {
        if (id == pendingId)
        {
            PurchasePending = false;
            pendingId = null;
            InteractionRejected?.Invoke();
        }
    }

    private void Save()
    {
        if (GameSaveReset.IsResetting || !YG2.isSDKEnabled)
            return;
        YG2.SaveProgress();
        strengthDirty = false;
        nextSave = Time.unscaledTime + 3;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && strengthDirty)
            Save();
    }

    private void OnDisable()
    {
        if (strengthDirty)
            Save();
    }

    private void OnDestroy()
    {
        YG2.onPurchaseSuccess -= Success;
        YG2.onPurchaseFailed -= Failure;
        YG2.onGetSDKData -= Load;
        if (training != null)
            training.strength.Changed -= StrengthChanged;
    }
}
