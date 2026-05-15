using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 实时报警监控面板。
/// 从 DeviceSignalConfigs.json 中读取 alarmLevel != None 的 BOOL 点位，
/// 每秒检测一次：
///   - 点位变 true  → 在列表中新增一条报警信息（报警时间、设备名、报警内容）
///   - 点位变 false → 记录恢复时间，该条记录变成可点击
///   - 点击记录    → 弹出确认窗，输入处理方法和操作人，确认后 POST 到服务器并移除
/// </summary>
public class WarningNotify : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Inspector 配置
    // ---------------------------------------------------------------
    public string configName="DeviceSignalConfigs.json";

    [Header("报警列表")]
    public Transform listContainer;          // 报警条目的父容器（如 ScrollView 的 Content）
    public GameObject alarmItemPrefabOdd;    // 奇数行预制体（第1、3、5…条）
    public GameObject alarmItemPrefabEven;   // 偶数行预制体（第2、4、6…条）

    [Header("确认弹窗")]
    public GameObject confirmPopup;          // 弹窗根节点，默认关闭
    public InputField inputHandlingMethod;   // 处理方法输入框
    //public InputField inputOperatorName;     // 操作人员输入框
    public Button btnConfirm;                // 确认按钮
    public Button btnCancel;                 // 取消按钮

    [Header("服务器")]
    public string baseUrl = "http://127.0.0.1:8000";
    public string plcId = "plc01";

    // ---------------------------------------------------------------
    // 内部数据结构
    // ---------------------------------------------------------------

    /// <summary>监控的报警点位信息</summary>
    private class AlarmPoint
    {
        public string key;                              // PLCConfigManager 中的键 (deviceName + displayName)
        public string deviceName;                       // 设备名称
        public string displayName;                      // 点位显示名（报警内容）
        public bool lastValue;                          // 上一次检测的 bool 值
        public DeviceSignalAlarmLevel alarmLevel;       // 报警等级
    }

    /// <summary>一条活跃的报警记录</summary>
    private class AlarmEntry
    {
        public AlarmPoint point;
        public DateTime triggerTime;
        public DateTime? recoveryTime;
        public bool isRecovered;
        public GameObject uiGo;
    }

    private List<AlarmPoint> _monitorPoints = new List<AlarmPoint>();
    private List<AlarmEntry> _activeAlarms  = new List<AlarmEntry>();
    private AlarmEntry _pendingConfirmEntry;   // 当前等待确认的记录

    // ---------------------------------------------------------------
    // 配置加载用的内部类（与 PLCConfigManager 保持一致）
    // ---------------------------------------------------------------
    [Serializable] private class CfgProject { public string sharedIpAddress; public List<CfgDevice> devices; }
    [Serializable] private class CfgDevice  { public string deviceName; public List<DeviceSignalPoint> points; }

    // ---------------------------------------------------------------
    // 生命周期
    // ---------------------------------------------------------------

    private void Start()
    {
        // 读取服务器地址
        string savedUrl = DataUtil.Deserializer<string>(Application.streamingAssetsPath + "/ServerIP.config");
        if (!string.IsNullOrWhiteSpace(savedUrl))
        {
            baseUrl = savedUrl.Trim().TrimEnd('/');
            // 移除可能多带的路径
            int idx = baseUrl.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) baseUrl = baseUrl.Substring(0, idx);
            idx = baseUrl.IndexOf("/data", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) baseUrl = baseUrl.Substring(0, idx);
            baseUrl = baseUrl.TrimEnd('/');
        }

        // 绑定弹窗按钮
        if (btnConfirm != null)
            btnConfirm.onClick.AddListener(OnConfirmClicked);
        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnCancelClicked);
        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        // 延迟一帧加载配置，确保 PLCConfigManager 已初始化
        Invoke("LoadAlarmPoints", 2f);

        // 每秒检查一次
        InvokeRepeating("CheckAlarms", 3f, 1f);
    }

    // ---------------------------------------------------------------
    // 配置加载
    // ---------------------------------------------------------------

    private void LoadAlarmPoints()
    {
        print("LoadAlarmPoints");
        string jsonPath = Application.streamingAssetsPath + "/"+configName;
        if (!File.Exists(jsonPath))
        {
            Debug.LogWarning("[WarningNotify] DeviceSignalConfigs.json 不存在: " + jsonPath);
            return;
        }

        string json = File.ReadAllText(jsonPath, Encoding.UTF8);
        CfgProject project = JsonUtility.FromJson<CfgProject>(json);
        if (project == null || project.devices == null) return;

        foreach (CfgDevice device in project.devices)
        {
            if (device == null || string.IsNullOrWhiteSpace(device.deviceName) || device.points == null)
                continue;

            string devName = device.deviceName.Trim();
            foreach (DeviceSignalPoint pt in device.points)
            {
                if (pt == null) continue;
                // 只监控 alarmLevel 不为 None 且数据类型为 BOOL 的点位
                if (pt.alarmLevel == DeviceSignalAlarmLevel.None) continue;
                if (pt.dataType != DeviceSignalDataType.BOOL) continue;
                if (string.IsNullOrWhiteSpace(pt.displayName)) continue;

                string key = devName + pt.displayName.Trim();
                _monitorPoints.Add(new AlarmPoint
                {
                    key         = key,
                    deviceName  = devName,
                    displayName = pt.displayName.Trim(),
                    lastValue   = false,
                    alarmLevel  = pt.alarmLevel,
                });
            }
        }

        Debug.Log($"[WarningNotify] 加载了 {_monitorPoints.Count} 个报警监控点位");
    }

    // ---------------------------------------------------------------
    // 每秒检测
    // ---------------------------------------------------------------

    private void CheckAlarms()
    {
        if (_monitorPoints == null || _monitorPoints.Count == 0) return;

        foreach (AlarmPoint pt in _monitorPoints)
        {
            bool current = GetBool(pt.key);

            // 上升沿：false → true → 触发报警
            if (current && !pt.lastValue)
            {
                OnAlarmTriggered(pt);
            }
            // 下降沿：true → false → 报警恢复
            else if (!current && pt.lastValue)
            {
                OnAlarmRecovered(pt);
            }

            pt.lastValue = current;
        }
    }

    // ---------------------------------------------------------------
    // 报警触发
    // ---------------------------------------------------------------

    private void OnAlarmTriggered(AlarmPoint pt)
    {
        // 如果该点位已有一条未处理的报警，跳过
        AlarmEntry existing = _activeAlarms.Find(e => e.point.key == pt.key && !e.isRecovered);
        if (existing != null) return;

        AlarmEntry entry = new AlarmEntry
        {
            point       = pt,
            triggerTime = DateTime.Now,
            isRecovered = false
        };

        // 实例化 UI（奇偶行交替使用不同预制体）
        int currentCount = listContainer != null ? listContainer.childCount : 0;
        GameObject prefab = (currentCount % 2 == 0) ? alarmItemPrefabOdd : alarmItemPrefabEven;
        if (prefab != null && listContainer != null)
        {
            GameObject go = Instantiate(prefab, listContainer);
            go.SetActive(true);
            entry.uiGo = go;

            // 设置显示内容
            AlarmItemUI ui = go.GetComponent<AlarmItemUI>();
            if (ui != null)
            {
                ui.SetInfo(
                    entry.triggerTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    pt.deviceName,
                    pt.displayName
                );
                // 初始状态：不可点击
                ui.SetClickable(false, null);
            }
        }

        _activeAlarms.Add(entry);
        Debug.Log($"[WarningNotify] 报警触发: {pt.deviceName} - {pt.displayName}");
    }

    // ---------------------------------------------------------------
    // 报警恢复
    // ---------------------------------------------------------------

    private void OnAlarmRecovered(AlarmPoint pt)
    {
        AlarmEntry entry = _activeAlarms.Find(e => e.point.key == pt.key && !e.isRecovered);
        if (entry == null) return;

        entry.recoveryTime = DateTime.Now;
        entry.isRecovered  = true;

        // 更新 UI：可点击
        if (entry.uiGo != null)
        {
            AlarmItemUI ui = entry.uiGo.GetComponent<AlarmItemUI>();
            if (ui != null)
            {
                ui.SetClickable(true, () => { ShowConfirmPopup(entry); });
            }
        }

        Debug.Log($"[WarningNotify] 报警恢复: {pt.deviceName} - {pt.displayName}");
    }

    // ---------------------------------------------------------------
    // 弹窗逻辑
    // ---------------------------------------------------------------

    private void ShowConfirmPopup(AlarmEntry entry)
    {
        _pendingConfirmEntry = entry;
        if (inputHandlingMethod != null) inputHandlingMethod.text = "自行复位";
      //  if (inputOperatorName  != null) inputOperatorName.text  = "";
        if (confirmPopup != null) confirmPopup.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        if (_pendingConfirmEntry == null) return;

        string handlingMethod = inputHandlingMethod != null ? inputHandlingMethod.text.Trim() : "";
        string operatorName   = LoginManager.Instance.CurrentUser   != null ? LoginManager.Instance.CurrentUser.name.Trim()   : "默认账户";

        if (string.IsNullOrEmpty(handlingMethod) || string.IsNullOrEmpty(operatorName))
        {
            Debug.LogWarning("[WarningNotify] 请填写处理方法和操作人员。");
            return;
        }

        // 关闭弹窗
        if (confirmPopup != null) confirmPopup.SetActive(false);

        AlarmEntry entry = _pendingConfirmEntry;
        _pendingConfirmEntry = null;

        // 提交到服务器
        StartCoroutine(PostAlarmRecord(entry, handlingMethod, operatorName));
    }

    private void OnCancelClicked()
    {
        _pendingConfirmEntry = null;
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }

    // ---------------------------------------------------------------
    // 提交报警记录到服务器
    // ---------------------------------------------------------------

    private IEnumerator PostAlarmRecord(AlarmEntry entry, string handlingMethod, string operatorName)
    {
        string endpoint = baseUrl.TrimEnd('/') + "/api/v1/alarm/record";

        AlarmRecordPayload payload = new AlarmRecordPayload
        {
            trigger_time    = entry.triggerTime.ToString("yyyy-MM-dd HH:mm:ss"),
            recovery_time   = entry.recoveryTime.HasValue ? entry.recoveryTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
            device_name     = entry.point.deviceName,
            alarm_content   = entry.point.displayName,
            handling_method = handlingMethod,
            operator_name   = operatorName,
            alarm_level     = entry.point.alarmLevel.ToString(),
            plc_id          = plcId
        };

        string json = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler   = new UploadHandlerRaw(body);
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

        // 从列表移除
        if (entry.uiGo != null)
            Destroy(entry.uiGo);
        _activeAlarms.Remove(entry);
    }

    // ---------------------------------------------------------------
    // 辅助
    // ---------------------------------------------------------------

    private bool GetBool(string key)
    {
        object obj = PLCConfigManager.Instance.GetValue(key);
        if (obj is bool)
            return (bool)obj;
        return false;
    }

    /// <summary>JSON 序列化用的内部类</summary>
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
