using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class StaticConfig
//{

//    public string PlcType = "S7200";
//    public string PlcIPAddress = "127.0.0.1:102";
//    public string PlcRack = "0";
//    public string PlcSlot = "0";
//    /// <summary>
//    /// 上班电量
//    /// </summary>
//    public string LastWorkElectricity = "";
//    /// <summary>
//    /// 本班电流
//    /// </summary>
//    public string CurWorkElectricity = "";
//    /// <summary>
//    /// 设置电流
//    /// </summary>
//    public string SetWorkElectricity = "";

//    public string OntologyOilLevel = "";


//    /// <summary>
//    /// 升档
//    /// </summary>
//    public string UpShift = "";
//    /// <summary>
//    /// 降档
//    /// </summary>
//    public string DownShift = "";
//    /// <summary>
//    /// 档位
//    /// </summary>
//    public string Gears = "";

//    public string ManualMode = "";

//    public string AutomaticMode = "";
//    /// <summary>
//    /// 分合闸
//    /// </summary>
//    public string SwitchStatus = "";
//    /// <summary>
//    /// 远程状态
//    /// </summary>
//    public string RemoteStatus = "";
//}
public class StaticConfig
{
    public Dictionary<string, StaticPlcConfig> staticConfigs;
    public void CreateDefult()
    {
        staticConfigs = new Dictionary<string, StaticPlcConfig>();

        StaticPlcConfig plcConfigElectricity = new StaticPlcConfig();
        plcConfigElectricity.DataBlocks.Add("LastWorkElectricity", "BD1.DBD1");
        plcConfigElectricity.DataBlocks.Add("CurWorkElectricity", "BD1.DBD1");
        plcConfigElectricity.DataBlocks.Add("SetWorkElectricity", "BD1.DBD1");
        staticConfigs.Add("Electricity", plcConfigElectricity);



        StaticPlcConfig plcConfigGears = new StaticPlcConfig();
        //plcConfigGears.DataBlocks.Add("UpShift", "BD1.DBD1");
        //plcConfigGears.DataBlocks.Add("DownShift", "BD1.DBD1");
        plcConfigGears.DataBlocks.Add("Gears", "BD1.DBD1");
        staticConfigs.Add("Gears", plcConfigGears);

        StaticPlcConfig plcConfigSystemMode = new StaticPlcConfig();
        plcConfigSystemMode.DataBlocks.Add("AutomaticMode", "BD1.DBD1");
        //plcConfigSystemMode.DataBlocks.Add("ManualMode", "BD1.DBD1");
        plcConfigSystemMode.DataBlocks.Add("SwitchStatus", "BD1.DBD1");
        plcConfigSystemMode.DataBlocks.Add("RemoteStatus", "BD1.DBD1");
        staticConfigs.Add("SystemMode", plcConfigSystemMode);

    }
}


public class StaticPlcConfig
{
    public string PlcType = "S7200";
    public string PlcIPAddress = "127.0.0.1:102";
    public string PlcRack = "0";
    public string PlcSlot = "0";
    public Dictionary<string, string> DataBlocks = new Dictionary<string, string>();
}
