using S7.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Protocols;
public class PLCSettingsPanel : UIPanel
{
    //public PLC_Connect plc;

    public Dropdown m_PlcType;
    public InputField m_IPAddress;
    public InputField m_Rack;
    public InputField m_Slot;
    public InputField m_decimalPlaces;
    public Text m_MsgText;
    
    public UGuiTable table;

    public Text ConnectStatus;
    public Image ConnectStatusIcon;

    public Sprite[] StatusIconSprites;

    public PlcDataConfig m_PlcParamData = null;

    public GameObject MenuGo;

    public GameObject addButton;
    public GameObject delButton;

    public string m_ParamTitle = "NONE";

    //通过这个来确定修改删除
    public List<ConfigDataBase> ConfigDataDeleteItems = new List<ConfigDataBase>();
    public List<ConfigDataBase> ConfigDataAddItems    = new List<ConfigDataBase>();

    //public List<ConfigDataBase> ConfigDataRenameItems = new List<ConfigDataBase>();
    public Dictionary<ConfigDataBase, string> ConfigDataRenameDic = new Dictionary<ConfigDataBase, string>();

    //public string GetPlcConnectStr(int index)
    //{
    //    switch (index)
    //    {
    //        case 0: return "PlcFirstDataConfig";
    //        case 1: return "PlcElecFurnaceDataConfig";
    //        case 2: return "PlcSecondaryDataConfig";
    //        case 3: return "PlcElectricityDataConfig";
    //        case 4: return "PlcVoltageDataConfig";
    //        case 101: return "PlcHisWarningDataConfig";
    //        default:
    //            if (index != -1)
    //                return "PlcElectrodeDataConfig";
    //            else
    //                return "";
    //    }
    //}
    public PlcDataConfig GetConfig(int index)
    {
        if (index >= 1000)
        {
            index = (index / 1000)*1000;
        }
        switch (index)
        {
            case 0: return GlobalInfo.m_FirstData;
            case 1: return GlobalInfo.m_ElectricFurnaceTempe;
            case 2: return GlobalInfo.m_SecondaryData;
            case 3: return GlobalInfo.m_ElectricityData;
            case 4: return GlobalInfo.m_VoltageData;
            //case 101: return GlobalInfo.m_HisWarningData;
            //三维数据电极
            case 1000:
                return GlobalInfo.m_ElectrodeData;
            //三维状态信息
            case 2000:
                return GlobalInfo.m_AlarmStatus3D;
            case 3000:
                return GlobalInfo.m_ElectrodeTiltData;
        }
        return null;
    }


    public static int GetPlcIndex(string plctype)
    {
        switch (plctype)
        {
            case "S7200": return 0;
            case "Logo0BA8": return 1;
            case "S7200Smart": return 2;
            case "S7300": return 3;
            case "S7400": return 4;
            case "S71200": return 5;
            case "S71500": return 6;
        }
        return -1;
    }

