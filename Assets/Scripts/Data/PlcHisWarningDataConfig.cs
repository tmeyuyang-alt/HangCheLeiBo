using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlcHisWarningDataConfig
{
    public List<HisWarningDataConfigData> Config;

    public PlcHisWarningDataConfig()
    {
        Config = new List<HisWarningDataConfigData>();
    }
    public void Save()
    {
        string path = Application.streamingAssetsPath + "/hiswarning_data_config.yaml";
        DataUtil.Serializer<PlcHisWarningDataConfig>(path, this);
    }
}

public class HisWarningDataConfigData
{
    public string IPAddress = "127.0.0.1:102";
    public string PlcType = "S7400";
    public short Rack = 0;
    public short Slot = 0;
    public string WarningName;
    public int WarningGroup;
    public string LimitValueDB = string.Empty;
    public string WarningSignalDB = string.Empty;
}