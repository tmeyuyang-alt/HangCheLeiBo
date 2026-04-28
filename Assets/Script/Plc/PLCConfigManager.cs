using S7.Net;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

public class PLCConfigManager : MonoBehaviour
{
    //public static PLCConfigManager Instance;

    public string configName = "plc2.config";
    public string deviceSignalJsonName = "DeviceSignalConfigs.json";
    public int defaultPort = 102;
    public short defaultRack = 0;
    public short defaultSlot = 1;
    
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

    // ========= 鈶?鏂板 =========
    private class ReconnectState {
        public int failCount = 0;
        public DateTime nextTryAt = DateTime.MinValue;
    }
    private readonly Dictionary<PLCConnect, ReconnectState> reconnectInfo
        = new Dictionary<PLCConnect, ReconnectState>();


    public Dictionary<string, PLCConfig> plcConfigs = new Dictionary<string, PLCConfig>();

    public Dictionary<string, PLCAddress> plcAddress = new Dictionary<string, PLCAddress>();
    /// <summary>
    /// PLC璋呰В鍏?
    /// </summary>
    public Dictionary<string, PLCConnect> plcConnectDic = new Dictionary<string, PLCConnect>();

    public Dictionary<PLCConnect, System.DateTime> plcTryConnect = new Dictionary<PLCConnect, DateTime>();


    public ConcurrentDictionary<int, DataBlockInfo> datablockSplit = new ConcurrentDictionary<int, DataBlockInfo>();
   
    public static System.Action OnUpdateUI;
    public static System.Action OnUpdate;

    public Dictionary<string, PLCConfig> plcConfigsTmp = new Dictionary<string, PLCConfig>();

    public class DataBlockInfo
    {
        public int max;
        public int min;
        public byte[] data;
    }
    [ContextMenu("Write")]
    public void WriteConfig()
    {
        string excelPath = Application.streamingAssetsPath + "/config.xlsx";
        string configPath = Application.streamingAssetsPath + "/"+configName;
        FileInfo fileInfo = new FileInfo(excelPath);
        plcConfigsTmp = new Dictionary<string, PLCConfig>();
        using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
        {
          
            ExcelWorksheet workSheet = excelPackage.Workbook.Worksheets[1];
            for (int i = 2; i < workSheet.Dimension.End.Row+1; i++)// 寰幆璇诲彇绗?-3琛屾暟鎹?
            {
                string key=workSheet.Cells[i, 1].Value.ToString()+ workSheet.Cells[i, 2].Value.ToString();
                plcConfigsTmp.Add(key, new PLCConfig { IPAddress = "192.168.10.55", Port = 102,Slot=1, DataBlock = workSheet.Cells[i, 3].Value.ToString(), DataType = workSheet.Cells[i, 4].Value.ToString(),ShortName =workSheet.Cells[i, 7].Value.ToString() });
                DataUtil.Serializer<Dictionary<string, PLCConfig>>(configPath, plcConfigsTmp);

            }

        }
    }
    private void Awake()
    {
        //Instance = this;

        if (!TryLoadConfigsFromJson())
        {
            string path = Application.streamingAssetsPath + "/" + configName;
            if (File.Exists(path))
            {
                plcConfigs = DataUtil.Deserializer<Dictionary<string, PLCConfig>>(path);
            }
            else
            {
                plcConfigs = new Dictionary<string, PLCConfig>();
            }
        }

        InitializeRuntimeState();

        ReadData();
        Thread thread = new Thread(ReadThread);
        thread.IsBackground = true;
        thread.Start();

    }

