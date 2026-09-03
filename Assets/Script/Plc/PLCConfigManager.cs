using S7.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum PLCValueSource
{
    ActiveCrane,
    OppositeCrane
}

public class PLCConfigManager : MonoBehaviour
{
    [Serializable]
    public class CranePlcConfig
    {
        public string displayName;
        public string deviceSignalJsonName;
    }

    public static PLCConfigManager Instance;

    public string deviceSignalJsonName = "DeviceSignalConfigs.json";
    public int defaultPort = 102;
    public short defaultRack = 0;
    public short defaultSlot = 1;

    [Header("行车切换")]
    public bool enableCraneSwitching = false;
    public int activeCraneIndex = 0;
    public CranePlcConfig[] craneConfigs =
    {
        new CranePlcConfig { displayName = "1号行车", deviceSignalJsonName = "PLC01.json" },
        new CranePlcConfig { displayName = "2号行车", deviceSignalJsonName = "PLC02.json" }
    };
    public string[] oppositePositionKeys =
    {
        "大车大车当前位置",
        "小车小车当前位置"
    };
    public bool createRuntimeSwitchButton = false;

    private static readonly string[] defaultOppositeValueKeys =
    {
        "\u5927\u8f66\u5927\u8f66\u5f53\u524d\u4f4d\u7f6e",
        "\u5c0f\u8f66\u5c0f\u8f66\u5f53\u524d\u4f4d\u7f6e",
        "\u63d0\u5347\u63d0\u5347\u5f53\u524d\u9ad8\u5ea6",
        "\u63d0\u5347\u5f00\u95ed\u5f53\u524d\u9ad8\u5ea6",
        "\u8fd0\u884c\u4fe1\u53f7\u5927\u8f66\u5f53\u524d\u4f4d\u7f6e",
        "\u8fd0\u884c\u4fe1\u53f7\u5c0f\u8f66\u5f53\u524d\u4f4d\u7f6e",
        "\u8fd0\u884c\u4fe1\u53f7\u6293\u6597\u5f53\u524d\u9ad8\u5ea6",
        "\u8fd0\u884c\u4fe1\u53f7\u6293\u6597\u5f53\u524d\u5f00\u5ea6"
    };

    [Header("PLC 自动重连")]
    public bool autoReconnect = true;
    public float reconnectInterval = 3f;
    public float maxReconnectInterval = 30f;

    [Serializable]
    private class DeviceSignalConfigProjectFile
    {
        public string sharedIpAddress;
        public List<DeviceSignalConfigJsonDevice> devices = new List<DeviceSignalConfigJsonDevice>();
    }

    [Serializable]
    private class DeviceSignalConfigJsonDevice
    {
        public string deviceName;
        public List<DeviceSignalPoint> points = new List<DeviceSignalPoint>();
    }

    private class ReconnectState
    {
        public int failCount = 0;
        public DateTime nextTryAt = DateTime.MinValue;
        public bool isConnecting = false;
    }

    private class PlcRuntimeState
    {
        public Dictionary<string, PLCConfig> configs = new Dictionary<string, PLCConfig>();
        public Dictionary<string, PLCConnect> connects = new Dictionary<string, PLCConnect>();
        public ConcurrentDictionary<int, DataBlockInfo> datablocks = new ConcurrentDictionary<int, DataBlockInfo>();
    }

    private readonly object runtimeStateLock = new object();
    private readonly Dictionary<PLCConnect, ReconnectState> reconnectInfo = new Dictionary<PLCConnect, ReconnectState>();

    public Dictionary<string, PLCConfig> plcConfigs = new Dictionary<string, PLCConfig>();
    public Dictionary<string, PLCAddress> plcAddress = new Dictionary<string, PLCAddress>();
    public Dictionary<string, PLCConnect> plcConnectDic = new Dictionary<string, PLCConnect>();
    public Dictionary<PLCConnect, DateTime> plcTryConnect = new Dictionary<PLCConnect, DateTime>();
    public ConcurrentDictionary<int, DataBlockInfo> datablockSplit = new ConcurrentDictionary<int, DataBlockInfo>();

    public static Action OnUpdateUI;
    public static Action OnUpdate;
    public static Action<int> OnActiveCraneChanged;

    public Dictionary<string, PLCConfig> plcConfigsTmp = new Dictionary<string, PLCConfig>();

