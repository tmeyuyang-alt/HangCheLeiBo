using Protocols;
using S7.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HisWarningConfigPanel : UIPanel
{

    public Dropdown m_PlcType;

    public Button SaveBtn;
    public Button AddBtn;
    public Button DelBtn;

    public UGuiTable m_Table;
    private PlcHisWarningDataConfig m_HisWarningConfig;


    public int GetPlcIndex(string plctype)
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

    public void GetPlcConnectStatus()
    {
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.PLC_CONNECT_STATUS;
        model.message = typeof(PlcHisWarningDataConfig).Name;
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);

    }

    void Start()
    {
        m_HisWarningConfig = GlobalInfo.m_HisWarningData;

        SaveBtn.onClick.AddListener(Save);
        DelBtn.onClick.AddListener(Delete);
        AddBtn.onClick.AddListener(AddItem);

        DataHandler.getInstance.OnGetPlcStatusCallback += OnGetPlcStatusCallback;
        //获取连接状态
        InvokeRepeating("GetPlcConnectStatus", 0, 0.8f);

        //PLC

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

        options.Add(new Dropdown.OptionData(CpuType.S7200.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.Logo0BA8.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7200Smart.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7300.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S7400.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S71200.ToString(), null));
        options.Add(new Dropdown.OptionData(CpuType.S71500.ToString(), null));

        m_PlcType.AddOptions(options);

        //获取当前历史警告配置
        //m_HisWarningConfig = 
      

        m_PlcType.onValueChanged.AddListener((value) =>
        {
            string plcName = m_PlcType.captionText.text;

            var index = m_Table.GetActiveLine();

            m_Table.GetItem(index, 0).GetComponent<InputField>().text = plcName;
        });

        m_Table.Clear(1);
        UpdateTable();

        
        m_Table.OnTextChanged += (x, y, text) =>
        {
            var item = m_HisWarningConfig.Config[y-1];

            switch (x)
            {
                case 0:
                    item.PlcType = text; break;
                case 1:
                    item.IPAddress = text;
                    break;
                case 2:
                    item.Rack = short.Parse(text);
                    break;
                case 3:
                    item.Slot = short.Parse(text);
                    break;
                case 4:
                    item.WarningName = text;
                    break;
                case 5:
                    item.WarningGroup = int.Parse(text);
                    break;
                case 6:
                    item.LimitValueDB = text; break;
                case 7:
                    item.WarningSignalDB = text; break;
            }
        };

    }

    private void OnGetPlcStatusCallback(string arg1, Dictionary<string, string> arg2)
    {
        if (arg1 == typeof(PlcHisWarningDataConfig).Name)
        {
            //TODO 更新状态

            int count = m_HisWarningConfig.Config.Count;

            for (int i = 0; i < count; i++)
            {
                m_Table.GetItem(i + 1, 8).GetComponent<Text>().text = "未连接";
            }

            for (int i = 0; i < count; i++)
            {
                string ipaddress = m_Table.GetItem(i+1, 1).GetComponent<InputField>().text;
                string rack = m_Table.GetItem(i+1, 2).GetComponent<InputField>().text;
                string slot = m_Table.GetItem(i+1, 3).GetComponent<InputField>().text;

                string key = ipaddress +":"+ rack +":"+ slot;
                if (arg2.ContainsKey(key))
                    m_Table.GetItem(i+1, 8).GetComponent<Text>().text = arg2[key];
            }
        }
    }

    public void Save()
    {
        if (m_HisWarningConfig != null)
        {
            m_HisWarningConfig.Save();


            //TODO 发送到服务器端
            {

                SocketModel model = new SocketModel();

                UpdateConfigDTO dto = new UpdateConfigDTO();
                dto.ConfigName = typeof(PlcHisWarningDataConfig).Name;
                
                string path = Application.streamingAssetsPath + "/hiswarning_data_config.yaml";

                dto.ConfigData = System.IO.File.ReadAllText(path);

                model.type = Protocol.Data;
                model.command = DataProtocol.UPDATE_CONFIG;

                model.message = SerializeTool.Encode2Str(dto);

                model.senderID = GlobalInfo.user.uid.ToString();

                model.token = GlobalInfo.user.token;

                ClientManager.getInstance.SendServer(model);
            }
        }
    }

    public void Delete()
    {
        var index = m_Table.GetActiveLine()-1;

        if (index < m_HisWarningConfig.Config.Count)
        {
            m_HisWarningConfig.Config.RemoveAt(index);

            UpdateTable();
        }
    }

    public void AddItem()
    {
        m_HisWarningConfig.Config.Add(new HisWarningDataConfigData() { WarningName = "新建项" });

        if (m_HisWarningConfig.Config.Count >= m_Table.Row - 1)
            m_Table.AddRowOne();
        UpdateTable();
    }

    private void UpdateTable()
    {
        int i = 0;
        for (; i < m_HisWarningConfig.Config.Count; i++)
        {
            var item = m_HisWarningConfig.Config[i];

            if (m_HisWarningConfig.Config.Count >= m_Table.Row - 1)
                m_Table.AddRowOne();
            //if (item != null)
            {
                //Debug.Log(m_Table.GetItem(i+1, 0).name);

                m_Table.GetItem(i + 1, 0).GetComponent<InputField>().text = item.PlcType;
                m_Table.GetItem(i + 1, 1).GetComponent<InputField>().text = item.IPAddress;
                m_Table.GetItem(i + 1, 2).GetComponent<InputField>().text = item.Rack.ToString();
                m_Table.GetItem(i + 1, 3).GetComponent<InputField>().text = item.Slot.ToString();
                m_Table.GetItem(i + 1, 4).GetComponent<InputField>().text = item.WarningName;
                m_Table.GetItem(i + 1, 5).GetComponent<InputField>().text = item.WarningGroup.ToString();
                m_Table.GetItem(i + 1, 6).GetComponent<InputField>().text = item.LimitValueDB.ToString();
                m_Table.GetItem(i + 1, 7).GetComponent<InputField>().text = item.WarningSignalDB.ToString();
                //m_Table.GetItem(i + 1, 8).GetComponent<Text>().text = "连接成功！";



                m_Table.GetItem(i + 1, 0).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 1).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 2).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 3).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 4).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 5).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 6).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 7).gameObject.SetActive(true);
                m_Table.GetItem(i + 1, 8).gameObject.SetActive(true);
            }
        }

        m_Table.Clear(i+1);

    }

    public override void OnClose()
    {
        base.OnClose();

        DataHandler.getInstance.OnGetPlcStatusCallback -= OnGetPlcStatusCallback;
        //CancelInvoke("OnGetPlcStatusCallback");
    }
}