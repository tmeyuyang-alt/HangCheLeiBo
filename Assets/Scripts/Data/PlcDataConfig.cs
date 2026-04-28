using System.Collections.Generic;


public class PlcDataConfig
{
    public string PlcType;
    public string IPAddress;
    public int rack;
    public int slot;
    public List<object> config = null;
    public string configType = "ConfigDataBase";
    private string fileName;

    public int decimalPlaces = 0;
    public PlcDataConfig(){ }
    public PlcDataConfig(string fileName) { this.fileName = fileName; }
    public virtual void CreateDefalut() {
        decimalPlaces = 3;
    }

    public virtual string GetConfigType()
    { 
        return typeof(ConfigDataBase).Name;
    }
    public List<T> GetConfig<T>()
    {
        return config as List<T>;
    }

    public void SetFileName(string name)
    {
        fileName = name;
    }

    public string GetFilePath()
    {
       return UnityEngine.Application.streamingAssetsPath + "/" + fileName;
    }
    public void Save()
    {
        
        string path = UnityEngine.Application.streamingAssetsPath +"/"+  fileName;

        UnityEngine.Debug.Log(path);

        if (System.IO.File.Exists(path))
        {
            DataUtil.Serializer(path, this);
        }
    }
}


public class ConfigDataBase
{
    public ConfigDataBase() { }
    public ConfigDataBase(string name, string db, bool candel = true)
    {
        this.name = name;
        this.datablock = db;
        this.canDelete = candel;
    }

    public string name;
    public string datablock;
    public bool canDelete;
    public string unlit;
}
public class ElectricityConfigData : ConfigDataBase
{
    public ElectricityConfigData()
    {

    }
    public ElectricityConfigData(string name, string db, bool candel = true) : base(name, db, candel)
    {

    }
    /// <summary>
    /// 自动手动操作
    /// </summary>
    public string oper_datablock;
    /// <summary>
    /// 上升指示
    /// </summary>
    public string up_datablock;
    /// <summary>
    /// 下降指示
    /// </summary>
    public string down_datablock;
    /// <summary>
    /// 垫料
    /// </summary>
    public string bedding_datablock;


    /// <summary>
    /// 高限报警
    /// </summary>
    public string highlimit_alarm_datablock;
    /// <summary>
    /// 低限报警
    /// </summary>
    public string lowlimit_alarm_datablock;
    /// <summary>
    /// 电流报警
    /// </summary>
    public string electricity_alarm_datablock;
}

public class ElectrodeConfigData : ConfigDataBase
{
    public ElectrodeConfigData()
    {

    }
    public ElectrodeConfigData(string name, string db, bool candel = true) : base(name, db, candel)
    {

    }

    /// <summary>
    /// 低限
    /// </summary>
    public string lowlimit_datablock;
    /// <summary>
    /// 高限
    /// </summary>
    public string highlimit_datablock;


    ///// <summary>
    ///// 低限值
    ///// </summary>
    //public string lowlimit;
    ///// <summary>
    ///// 高限值
    ///// </summary>
    //public string highlimit;

}
/// <summary>
/// 电极倾斜数据
/// </summary>
public class ElectrodeTiltConfigData : ConfigDataBase
{
    public ElectrodeTiltConfigData()
    {

    }
    public ElectrodeTiltConfigData(string name, string db, bool candel = true) : base(name, db, candel)
    {

    }
    public string y_datablock;
    
    //public string z_datablock;
}


/// <summary>
/// 电炉温度数据
/// </summary>
public class ElectricFurnaceTempeData : ConfigDataBase
{
    public ElectricFurnaceTempeData()
    {

    }
    public ElectricFurnaceTempeData(string name, string db, bool candel = true) : base(name, db, candel)
    {

    }

    /// <summary>
    /// 低限
    /// </summary>
    //public string lowlimit_datablock;
    /// <summary>
    /// 高限
    /// </summary>
    public string highlimit_datablock;
    /// <summary>
    /// 报警
    /// </summary>
    //public string alarm_datablock;


    /// <summary>
    /// 低限值
    /// </summary>
    //public string lowlimit;
    /// <summary>
    /// 高限值
    ///// </summary>
    //public string highlimit;

}