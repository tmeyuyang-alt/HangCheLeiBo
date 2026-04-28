using Protocols;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonParameterSettingsPanel : UIPanel
{

    public UGuiTable table;

    public Button button;

    List<string> names = new List<string>();
    List<string[]> datablocks = new List<string[]>();

    public bool inital = false;

    public int FieldNumber = 1; //只要高限  或者有高低限

    public string CurrentConfigName = "";
    public PlcDataConfig GetConfig(int index)
    {
        switch (index)
        {
            case 0: return GlobalInfo.m_FirstData;
            case 1: return GlobalInfo.m_ElectricFurnaceTempe;
            case 2: return GlobalInfo.m_SecondaryData;
            case 3: return GlobalInfo.m_ElectricityData;
            case 4: return GlobalInfo.m_VoltageData;
            //三维数据部分
            default: return GlobalInfo.m_ElectrodeData;
        }
    }

    //public PLC_Connect GetPlcConnect(int index)
    //{
    //    switch (index)
    //    {
    //        case 0: return DataCenter.Instance.GetPLCConnect("PlcFirstDataConfig");
    //        case 1: return DataCenter.Instance.GetPLCConnect("PlcElecFurnaceDataConfig");
    //        case 2: return DataCenter.Instance.GetPLCConnect("PlcSecondaryDataConfig");
    //        case 3: return DataCenter.Instance.GetPLCConnect("PlcElectricityDataConfig");
    //        case 4: return DataCenter.Instance.GetPLCConnect("PlcVoltageDataConfig");
    //        default: return null;
    //    }
    //}

    public override void OnEnter(object param)
    {
        base.OnEnter(param);

        names.Clear();

        var PlcDataConfig = GetConfig(MainPanel.CurrentIndex);

        if (PlcDataConfig.GetType().Name == "PlcElectrodeDataConfig")
        { 
            FieldNumber = 2;

            var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("低限", "Input"),
                                                        new UGuiTable.TableHeader("高限", "Input"),
                                                        };
            table.Col = 3;
            table.Row = 12;
            table.SetHeader(headers);
        }
        else
        {
            FieldNumber = 1;

            var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("温度高限", "Input"), };

            table.Col = 2;
            table.Row = 12;
            table.SetHeader(headers);

        }

        table.Inital();

        //读取参数
        //table.GetItem(1, 0).GetComponent<InputField>().text = "";
        //table.GetItem(1, 1).GetComponent<InputField>().text = param.ToString();


        for (int i = 0; i < PlcDataConfig.config.Count; i++)
        {

            if (PlcDataConfig.config.Count >= table.Row - 1)
                table.AddRowOne();

            ConfigDataBase cdb = null;

            if (PlcDataConfig.GetConfigType() == typeof(ElectrodeConfigData).Name)
            {
                cdb = DataUtil.GetConfigData<ElectrodeConfigData>(PlcDataConfig.config, i);
                datablocks.Add(new string[] { (cdb as ElectrodeConfigData).lowlimit_datablock, (cdb as ElectrodeConfigData).highlimit_datablock });
            }
            else if (PlcDataConfig.GetConfigType() == typeof(ElectricFurnaceTempeData).Name)
            {
                cdb = DataUtil.GetConfigData<ElectricFurnaceTempeData>(PlcDataConfig.config, i);
                datablocks.Add(new string[] { (cdb as ElectricFurnaceTempeData).highlimit_datablock });
            }
            else if (PlcDataConfig.GetConfigType() == typeof(ConfigDataBase).Name)
            {
                cdb = DataUtil.GetConfigData<ConfigDataBase>(PlcDataConfig.config, i);
            }
            names.Add(cdb.name);
            table.GetItem(i + 1, 0).GetComponent<InputField>().text = cdb.name;
            table.GetItem(i + 1, 1).GetComponent<InputField>().text = ""; //TODO获取值 
        }

        //发送请求
        DataCenter.Instance.GetPlcData(PlcDataConfig.GetType().Name);

        CurrentConfigName = PlcDataConfig.GetType().Name;

        table.OnTextChanged += (x, y, str) =>
        {
            
        };

    }
    void Start()
    {
        //OnEnter("");

        button.onClick.AddListener(() =>
        {
            //收集数据然后提交
            List<PLCData> plcDatas = new List<PLCData>();

            for (int i = 0; i < names.Count; i++)
            {
                if (datablocks[i] == null) continue;

                if (datablocks[i].Length == 1)
                {
                    plcDatas.Add(new PLCData()
                    {
                        DB = datablocks[i][0],
                        Value =table.GetItem(i + 1, 1).GetComponent<InputField>().text,
                        Name = names[i],
                        Type = DataUtil.PlcToCsharpType(datablocks[i][0])
                    }); ;
                }
                else if (datablocks[i].Length == 2)
                {
                    plcDatas.Add(new PLCData()
                    {
                        DB = datablocks[i][0],
                        Value = table.GetItem(i + 1, 1).GetComponent<InputField>().text
                        ,Name = names[i],
                        Type = DataUtil.PlcToCsharpType(datablocks[i][0])
                    });

                    plcDatas.Add(new PLCData()
                    {
                        DB = datablocks[i][1],
                        Value = table.GetItem(i + 1, 2).GetComponent<InputField>().text,
                        Name = names[i],
                        Type = DataUtil.PlcToCsharpType(datablocks[i][1])
                    });
                }
            }

            DataCenter.Instance.SetPlcData(CurrentConfigName,plcDatas);
        });

        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }

    private void OnGetPlcDataCallback(string configName, List<PLCData> datas)
    {
        if (inital) return; 



        if (configName == CurrentConfigName)
        {
            inital = true;
            Dictionary<string, string> highlimit = new Dictionary<string, string>();
            Dictionary<string, string> lowlimit = new Dictionary<string, string>();
            foreach (var data in datas)
            {
                if (data.SubName == "highlimit_datablock" && !highlimit.ContainsKey(data.Name))//高限
                {
                    highlimit.Add(data.Name, data.Value);
                }
                else if (data.SubName == "lowlimit_datablock" && !lowlimit.ContainsKey(data.Name))//低限
                {
                    lowlimit.Add(data.Name, data.Value);
                }
            }




            //赋默认值
            for (int i = 0; i < names.Count; i++)
            {
                //if (values.ContainsKey(names[i]))
                {
           
                    if (FieldNumber == 2)
                    {
                        string key = names[i];
                        table.GetItem(i + 1, 1).GetComponent<InputField>().text =  lowlimit[key];
                        table.GetItem(i + 1, 2).GetComponent<InputField>().text =  highlimit[key]; 
                    }
                    else
                    {
                        if (highlimit.ContainsKey(names[i]))
                        {
                            table.GetItem(i + 1, 1).GetComponent<InputField>().text = highlimit[names[i]];
                        }
                        else
                        {
                            Debug.Log(names[i]);
                        }
                    }
                }
            }
        }
    }

    public override void OnClose()
    {
        base.OnClose();
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }
}
