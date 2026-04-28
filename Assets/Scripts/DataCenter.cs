using Protocols;
using S7.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataCenter : MonoBehaviour
{

    public static DataCenter Instance;
    //List<PLC_Connect> plc_Connects = new List<PLC_Connect>();
    private Dictionary<string,PLC_Connect> plc_Connects = new Dictionary<string,PLC_Connect>();

    private StaticConfig staticConfig;
    void Start()
    {
        Instance = this;
        staticConfig = GlobalInfo.m_StaticConfig;


        //添加配置 这部分已经转移服务器端了
        //AddPlcConnects(staticConfig);
        //AddPlcConnects<PlcElecFurnaceDataConfig>(GlobalInfo.m_ElectricFurnaceTempe);
        //AddPlcConnects<PlcSecondaryDataConfig>(GlobalInfo.m_SecondaryData);
        //AddPlcConnects<PlcFirstDataConfig>(GlobalInfo.m_FirstData);
        //AddPlcConnects<PlcElectricityDataConfig>(GlobalInfo.m_ElectricityData);
        //AddPlcConnects<PlcVoltageDataConfig>(GlobalInfo.m_VoltageData);
        //AddPlcConnects<PlcElectrodeDataConfig>(GlobalInfo.m_ElectrodeData);
        //GetPLCConnect("StaticConfig").Open();
        //延迟连接


    }
    public void GetOtherPlcConfig()
    {
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.GET_OTHER_PLC_CONFIG;
        model.message = null;
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    public void ConnectStaticConfigPlc()
    {
        if (staticConfig==null|| staticConfig.staticConfigs == null)
            return;
        foreach (var config in staticConfig.staticConfigs)
        {

            string[] ipandport = config.Value.PlcIPAddress.Split(new char[] { ':', '；', ' ' });

            string ipaddrees = "";
            int port = 0;
            short rack = 0;
            short slot = 0;

            if (ipandport.Length == 2)
            {
                ipaddrees = ipandport[0];
                port = int.Parse(ipandport[1]);
            }
            short.TryParse(config.Value.PlcRack, out rack);
            short.TryParse(config.Value.PlcSlot, out slot);

            PlcConnectDTO dto = new PlcConnectDTO();
            dto.config = typeof(StaticConfig).Name;
            dto.IPAddress = ipaddrees;
            dto.port = port;
            dto.PlcType = config.Value.PlcType.ToString();
            dto.rack = rack;
            dto.slot = slot;

            SocketModel model = new SocketModel();

            model.type = Protocol.Data;
            model.command = DataProtocol.PLC_CONNECT;
            model.message = SerializeTool.Encode2Str(dto);
            model.senderID = GlobalInfo.user.uid.ToString();
            model.token = GlobalInfo.user.token;
            ClientManager.getInstance.SendServer(model);
        }
    }

    public PLC_Connect GetPLCConnect(string key)
    {
        if (plc_Connects.ContainsKey(key)) return plc_Connects[key];
        else return null;
    }
    public void AddPlcConnects<T>(T config) where T : PlcDataConfig
    {
        GameObject plcGo = new GameObject();
        var plc = plcGo.AddComponent<PLC_Connect>();
        plcGo.name = "PlcConnect";
        string[] ipandport = config.IPAddress.Split(new char[] { ':', '；', ' ' });
        if (ipandport.Length == 2)
        {
            plc.ipaddrees = ipandport[0];
            plc.port = int.Parse(ipandport[1]);
        }
        plc_Connects.Add(config.GetType().Name, plc);
        Debug.Log(config.GetType().Name);
    }

    public void AddPlcConnects(StaticConfig config)
    {
        if (config == null || config.staticConfigs==null)
            return;
        foreach (var item in config.staticConfigs)
        {
            GameObject plcGo = new GameObject();
            var plc = plcGo.AddComponent<PLC_Connect>();
            plcGo.name = "PlcConnect";

            string[] ipandport = item.Value.PlcIPAddress.Split(new char[] { ':', '；', ' ' });
            if (ipandport.Length == 2)
            {
                plc.ipaddrees = ipandport[0];
                plc.port = int.Parse(ipandport[1]);
            }
            plc_Connects.Add(config.GetType().Name, plc);
            Debug.Log(config.GetType().Name);

        }
    }


    public void GetPlcData(string config)
    {
        SocketModel model = new SocketModel();

        model.type = Protocols.Protocol.Data;
        model.area = -1;
        model.command = Protocols.DataProtocol.GET_PLC_DATA;
        model.message = SerializeTool.Encode2Str(new GetPlcDataDTO() { config = config });
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    public void SetPlcData(string config,List<PLCData> plcDatas)
    {
        SocketModel model = new SocketModel();

        model.type = Protocols.Protocol.Data;
        model.area = -1;
        model.command = Protocols.DataProtocol.SET_PLC_DATA;

        SetPlcDataDTO dto = new SetPlcDataDTO();

        dto.plcDatas = plcDatas;
        dto.config = config;
       
        model.message = SerializeTool.Encode2Str(dto);

        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }
}