    private bool TryLoadConfigsFromJson()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, deviceSignalJsonName);
        if (!File.Exists(jsonPath))
        {
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
            Dictionary<string, PLCConfig> loadedConfigs = new Dictionary<string, PLCConfig>();

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

                    if (loadedConfigs.ContainsKey(key))
                    {
                        Debug.LogWarning($"[PLCConfigManager] 检测到重复配置键，后一个将覆盖前一个: {key}");
                    }

                    loadedConfigs[key] = config;
                }
            }

            if (loadedConfigs.Count == 0)
            {
                Debug.LogWarning($"[PLCConfigManager] JSON 中没有可用的点位配置: {jsonPath}");
                return false;
            }

            plcConfigs = loadedConfigs;
            Debug.Log($"[PLCConfigManager] 已从 JSON 加载 {plcConfigs.Count} 条 PLC 配置: {jsonPath}");
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

    private void InitializeRuntimeState()
    {
        plcAddress.Clear();
        plcConnectDic.Clear();
        plcTryConnect.Clear();
        datablockSplit.Clear();

        foreach (var config in plcConfigs)
        {
            if (config.Value == null || string.IsNullOrWhiteSpace(config.Value.DataBlock))
            {
                continue;
            }

            string key = GetConfigConnectKey(config.Value);
            if (!plcConnectDic.ContainsKey(key))
            {
                PLCConnect connect = new PLCConnect();
                connect.ipaddrees = config.Value.IPAddress;
                connect.port = config.Value.Port;
                connect.rack = config.Value.Rack;
                connect.slot = config.Value.Slot;
                plcConnectDic.Add(key, connect);
            }

            if (!plcConnectDic[key].IsConnected())
            {
                plcConnectDic[key].Open();
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
                if (datablockSplit.ContainsKey(addr.DbNumber))
                {
                    datablockSplit[addr.DbNumber].max = Mathf.Max(addr.StartByte + byteSize, datablockSplit[addr.DbNumber].max);
                    datablockSplit[addr.DbNumber].min = Mathf.Min(addr.StartByte, datablockSplit[addr.DbNumber].min);
                }
                else
                {
                    datablockSplit.TryAdd(addr.DbNumber, new DataBlockInfo());
                    datablockSplit[addr.DbNumber].max = addr.StartByte + byteSize;
                    datablockSplit[addr.DbNumber].min = addr.StartByte;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PLCConfigManager] 地址解析失败，key={config.Key}, address={config.Value.DataBlock}\n{ex}");
            }
        }
    }

    public string GetDBAddrByStringKey(string arg)
    {
        string addr = null;
        foreach (var item in plcConfigs)
        {
            if (item.Key==arg)
            {
                addr = item.Value.DataBlock;
            }
        }
        //print(arg+"====="+ addr);
        return addr;
    }

    public void ReadThread()
    {
        while (true)
        {
            Thread.Sleep(1000 / 15);

             ReadData();
        }
    }

    private float timer = 9999;

    public float checkNetTimr = 0;

    public void NetCheck()
    {
//print(plcConnectDic.Count);
        if (plcConnectDic.Count>0)
        {
            foreach (var VARIABLE in plcConnectDic)
            { 
                
               // VARIABLE.Value.GetPlc().isa
                if (!VARIABLE.Value.IsConnected())
                {
                    //VARIABLE.Value.GetPlc().IsConnected
                    VARIABLE.Value.Open();
                }
            }
        }
       
    }
    
    private void Update()
    {
      
        // checkNetTimr += Time.deltaTime;
        // if (checkNetTimr > 3)
        // { 
        //     checkNetTimr = 0;
        //    NetCheck();
        //   
        // }
        //Update
        timer += Time.deltaTime;
        if (timer > 0.016666f)
        {
            timer = 0.0f;

            if (OnUpdate != null)
            {
                OnUpdate.Invoke();
                OnUpdateUI.Invoke();
            }

        }
        //print("PLC");
    }

    public Plc GetPlc(string key)
    {
       

        string connectKey = string.Empty;

        if (plcConfigs.ContainsKey(key))
        {
            connectKey = GetConfigConnectKey(plcConfigs[key]);

        }
        var plcConnect = GetPLCConnect(connectKey);


        return plcConnect.GetPlc(); 
    }

    public string GetShortName(string key)
    {
        string shortName = string.Empty;
        foreach (var item in plcConfigs)
        {
            if (item.Key == key)
            {
                shortName = item.Value.ShortName;
                if (shortName=="")
                {
                    shortName = key;
                }
            }
        }

        return shortName;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key">IP鍦板潃:绔彛:Rack:Solt</param>
    /// <returns></returns>
    public PLCConnect GetPLCConnect(string key)
    {
        if (plcConnectDic.ContainsKey(key))
        {
            return plcConnectDic[key];
        }
        return null;
    }
    public async void SetValue(string key,object value)
    {
        print(key);
        string connectKey = string.Empty;

        if (plcConfigs.ContainsKey(key))
        {
            connectKey = GetConfigConnectKey(plcConfigs[key]);

        }
        var plcConnect = GetPLCConnect(connectKey);

        // if (plcConnect != null)
        // {
        //     if (!TryConnect(plcConnect)) return;
        // }
        // else
        // {
        //     Debug.LogError("PLC鑾峰彇澶辫触锛?);
        //     return;
        // }
         plcConnect.Write(plcConfigs[key].DataBlock,value,key+"设置值为："+value);

         await Task.Delay(500);
         
         if (value is bool)
         {
             value = false;
             //plcConnect.Write(plcConfigs[key].DataBlock,value,(GetShortName(key)+"设置值为："+value));
         }

         PopCtrl.Instance.ShowPop(GetShortName(key) + "设定成功");
         
    }
    public async void SetValueConfirm(string key,object value)
    {
        print(key);
        string connectKey = string.Empty;

        if (plcConfigs.ContainsKey(key))
        {
            connectKey = GetConfigConnectKey(plcConfigs[key]);

        }
        var plcConnect = GetPLCConnect(connectKey);

        // if (plcConnect != null)
        // {
        //     if (!TryConnect(plcConnect)) return;
        // }
        // else
        // {
        //     Debug.LogError("PLC鑾峰彇澶辫触锛?);
        //     return;
        // }
        plcConnect.WriteNoLog(plcConfigs[key].DataBlock,value);

        await Task.Delay(500);
         
        if (value is bool)
        {
            value = false;
            //plcConnect.Write(plcConfigs[key].DataBlock,value,(GetShortName(key)+"设置值为："+value));
        }

        PopCtrl.Instance.ShowPop(GetShortName(key) + "设定成功");
         
    }
    public async void SetValueNoNotify(string key,object value)
    {
        print(key);
        string connectKey = string.Empty;

        if (plcConfigs.ContainsKey(key))
        {
            connectKey = GetConfigConnectKey(plcConfigs[key]);

        }
        var plcConnect = GetPLCConnect(connectKey);

        // if (plcConnect != null)
        // {
        //     if (!TryConnect(plcConnect)) return;
        // }
        // else
        // {
        //     Debug.LogError("PLC鑾峰彇澶辫触锛?);
        //     return;
        // }
        plcConnect.Write(plcConfigs[key].DataBlock,value);

        await Task.Delay(500);
         
        if (value is bool)
        {
            value = false;
            plcConnect.Write(plcConfigs[key].DataBlock,value);
        }

        //PopCtrl.Instance.ShowPop(GetShortName(key) + "璁惧畾鎴愬姛");
    }
   public async void SetValue(string key, object value, bool isKeep = false)
{
    if (!plcConfigs.ContainsKey(key))
    {
        Debug.LogError($"[WRITE] 閰嶇疆缂哄け: {key}");
        return;
    }

    string connectKey = GetConfigConnectKey(plcConfigs[key]);
    var conn = GetPLCConnect(connectKey);
    if (conn == null)
    {
        Debug.LogError($"[WRITE] 鏈壘鍒拌繛鎺? {connectKey}");
        return;
    }

    // 纭繚宸茶繛鎺ワ紙鍚屾鎵撳紑锛岄伩鍏嶁€滄病涓嬪彂鈥濓級
    if (!conn.IsConnected())
    {
        try { conn.Open(); }
        catch (Exception e)
        {
            Debug.LogError($"[WRITE] 杩炴帴澶辫触: {connectKey} 鈻?{e.Message}");
            return;
        }
    }

    try
    {
        var adr = GetPLCAddress(plcConfigs[key].DataBlock);
        Debug.Log($"[WRITE] DB{adr.DbNumber} start={adr.StartByte} bit={adr.BitNumber} type={plcConfigs[key].DataType} value={value}");

        conn.Write(plcConfigs[key].DataBlock, value,key);

        if (!isKeep && value is bool)
        {
            await Task.Delay(300);
            conn.Write(plcConfigs[key].DataBlock, false);
        }

        PopCtrl.Instance?.ShowPop(GetShortName(key) + "设定成功");
    }
    catch (S7.Net.PlcException ex)
    {
        Debug.LogError($"[WRITE-FAIL] key={key} 鈻?{ex.Message}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[WRITE-ERR] key={key} 鈻?{ex}");
    }
}

    public void SetValue(string key, int offset, object value)
    {
        string connectKey = string.Empty;
        if (plcConfigs.ContainsKey(key))
        {
            connectKey = GetConfigConnectKey(plcConfigs[key]);

        }
        var plcConnect = GetPLCConnect(connectKey);

        if (plcConnect != null)
        {
            if (!TryConnect(plcConnect)) return;
        }
        else
        {
            //Debug.LogError("PLC鑾峰彇澶辫触锛?);
            return;
        }
        PLCAddress adr = GetPLCAddress(plcConfigs[key].DataBlock);
        plcConnect.Write(adr.DataType, adr.DbNumber, adr.StartByte+offset, value, adr.BitNumber);
    }

    public void SetValueBit(string key, int offset, object value)
    {
        string connectKey = string.Empty;
        if (plcConfigs.ContainsKey(key))
        {
            connectKey = GetConfigConnectKey(plcConfigs[key]);

        }
        var plcConnect = GetPLCConnect(connectKey);

        if (plcConnect != null)
        {
            if (!TryConnect(plcConnect)) return;
        }
        else
        {
            //Debug.LogError("PLC鑾峰彇澶辫触锛?);
            return;
        }

        PLCAddress adr = GetPLCAddress(plcConfigs[key].DataBlock);

        int ByteOffset = (adr.BitNumber + offset) / 8;
        int bitOffset =  (adr.BitNumber + offset) % 8;

        plcConnect.Write(adr.DataType, adr.DbNumber, adr.StartByte + ByteOffset, value,  bitOffset);
    }
    
    public   float GetFloatValue(string dateblock)
    {
        object obj = GetValue(dateblock);
        //print("GetFloat---" + obj);
        if (obj is float)
            return ((float)obj);
        return 0;
    }
    public int GetIntValue(string dateblock)
    {
        object obj = GetValue(dateblock); 
        if (obj != null)
            // print("GetINT---" + (int)obj);
            if (obj is int)
                return ((int)obj);
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
        if (!plcConfigs.ContainsKey(key))
        {
            print(key);
            return null;
        }
        
        PLCAddress adr = GetPLCAddress(plcConfigs[key].DataBlock);

        // if (key == "淇″彿灏忚溅B姝ｄ綅缃?)
        // {
        //    // print(adr.DbNumber+"-"+adr.BitNumber);
        // }
        //鍒ゆ柇鏈夋病鏈夋暟鎹?
        if (datablockSplit.ContainsKey(adr.DbNumber))
        {
            //print("T1");
           var datablockInfo = datablockSplit[adr.DbNumber];

            if (datablockInfo.data == null) return null;

            switch (plcConfigs[key].DataType)
            {
                case "Real":
                    byte[] array = new byte[4];

                    for (int i = 0; i < 4; i++)
                        array[i] = datablockInfo.data[adr.StartByte - datablockInfo.min + offset + i];

                    float retReal = S7.Net.Types.Real.FromByteArray(array);

                    return retReal;

                case "REAL":
                    byte[] array2 = new byte[4];

                    for (int i = 0; i < 4; i++)
                        array2[i] = datablockInfo.data[adr.StartByte - datablockInfo.min + offset + i];

                    float retReal2 = S7.Net.Types.Real.FromByteArray(array2);

                    return retReal2;
                case "Int":
                    byte[] arrayInt = new byte[2];

                    for (int i = 0; i < 2; i++)
                        arrayInt[i] = datablockInfo.data[adr.StartByte - datablockInfo.min + offset + i];

                    int retInt = S7.Net.Types.Int.FromByteArray(arrayInt);

                    return retInt;
                case "DInt":
                    byte[] arrayDInt = new byte[4];

                    for (int i = 0; i < 4; i++)
                        arrayDInt[i] = datablockInfo.data[adr.StartByte - datablockInfo.min + offset + i];

                    int retDInt = S7.Net.Types.DInt.FromByteArray(arrayDInt);
                    return retDInt;
                case "LInt":
                    byte[] arrayLInt = new byte[8];

                    for (int i = 0; i < 8; i++)
                        arrayLInt[i] = datablockInfo.data[adr.StartByte - datablockInfo.min + offset + i];

                    int retLInt = S7.Net.Types.DInt.FromByteArray(arrayLInt);
                    return retLInt;
                case "Byte":
                    byte retByt;

                    retByt = datablockInfo.data[adr.StartByte + offset - datablockInfo.min];

                    return retByt;
                case "Bool":
                    try
                    {
                        byte byt = datablockInfo.data[adr.StartByte + offset - datablockInfo.min];

                        bool retBool = S7.Net.Types.Boolean.GetValue(byt, adr.BitNumber);
                        return retBool;
                    }catch{
                    
                    }
                    return false;
                case "BOOL":
                    try
                    {
                        byte byt = datablockInfo.data[adr.StartByte + offset - datablockInfo.min];

                        bool retBool = S7.Net.Types.Boolean.GetValue(byt, adr.BitNumber);
                        return retBool;
                    }
                    catch
                    {

                    }
                    return false;
                case "Word":
                    Debug.LogError("Word");
                    break;

                case "DWord":
                    Debug.LogError("DWord");
                    break;
            }
        }
        return null;
    }

    public PLCAddress GetPLCAddress(string datablock)
    {
        PLCAddress adr = null;
        if (plcAddress.ContainsKey(datablock))
        {
            adr = plcAddress[datablock];
        }
        else
        {
            adr = new PLCAddress(datablock);
            plcAddress.Add(datablock, adr);
        }
        return adr;
    }
    public object GetValueBit(string key, int offset = 0)
    {
        PLCAddress adr = GetPLCAddress(plcConfigs[key].DataBlock);
        //鍒ゆ柇鏈夋病鏈夋暟鎹?
        if (datablockSplit.ContainsKey(adr.DbNumber))
        {
            var datablockInfo = datablockSplit[adr.DbNumber];

            if (datablockInfo.data == null) return null;

            //TODO锛氬垽鏂被鍨?骞惰浆鍖栫被鍨?
            switch (plcConfigs[key].DataType)
            {
                case "Bool":

                    int ByteOffset = (adr.BitNumber + offset) / 8;
                    int bitOffset = (adr.BitNumber + offset) % 8;

                    byte byt = datablockInfo.data[adr.StartByte + ByteOffset - datablockInfo.min];

                    bool ret = S7.Net.Types.Boolean.GetValue(byt, bitOffset);

                    return ret;
            }
        }
        return null;
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
        if (connect.IsConnected()) return true;

        if (plcTryConnect.ContainsKey(connect))
        {
            if ((System.DateTime.Now - plcTryConnect[connect]).TotalSeconds > 3.0f)
            {
                plcTryConnect[connect] = System.DateTime.Now;
                connect.OpenAsync();
                return false;
            }
        }
        else {
            plcTryConnect.Add(connect, System.DateTime.Now);
            connect.OpenAsync();
        }
        return false;
    }

 // 姣忔鏈€澶ц鍙栧瓧鑺傦紙鎸?S7-300/1200/1500 鐨勫父瑙?PDU 鍙?200锛屽繀瑕佹椂鍙皟鍒?222锛?
private const int MAX_DB_READ = 200;

// 缁熶竴鐨勫畨鍏ㄨ鍙栵細鑷姩鍒嗙墖銆佹嫹璐濆埌鐩爣缂撳啿鍖恒€佸甫璇︾粏鎶ラ敊
private bool SafeReadDbBytes(Plc plc, int dbNumber, int startAddr, int totalCount, byte[] target)
{
    try
    {
        int remaining = totalCount;
        int offset = 0;

        while (remaining > 0)
        {
            int chunk = Math.Min(MAX_DB_READ, remaining);
            // 鍏抽敭锛氭瘡娆′粠 (startAddr + offset) 璇?chunk 瀛楄妭
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
        Debug.LogError($"[READ-FAIL] DB{dbNumber} start={startAddr} len={totalCount} 鈻?{ex.Message}");
        return false;
    }
    catch (Exception ex)
    {
        Debug.LogError($"[READ-ERR] DB{dbNumber} start={startAddr} len={totalCount} 鈻?{ex}");
        return false;
    }
}

// 鐩存帴鏇挎崲鍘熸潵鐨?ReadData()
public void ReadData()
{
    foreach (var kv in plcConnectDic)
    {
        var plc = kv.Value.GetPlc();
        if (plc == null || !plc.IsConnected) continue;

        foreach (var db in datablockSplit)
        {
            int dbNumber  = db.Key;
            int startAddr = db.Value.min;
            int count     = db.Value.max - db.Value.min;

            if (count <= 0) continue;

            if (db.Value.data == null || db.Value.data.Length != count)
                db.Value.data = new byte[count];

            // 鏍稿績锛氬缁堣蛋瀹夊叏鍒嗙墖
            bool ok = SafeReadDbBytes(plc, dbNumber, startAddr, count, db.Value.data);
            if (!ok)
            {
                // 宸叉湁璇︾粏鏃ュ織锛岃繖閲屽彲瑙嗛渶瑕佺户缁鐞?
            }
        }

        // 浣犲師鏉ヨ繖閲屽彧璇荤涓€鍙?PLC锛岃繖閲屼繚鎸佷竴鑷?
        break;
    }
}


    public void ClearData()
    {
  
    }

    //public int GetBitSize(string datablock)
    //{
    //    if (plcConfigs.ContainsKey(datablock))
    //    {
    //        return GetBitSize(plcConfigs[datablock].DataType);
    //    }
    //    return 0;
    //}
    public int GetBitSize(string type)
    {
        //print("type----"+type);
        switch (type)
        {
            /// <summary>
            /// S7 Bit variable type (bool)
            /// </summary>
            case "Bit":
            case "Bool":
                return 1;
            /// <summary>
            /// S7 Byte variable type (8 bits)
            /// </summary>
            case "Byte":
                return 8;

            /// <summary>
            /// S7 Word variable type (16 bits, 2 bytes)
            /// </summary>
            case "Word":
                return 16;

            /// <summary>
            /// S7 DWord variable type (32 bits, 4 bytes)
            /// </summary>
            case "DWord":
                return 32;

            /// <summary>
            /// S7 Int variable type (16 bits, 2 bytes)
            /// </summary>
            case "Int":
                return 16;
            /// <summary>
            /// DInt variable type (32 bits, 4 bytes)
            /// </summary>
            case "DInt":
                return 32;
            /// <summary>
            /// Real variable type (32 bits, 4 bytes)
            /// </summary>
            case "Real":
                return 32;
            /// <summary>
            /// LReal variable type (64 bits, 8 bytes)
            /// </summary>
            case "LReal":
                return 64;

            case "LInt":
                return 64;
        }
       // Debug.LogError("鏈畾涔夊ぇ灏?);
        return 0;
    }
}
//public enum DataBlockKey
//{
//    None = 0,
//    /// <summary>
//    /// 鍚姩
//    /// </summary>
//    Start = 1,
//    /// <summary>
//    /// 鍋滄
//    /// </summary>
//    StopStatus,
//    /// <summary>
//    /// 鏆傚仠
//    /// </summary>
//    PauseStatus,
//    /// <summary>
//    /// 缁х画
//    /// </summary>
//    Continue,
//    /// <summary>
//    /// 鎶撴枟閲嶉噺
//    /// </summary>
//    CrabBucketWeight,
//    /// <summary>
//    /// 婕忔枟A閲嶉噺
//    /// </summary>
//    FunnelAWeight,
//    /// <summary>
//    /// 婕忔枟B閲嶉噺
//    /// </summary>
//    FunnelBWeight,

//    //****鏁呴殰鐘舵€?***

//    //****澶у皬杞︾數鏈虹數娴?***
//    BigCarMotorCurrent,

//    SmallCarMotorCurrent,

//    BigCarFrequency,

//    SmallCarFrequency,

//    LiftCurrent,

//    OpenCloseCurrent,

//    LiftFrequency,

//    OpenCloseFrequency,

//    //Hopper1Weight,

//    //Hopper2Weight,

//    LiftTransducer,

//    OpenCloseTransducer,

//    SmallCarTransducer,

//    BigCarTransducer,

//    //寮€濮嬭缃?
//    CoverPlateCurrentSetStart,
//    CoverPlateTimeSetStart,

//    /// <summary>
//    /// 宸﹀彸琛岀▼
//    /// </summary>
//    XAxis,
//    /// <summary>
//    /// 鍓嶅悗琛岀▼
//    /// </summary>
//    ZAxis,
//    /// <summary>
//    /// 涓婁笅琛岀▼
//    /// </summary>
//    YAxis,

//    LeftLimit,

//    RightLimit,

//    ForwardLimit,

//    BackLimit,

//    TopLimit,

//    //BottomLimit,
//    /// <summary>
//    /// 鎶撴枟鎵撳紑
//    /// </summary>
//    CrabBucketOpen,
//    /// <summary>
//    /// 鎶撴枟鍏抽棴
//    /// </summary>
//    CrabBucketClose,

//    /// <summary>
//    /// 鎶撴福姹犺捣濮嬪湴鍧€
//    /// </summary>
//    SlagPoolStart,

//    /// <summary>
//    /// 鎵撳紑鐩栨澘璧峰鍦板潃
//    /// </summary>
//    OpenCoverPlateStart,
//    /// <summary>
//    /// 鍏抽棴鐩栨澘璧峰鍦板潃
//    /// </summary>
//    CloseCoverPlateStart,
//    /// <summary>
//    /// 鐩栨澘鐘舵€?
//    /// </summary>
//    CoverPlateStatusStart,
//    /// <summary>
//    /// 灏卞湴鎿嶄綔
//    /// </summary>
//    LocalOperateStatus,
//    /// <summary>
//    /// 杩滅▼鎿嶄綔
//    /// </summary>
//    Remote1OperateStatus,
//    /// <summary>
//    /// 閬ユ帶鎿嶄綔
//    /// </summary>
//    Remote2OperateStatus,

//    AutoControlModel,
//    ManualControlModel,

//    /// <summary>
//    /// 鎿嶄綔鐘舵€?
//    /// </summary>
//    ControlStatus,
//    /// <summary>
//    /// 杩愯鐘舵€?
//    /// </summary>
//    RunStatus,

//    /// <summary>
//    /// 鐩栨澘鐢垫満鐢垫祦
//    /// </summary>
//    CoverPlateMotorCurrent,
//    /// <summary>
//    /// 鎶撴福椤哄簭
//    /// </summary>
//    SlagPoolOrderStart,
//    /// <summary>
//    /// 鏂欐枟浣嶇疆
//    /// </summary>
//    HopperPos,
//    /// <summary>
//    /// 娓呯┖
//    /// </summary>
//    ClearSlagPool,
//    CalibrationX,
//    CalibrationY,
//    CalibrationZ,
//    //鎻愮怀
//    LiftStartRope,
//    LiftEndRope,
//    //鎶撶怀
//    GraspStartRope,
//    GraspEndRope,

//    //鎻愮怀
//    LiftStartRopeFault,
//    LiftEndRopeFault,
//    //鎶撶怀
//    GraspStartRopeFault,
//    GraspEndRopeFault,
//    /// <summary>
//    /// 澶ц溅鏍囩
//    /// </summary>
//    BigCarLabelStatusStart,
//    /// <summary>
//    /// 鏁ｇ儹鍣ㄩ鏈虹數娴?
//    /// </summary>
//    HeatSinkFunCurrent,
//    /// <summary>
//    /// 鐢垫満椋庢満鐢垫祦
//    /// </summary>
//    ResistanceFunCurrent,
//    /// <summary>
//    /// 鏂伴椋庢満鐢垫祦
//    /// </summary>
//    FreshAirFanCurrent,
//    /// <summary>
//    /// 鏁ｇ儹鍣ㄩ鏈烘晠闅?
//    /// </summary>
//    HeatSinkFunFault,
//    /// <summary>
//    /// 鐢垫満椋庢満鏁呴殰
//    /// </summary>
//    ResistanceFunFault,
//    /// <summary>
//    /// 鏂伴椋庢満鏁呴殰
//    /// </summary>
//    FreshAirFanFault,
//    /// <summary>
//    /// PLC瀛愮珯
//    /// </summary>
//    PLCChildStoodStatus,
//    /// <summary>
//    /// 鏂伴椋庢満鎶曞叆
//    /// </summary>
//    FreshAirFanInput,

//    /// <summary>
//    /// 鎻愮怀涓婁紶鎰熷櫒
//    /// </summary>
//    LiftRopeUpSensor,
//    /// <summary>
//    /// 鎻愮怀涓嬩紶鎰熷櫒
//    /// </summary>
//    LiftRopeDownSensor,
//    /// <summary>
//    /// 鎶撶怀涓婁紶鎰熷櫒
//    /// </summary>
//    GraspRopeUpSensor,
//    /// <summary>
//    /// 鎶撶怀涓嬩紶鎰熷櫒
//    /// </summary>
//    GraspRopeDownSensor,
//    /// <summary>
//    /// 浼犳劅鍣ㄦ晠闅?
//    /// </summary>
//    RopeSensorFault,
//}