    private PlcRuntimeState activeState = new PlcRuntimeState();
    private PlcRuntimeState oppositeState = new PlcRuntimeState();
    private Thread readThread;
    private bool stopReadThread = false;
    private float timer = 9999;

    public float checkNetTimr = 0;

    public class DataBlockInfo
    {
        public int max;
        public int min;
        public byte[] data;
    }

    private void Awake()
    {
        Instance = this;
        ApplyCraneSelection(activeCraneIndex, false);

        ReadData();
        readThread = new Thread(ReadThread);
        readThread.IsBackground = true;
        readThread.Start();
    }

    private void Start()
    {
        if (enableCraneSwitching && createRuntimeSwitchButton && FindObjectOfType<CraneSwitchButton>() == null)
        {
            CraneSwitchButton switchButton = gameObject.AddComponent<CraneSwitchButton>();
            switchButton.plcConfigManager = this;
            switchButton.createRuntimeButtonIfMissing = true;
        }
    }

    private void OnDestroy()
    {
        stopReadThread = true;

        lock (runtimeStateLock)
        {
            CloseRuntimeState(activeState);
            CloseRuntimeState(oppositeState);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ApplyCraneSelection(int requestedIndex, bool notify)
    {
        lock (runtimeStateLock)
        {
            CloseRuntimeState(activeState);
            CloseRuntimeState(oppositeState);

            reconnectInfo.Clear();
            lock (plcAddress)
            {
                plcAddress.Clear();
            }

            if (enableCraneSwitching && craneConfigs != null && craneConfigs.Length > 0)
            {
                activeCraneIndex = Mathf.Clamp(requestedIndex, 0, craneConfigs.Length - 1);
                activeState = CreateRuntimeState(GetCraneConfigFileName(activeCraneIndex), null);

                int oppositeIndex = GetOppositeCraneIndex();
                oppositeState = oppositeIndex >= 0
                    ? CreateRuntimeState(GetCraneConfigFileName(oppositeIndex), BuildAllowedOppositePositionKeys())
                    : new PlcRuntimeState();
            }
            else
            {
                activeCraneIndex = 0;
                activeState = CreateRuntimeState(deviceSignalJsonName, null);
                oppositeState = new PlcRuntimeState();
            }

            plcConfigs = activeState.configs;
            plcConnectDic = activeState.connects;
            datablockSplit = activeState.datablocks;
        }

        if (notify)
        {
            OnActiveCraneChanged?.Invoke(activeCraneIndex);
        }
    }

    private PlcRuntimeState CreateRuntimeState(string configFileName, HashSet<string> allowedKeys)
    {
        PlcRuntimeState state = new PlcRuntimeState();
        if (TryLoadConfigsFromJson(configFileName, state.configs, allowedKeys))
        {
            InitializeRuntimeState(state);
        }

        return state;
    }

    private bool TryLoadConfigsFromJson(string configFileName, Dictionary<string, PLCConfig> targetConfigs, HashSet<string> allowedKeys)
    {
        targetConfigs.Clear();

        string jsonPath = Path.Combine(Application.streamingAssetsPath, configFileName);
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"[PLCConfigManager] JSON 配置不存在: {jsonPath}");
            return false;
        }

        try
        {
            string json = File.ReadAllText(jsonPath, Encoding.UTF8);
            DeviceSignalConfigProjectFile projectFile = JsonUtility.FromJson<DeviceSignalConfigProjectFile>(json);
            if (projectFile == null || projectFile.devices == null || projectFile.devices.Count == 0)
            {
                Debug.LogWarning($"[PLCConfigManager] JSON 配置为空: {jsonPath}");
                return false;
            }

            string sharedIpAddress = string.IsNullOrWhiteSpace(projectFile.sharedIpAddress) ? string.Empty : projectFile.sharedIpAddress.Trim();

            foreach (DeviceSignalConfigJsonDevice device in projectFile.devices)
            {
                if (device == null || string.IsNullOrWhiteSpace(device.deviceName) || device.points == null)
                {
                    continue;
                }

                string deviceName = device.deviceName.Trim();
                foreach (DeviceSignalPoint point in device.points)
                {
                    if (point == null || string.IsNullOrWhiteSpace(point.displayName) || string.IsNullOrWhiteSpace(point.address))
                    {
                        continue;
                    }

                    string key = deviceName + point.displayName.Trim();
                    if (allowedKeys != null && !allowedKeys.Contains(key))
                    {
                        continue;
                    }

                    PLCConfig config = new PLCConfig
                    {
                        IPAddress = sharedIpAddress,
                        Port = defaultPort,
                        Rack = defaultRack,
                        Slot = defaultSlot,
                        DataBlock = point.address.Trim(),
                        DataType = ConvertDeviceSignalDataType(point.dataType),
                        ShortName = point.displayName.Trim()
                    };

                    if (targetConfigs.ContainsKey(key))
                    {
                        Debug.LogWarning($"[PLCConfigManager] 检测到重复配置键，后一个将覆盖前一个: {key}");
                    }

                    targetConfigs[key] = config;
                }
            }

            if (targetConfigs.Count == 0)
            {
                Debug.LogWarning($"[PLCConfigManager] JSON 中没有可用的点位配置: {jsonPath}");
                return false;
            }

            Debug.Log($"[PLCConfigManager] 已从 JSON 加载 {targetConfigs.Count} 条 PLC 配置: {jsonPath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PLCConfigManager] JSON 配置加载失败: {jsonPath}\n{ex}");
            return false;
        }
    }

