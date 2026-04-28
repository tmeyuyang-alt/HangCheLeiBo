using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalInfo
{
    public const string otherPlcDataConfig = "OtherPlcDataConfig";

    //FirstData             //一次侧数据
    //SecondaryData         //二次侧数据
    //VoltageData           //电极电压数据
    //ElectricityData       //电极电流数据
    //ElectricFurnaceTempe  //电炉温度

    public static string publicKey = string.Empty;
    public static UserDTO user;

    /// <summary>
    /// 一次侧数据
    /// </summary>
    public static PlcFirstDataConfig       m_FirstData             = null;
    /// <summary>
    /// 二次侧数据
    /// </summary>
    public static PlcSecondaryDataConfig   m_SecondaryData         = null;
    /// <summary>
    /// 电极电压数据
    /// </summary>
    public static PlcVoltageDataConfig     m_VoltageData           = null;
    /// <summary>
    /// 电极电流数据
    /// </summary>
    public static PlcElectricityDataConfig m_ElectricityData       = null;
    /// <summary>
    /// 电炉温度
    /// </summary>
    public static PlcElecFurnaceDataConfig m_ElectricFurnaceTempe  = null;
    /// <summary>
    /// 电极参数配置
    /// </summary>
    public static PlcElectrodeDataConfig m_ElectrodeData = null;

    /// <summary>
    /// 电极倾斜数据配置
    /// </summary>
    public static PlcElectrodeTiltDataConfig m_ElectrodeTiltData = null;

    /// <summary>
    /// 3D面板的的报警数据
    /// </summary>
    public static PlcAlarmStatus3DConfig m_AlarmStatus3D = null;

    //public static PlcHisWarningDataConfig m_HisWarningData = null;

    public static StaticConfig m_StaticConfig = null;

    public static PlcHisWarningDataConfig m_HisWarningData = null;

    //配置

    //public static Dictionary<string, PlcDataConfig> m_PlcParamDataDict = null;

    [RuntimeInitializeOnLoadMethod]
    public static void Inital()
    {
        //加载配置文件
        string root = Application.streamingAssetsPath;

        m_FirstData = LoadConfig<PlcFirstDataConfig>(root + "/first_data_config.yaml");
        m_SecondaryData = LoadConfig<PlcSecondaryDataConfig>(root + "/secondary_data_config.yaml");
        m_VoltageData = LoadConfig<PlcVoltageDataConfig>(root + "/voltage_data_config.yaml");
        m_ElectricityData = LoadConfig<PlcElectricityDataConfig>(root + "/electricity_data_config.yaml");
        m_ElectricFurnaceTempe = LoadConfig<PlcElecFurnaceDataConfig>(root + "/elecfurnace_temp_data_config.yaml");
        m_ElectrodeData = LoadConfig<PlcElectrodeDataConfig>(root + "/electrode_data_config.yaml");
        m_AlarmStatus3D = LoadConfig<PlcAlarmStatus3DConfig>(root + "/alarm_status_data_config.yaml");
        m_ElectrodeTiltData = LoadConfig<PlcElectrodeTiltDataConfig>(root + "/electricity_tilt_data_config.yaml");

        //静态配置
        m_StaticConfig = LoadStaticConfig(root + "/staticConfig.yaml");

        string hiswarning_path = Application.streamingAssetsPath + "/hiswarning_data_config.yaml";

        if (System.IO.File.Exists(hiswarning_path))
            m_HisWarningData = DataUtil.Deserializer<PlcHisWarningDataConfig>(hiswarning_path);
        else
            m_HisWarningData = new PlcHisWarningDataConfig();
    }

    public static StaticConfig LoadStaticConfig(string path)
    {
        if (System.IO.File.Exists(path))
        {
            return DataUtil.Deserializer<StaticConfig>(path);
        }
        else
        {
            var staticConfig = new StaticConfig();

            DataUtil.Serializer<StaticConfig>(path, staticConfig);

            return staticConfig;
        }
    }

    public static T LoadConfig<T>(string path) where T : PlcDataConfig, new()
    {
        T data;
        int lastIndex = path.LastIndexOf('/') + 1;
        string fileName = path.Substring(lastIndex, path.Length - lastIndex);
        if (System.IO.File.Exists(path))
        {
            data = DataUtil.Deserializer<T>(path);

            data.SetFileName(fileName);
        }
        else
        {
            data = new T();
            data.SetFileName(fileName);
            //生成默认配置
            data.CreateDefalut();

            DataUtil.Serializer<T>(path, data);
        }
        return data;
    }

    public static void SaveConfig<T>(T data, string path) where T : PlcDataConfig
    {
        if (System.IO.File.Exists(path))
        {
            DataUtil.Serializer<T>(path, data);
        }
    }
}