    private void OnConnectCallback(PackIn p)
    {
        SetConnectStatus(p.code == CodeConstant.OK);
    }
    private void OnDisconnectCallback(PackIn p)
    {
        SetConnectStatus(p.code != CodeConstant.OK);
    }
    public override void OnEnter(object param)
    {
        SetConnectStatus(false);

        if (param!=null && param is int)
        {
            m_PlcParamData = GetConfig((int)param);
        }
        else
        {
            m_PlcParamData = GetConfig(MainPanel.CurrentIndex);
        }

        if (m_PlcParamData == null)
            return;


        DataHandler.getInstance.OnConnectCallback       += OnConnectCallback;
        DataHandler.getInstance.OnDisconnectCallback    += OnDisconnectCallback;
        DataHandler.getInstance.OnGetPlcStatusCallback  += OnGetPlcStatusCallback;

        InvokeRepeating("GetConnectStatus", 0, 1);

        List <Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        //S7200 = 0,
        //Logo0BA8 = 1,
        //S7200Smart = 2,
        //S7300 = 10,
        //S7400 = 20,
        //S71200 = 30,
        //S71500 = 40,

        options.Add(new Dropdown.OptionData(CpuType.S7200.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.Logo0BA8.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7200Smart.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7300.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7400.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S71200.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S71500.ToString(), null));

        m_PlcType.AddOptions(options);


        m_IPAddress.text = m_PlcParamData.IPAddress;
        m_Rack.text = m_PlcParamData.rack.ToString();
        m_Slot.text = m_PlcParamData.slot.ToString();
        m_PlcType.value = GetPlcIndex(m_PlcParamData.PlcType);
        m_decimalPlaces.text = m_PlcParamData.decimalPlaces.ToString();
        //m_PlcType.captionText.text = m_PlcParamData.PlcType;

        int lineId = 1;
        //配置表头

        string configType = m_PlcParamData.GetConfigType();

        table.OnTextChanged += OnTableValueChanged;

        Debug.Log(m_PlcParamData.GetType().Name);

        if (m_PlcParamData.GetType().Name == typeof(PlcVoltageDataConfig).Name
          || m_PlcParamData.GetType().Name == typeof(PlcFirstDataConfig).Name
          || m_PlcParamData.GetType().Name == typeof(PlcElectricityDataConfig).Name
          || m_PlcParamData.GetType().Name == typeof(PlcElectrodeDataConfig).Name
          )
        {
            this.addButton.SetActive(false);
            this.delButton.SetActive(false);
        }
        else
        {
            this.addButton.SetActive(true);
            this.delButton.SetActive(true);
        }

            //设置表格表头
            if (configType == typeof(ConfigDataBase).Name)
        {
            UGuiTable.TableHeader[] headers = null;

            if (m_PlcParamData.GetType().Name == typeof(PlcVoltageDataConfig).Name
             || m_PlcParamData.GetType().Name == typeof(PlcFirstDataConfig).Name)
            {
                headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("DB", "Input"),
                                                        //new UGuiTable.TableHeader("", "Input")
            };
            }
            else
            {
                headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("DB", "Input"),
                                                        new UGuiTable.TableHeader("单位", "Input") };
            }
            table.Col = headers.Length;
            table.SetHeader(headers);
            table.Inital();
        }
        else if (configType == typeof(ElectricityConfigData).Name)
        {
            var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("DB", "Input"),
                                                        new UGuiTable.TableHeader("自动手动操作 DB", "Input"),
                                                        new UGuiTable.TableHeader("上升指示 DB", "Input"),
                                                        new UGuiTable.TableHeader("下降指示 DB", "Input"),
                                                        new UGuiTable.TableHeader("塌料 DB", "Input")

                                                        ,new UGuiTable.TableHeader("高限报警", "Input"),
                                                        new UGuiTable.TableHeader("低限报警", "Input"),
                                                        new UGuiTable.TableHeader("电流报警", "Input")
            };
            table.Col = headers.Length;
            table.SetHeader(headers);

            table.Inital();
        }
        else if (configType == typeof(ElectrodeConfigData).Name)
        {
            var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("高度DB", "Input"),
                                                        new UGuiTable.TableHeader("低限DB", "Input"),
                                                        new UGuiTable.TableHeader("高限DB", "Input"),
                                                        //new UGuiTable.TableHeader("低限", "Input"),
                                                        //new UGuiTable.TableHeader("高限", "Input"),
            };
            table.Col = headers.Length;
            table.SetHeader(headers);

            table.Inital();
        }
        else if (configType == typeof(ElectricFurnaceTempeData).Name)
        {
            var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("温度DB", "Input"),
                                                        //new UGuiTable.TableHeader("低限DB", "Input"),
                                                        new UGuiTable.TableHeader("高限DB", "Input"),
                                                        new UGuiTable.TableHeader("单位", "Input")
                                                        //new UGuiTable.TableHeader("报警DB", "Input"), 刚刚删除
                                                        //new UGuiTable.TableHeader("高限", "Input"),
            };
            table.Col = headers.Length;
            table.SetHeader(headers);

            table.Inital();
        }
        else if (configType == typeof(ElectrodeTiltConfigData).Name)
        {
            var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("X轴", "Input"),
                                                        new UGuiTable.TableHeader("Y轴", "Input"),
                                                        //new UGuiTable.TableHeader("Z轴", "Input"),
            };
            table.Col = headers.Length;
            table.SetHeader(headers);

            table.Inital();
        }
        table.OnMoveItem += (f) =>
        {
            //去掉标题
            int index = table.GetActiveLine() - 1;

            if (f == -1)
            {
                if (index - 1 < 0)
                {
                    return;
                }
                object obj = m_PlcParamData.config[index];
                m_PlcParamData.config[index] = m_PlcParamData.config[index - 1];
                m_PlcParamData.config[index - 1] = obj;
            }
            else
            {
                if (index + 1 >= m_PlcParamData.config.Count)
                {
                    return;
                }
                object obj = m_PlcParamData.config[index];
                m_PlcParamData.config[index] = m_PlcParamData.config[index + 1];
                m_PlcParamData.config[index + 1] = obj;
            }
            table.SetActiveLine(index + f + 1);
            UpdateTable();
        };

        var config = m_PlcParamData.config;

        int additem = config.Count - (table.Row - 1);
        if (additem > 0)
        {
            for (int i = 0; i < additem; i++)
            {
                table.AddRowOne();
            }
        }

        //int index = 0;
        //foreach (var item in config)
        for (int index = 0; index < config.Count; index++)
        {
            var item = config[index];

            var data = item as Dictionary<object, object>;


            if (configType == typeof(ConfigDataBase).Name)
            {
                ConfigDataBase cdb = DataUtil.GetConfigData<ConfigDataBase>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    var tinput3 = table.GetItem(lineId, 2);
                    if (tinput3)
                    {
                        var input3 = tinput3.GetComponent<InputField>();
                        if (input3 != null)
                            input3.text = cdb.unlit;
                    }
                }
            }
            else if (configType == typeof(ElectricityConfigData).Name)
            {
                ElectricityConfigData cdb = DataUtil.GetConfigData<ElectricityConfigData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.oper_datablock;
                    table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.up_datablock;
                    table.GetItem(lineId, 4).GetComponent<InputField>().text = cdb.down_datablock;
                    table.GetItem(lineId, 5).GetComponent<InputField>().text = cdb.bedding_datablock;

                    table.GetItem(lineId, 6).GetComponent<InputField>().text = cdb.highlimit_alarm_datablock;
                    table.GetItem(lineId, 7).GetComponent<InputField>().text = cdb.lowlimit_alarm_datablock;
                    table.GetItem(lineId, 8).GetComponent<InputField>().text = cdb.electricity_alarm_datablock;
                }
            }
            else if (configType == typeof(ElectrodeConfigData).Name)
            {
                ElectrodeConfigData cdb = DataUtil.GetConfigData<ElectrodeConfigData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.lowlimit_datablock;
                    table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.highlimit_datablock;

                    //table.GetItem(lineId, 4).GetComponent<InputField>().text = cdb.lowlimit;
                    //table.GetItem(lineId, 5).GetComponent<InputField>().text = cdb.highlimit;
                }
            }
            else if (configType == typeof(ElectricFurnaceTempeData).Name)
            {
                ElectricFurnaceTempeData cdb = DataUtil.GetConfigData<ElectricFurnaceTempeData>(config, index);
                if (cdb != null)
                {
                    //if (cdb.highlimit == null) cdb.highlimit = "";
                    if (cdb.highlimit_datablock == null) cdb.highlimit_datablock = "";


                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.highlimit_datablock;
                    table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.unlit;

                    //table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.alarm_datablock;

                    //table.GetItem(lineId, 4).GetComponent<InputField>().text = cdb.lowlimit;
                    //table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.highlimit;
                }
            }
            else if (configType == typeof(ElectrodeTiltConfigData).Name)
            {
                ElectrodeTiltConfigData cdb = DataUtil.GetConfigData<ElectrodeTiltConfigData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;
                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.y_datablock;
                    //table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.z_datablock;
                }
            }
            lineId++;
        }
        table.Clear(lineId);
    }

    private void OnGetPlcStatusCallback(string configName, Dictionary<string, string> arg2)
    {
        if (configName==m_PlcParamData.GetType().Name)
        {
            if (arg2.ContainsKey(m_PlcParamData.GetType().Name))
            {
                SetConnectStatus(arg2[m_PlcParamData.GetType().Name] == "已连接");
            }

        }
    }

    private void UpdateTable()
    {
        int lineId = 1;

        string configType = m_PlcParamData.GetConfigType();

        var config = m_PlcParamData.config;

        //int index = 0;
        //foreach (var item in config)
        for (int index = 0; index < config.Count; index++)
        {
            var item = config[index];

            var data = item as Dictionary<object, object>;


            if (configType == typeof(ConfigDataBase).Name)
            {
                ConfigDataBase cdb = DataUtil.GetConfigData<ConfigDataBase>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;
                    var input3 = table.GetItem(lineId, 2)?.GetComponent<InputField>();
                    if (input3 != null)
                        input3.text = cdb.unlit;


                    table.GetItem(lineId, 0).gameObject.SetActive(true);
                    table.GetItem(lineId, 1).gameObject.SetActive(true);
                    table.GetItem(lineId, 2)?.gameObject.SetActive(true);
                }
            }
            else if (configType == typeof(ElectricityConfigData).Name)
            {
                ElectricityConfigData cdb = DataUtil.GetConfigData<ElectricityConfigData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.oper_datablock;
                    table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.up_datablock;
                    table.GetItem(lineId, 4).GetComponent<InputField>().text = cdb.down_datablock;
                    table.GetItem(lineId, 5).GetComponent<InputField>().text = cdb.bedding_datablock;

                    table.GetItem(lineId, 6).GetComponent<InputField>().text = cdb.highlimit_alarm_datablock;
                    table.GetItem(lineId, 7).GetComponent<InputField>().text = cdb.lowlimit_alarm_datablock;
                    table.GetItem(lineId, 8).GetComponent<InputField>().text = cdb.electricity_alarm_datablock;

                    table.GetItem(lineId, 0).gameObject.SetActive(true);
                    table.GetItem(lineId, 1).gameObject.SetActive(true);
                    table.GetItem(lineId, 2).gameObject.SetActive(true);
                    table.GetItem(lineId, 3).gameObject.SetActive(true);
                    table.GetItem(lineId, 4).gameObject.SetActive(true);
                    table.GetItem(lineId, 5).gameObject.SetActive(true);
                    table.GetItem(lineId, 6).gameObject.SetActive(true);
                    table.GetItem(lineId, 7).gameObject.SetActive(true);
                    table.GetItem(lineId, 8).gameObject.SetActive(true);
                }
            }
            else if (configType == typeof(ElectrodeConfigData).Name)
            {
                ElectrodeConfigData cdb = DataUtil.GetConfigData<ElectrodeConfigData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.lowlimit_datablock;
                    table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.highlimit_datablock;

                    //table.GetItem(lineId, 4).GetComponent<InputField>().text = cdb.lowlimit;
                    //table.GetItem(lineId, 5).GetComponent<InputField>().text = cdb.highlimit;

                    table.GetItem(lineId, 0).gameObject.SetActive(true);
                    table.GetItem(lineId, 1).gameObject.SetActive(true);

                    table.GetItem(lineId, 2).gameObject.SetActive(true);
                    table.GetItem(lineId, 3).gameObject.SetActive(true);

                    table.GetItem(lineId, 4).gameObject.SetActive(true);
                    table.GetItem(lineId, 5).gameObject.SetActive(true);
                }
            }
            else if (configType == typeof(ElectricFurnaceTempeData).Name)
            {
                ElectricFurnaceTempeData cdb = DataUtil.GetConfigData<ElectricFurnaceTempeData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    //table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.lowlimit_datablock;
                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.highlimit_datablock;
                    table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.unlit;

                    //table.GetItem(lineId, 4).GetComponent<InputField>().text = cdb.lowlimit;
                    //table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.highlimit;

                    table.GetItem(lineId, 0).gameObject.SetActive(true);
                    table.GetItem(lineId, 1).gameObject.SetActive(true);

                    table.GetItem(lineId, 2).gameObject.SetActive(true);
                    table.GetItem(lineId, 3).gameObject.SetActive(true);

                    //table.GetItem(lineId, 4).gameObject.SetActive(true);
                    //table.GetItem(lineId, 5).gameObject.SetActive(true);
                }
            }
            else if (configType == typeof(ElectrodeTiltConfigData).Name)
            {
                ElectrodeTiltConfigData cdb = DataUtil.GetConfigData<ElectrodeTiltConfigData>(config, index);
                if (cdb != null)
                {
                    table.GetItem(lineId, 0).GetComponent<InputField>().text = cdb.name;
                    table.GetItem(lineId, 1).GetComponent<InputField>().text = cdb.datablock;

                    table.GetItem(lineId, 2).GetComponent<InputField>().text = cdb.y_datablock;
                    //table.GetItem(lineId, 3).GetComponent<InputField>().text = cdb.z_datablock;

                    table.GetItem(lineId, 0).gameObject.SetActive(true);
                    table.GetItem(lineId, 1).gameObject.SetActive(true);
                    table.GetItem(lineId, 2).gameObject.SetActive(true);
                    //table.GetItem(lineId, 3).gameObject.SetActive(true);

                }
            }

            lineId++;
        }
        table.Clear(lineId);
    }
    

    public void OnTableValueChanged(int x, int y,string text)
    {
        if (y > m_PlcParamData.config.Count)
            return;

        if (table.hasHeader)
        {
            var obj = m_PlcParamData.config[y - 1];

            if (obj != null)
            {
                var cdb = (obj as ConfigDataBase);
                //判断是否有重命名  不处理AddItem的重命名
                if(cdb!=null)
                if (cdb.name != text && x == 0)
                {
                    if (!ConfigDataRenameDic.ContainsKey(cdb) && !ConfigDataAddItems.Contains(cdb))
                        ConfigDataRenameDic.Add(cdb, cdb.name);
                }

                //判断类型写入数据
                if (obj is ElectricityConfigData)
                {
                    switch (x)
                    {
                        case 0: (obj as ElectricityConfigData).name = text.ToString(); break;
                        case 1: (obj as ElectricityConfigData).datablock = text.ToString(); break;
                        case 2: (obj as ElectricityConfigData).oper_datablock = text.ToString(); break;
                        case 3: (obj as ElectricityConfigData).up_datablock = text.ToString(); break;
                        case 4: (obj as ElectricityConfigData).down_datablock = text.ToString(); break;
                        case 5: (obj as ElectricityConfigData).bedding_datablock = text.ToString(); break;
                        case 6: (obj as ElectricityConfigData).highlimit_alarm_datablock = text.ToString(); break;
                        case 7: (obj as ElectricityConfigData).lowlimit_alarm_datablock = text.ToString(); break;
                        case 8: (obj as ElectricityConfigData).electricity_alarm_datablock = text.ToString(); break;
                    }
                }
                else if (obj is ElectrodeConfigData)
                {
                    switch (x)
                    {
                        case 0: (obj as ElectrodeConfigData).name = text.ToString(); break;
                        case 1: (obj as ElectrodeConfigData).datablock = text.ToString(); break;
                        case 2: (obj as ElectrodeConfigData).lowlimit_datablock = text.ToString(); break;
                        case 3: (obj as ElectrodeConfigData).highlimit_datablock = text.ToString(); break;

                        //case 4: (obj as ElectrodeConfigData).lowlimit  = text.ToString(); break;
                        //case 5: (obj as ElectrodeConfigData).highlimit = text.ToString(); break;
                    }
                }
                else if (obj is ElectricFurnaceTempeData)
                {
                    switch (x)
                    {
                        case 0: (obj as ElectricFurnaceTempeData).name = text.ToString(); break;
                        case 1: (obj as ElectricFurnaceTempeData).datablock = text.ToString(); break;
                        case 2: (obj as ElectricFurnaceTempeData).highlimit_datablock = text.ToString(); break;
                        case 3: (obj as ElectricFurnaceTempeData).unlit = text.ToString(); break;
                        //case 3: (obj as ElectricFurnaceTempeData).highlimit = text.ToString();break;
                    }
                }
                else if (obj is ElectrodeTiltConfigData)
                {
                    switch (x)
                    {
                        case 0: (obj as ElectrodeTiltConfigData).name = text.ToString(); break;
                        case 1: (obj as ElectrodeTiltConfigData).datablock = text.ToString(); break;
                        case 2: (obj as ElectrodeTiltConfigData).y_datablock = text.ToString(); break;
                        //case 3: (obj as ElectrodeTiltConfigData).z_datablock = text.ToString(); break;
                    }
                }
                else if (obj is ConfigDataBase)
                {
                    switch (x)
                    {
                        case 0: (obj as ConfigDataBase).name = text.ToString(); break;
                        case 1: (obj as ConfigDataBase).datablock = text.ToString(); break;
                        case 2: (obj as ConfigDataBase).unlit = text.ToString(); break;
                    }
                }
            }
        }
    }



    public void Connect()
    {
        string[] ipandport = m_IPAddress.text.Split(new char[] { ':', '；', ' ' });

        string ipaddrees = "";
        int port = 0;
        short rack;
        short slot;
        CpuType type;


        if (ipandport.Length == 2)
        {
            ipaddrees = ipandport[0];
            port = int.Parse(ipandport[1]);
        }

        rack = short.Parse(m_Rack.text);
        slot = short.Parse(m_Slot.text);
        type = (CpuType)Enum.Parse(typeof(CpuType), m_PlcType.captionText.text);


        m_PlcParamData.PlcType = type.ToString();
        m_PlcParamData.IPAddress = m_IPAddress.text;
        m_PlcParamData.rack = rack;
        m_PlcParamData.slot = slot;
        //plc.Open();


        //发送服务器连接问题
        PlcConnectDTO dto = new PlcConnectDTO();
        dto.config =  m_PlcParamData.GetType().Name;// GetPlcConnectStr(MainPanel.CurrentIndex);
        dto.IPAddress = ipaddrees;
        dto.port = port;
        dto.PlcType = type.ToString();
        dto.rack = rack;
        dto.slot = slot;

        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        //model.area = DataProtocol.;
        model.command = DataProtocol.PLC_CONNECT;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    public void Disconnect()
    {
        SocketModel model = new SocketModel();

        PlcConnectDTO dto = new PlcConnectDTO();
        dto.config = m_PlcParamData.GetType().Name;// GetPlcConnectStr(MainPanel.CurrentIndex);


        model.type = Protocol.Data;
        //model.area = DataProtocol.;
        model.command = DataProtocol.PLC_DISCONNECT;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    private void SetConnectStatus(bool connected)
    {
        ConnectStatus.text = connected ? "已连接" : "连接断开";
        ConnectStatus.color = connected ? new Color(0.455f, 0.894f, 0.713f) : Color.red;
        ConnectStatusIcon.sprite = connected ? StatusIconSprites[0] : StatusIconSprites[1];
    }

    public void GetConnectStatus()
    {
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.PLC_CONNECT_STATUS;
        model.message = m_PlcParamData.GetType().Name;
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);

    }

    public void SaveConfig()
    {
        //保存配置
        if (m_PlcParamData == null)
        {
            m_PlcParamData = new PlcDataConfig();
        }
        m_PlcParamData.decimalPlaces = int.Parse(m_decimalPlaces.text);

        m_PlcParamData.IPAddress = m_IPAddress.text;
        m_PlcParamData.PlcType = m_PlcType.captionText.text;
        m_PlcParamData.rack = int.Parse(m_Rack.text);
        m_PlcParamData.slot = int.Parse(m_Slot.text);


        Dictionary<string, string> temp = new Dictionary<string, string>();
        bool repeatKey = false;
        bool nameEmpty = false;
        //判断是否有重复的
        if (m_PlcParamData.config != null)
        {
            for (int i = 0; i < m_PlcParamData.config.Count; i++)
            {
              ConfigDataBase cdb =  m_PlcParamData.config[i] as ConfigDataBase;

                if (cdb.name == "" || cdb.name ==null)
                {
                    nameEmpty = true;
                }
                if (cdb != null && !temp.ContainsKey(cdb.name))
                {
                    temp.Add(cdb.name, cdb.name);
                }
                else
                {
                    if (cdb != null)
                    {
                        //存在重复
                        repeatKey = true;
                    }
                }
            }
        }

        m_MsgText.enabled = repeatKey || nameEmpty;

        if (repeatKey)
        {
            m_MsgText.text = "参数名称重复，请修改后重新提交。";
        }
        else if (nameEmpty)
        {
            m_MsgText.text = "参数名称为不能为空！";
        }
        if(repeatKey || nameEmpty) return;

        m_PlcParamData.Save();

        //TODO 发送到服务器端
        {

            SocketModel model = new SocketModel();

            UpdateConfigDTO dto = new UpdateConfigDTO();
            dto.ConfigName = m_PlcParamData.GetType().Name;// GetPlcConnectStr(MainPanel.CurrentIndex);
            dto.ConfigData = System.IO.File.ReadAllText(m_PlcParamData.GetFilePath());

            model.type = Protocol.Data;
            model.command = DataProtocol.UPDATE_CONFIG;

            model.message = SerializeTool.Encode2Str(dto);

            model.senderID = GlobalInfo.user.uid.ToString();

            model.token = GlobalInfo.user.token;

            ClientManager.getInstance.SendServer(model);
        }






        //删除配置
        if (ConfigDataDeleteItems.Count > 0)
        {
            List<ConfigItemDTO> DeleteItems = new List<ConfigItemDTO>();
            for (int i = 0; i < ConfigDataDeleteItems.Count; i++)
            {
                DeleteItems.Add(new ConfigItemDTO()
                {
                    name = ConfigDataDeleteItems[i].name
                });
            }
            SocketModel delModel = new SocketModel();
            delModel.type = Protocol.Data;
            delModel.command = DataProtocol.DEL_CONFIG_ITEM;
            delModel.message = SerializeTool.Encode2Str(DeleteItems);
            delModel.senderID = GlobalInfo.user.uid.ToString();
            delModel.token = GlobalInfo.user.token;
            ClientManager.getInstance.SendServer(delModel);

            Debug.Log("发送数据" + DeleteItems[0].name);
        }
        if (ConfigDataAddItems.Count > 0)
        {
            List<ConfigItemDTO> AddItems = new List<ConfigItemDTO>();
            for (int i = 0; i < ConfigDataAddItems.Count; i++)
            {
                AddItems.Add(new ConfigItemDTO()
                {
                    name = ConfigDataAddItems[i].name,
                    attachment = ConfigDataAddItems[i].datablock
                });
            }

            SocketModel addModel = new SocketModel();
            
            addModel.type = Protocol.Data;
            addModel.command = DataProtocol.ADD_CONFIG_ITEM;
            addModel.message = SerializeTool.Encode2Str(AddItems);
            addModel.senderID = GlobalInfo.user.uid.ToString();
            addModel.token = GlobalInfo.user.token;

            ClientManager.getInstance.SendServer(addModel);

            Debug.Log("发送数据 添加" + AddItems[0].name);
        }

        //if(ConfigDataRenameItems.Count>0)
        //{

        //}

        if (ConfigDataRenameDic.Count > 0)
        {
            List<ConfigItemDTO> RenameItems = new List<ConfigItemDTO>();
            foreach (var item in ConfigDataRenameDic)
            {
                RenameItems.Add(new ConfigItemDTO()
                {
                    name = item.Key.name,
                    attachment = item.Value
                });
            }

            SocketModel renameModel = new SocketModel();

            renameModel.type = Protocol.Data;
            renameModel.command = DataProtocol.RENAME_CONFIG_ITEM;
            renameModel.message = SerializeTool.Encode2Str(RenameItems);
            renameModel.senderID = GlobalInfo.user.uid.ToString();
            renameModel.token = GlobalInfo.user.token;

            ClientManager.getInstance.SendServer(renameModel);

            Debug.Log("重命名" + RenameItems[0].name);

        }
        ConfigDataDeleteItems.Clear();
        ConfigDataAddItems.Clear();
        ConfigDataRenameDic.Clear();

        //触发事件
        EventCenter.Instance.TriggerEvent(EventName.PlcSettingsPanelSave, this, System.EventArgs.Empty);
    }
    public Vector2Int tempFocus = new Vector2Int(-1,-1);

    public void MouseExitTable()
    {
        table.GetGridFocus = new Vector2Int(-1, -1);
    }
    public void Update()
    {
        //右键
        if (Input.GetMouseButtonUp(1))
        {
           var focus = table.GetGridFocus;
            
            tempFocus = focus;

            if (focus.x == 0) return;

            if (focus.x != -1 && focus.y != -1)
            {
                if (MenuGo == null)
                {
                    MenuGo = GameObject.Instantiate(Resources.Load("Menu")) as GameObject;

                    MenuGo.transform.SetParent(UIManager.Instance.transform, false);


                    MenuGo.GetComponent<MenuUI>().OnSelected += (str) =>
                    {
                        int line = tempFocus.y;
                        int index = tempFocus.x;

                        var item = table.GetItem(line, index).GetComponentInChildren<InputField>();


                        //分析类型

                        if (item.text == "") return;

                         string[] splits = item.text.Split('&');

                        if (splits.Length > 0)
                        {
                            item.text =splits[0]+"&" + str;
                        }
                    };
                }
                else {
                    MenuGo.gameObject.SetActive(true);
                }

                MenuGo.gameObject.transform.position = Input.mousePosition;
            }
        }
    }

    public void AddItem()
    {
        string configType = m_PlcParamData.GetConfigType();

        ConfigDataBase config = null;

        if (configType == typeof(ElectricityConfigData).Name)
        {
            config = new ElectricityConfigData();
        }
        else if (configType == typeof(ElectrodeConfigData).Name)
        {
            config = new ElectrodeConfigData();
        }
        else if (configType == typeof(ElectricFurnaceTempeData).Name)
        {
            config = new ElectricFurnaceTempeData();
        }
        else if (configType == typeof(ConfigDataBase).Name)
        {
            config = new ConfigDataBase();
        }

        config.name = "新建项";
        config.datablock = "";

        m_PlcParamData.config.Add(config);

        //AddItems.Add(new ConfigItemDTO()
        //{
        //    name = config.name,
        //    attachment = config.datablock
        //});

        //添加配置
        ConfigDataAddItems.Add(config);

        //需要判断有些页面是否需要增加
        if(m_PlcParamData.config.Count>= table.Row-1)
            table.AddRowOne();

        //让他下一帧生效
        table.SetActiveLine(m_PlcParamData.config.Count);
        //刷新UI
        UpdateTable();
    }
    public void RemoveItem()
    {
        //去除标题 所以-1

        int i = table.GetActiveLine()-1;

        if (i >=0 && i < m_PlcParamData.config.Count)
        {
            Debug.Log("删除" + i);

            ConfigDataBase cdb = m_PlcParamData.config[i] as ConfigDataBase;

            //判断数据是否在AddItem里面
            if (!ConfigDataAddItems.Remove(cdb))
            {
                //ConfigDataDeleteItems.Add(new ConfigItemDTO()
                //{
                //    name = cdb.name
                //});
                ConfigDataDeleteItems.Add(cdb);
            }

            
            
            //保存删除项
            m_PlcParamData.config.RemoveAt(i);
        }

        //table.RemoveLine();
        UpdateTable();
    }
    public void OnDestroy()
    {
        DataHandler.getInstance.OnConnectCallback -= OnConnectCallback;
        DataHandler.getInstance.OnDisconnectCallback -= OnDisconnectCallback;
        DataHandler.getInstance.OnGetPlcStatusCallback -= OnGetPlcStatusCallback;
        if (this.table != null)
            this.table.OnTextChanged -= OnTableValueChanged;

        if(MenuGo)
        GameObject.Destroy(MenuGo);
    }
}