    private string ConvertDeviceSignalDataType(DeviceSignalDataType dataType)
    {
        switch (dataType)
        {
            case DeviceSignalDataType.BOOL:
                return "Bool";
            case DeviceSignalDataType.INT:
                return "Int";
            case DeviceSignalDataType.REAL:
                return "Real";
            case DeviceSignalDataType.DINT:
                return "DInt";
            case DeviceSignalDataType.LINT:
                return "LInt";
            default:
                return "Bool";
        }
    }

    private void InitializeRuntimeState(PlcRuntimeState state)
    {
        state.connects.Clear();
        state.datablocks.Clear();

        foreach (var config in state.configs)
        {
            if (config.Value == null || string.IsNullOrWhiteSpace(config.Value.DataBlock))
            {
                continue;
            }

            string key = GetConfigConnectKey(config.Value);
            bool isNewConnect = false;
            if (!state.connects.ContainsKey(key))
            {
                PLCConnect connect = new PLCConnect();
                connect.ipaddrees = config.Value.IPAddress;
                connect.port = config.Value.Port;
                connect.rack = config.Value.Rack;
                connect.slot = config.Value.Slot;
                state.connects.Add(key, connect);
                reconnectInfo[connect] = new ReconnectState();
                isNewConnect = true;
            }

            if (isNewConnect && !state.connects[key].IsConnected())
            {
                EnsureConnected(state.connects[key], key, true);
            }

            try
            {
                var addr = GetPLCAddress(config.Value.DataBlock);
                int bitSize = GetBitSize(config.Value.DataType);
                if (config.Value.Number != 0)
                {
                    bitSize *= config.Value.Number;
                }

                int byteSize = Mathf.CeilToInt(bitSize * 1.0f / 8);
                if (state.datablocks.ContainsKey(addr.DbNumber))
                {
                    state.datablocks[addr.DbNumber].max = Mathf.Max(addr.StartByte + byteSize, state.datablocks[addr.DbNumber].max);
                    state.datablocks[addr.DbNumber].min = Mathf.Min(addr.StartByte, state.datablocks[addr.DbNumber].min);
                }
                else
                {
                    state.datablocks.TryAdd(addr.DbNumber, new DataBlockInfo());
                    state.datablocks[addr.DbNumber].max = addr.StartByte + byteSize;
                    state.datablocks[addr.DbNumber].min = addr.StartByte;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PLCConfigManager] 地址解析失败，key={config.Key}, address={config.Value.DataBlock}\n{ex}");
            }
        }
    }

    private HashSet<string> BuildAllowedOppositePositionKeys()
    {
        HashSet<string> keys = new HashSet<string>();
        foreach (string key in defaultOppositeValueKeys)
        {
            keys.Add(key);
        }

        if (oppositePositionKeys == null)
        {
            return keys;
        }

        for (int i = 0; i < oppositePositionKeys.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(oppositePositionKeys[i]))
            {
                keys.Add(oppositePositionKeys[i].Trim());
            }
        }

