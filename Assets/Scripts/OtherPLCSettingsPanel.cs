using Protocols;
using S7.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OtherPLCSettingsPanel : UIPanel
{
    const string otherPlcDataConfig = "OtherPlcDataConfig";
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

    //private StaticConfig m_StaticConfig = null;

    public GameObject MenuGo;

    public string m_ParamTitle = "NONE";


    public void GetPlcConfig()
    {
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.GET_OTHER_PLC_CONFIG;
        model.message = null;
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    public void OnGetOtherPlcConfig(StaticConfig config)
    {
        //m_StaticConfig = config;

        GlobalInfo.m_StaticConfig = config;

        if (config.staticConfigs.ContainsKey("Electricity"))
        {
            SetParamNameAndValue(1, "上班电流", config.staticConfigs["Electricity"].DataBlocks["LastWorkElectricity"]);
            SetParamNameAndValue(2, "当前电流", config.staticConfigs["Electricity"].DataBlocks["CurWorkElectricity"]);
            SetParamNameAndValue(3, "设置电流", config.staticConfigs["Electricity"].DataBlocks["SetWorkElectricity"]);
        }
        if (config.staticConfigs.ContainsKey("Gears"))
        {
            SetParamNameAndValue(4, "设置挡位", config.staticConfigs["Gears"].DataBlocks["Gears"]);
            SetParamNameAndValue(5, "升挡", config.staticConfigs["Gears"].DataBlocks["UpShift"]);
            SetParamNameAndValue(6, "降挡", config.staticConfigs["Gears"].DataBlocks["DownShift"]);
        }
        if (config.staticConfigs.ContainsKey("SystemMode"))
        {
            var systemMode = config.staticConfigs["SystemMode"];

            SetParamNameAndValue(7, "自动模式", systemMode.DataBlocks["AutomaticMode"]);
            //SetParamNameAndValue(8, "手动模式", systemMode.DataBlocks["ManualMode"]);
            SetParamNameAndValue(8, "分合闸", systemMode.DataBlocks["SwitchStatus"]);
            SetParamNameAndValue(9, "远程模式", systemMode.DataBlocks["RemoteStatus"]);


            //config.Value.PlcType = type.ToString();
            //config.Value.PlcIPAddress = m_IPAddress.text;
            //config.Value.PlcRack = rack.ToString();
            //config.Value.PlcSlot = slot.ToString();

            this.m_PlcType.value = PLCSettingsPanel.GetPlcIndex(systemMode.PlcType);
            this.m_IPAddress.text = systemMode.PlcIPAddress;
            this.m_Rack.text = systemMode.PlcRack;
            this.m_Slot.text = systemMode.PlcSlot;
        }
    }
    public override void OnExit()
    {
        DataHandler.getInstance.OnGetOtherPlcConfigCallback -= OnGetOtherPlcConfig;

        base.OnExit();
    }
    public override void OnEnter(object param)
    {
        DataHandler.getInstance.OnGetOtherPlcConfigCallback += OnGetOtherPlcConfig;
        DataHandler.getInstance.OnGetPlcStatusCallback += OnGetPlcStatusCallback;
        InvokeRepeating("GetConnectStatus", 0, 1);

        base.OnEnter(param);

        this.GetPlcConfig();


        //S7200 = 0,
        //Logo0BA8 = 1,
        //S7200Smart = 2,
        //S7300 = 10,
        //S7400 = 20,
        //S71200 = 30,
        //S71500 = 40,

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        options.Add(new Dropdown.OptionData(CpuType.S7200.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.Logo0BA8.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7200Smart.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7300.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7400.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S71200.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S71500.ToString(), null));

        m_PlcType.AddOptions(options);



        var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                        new UGuiTable.TableHeader("DB", "Input"),
                                                        //new UGuiTable.TableHeader("自动手动操作 DB", "Input")
        };
        table.Col = headers.Length;
        table.SetHeader(headers);

        table.Inital();

        SetParamNameAndValue(1, "上班电流", "");
        SetParamNameAndValue(2, "当前电流", "");
        SetParamNameAndValue(3, "设置电流", "");
        SetParamNameAndValue(4, "设置挡位", "");
        
        SetParamNameAndValue(5, "升挡", "");
        SetParamNameAndValue(6, "降档", "");

        SetParamNameAndValue(7, "自动模式", "");
        //SetParamNameAndValue(8, "手动模式", "");
        SetParamNameAndValue(8, "分合闸", "");
        SetParamNameAndValue(9, "远程模式", "");

        this.table.OnTextChanged += OnTableValueChanged;

    }

    public void SetParamNameAndValue(int line, string pname, string value)
    {
        table.GetItem(line,0).GetComponent<InputField>().text = pname;
        table.GetItem(line,1).GetComponent<InputField>().text = value;
    }

    public void OnTableValueChanged(int x, int y, string text)
    {
        if (x != 0)
        {
            var config = GlobalInfo.m_StaticConfig;
            if (config != null)
            {
                switch (y)
                {
                    case 1:
                        config.staticConfigs["Electricity"].DataBlocks["LastWorkElectricity"] = text;
                        break;
                    case 2:
                        config.staticConfigs["Electricity"].DataBlocks["CurWorkElectricity"] = text; 
                        break;
                    case 3:
                        config.staticConfigs["Electricity"].DataBlocks["SetWorkElectricity"] = text;
                        break;
                    case 4:
                        config.staticConfigs["Gears"].DataBlocks["Gears"] = text;
                        break;
                    case 5:
                        config.staticConfigs["Gears"].DataBlocks["UpShift"] = text;
                        break;
                    case 6:
                        config.staticConfigs["Gears"].DataBlocks["DownShift"] = text;
                        break;
                    case 7:
                        config.staticConfigs["SystemMode"].DataBlocks["AutomaticMode"] = text;
                        break;
                    //case 8:
                    //    config.staticConfigs["SystemMode"].DataBlocks["ManualMode"] = text;
                    //    break;
                    case 8:
                        config.staticConfigs["SystemMode"].DataBlocks["SwitchStatus"] = text;
                        break;
                    case 9:
                        config.staticConfigs["SystemMode"].DataBlocks["RemoteStatus"] = text;
                        break;
                }
            }
        }
    }

    public void SetStaticConfigValue(string category, string dataBlockKey,string text)
    {
        var config = GlobalInfo.m_StaticConfig;
        if (config.staticConfigs.ContainsKey(category))
        {
            if (config.staticConfigs[category].DataBlocks.ContainsKey(dataBlockKey))
            {
                config.staticConfigs[category].DataBlocks[dataBlockKey] = text;
            }
            else
            {
                config.staticConfigs[category].DataBlocks.Add(dataBlockKey, text);
            }
        }
    }

    public void SaveConfig()
    {
        if (GlobalInfo.m_StaticConfig != null)
        {
            short rack;
            short slot;
            CpuType type;
            rack = short.Parse(m_Rack.text);
            slot = short.Parse(m_Slot.text);

            type = (CpuType)Enum.Parse(typeof(CpuType), m_PlcType.captionText.text);

            foreach (var config in GlobalInfo.m_StaticConfig.staticConfigs)
            {
                config.Value.PlcType = type.ToString();
                config.Value.PlcIPAddress = m_IPAddress.text;
                config.Value.PlcRack = rack.ToString();
                config.Value.PlcSlot = slot.ToString();
            }

            SocketModel model = new SocketModel();

            UpdateConfigDTO dto = new UpdateConfigDTO();
            dto.ConfigName = otherPlcDataConfig;// "OtherPlcDataConfig";
            dto.ConfigData = DataUtil.SerializerToString(GlobalInfo.m_StaticConfig);

            model.type = Protocol.Data;
            model.command = DataProtocol.UPDATE_CONFIG;

            model.message = SerializeTool.Encode2Str(dto);

            model.senderID = GlobalInfo.user.uid.ToString();

            model.token = GlobalInfo.user.token;

            ClientManager.getInstance.SendServer(model);
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

        if (GlobalInfo.m_StaticConfig == null)
        {
            GlobalInfo.m_StaticConfig.CreateDefult();
        }

        foreach (var config in GlobalInfo.m_StaticConfig.staticConfigs)
        {
            config.Value.PlcType = type.ToString();
            config.Value.PlcIPAddress = m_IPAddress.text;
            config.Value.PlcRack = rack.ToString();
            config.Value.PlcSlot = slot.ToString();
        }

        //发送服务器连接问题
        PlcConnectDTO dto = new PlcConnectDTO();
        dto.config = otherPlcDataConfig;// m_PlcParamData.GetType().Name;
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
        dto.config = otherPlcDataConfig;
        
        model.type = Protocol.Data;
        model.command = DataProtocol.PLC_DISCONNECT;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    public void GetConnectStatus()
    {
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.PLC_CONNECT_STATUS;
        model.message = otherPlcDataConfig;
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    private void OnGetPlcStatusCallback(string configName, Dictionary<string, string> arg2)
    {
        if (configName == otherPlcDataConfig)
        {
            SetConnectStatus(arg2[otherPlcDataConfig] == "已连接");
        }
    }
    private void SetConnectStatus(bool connected)
    {
        ConnectStatus.text = connected ? "已连接" : "连接断开";
        ConnectStatus.color = connected ? new Color(0.455f, 0.894f, 0.713f) : Color.red;
        ConnectStatusIcon.sprite = connected ? StatusIconSprites[0] : StatusIconSprites[1];
    }

    public Vector2Int tempFocus = new Vector2Int(-1, -1);

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
                            item.text = splits[0] + "&" + str;
                        }
                    };
                }
                else
                {
                    MenuGo.gameObject.SetActive(true);
                }

                MenuGo.gameObject.transform.position = Input.mousePosition;
            }
        }
    }

    public override void OnClose()
    {
        table.OnTextChanged -= OnTableValueChanged;
        DataHandler.getInstance.OnGetOtherPlcConfigCallback -= OnGetOtherPlcConfig;
        DataHandler.getInstance.OnGetPlcStatusCallback -= OnGetPlcStatusCallback;
        if(MenuGo)
        GameObject.Destroy(MenuGo);
        base.OnClose();
    }
}
