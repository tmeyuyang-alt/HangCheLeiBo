using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Michsky.UI.ModernUIPack;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 实时报警监控面板。
/// 从 DeviceSignalConfigs.json 中读取 alarmLevel != None 的 BOOL 点位。
/// </summary>
public class WarningNotify : MonoBehaviour
{
    private const string AutoRecoveryHandlingMethod = "自动恢复";

    public string configName = "DeviceSignalConfigs.json";

    [Header("报警列表")]
    public Transform listContainer;
    public GameObject alarmItemPrefabOdd;
    public GameObject alarmItemPrefabEven;

    [Header("确认弹窗")]
    public GameObject confirmPopup;
    public InputField inputHandlingMethod;
    public Button btnConfirm;
    public Button btnCancel;
    [Tooltip("开启后报警恢复时跳过确认弹窗，自动提交处理方法为“自动恢复”的报警记录。")]
    public bool autoPostOnRecovery;

    [Header("服务器")]
    public string baseUrl = "http://127.0.0.1:8000";
    public string plcId = "plc01";

    [Header("报警声音")]
    public AudioClip alarmAudioClip;
    public AudioSource alarmAudioSource;
    public bool loopAlarmAudio = true;
    public bool alarmAudioEnabled = true;

    [Header("报警提示设置")]
    public SwitchManager alarmAudioSwitch;
    public SwitchManager alarmPopupSwitch;
    public bool alarmPopupEnabled = true;
    public string alarmAudioSwitchTag = "WarningNotifyAlarmAudio";
    public string alarmPopupSwitchTag = "WarningNotifyAlarmPopup";

    private class AlarmPoint
    {
        public string key;
        public string deviceName;
        public string displayName;
        public bool lastValue;
        public DeviceSignalAlarmLevel alarmLevel;
    }

    private class AlarmEntry
    {
        public AlarmPoint point;
        public DateTime triggerTime;
        public DateTime? recoveryTime;
        public bool isRecovered;
        public GameObject uiGo;
    }

    [Serializable] private class CfgProject { public string sharedIpAddress; public List<CfgDevice> devices; }
    [Serializable] private class CfgDevice { public string deviceName; public List<DeviceSignalPoint> points; }

    private readonly List<AlarmPoint> _monitorPoints = new List<AlarmPoint>();
    private readonly List<AlarmEntry> _activeAlarms = new List<AlarmEntry>();
    private AlarmEntry _pendingConfirmEntry;

    private void Awake()
    {
        InitializeAlarmSwitches();
    }