        return keys;
    }

    private string GetCraneConfigFileName(int craneIndex)
    {
        if (craneConfigs == null || craneIndex < 0 || craneIndex >= craneConfigs.Length || craneConfigs[craneIndex] == null ||
            string.IsNullOrWhiteSpace(craneConfigs[craneIndex].deviceSignalJsonName))
        {
            return deviceSignalJsonName;
        }

        return craneConfigs[craneIndex].deviceSignalJsonName.Trim();
    }

    private int GetOppositeCraneIndex()
    {
        if (!enableCraneSwitching || craneConfigs == null || craneConfigs.Length < 2)
        {
            return -1;
        }

        return activeCraneIndex == 0 ? 1 : 0;
    }

    private void CloseRuntimeState(PlcRuntimeState state)
    {
        if (state == null || state.connects == null)
        {
            return;
        }

        foreach (var kv in state.connects)
        {
            kv.Value?.Close();
        }
    }

    public void SwitchToNextCrane()
    {
        if (!enableCraneSwitching || craneConfigs == null || craneConfigs.Length == 0)
        {
            Debug.LogWarning("[PLCConfigManager] 未启用行车切换或未配置行车列表");
            return;
        }

        int nextIndex = (activeCraneIndex + 1) % craneConfigs.Length;
        SwitchToCrane(nextIndex);
    }

    public void SwitchToCrane(int craneIndex)
    {
        if (!enableCraneSwitching || craneConfigs == null || craneConfigs.Length == 0)
        {
            Debug.LogWarning("[PLCConfigManager] 未启用行车切换或未配置行车列表");
            return;
        }

        int clampedIndex = Mathf.Clamp(craneIndex, 0, craneConfigs.Length - 1);
        if (clampedIndex == activeCraneIndex)
        {
            OnActiveCraneChanged?.Invoke(activeCraneIndex);
            return;
        }

        ApplyCraneSelection(clampedIndex, true);
        Debug.Log($"[PLCConfigManager] 已切换主控行车: {GetActiveCraneDisplayName()}");
    }

    public string GetActiveCraneDisplayName()
    {
        if (enableCraneSwitching && craneConfigs != null && activeCraneIndex >= 0 && activeCraneIndex < craneConfigs.Length &&
            craneConfigs[activeCraneIndex] != null && !string.IsNullOrWhiteSpace(craneConfigs[activeCraneIndex].displayName))
        {
            return craneConfigs[activeCraneIndex].displayName.Trim();
        }

        return "当前行车";
    }

    public int GetActiveCraneNumber()
    {
        return activeCraneIndex + 1;
    }

    public bool TryGetActiveCranePlcId(out string plcId)
    {
        plcId = string.Empty;
        if (!enableCraneSwitching || craneConfigs == null || craneConfigs.Length <= 0)
        {
            return false;
        }

        plcId = $"plc{GetActiveCraneNumber():00}";
        return true;
    }

    public PLCValueSource GetValueSourceForCraneNumber(int craneNumber)
    {
        if (!enableCraneSwitching || craneConfigs == null || craneConfigs.Length <= 0)
        {
            return PLCValueSource.ActiveCrane;
        }

        int craneIndex = craneNumber - 1;
        if (craneIndex < 0 || craneIndex >= craneConfigs.Length)
        {
            return PLCValueSource.ActiveCrane;
        }

        if (craneIndex == activeCraneIndex)
        {
            return PLCValueSource.ActiveCrane;
        }

        return craneIndex == GetOppositeCraneIndex() ? PLCValueSource.OppositeCrane : PLCValueSource.ActiveCrane;
    }

    public string GetOppositeCraneDisplayName()
    {
        int oppositeIndex = GetOppositeCraneIndex();
        if (oppositeIndex >= 0 && craneConfigs[oppositeIndex] != null && !string.IsNullOrWhiteSpace(craneConfigs[oppositeIndex].displayName))
        {
            return craneConfigs[oppositeIndex].displayName.Trim();
        }
        return "对侧行车";
    }

    public string GetDBAddrByStringKey(string arg)
    {
        lock (runtimeStateLock)
        {
            string addr = null;
            foreach (var item in plcConfigs)
            {
                if (item.Key == arg)
                {
                    addr = item.Value.DataBlock;
                }
            }
            return addr;
        }
    }

    public void ReadThread()
    {
        while (!stopReadThread)
        {
            Thread.Sleep(1000 / 15);
            ReadData();
        }
    }

    public void NetCheck()
    {
        lock (runtimeStateLock)
        {
            NetCheck(activeState);
            NetCheck(oppositeState);
        }
    }

    private void NetCheck(PlcRuntimeState state)
    {
        if (state == null || state.connects.Count <= 0)
        {
            return;
        }

        foreach (var kv in state.connects)
        {
            if (!kv.Value.IsConnected())
            {
                kv.Value.Open();
            }
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > 0.016666f)
        {
            timer = 0.0f;
            OnUpdate?.Invoke();
            OnUpdateUI?.Invoke();
        }
    }

    public Plc GetPlc(string key)
    {
        lock (runtimeStateLock)
        {
            string connectKey = string.Empty;

            if (plcConfigs.ContainsKey(key))
            {
                connectKey = GetConfigConnectKey(plcConfigs[key]);
            }

            var plcConnect = GetPLCConnect(connectKey);
            return plcConnect?.GetPlc();
        }
    }

    public string GetShortName(string key)
    {
        lock (runtimeStateLock)
        {
            string shortName = string.Empty;
            foreach (var item in plcConfigs)
            {
                if (item.Key == key)
                {
                    shortName = item.Value.ShortName;
                    if (shortName == "")
                    {
                        shortName = key;
                    }
                }
            }

            return shortName;
        }
    }

    public string GetConfigConnectKey(PLCConfig config)
    {
        StringBuilder strbuild = new StringBuilder();

        strbuild.Append(config.IPAddress);
        strbuild.Append(':');
        strbuild.Append(config.Port);
        strbuild.Append(':');
        strbuild.Append(config.Rack);
        strbuild.Append(':');
        strbuild.Append(config.Slot);

        return strbuild.ToString();
    }

    public PLCConnect GetPLCConnect(string key)
    {
        if (plcConnectDic.ContainsKey(key))
        {
            return plcConnectDic[key];
        }
        return null;
    }

    private bool TryGetConnectedPLCConnectByConfigKey(string key, string operationName, out PLCConnect plcConnect, out PLCConfig config)
    {
        lock (runtimeStateLock)
        {
            plcConnect = null;
            config = null;

            if (!plcConfigs.ContainsKey(key))
            {
                Debug.LogError($"[PLCConfigManager] {operationName}失败，未找到 PLC 配置: {key}");
                return false;
            }

            config = plcConfigs[key];
            string connectKey = GetConfigConnectKey(config);
            plcConnect = GetPLCConnect(connectKey);
            if (plcConnect == null)
            {
                Debug.LogError($"[PLCConfigManager] {operationName}失败，未找到 PLC 连接: {connectKey}");
                return false;
            }

            if (!EnsureConnected(plcConnect, connectKey, true))
            {
                Debug.LogError($"[PLCConfigManager] {operationName}失败，PLC 未连接: {connectKey}");
                return false;
            }

            return true;
        }
    }

    public void SetBool(string key, object value)
    {
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "写入 Bool", out plcConnect, out config)) return;

        plcConnect.Write(config.DataBlock, value, key + "设置值为：" + value);
    }

    public async void SetPulseBool(string key, object value)
    {
        print(key);
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "脉冲写入", out plcConnect, out config)) return;

        plcConnect.Write(config.DataBlock, value, key + "设置值为：" + value);

        await Task.Delay(500);

        if (value is bool)
        {
            value = false;
            plcConnect.Write(config.DataBlock, value, GetShortName(key) + "设置值为：" + value);
        }
    }

    public async void SetValue(string key, object value)
    {
        print(key);
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "写入数值", out plcConnect, out config)) return;

        plcConnect.Write(config.DataBlock, value, key + "设置值为：" + value);

        await Task.Delay(500);
        PopCtrl.Instance.ShowPop(GetShortName(key) + "设定成功");
    }

    public async void SetValueConfirm(string key, object value)
    {
        print(key);
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "确认写入", out plcConnect, out config)) return;

        plcConnect.WriteNoLog(config.DataBlock, value);

        await Task.Delay(500);

        if (value is bool)
        {
            value = false;
        }

        PopCtrl.Instance.ShowPop(GetShortName(key) + "设定成功");
    }

    public async void SetValueNoNotify(string key, object value)
    {
        print(key);
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "静默写入", out plcConnect, out config)) return;

        plcConnect.Write(config.DataBlock, value);

        await Task.Delay(500);

        if (value is bool)
        {
            value = false;
            plcConnect.Write(config.DataBlock, value);
        }
    }

    public async void SetValue(string key, object value, bool isKeep = false)
    {
        PLCConnect conn;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "写入数值", out conn, out config)) return;

        try
        {
            var adr = GetPLCAddress(config.DataBlock);
            Debug.Log($"[WRITE] DB{adr.DbNumber} start={adr.StartByte} bit={adr.BitNumber} type={config.DataType} value={value}");

            conn.Write(config.DataBlock, value, key);

            if (!isKeep && value is bool)
            {
                await Task.Delay(300);
                conn.Write(config.DataBlock, false);
            }

            PopCtrl.Instance?.ShowPop(GetShortName(key) + "设定成功");
        }
        catch (S7.Net.PlcException ex)
        {
            Debug.LogError($"[WRITE-FAIL] key={key} -> {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WRITE-ERR] key={key} -> {ex}");
        }
    }

    public void SetValue(string key, int offset, object value)
    {
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "偏移写入", out plcConnect, out config)) return;

        PLCAddress adr = GetPLCAddress(config.DataBlock);
        plcConnect.Write(adr.DataType, adr.DbNumber, adr.StartByte + offset, value, adr.BitNumber);
    }

    public void SetValueBit(string key, int offset, object value)
    {
        PLCConnect plcConnect;
        PLCConfig config;
        if (!TryGetConnectedPLCConnectByConfigKey(key, "位写入", out plcConnect, out config)) return;

        PLCAddress adr = GetPLCAddress(config.DataBlock);

        int byteOffset = (adr.BitNumber + offset) / 8;
        int bitOffset = (adr.BitNumber + offset) % 8;

        plcConnect.Write(adr.DataType, adr.DbNumber, adr.StartByte + byteOffset, value, bitOffset);
    }

    public float GetFloatValue(string key)
    {
        return GetFloatValue(key, PLCValueSource.ActiveCrane);
    }

    public float GetFloatValue(string key, PLCValueSource valueSource)
    {
        object obj = GetValue(key, 0, valueSource);
        if (obj is float)
            return (float)obj;
        return 0;
    }

    public int GetIntValue(string key)
    {
        object obj = GetValue(key);
        if (obj != null)
            if (obj is int)
                return (int)obj;
        return 0;
    }

    public bool GetBool(string key)
    {
        object obj = GetValue(key);
        if (obj is bool)
            return (bool)obj;
        return false;
    }

    public object GetValue(string key, int offset = 0)
    {
        return GetValue(key, offset, PLCValueSource.ActiveCrane);
    }

    public object GetValue(string key, int offset, PLCValueSource valueSource)
    {
        PlcRuntimeState state;
        lock (runtimeStateLock)
        {
            state = valueSource == PLCValueSource.OppositeCrane ? oppositeState : activeState;
        }

        return GetValueFromState(state, key, offset);
    }

    public object GetOppositeValue(string key, int offset = 0)
    {
        return GetValue(key, offset, PLCValueSource.OppositeCrane);
    }

    private object GetValueFromState(PlcRuntimeState state, string key, int offset)
    {
        if (state == null || !state.configs.TryGetValue(key, out PLCConfig config))
        {
            return null;
        }

        PLCAddress adr = GetPLCAddress(config.DataBlock);

        if (!state.datablocks.TryGetValue(adr.DbNumber, out DataBlockInfo datablockInfo))
        {
            return null;
        }

        switch (config.DataType)
        {
            case "Real":
            case "REAL":
                byte[] arrayReal = ReadValueBytes(datablockInfo, adr.StartByte, offset, 4);
                return arrayReal == null ? null : (object)S7.Net.Types.Real.FromByteArray(arrayReal);
            case "Int":
                byte[] arrayInt = ReadValueBytes(datablockInfo, adr.StartByte, offset, 2);
                return arrayInt == null ? null : (object)(int)S7.Net.Types.Int.FromByteArray(arrayInt);
            case "DInt":
                byte[] arrayDInt = ReadValueBytes(datablockInfo, adr.StartByte, offset, 4);
                return arrayDInt == null ? null : (object)S7.Net.Types.DInt.FromByteArray(arrayDInt);
            case "LInt":
                byte[] arrayLInt = ReadValueBytes(datablockInfo, adr.StartByte, offset, 8);
                return arrayLInt == null ? null : (object)S7.Net.Types.DInt.FromByteArray(arrayLInt);
            case "Byte":
                byte[] arrayByte = ReadValueBytes(datablockInfo, adr.StartByte, offset, 1);
                return arrayByte == null ? null : (object)arrayByte[0];
            case "Bool":
            case "BOOL":
                byte[] arrayBool = ReadValueBytes(datablockInfo, adr.StartByte, offset, 1);
                return arrayBool == null ? null : (object)S7.Net.Types.Boolean.GetValue(arrayBool[0], adr.BitNumber);
            case "Word":
                Debug.LogError("Word");
                break;
            case "DWord":
                Debug.LogError("DWord");
                break;
        }

        return null;
    }

    private byte[] ReadValueBytes(DataBlockInfo datablockInfo, int startByte, int offset, int count)
    {
        byte[] data = Volatile.Read(ref datablockInfo.data);
        if (data == null) return null;

        int startIndex = startByte - datablockInfo.min + offset;
        if (startIndex < 0 || startIndex + count > data.Length)
        {
            return null;
        }

        byte[] value = new byte[count];
        Buffer.BlockCopy(data, startIndex, value, 0, count);
        return value;
    }

    public PLCAddress GetPLCAddress(string datablock)
    {
        lock (plcAddress)
        {
            if (plcAddress.TryGetValue(datablock, out PLCAddress adr))
            {
                return adr;
            }

            adr = new PLCAddress(datablock);
            plcAddress.Add(datablock, adr);
            return adr;
        }
    }

    public object GetValueBit(string key, int offset = 0)
    {
        lock (runtimeStateLock)
        {
            if (!plcConfigs.ContainsKey(key))
            {
                return null;
            }

            PLCAddress adr = GetPLCAddress(plcConfigs[key].DataBlock);
            if (datablockSplit.ContainsKey(adr.DbNumber))
            {
                var datablockInfo = datablockSplit[adr.DbNumber];

                if (datablockInfo.data == null) return null;

                switch (plcConfigs[key].DataType)
                {
                    case "Bool":
                        int byteOffset = (adr.BitNumber + offset) / 8;
                        int bitOffset = (adr.BitNumber + offset) % 8;
                        byte[] arrayBool = ReadValueBytes(datablockInfo, adr.StartByte + byteOffset, 0, 1);
                        return arrayBool == null ? null : (object)S7.Net.Types.Boolean.GetValue(arrayBool[0], bitOffset);
                }
            }

            return null;
        }
    }

    public bool TryGetBool(string key, out bool value, int offset = 0)
    {
        object obj = GetValue(key, offset);
        value = false;

        if (obj != null && obj is bool)
        {
            value = (bool)obj;
            return true;
        }

        return false;
    }

    public bool TryGetInt16(string key, out Int16 value, int offset = 0)
    {
        object obj = GetValue(key, offset);

        if (obj != null && obj is Int16)
        {
            value = (Int16)obj;
            return true;
        }
        value = 0;
        return false;
    }

    public bool TryConnect(PLCConnect connect)
    {
        return EnsureConnected(connect, string.Empty);
    }

    private bool EnsureConnected(PLCConnect connect, string connectKey, bool force = false)
    {
        if (connect == null)
        {
            return false;
        }

        if (connect.IsConnected())
        {
            ResetReconnectState(connect);
            return true;
        }

        if (!autoReconnect && !force)
        {
            return false;
        }

        ReconnectState state = GetReconnectState(connect);
        DateTime now = DateTime.Now;

        lock (state)
        {
            if (state.isConnecting)
            {
                return false;
            }

            if (!force && now < state.nextTryAt)
            {
                return false;
            }

            state.isConnecting = true;
        }

        try
        {
            connect.Close();
            connect.Open();
            ResetReconnectState(connect);
            Debug.Log($"[PLCConfigManager] PLC 已连接: {GetReconnectLogName(connectKey, connect)}");
            return true;
        }
        catch (Exception ex)
        {
            ScheduleReconnect(connect, connectKey, ex.Message);
            return false;
        }
        finally
        {
            lock (state)
            {
                state.isConnecting = false;
            }
        }
    }

    private ReconnectState GetReconnectState(PLCConnect connect)
    {
        lock (reconnectInfo)
        {
            if (!reconnectInfo.TryGetValue(connect, out ReconnectState state))
            {
                state = new ReconnectState();
                reconnectInfo[connect] = state;
            }

            return state;
        }
    }

    private void ResetReconnectState(PLCConnect connect)
    {
        ReconnectState state = GetReconnectState(connect);
        lock (state)
        {
            state.failCount = 0;
            state.nextTryAt = DateTime.MinValue;
        }
    }

    private void ScheduleReconnect(PLCConnect connect, string connectKey, string reason)
    {
        ReconnectState state = GetReconnectState(connect);
        lock (state)
        {
            state.failCount++;
            double interval = Math.Min(maxReconnectInterval, reconnectInterval * Math.Max(1, state.failCount));
            state.nextTryAt = DateTime.Now.AddSeconds(interval);
            Debug.LogWarning($"[PLCConfigManager] PLC 连接失败，{interval:0.#} 秒后重试: {GetReconnectLogName(connectKey, connect)}，原因: {reason}");
        }
    }

    private void MarkDisconnected(PLCConnect connect, string connectKey, string reason)
    {
        if (connect == null)
        {
            return;
        }

        connect.Close();
        ScheduleReconnect(connect, connectKey, reason);
    }

    private string GetReconnectLogName(string connectKey, PLCConnect connect)
    {
        if (!string.IsNullOrWhiteSpace(connectKey))
        {
            return connectKey;
        }

        return $"{connect.ipaddrees}:{connect.port}:{connect.rack}:{connect.slot}";
    }

    private const int MAX_DB_READ = 200;

    private bool SafeReadDbBytes(Plc plc, int dbNumber, int startAddr, int totalCount, byte[] target)
    {
        try
        {
            int remaining = totalCount;
            int offset = 0;

            while (remaining > 0)
            {
                int chunk = Math.Min(MAX_DB_READ, remaining);
                byte[] part = plc.ReadBytes(DataType.DataBlock, dbNumber, startAddr + offset, chunk);
                if (part == null || part.Length != chunk)
                {
                    Debug.LogError($"[READ-SIZE-MISMATCH] DB{dbNumber} start={startAddr + offset} len={chunk} got={(part?.Length ?? -1)}");
                    return false;
                }

                Buffer.BlockCopy(part, 0, target, offset, chunk);
                offset += chunk;
                remaining -= chunk;
            }

            return true;
        }
        catch (S7.Net.PlcException ex)
        {
            Debug.LogError($"[READ-FAIL] DB{dbNumber} start={startAddr} len={totalCount} -> {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[READ-ERR] DB{dbNumber} start={startAddr} len={totalCount} -> {ex}");
            return false;
        }
    }

    public void ReadData()
    {
        PlcRuntimeState activeStateSnapshot;
        PlcRuntimeState oppositeStateSnapshot;
        lock (runtimeStateLock)
        {
            activeStateSnapshot = activeState;
            oppositeStateSnapshot = oppositeState;
        }

        ReadRuntimeData(activeStateSnapshot);
        ReadRuntimeData(oppositeStateSnapshot);
    }

    private void ReadRuntimeData(PlcRuntimeState state)
    {
        if (state == null || state.connects == null || state.datablocks == null)
        {
            return;
        }

        foreach (var kv in state.connects)
        {
            PLCConnect connect = kv.Value;
            if (!EnsureConnected(connect, kv.Key))
            {
                continue;
            }

            lock (connect.SyncRoot)
            {
                var plc = connect.GetPlc();
                if (plc == null || !plc.IsConnected)
                {
                    MarkDisconnected(connect, kv.Key, "PLC 状态为未连接");
                    continue;
                }

                foreach (var db in state.datablocks)
                {
                    int dbNumber = db.Key;
                    int startAddr = db.Value.min;
                    int count = db.Value.max - db.Value.min;

                    if (count <= 0) continue;

                    byte[] data = new byte[count];

                    bool ok = SafeReadDbBytes(plc, dbNumber, startAddr, count, data);
                    if (!ok)
                    {
                        MarkDisconnected(connect, kv.Key, $"读取 DB{dbNumber} 失败");
                        break;
                    }

                    Volatile.Write(ref db.Value.data, data);
                }
            }

            break;
        }
    }

    public void ClearData()
    {
    }

    public int GetBitSize(string type)
    {
        switch (type)
        {
            case "Bit":
            case "Bool":
                return 1;
            case "Byte":
                return 8;
            case "Word":
                return 16;
            case "DWord":
                return 32;
            case "Int":
                return 16;
            case "DInt":
                return 32;
            case "Real":
                return 32;
            case "LReal":
                return 64;
            case "LInt":
                return 64;
        }
        return 0;
    }
}
