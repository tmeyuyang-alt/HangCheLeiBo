using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlcFirstDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        this.PlcType = "PLC";
        this.rack = 0;
        this.slot = 0;
        this.IPAddress = "127.0.0.1:5056";

        if (config == null)
            config = new List<object>();

        config.Add(new ConfigDataBase("一次电流A", "", false));
        config.Add(new ConfigDataBase("一次电流B", "", false));
        config.Add(new ConfigDataBase("一次电流C", "", false));

        config.Add(new ConfigDataBase("功率因数", "", false));
        config.Add(new ConfigDataBase("无功功率", "", false));
        config.Add(new ConfigDataBase("有功功率", "", false));
        config.Add(new ConfigDataBase("一次电压", "", false));
    }
}


public class PlcSecondaryDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();
        if (config == null)
            config = new List<object>();

        config.Add(new ConfigDataBase("二次电流A1", ""));
        config.Add(new ConfigDataBase("一次电流B1", ""));
        config.Add(new ConfigDataBase("一次电流C1", ""));

        config.Add(new ConfigDataBase("二次电流A2", ""));
        config.Add(new ConfigDataBase("二次电流B2", ""));
        config.Add(new ConfigDataBase("二次电流C2", ""));
    }
}

public class PlcVoltageDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        base.CreateDefalut();
        if (config == null)
            config = new List<object>();

        config.Add(new ConfigDataBase("A1", "",false));
        config.Add(new ConfigDataBase("B1", "",false));
        config.Add(new ConfigDataBase("C1", "",false));

        config.Add(new ConfigDataBase("A2", "", false));
        config.Add(new ConfigDataBase("B2", "", false));
        config.Add(new ConfigDataBase("C2", "", false));
    }
}

public class PlcElectricityDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        base.CreateDefalut();
        if (config == null)
            config = new List<object>();

        config.Add(new ElectricityConfigData("A1", "DBX1.0", false));;
        config.Add(new ElectricityConfigData("B1", "DBX1.0", false));
        config.Add(new ElectricityConfigData("C1", "DBX1.0", false));

        config.Add(new ElectricityConfigData("A2", "DBX1.0", false));
        config.Add(new ElectricityConfigData("B2", "DBX1.0", false));
        config.Add(new ElectricityConfigData("C2", "DBX1.0", false));

        configType = "ElectricityConfigData";

    }

    public override string GetConfigType()
    {
        return typeof(ElectricityConfigData).Name;
    }
}
public class PlcElecFurnaceDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        if (config == null)
            config = new List<object>();

        config.Add(new ElectricFurnaceTempeData("炉壁温度A", ""));
        config.Add(new ElectricFurnaceTempeData("炉壁温度B", ""));
        config.Add(new ElectricFurnaceTempeData("炉壁温度C", ""));

        configType = "ElectricFurnaceTempeData";
    }

    public override string GetConfigType()
    {
        return typeof(ElectricFurnaceTempeData).Name;
    }
}

//三维配置
public class PlcElectrodeDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        if (config == null)
            config = new List<object>();

        config.Add(new ElectrodeConfigData("A1", "", false)); ;
        config.Add(new ElectrodeConfigData("B1", "", false));
        config.Add(new ElectrodeConfigData("C1", "", false));

        config.Add(new ElectrodeConfigData("A2", "", false));
        config.Add(new ElectrodeConfigData("B2", "", false));
        config.Add(new ElectrodeConfigData("C2", "", false));

        configType = "ElectrodeConfigData";

    }

    public override string GetConfigType()
    {
        return typeof(ElectrodeConfigData).Name;
    }
}
/// <summary>
/// 电极倾斜数据配置
/// </summary>
public class PlcElectrodeTiltDataConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        if (config == null)
            config = new List<object>();

        config.Add(new ElectrodeTiltConfigData("电极A1", "", false)); ;
        config.Add(new ElectrodeTiltConfigData("电极B1", "", false));
        config.Add(new ElectrodeTiltConfigData("电极C1", "", false));

        config.Add(new ElectrodeTiltConfigData("电极A2", "", false));
        config.Add(new ElectrodeTiltConfigData("电极B2", "", false));
        config.Add(new ElectrodeTiltConfigData("电极C2", "", false));

        configType = "ElectrodeTiltConfigData";

    }

    public override string GetConfigType()
    {
        return typeof(ElectrodeTiltConfigData).Name;
    }
}

public class PlcAlarmStatus3DConfig : PlcDataConfig
{
    public override void CreateDefalut()
    {
        base.CreateDefalut();

        if (config == null)
            config = new List<object>();

        config.Add(new ConfigDataBase("本体重瓦斯", "", false)); 
        config.Add(new ConfigDataBase("分柜重瓦斯", "", false)); 
        config.Add(new ConfigDataBase("本体轻瓦斯", "", false)); 
        config.Add(new ConfigDataBase("分柜轻瓦斯", "", false)); 
        config.Add(new ConfigDataBase("油温高高", "", false)); 
        config.Add(new ConfigDataBase("油温高", "", false)); 
        config.Add(new ConfigDataBase("压力释放", "", false)); 
        config.Add(new ConfigDataBase("本体油位异常", "", false)); 
        config.Add(new ConfigDataBase("分柜油位异常", "", false)); 

        configType = "ConfigDataBase";

    }

    public override string GetConfigType()
    {
        return typeof(ConfigDataBase).Name;
    }
}

//历史警告配置
//public class PlcHisWarningDataConfig : PlcDataConfig
//{
//    public override void CreateDefalut()
//    {
//        base.CreateDefalut();

//        if (config == null)
//            config = new List<object>();

//        configType = "PlcHisWarningDataConfig";

//    }

//    public override string GetConfigType()
//    {
//        return typeof(PlcHisWarningDataConfig).Name;
//    }
//}