    private void Start()
    {
        EnsureAlarmAudioSource();
        LoadServerConfig();

        if (btnConfirm != null)
            btnConfirm.onClick.AddListener(OnConfirmClicked);
        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnCancelClicked);
        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        Invoke("LoadAlarmPoints", 2f);
        InvokeRepeating("CheckAlarms", 3f, 1f);
    }

    private void OnDisable()
    {
        StopAlarmAudio();
    }

    private void LoadServerConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "ServerIP.config");
        if (!File.Exists(path))
            return;

        string savedUrl = DataUtil.Deserializer<string>(path);
        if (string.IsNullOrWhiteSpace(savedUrl))
            return;

        baseUrl = savedUrl.Trim().TrimEnd('/');
        int idx = baseUrl.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) baseUrl = baseUrl.Substring(0, idx);
        idx = baseUrl.IndexOf("/data", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) baseUrl = baseUrl.Substring(0, idx);
        baseUrl = baseUrl.TrimEnd('/');
    }

    private void LoadAlarmPoints()
    {
        _monitorPoints.Clear();

        string jsonPath = Path.Combine(Application.streamingAssetsPath, configName);
        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning("[WarningNotify] DeviceSignalConfigs.json 不存在: " + jsonPath);
            return;
        }

        string json = File.ReadAllText(jsonPath, Encoding.UTF8);
        CfgProject project = JsonUtility.FromJson<CfgProject>(json);
        if (project == null || project.devices == null)
            return;

        foreach (CfgDevice device in project.devices)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.deviceName) || device.points == null)
                continue;

            string devName = device.deviceName.Trim();
            foreach (DeviceSignalPoint pt in device.points)
            {
                if (pt == null) continue;
                if (pt.alarmLevel == DeviceSignalAlarmLevel.None) continue;
                if (pt.dataType != DeviceSignalDataType.BOOL) continue;
                if (string.IsNullOrWhiteSpace(pt.displayName)) continue;

                _monitorPoints.Add(new AlarmPoint
                {
                    key = devName + pt.displayName.Trim(),
                    deviceName = devName,
                    displayName = pt.displayName.Trim(),
                    lastValue = false,
                    alarmLevel = pt.alarmLevel,
                });
            }
        }

        Debug.Log($"[WarningNotify] 加载了 {_monitorPoints.Count} 个报警监控点位");
    }

    private void CheckAlarms()
    {
        if (_monitorPoints.Count == 0)
            return;

        foreach (AlarmPoint pt in _monitorPoints)
        {
            bool current = GetBool(pt.key);
            if (current && !pt.lastValue)
                OnAlarmTriggered(pt);
            else if (!current && pt.lastValue)
                OnAlarmRecovered(pt);

            pt.lastValue = current;
        }
    }

    private void OnAlarmTriggered(AlarmPoint pt)
    {
        AlarmEntry existing = _activeAlarms.Find(e => e.point.key == pt.key && !e.isRecovered);
        if (existing != null)
            return;

        AlarmEntry entry = new AlarmEntry
        {
            point = pt,
            triggerTime = DateTime.Now,
            isRecovered = false
        };

        int currentCount = listContainer != null ? listContainer.childCount : 0;
        GameObject prefab = (currentCount % 2 == 0) ? alarmItemPrefabOdd : alarmItemPrefabEven;
        if (prefab != null && listContainer != null)
        {
            GameObject go = Instantiate(prefab, listContainer);
            go.SetActive(true);
            entry.uiGo = go;

            AlarmItemUI ui = go.GetComponent<AlarmItemUI>();
            if (ui != null)
            {
                ui.SetInfo(entry.triggerTime.ToString("yyyy-MM-dd HH:mm:ss"), pt.deviceName, pt.displayName);
                ui.SetClickable(false, null);
            }
        }

        _activeAlarms.Add(entry);
        ShowAlarmPopup(entry);
        UpdateAlarmAudio();
        Debug.Log($"[WarningNotify] 报警触发: {pt.deviceName} - {pt.displayName}");
    }

    private void OnAlarmRecovered(AlarmPoint pt)
    {
        AlarmEntry entry = _activeAlarms.Find(e => e.point.key == pt.key && !e.isRecovered);
        if (entry == null)
            return;

        entry.recoveryTime = DateTime.Now;
        entry.isRecovered = true;
        UpdateAlarmAudio();

        Debug.Log($"[WarningNotify] 报警恢复: {pt.deviceName} - {pt.displayName}");

        if (autoPostOnRecovery)
        {
            if (entry.uiGo != null)
            {
                AlarmItemUI ui = entry.uiGo.GetComponent<AlarmItemUI>();
                if (ui != null)
                    ui.SetClickable(true, null);
            }

            StartCoroutine(PostAlarmRecord(entry, AutoRecoveryHandlingMethod, GetCurrentOperatorName()));
            return;
        }

        if (entry.uiGo != null)
        {
            AlarmItemUI ui = entry.uiGo.GetComponent<AlarmItemUI>();
            if (ui != null)
                ui.SetClickable(true, () => ShowConfirmPopup(entry));
        }
    }

    private void ShowConfirmPopup(AlarmEntry entry)
    {
        _pendingConfirmEntry = entry;
        if (inputHandlingMethod != null)
            inputHandlingMethod.text = "自行复位";
        if (confirmPopup != null)
            confirmPopup.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        if (_pendingConfirmEntry == null)
            return;

        string handlingMethod = inputHandlingMethod != null ? inputHandlingMethod.text.Trim() : "";
        string operatorName = GetCurrentOperatorName();

        if (string.IsNullOrEmpty(handlingMethod) || string.IsNullOrEmpty(operatorName))
        {
            Debug.LogWarning("[WarningNotify] 请填写处理方法和操作人员。");
            return;
        }

        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        AlarmEntry entry = _pendingConfirmEntry;
        _pendingConfirmEntry = null;
        StartCoroutine(PostAlarmRecord(entry, handlingMethod, operatorName));
    }

    private void OnCancelClicked()
    {
        _pendingConfirmEntry = null;
        if (confirmPopup != null)
            confirmPopup.SetActive(false);
    }

    private string GetCurrentOperatorName()
    {
        if (LoginManager.Instance != null && LoginManager.Instance.CurrentUser != null)
            return LoginManager.Instance.CurrentUser.name.Trim();

        return "默认账户";
    }

    private IEnumerator PostAlarmRecord(AlarmEntry entry, string handlingMethod, string operatorName)
    {
        string endpoint = baseUrl.TrimEnd('/') + "/api/v1/alarm/record";
        AlarmRecordPayload payload = new AlarmRecordPayload
        {
            trigger_time = entry.triggerTime.ToString("yyyy-MM-dd HH:mm:ss"),
            recovery_time = entry.recoveryTime.HasValue ? entry.recoveryTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
            device_name = entry.point.deviceName,
            alarm_content = entry.point.displayName,
            handling_method = handlingMethod,
            operator_name = operatorName,
            alarm_level = entry.point.alarmLevel.ToString(),
            plc_id = plcId
        };

        string json = JsonUtility.ToJson(payload);
        using (UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[WarningNotify] 报警记录提交失败: {request.error}\n{request.downloadHandler.text}");
                yield break;
            }

            Debug.Log($"[WarningNotify] 报警记录已提交: {entry.point.deviceName} - {entry.point.displayName}");
        }

        if (entry.uiGo != null)
            Destroy(entry.uiGo);
        _activeAlarms.Remove(entry);
        UpdateAlarmAudio();
    }

    private bool GetBool(string key)
    {
        object obj = PLCConfigManager.Instance.GetValue(key);
        return obj is bool value && value;
    }

    private void EnsureAlarmAudioSource()
    {
        if (alarmAudioSource == null)
            alarmAudioSource = GetComponent<AudioSource>();

        if (alarmAudioSource == null)
            alarmAudioSource = gameObject.AddComponent<AudioSource>();

        alarmAudioSource.playOnAwake = false;
        alarmAudioSource.loop = loopAlarmAudio;

        if (alarmAudioClip != null)
            alarmAudioSource.clip = alarmAudioClip;
    }

    private void UpdateAlarmAudio()
    {
        if (!alarmAudioEnabled)
        {
            StopAlarmAudio();
            return;
        }

        bool hasActiveAlarm = _activeAlarms.Exists(e => e != null && !e.isRecovered);
        if (hasActiveAlarm)
            PlayAlarmAudio();
        else
            StopAlarmAudio();
    }

    private void PlayAlarmAudio()
    {
        if (!alarmAudioEnabled)
            return;

        EnsureAlarmAudioSource();

        if (alarmAudioClip != null && alarmAudioSource.clip != alarmAudioClip)
            alarmAudioSource.clip = alarmAudioClip;

        alarmAudioSource.loop = loopAlarmAudio;

        if (alarmAudioSource.clip == null)
        {
            Debug.LogWarning("[WarningNotify] Alarm audio clip is not assigned.");
            return;
        }

        if (!alarmAudioSource.isPlaying)
            alarmAudioSource.Play();
    }

    private void StopAlarmAudio()
    {
        if (alarmAudioSource != null && alarmAudioSource.isPlaying)
            alarmAudioSource.Stop();
    }

    private void InitializeAlarmSwitches()
    {
        ConfigureSwitch(alarmAudioSwitch, alarmAudioSwitchTag, alarmAudioEnabled);
        ConfigureSwitch(alarmPopupSwitch, alarmPopupSwitchTag, alarmPopupEnabled);

        alarmAudioEnabled = ReadSwitchValue(alarmAudioSwitchTag, alarmAudioEnabled);
        alarmPopupEnabled = ReadSwitchValue(alarmPopupSwitchTag, alarmPopupEnabled);

        BindSwitchEvents(alarmAudioSwitch, SetAlarmAudioEnabled, SetAlarmAudioDisabled);
        BindSwitchEvents(alarmPopupSwitch, SetAlarmPopupEnabled, SetAlarmPopupDisabled);

        if (!alarmAudioEnabled)
            StopAlarmAudio();
    }

    private void ConfigureSwitch(SwitchManager switchManager, string switchTag, bool defaultValue)
    {
        if (switchManager == null)
            return;

        switchManager.saveValue = true;
        switchManager.switchTag = switchTag;
        switchManager.isOn = ReadSwitchValue(switchTag, defaultValue);
    }

    private bool ReadSwitchValue(string switchTag, bool defaultValue)
    {
        string saved = PlayerPrefs.GetString(switchTag + "Switch", string.Empty);
        if (string.Equals(saved, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(saved, "false", StringComparison.OrdinalIgnoreCase))
            return false;

        return defaultValue;
    }

    private void BindSwitchEvents(SwitchManager switchManager, UnityEngine.Events.UnityAction onAction, UnityEngine.Events.UnityAction offAction)
    {
        if (switchManager == null)
            return;

        switchManager.OnEvents.RemoveListener(onAction);
        switchManager.OffEvents.RemoveListener(offAction);
        switchManager.OnEvents.AddListener(onAction);
        switchManager.OffEvents.AddListener(offAction);
    }

    private void SetAlarmAudioEnabled()
    {
        alarmAudioEnabled = true;
        PlayerPrefs.SetString(alarmAudioSwitchTag + "Switch", "true");
        PlayerPrefs.Save();
        UpdateAlarmAudio();
    }

    private void SetAlarmAudioDisabled()
    {
        alarmAudioEnabled = false;
        PlayerPrefs.SetString(alarmAudioSwitchTag + "Switch", "false");
        PlayerPrefs.Save();
        StopAlarmAudio();
    }

    private void SetAlarmPopupEnabled()
    {
        alarmPopupEnabled = true;
        PlayerPrefs.SetString(alarmPopupSwitchTag + "Switch", "true");
        PlayerPrefs.Save();
    }

    private void SetAlarmPopupDisabled()
    {
        alarmPopupEnabled = false;
        PlayerPrefs.SetString(alarmPopupSwitchTag + "Switch", "false");
        PlayerPrefs.Save();
    }

    private void ShowAlarmPopup(AlarmEntry entry)
    {
        if (!alarmPopupEnabled || entry == null || entry.point == null)
            return;

        string text = $"报警触发：{entry.point.deviceName} - {entry.point.displayName}";
        PopCtrl.Instance?.ShowWarningPop(text);
    }

    [Serializable]
    private class AlarmRecordPayload
    {
        public string trigger_time;
        public string recovery_time;
        public string device_name;
        public string alarm_content;
        public string handling_method;
        public string operator_name;
        public string alarm_level;
        public string plc_id;
    }
}
