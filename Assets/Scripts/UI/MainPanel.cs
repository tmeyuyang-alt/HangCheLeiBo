using Protocols;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : UIPanel
{
    public TitleToggle[] toggles;

    private TitleToggle lastToggle;
    private TitlePanel titlePanel = null;
    public static int CurrentIndex = -1;

    public DataList[] dataLists;

    public Text LastWorkElectricity;
    public Text CurWorkElectricity;
    public Text SetWorkElectricity;
    public Text Gears;
    public Text SwitchStatusText;
    public Text RemoteStatusText;


    private List<ElectrodeParamPanel> m_electrodeParamPanels = new List<ElectrodeParamPanel>();
    /// <summary>
    /// 电极标签
    /// </summary>
    private List<GameObject> m_electrodeLabels = new List<GameObject>();

    public Button ManualMode;
    public Button AutomaticMode;
    public Button UpShift;
    public Button DownShift;

    public GameObject electrodePanel3D;
    public GameObject nameObj;

    public static GameObject electrodePanels = null;
    public Dictionary<string, TemperaturePanel3d> TemperaturePanel3dDic = new Dictionary<string, TemperaturePanel3d>();
    public Dictionary<string, AlarmStatusPanel3D> AlarmStatusPanel3dDic = new Dictionary<string, AlarmStatusPanel3D>();
    //public Dictionary<string, TiltArrow> TiltArrow3dDic = new Dictionary<string, TiltArrow>();


    public int Bind(TitleToggle toggle)
    {
        toggle.OnClick += () =>
        {

            if (CurrentIndex != toggle.id)
            {
                CurrentIndex = toggle.id;
                Selected(toggle);
            }
            else
            {
                UnSelected(toggle);
                CurrentIndex = -1;
            }

        };
        return toggle.id;
    }

    public Button SetElectricityBtn;
    private void Awake()
    {
        EventCenter.Instance.RegisterEventHandler(EventName.ChangeModelMirror, UpdatePosition);
    }
    private void Start()
    {
        //SetElectricityBtn.interactable = GlobalInfo.user.permission == 3;

        TemperaturePanel3dDic.Clear();
        AlarmStatusPanel3dDic.Clear();
        //TiltArrow3dDic.Clear();

        this.Gears.GetComponentInParent<Button>().onClick.AddListener(() =>
        {
            string value = "0";

            value = Gears.text.Replace("挡", "").Trim();
            
            UIManager.Instance.OpenPanel<GearsParameterSettingsPanel>(value);
        });
        SetElectricityBtn.onClick.AddListener(() =>
        {
            string value = "0";

            value = SetElectricityBtn.transform.Find("setworkele").GetComponent<Text>().text;

            int intValue = 0;

            if (int.TryParse(value, out intValue))
                value = intValue.ToString();
            else
                value = "0";

            UIManager.Instance.OpenPanel<ParameterSettingsPanel>(value);
        });

        UpShift.onClick.AddListener(() =>
        {
            StartCoroutine(TriggerGearsSwitch(true));
        });

        DownShift.onClick.AddListener(() =>
        {
            StartCoroutine(TriggerGearsSwitch(false));
        });

        if (GlobalInfo.user.permission == 3 || GlobalInfo.user.permission == 2)
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                int index = i;
                //if (GlobalInfo.user.permission == 2 && i != 1)
                //{
                //    toggles[i].enabled = false;
                //    continue;
                //}
                toggles[i].OnClick += () =>
                {

                    if (CurrentIndex != index)
                    {
                        CurrentIndex = index;
                        Selected(toggles[index]);
                    }
                    else
                    {
                        UnSelected(toggles[index]);
                        CurrentIndex = -1;
                    }

                };
            }
        }
        else
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                int index = i;
                toggles[i].enabled = false;
            }
        }

        //创建3dElectrodePanel
        var electrode_obj = GameObject.Find("dianji");
        var arrowRes = Resources.Load("Arrow");

        this.m_electrodeParamPanels.Clear();

        if(electrodePanels==null)
            electrodePanels = new GameObject("electrodePanels");

        for (int i = 0; i < 6; i++)
        {
            GameObject electrode = GameObject.Instantiate(electrodePanel3D);

            var electrodeData = DataUtil.GetConfigData<ElectrodeConfigData>(GlobalInfo.m_ElectrodeData.config, i);
            var electrodeTiltData = DataUtil.GetConfigData<ElectrodeTiltConfigData>(GlobalInfo.m_ElectrodeTiltData.config, i);

            //electrode.transform.Find("BG/TitleText").GetComponent<Text>().text = electrodeData.name;
            electrode.GetComponent<ElectrodeParamPanel>().electrodeName = electrodeData.name;

            //var editable = electrode.AddComponent<EditablePosUI>();

            //editable.key = electrodeData.name;

            electrode.GetComponentInChildren<TitleToggle>().id = 1000 + i;

            m_electrodeParamPanels.Add(electrode.GetComponent<ElectrodeParamPanel>());

            electrode.transform.parent = electrodePanels.transform;
            //DataHandler.getInstance.OnGetPlcDataCallback += (string n,List<PLCData> o) => {
            //};

            if (GlobalInfo.user.permission == 1)
            {
                electrode.transform.GetChild(0).GetComponent<TitleToggle>().enabled = false;
            }

            float x = Mathf.Cos(-1 * Mathf.PI * 0.3333f * (i + 2));
            float z = Mathf.Sin(-1 * Mathf.PI * 0.3333f * (i + 2));

            //箭头
            var arrow = GameObject.Instantiate(arrowRes) as GameObject;
            arrow.transform.position = electrode_obj.transform.position + new Vector3(2.5f * x, 1.8f, 2.5f * z);

            arrow.GetComponent<TiltArrow>().uifoward = -new Vector3(x, 0, z).normalized;

            //TiltArrow3dDic.Add(electrodeTiltData.name, arrow.GetComponent<TiltArrow>());

            float radius = 3.5f;

            //面板
            //electrode.transform.position = electrode_obj.transform.position +new Vector3(4.5f * x,0.25f, 4.5f * z);
            electrode.transform.position = electrode_obj.transform.position + new Vector3(radius * x, 2.0f, radius * z);
            electrode.transform.forward = -new Vector3(x, 0, z).normalized;

            ////倾斜角度
            //tiltUI.transform.position = electrode_obj.transform.position + new Vector3(2.5f * x, 1.8f, 2.5f * z);
            //tiltUI.transform.forward = - new Vector3(x, 0, z).normalized;

            //名字
            var r = 2.5f;
            var nameGo = GameObject.Instantiate(this.nameObj);

            var pos = electrode_obj.transform.position + new Vector3(r * x, -0.9f, r * z);

            var vec2 = electrode_obj.transform.position;

            var vec = pos - vec2;

            if (vec.x > 0)
                nameGo.transform.position = pos + Vector3.forward * 0.65f + Vector3.right * 0.25f;
            else
                nameGo.transform.position = pos + Vector3.forward * 0.65f - Vector3.right * 0.25f;

            nameGo.GetComponentInChildren<Text>().text = electrodeData.name;

            this.m_electrodeLabels.Add(nameGo);
            //var center = electrode_obj.transform.position;
            //center.y = nameGo.transform.position.y;
            //nameGo.transform.LookAt(center);
        }

        dataLists[0].BindGetTextFunc((item) => { return item.GetChild(1).GetComponent<Text>(); },
                                     (item) => { return item.GetChild(2).GetComponent<Text>(); });

        dataLists[0].onValueChanged += (Transform item, object obj) =>
        {
            if (obj != null)
            {
                float value = 0;
                float.TryParse(obj.ToString(),out value); //ToFloat(obj);

                item.Find("bar").GetChild(0).GetComponent<Image>().fillAmount =  value/ 2000.0f;
            }
        };


        dataLists[1].BindGetTextFunc((item) => { return item.GetChild(0).GetComponent<Text>(); },
                                     (item) => { return item.GetChild(1).GetComponent<Text>(); });


        //数据转化
        for (int i = 0; i < GlobalInfo.m_SecondaryData.config.Count; i++)
        {
            DataUtil.GetConfigData<ConfigDataBase>(GlobalInfo.m_SecondaryData.config, i);
        }
        for (int i = 0; i < GlobalInfo.m_ElectricFurnaceTempe.config.Count; i++)
        {
            DataUtil.GetConfigData<ElectricFurnaceTempeData>(GlobalInfo.m_ElectricFurnaceTempe.config, i);
        }
        for (int i = 0; i < GlobalInfo.m_FirstData.config.Count; i++)
        {
            DataUtil.GetConfigData<ConfigDataBase>(GlobalInfo.m_FirstData.config, i);
        }


        dataLists[0].SetConfig(GlobalInfo.m_SecondaryData.config);
        dataLists[1].SetConfig(GlobalInfo.m_ElectricFurnaceTempe.config);


        //创建温度面板
        var tempConfig = GlobalInfo.m_ElectricFurnaceTempe.config;
        Vector3 lastPos = Vector3.zero;
        int tempID = 0;
        for (int i = 0; i < tempConfig.Count; i++)
        {

            var name = DataUtil.GetConfigData<ElectricFurnaceTempeData>(tempConfig, i).name;

            if (DataUtil.GetConfigData<ElectricFurnaceTempeData>(tempConfig, i).highlimit_datablock == "")
            {
                continue;
            }

            var temppanel = GameObject.Instantiate(Resources.Load("TemperaturePanel3d")) as GameObject;


            if (tempID < 7)
            {
                temppanel.transform.position += Vector3.up * tempID * 0.8f;
            }
            else
            {
                temppanel.transform.position += Vector3.right * (tempID - 7) * 4;
                //lastPos = temppanel.transform.position;
            }
            // (tempConfig[i] as ElectricFurnaceTempeData).name;
            var panelcom =  temppanel.GetComponent<TemperaturePanel3d>();
            panelcom.SetName(name);

            if (!TemperaturePanel3dDic.ContainsKey(name))
                TemperaturePanel3dDic.Add(name, panelcom);

            tempID++;
        }

        //创建三维状态数据  [2000,2999] 
        var alarmStatus = GlobalInfo.m_AlarmStatus3D.config;
        int alaramId = 0;
        for (int i = 0; i < alarmStatus.Count; i++)
        {
            var name = DataUtil.GetConfigData<ElectricFurnaceTempeData>(alarmStatus, i).name;

            var panel = GameObject.Instantiate(Resources.Load("Status")) as GameObject;

            var panelcom = panel.GetComponent<AlarmStatusPanel3D>();

            panel.AddComponent<AddMainPanelToggle>();
            panel.GetComponent<TitleToggle>().id = 2000 + alaramId;

            if (GlobalInfo.user.permission != 3)
            {
                panel.GetComponent<TitleToggle>().enabled = false;
            }


            panelcom.SetName(name);

            AlarmStatusPanel3dDic.Add(name, panelcom);

            panel.transform.position += Vector3.up * alaramId;

            alaramId++;
        }

        {
            //var toggle = GameObject.Find("dianji").GetComponent<Toggle3D>();
            //Bind(toggle);

            var toggles = GameObject.FindObjectsOfType<Toggle3D>();
            foreach (var toggle in toggles)
            {
                Bind(toggle);
            }
        }

        InvokeRepeating("UpdateUI", 0, 1/30.0f);

        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }

    public void UpdateOtherPlcData(List<PLCData> plcData)
    {
        //if (configName == "Electricity" || configName == GlobalInfo.otherPlcDataConfig)
        {
            var lastEle = plcData.Find(s => s.Name == "LastWorkElectricity");
            var curEle = plcData.Find(s => s.Name == "CurWorkElectricity");
            var setele = plcData.Find(s => s.Name == "SetWorkElectricity");

            if (lastEle != null)
                LastWorkElectricity.text = lastEle.Value + "  <size=18><color=#FFFFF>kw/h</color></size>";
            if (curEle != null)
                CurWorkElectricity.text = curEle.Value + "  <size=18><color=#FFFFF>kw/h</color></size>";
            if (setele != null)
                SetWorkElectricity.text = setele.Value;// + "  <size=18><color=#FFFFF>A</color></size>";
        }
        //else if (configName == "Gears" || configName == GlobalInfo.otherPlcDataConfig)
        {
            //if (plcData.Count > 0)
            //    Gears.text = plcData[0].Value + "档";

            foreach (var item in plcData)
            {
                if (item.Name == "Gears")
                    Gears.text = item.Value + "挡";
            }

        }
        //else if (configName == "SystemMode" || configName == GlobalInfo.otherPlcDataConfig)
        {
            var autoMode = plcData.Find(s => s.Name == "AutomaticMode");
            //var manualMode = plcData.Find(s => s.Name == "ManualMode");

            var SwitchStatus = plcData.Find(s => s.Name == "SwitchStatus");
            var RemoteStatus = plcData.Find(s => s.Name == "RemoteStatus");


            bool a = false;
            if (autoMode != null)
                bool.TryParse(autoMode.Value, out a);

            //bool b = false;
            //if (manualMode != null)
            //    bool.TryParse(manualMode.Value, out b);


            bool bSwitch = false;
            if (SwitchStatus != null)
                bool.TryParse(SwitchStatus.Value, out bSwitch);

            bool bRemote = false;
            if (RemoteStatus != null)
                bool.TryParse(RemoteStatus.Value, out bRemote);


            SwitchStatusText.text = "分合闸：" + (bSwitch ? "合" : "分");
            //RemoteStatusText.text = "远程模式：" + (bRemote ? "开" : "关");
            RemoteStatusText.text = bRemote?"远程模式" :"本地模式";


            this.AutomaticMode.interactable = a;
            this.ManualMode.interactable = !a;

        }
    }
        
    public void OnGetPlcDataCallback(string configName, List<PLCData> plcData)
    {
        if (configName == GlobalInfo.otherPlcDataConfig)
        {
            UpdateOtherPlcData(plcData);
        }
        else if (configName == typeof(PlcElecFurnaceDataConfig).Name)
        {
            var datas = plcData;

            for (int i = 0; i < datas.Count; i++)
            {
                if (TemperaturePanel3dDic.ContainsKey(datas[i].Name))
                {
                    TemperaturePanel3dDic[datas[i].Name].OnDataUpdate(datas[i].SubName, datas[i].Value);
                }
            }
        }
        else if (configName == typeof(PlcAlarmStatus3DConfig).Name)
        {
            var datas = plcData;

            for (int i = 0; i < datas.Count; i++)
            {
                if (AlarmStatusPanel3dDic.ContainsKey(datas[i].Name))
                {

                    AlarmStatusPanel3dDic[datas[i].Name].SetName(datas[i].Name, bool.Parse(datas[i].Value));
                }
            }
        }
        //else if (configName == typeof(PlcElectrodeTiltDataConfig).Name)
        //{
        //    var datas = plcData;

        //    for (int i = 0; i < datas.Count; i++)
        //    {
        //      Debug.Log(datas[i].Name);
        //    }
        //}

        if (configName == typeof(PlcElectrodeDataConfig).Name)
        {
            foreach (var electrode in m_electrodeParamPanels)
            {
                electrode.OnReceiveData(plcData, false);
            }
        }
        else if (configName == typeof(PlcElectrodeTiltDataConfig).Name)
        {
            //for (int i = 0; i < plcData.Count; i++)
            //{
            //    if (TiltArrow3dDic.ContainsKey(plcData[i].Name))
            //    {
            //        TiltArrow3dDic[plcData[i].Name].OnReceiveData(plcData[i]);
            //    }
            //}
            foreach (var electrode in m_electrodeParamPanels)
            {
                electrode.OnReceiveData(plcData, true);
            }
        }
    }


    public override void OnEnter(object param)
    {
        base.OnEnter(param);

        if (titlePanel == null)
            titlePanel = GameObject.FindObjectOfType<TitlePanel>();
        titlePanel?.ShowClose();
    }
    private float timer = 0;

    public void Update()
    {
        //电极数据
        timer += Time.deltaTime;
        if (timer  > 0.05f)
        {
            timer = 0;
            DataCenter.Instance.GetPlcData(typeof(PlcElectrodeDataConfig).Name);
            DataCenter.Instance.GetPlcData(typeof(PlcElectrodeTiltDataConfig).Name);
            DataCenter.Instance.GetPlcData(GlobalInfo.otherPlcDataConfig);
        }
    }


    private void UpdateUI()
    {
        //var plc = DataCenter.Instance.GetPLCConnect("StaticConfig");

        //if (plc == null) return;
        //LastWorkElectricity.text = ToString(plc.Read(GlobalInfo.m_StaticConfig.LastWorkElectricity)) + "  <size=18><color=#FFFFF>kw/h</color></size>";
        //CurWorkElectricity.text = ToString(plc.Read(GlobalInfo.m_StaticConfig.CurWorkElectricity))+ "  <size=18><color=#FFFFF>kw/h</color></size>";
        //SetWorkElectricity.text = ToString(plc.Read(GlobalInfo.m_StaticConfig.SetWorkElectricity))+ "  <size=18><color=#FFFFF>A</color></size>";
        //Gears.text = ToString(plc.Read(GlobalInfo.m_StaticConfig.Gears))+ "档";

        DataCenter.Instance.GetPlcData("SystemMode");
        DataCenter.Instance.GetPlcData("Electricity");
        DataCenter.Instance.GetPlcData("Gears");
        DataCenter.Instance.GetPlcData(typeof(PlcAlarmStatus3DConfig).Name);



    }
    private void UpdatePosition(object sender,EventArgs args)
    {
        var center_obj = GameObject.Find("dianji");

        for (int i = 0; i < m_electrodeParamPanels.Count; i++)
        {
            GameObject electrode = m_electrodeParamPanels[i].gameObject;


            float x = Mathf.Cos(-1 * Mathf.PI * 0.3333f * (i + 2));
            float z = Mathf.Sin(-1 * Mathf.PI * 0.3333f * (i + 2));

            float radius = 3.5f;

            //面板
            electrode.transform.position = center_obj.transform.position + new Vector3(radius * x, 2.0f, radius * z);
            electrode.transform.forward = -new Vector3(x, 0, z).normalized;

            m_electrodeParamPanels[i].initalPos = electrode.transform.position;
            m_electrodeParamPanels[i].centerPos = center_obj.transform.position;
            m_electrodeParamPanels[i].Inital();
            //名字
            var r = 2.5f;

            var pos = center_obj.transform.position + new Vector3(r * x, -0.9f, r * z);

            var vec2 = center_obj.transform.position;

            var vec = pos - vec2;

            var nameGo = this.m_electrodeLabels[i];

            if (vec.x > 0)
                nameGo.transform.position = pos + Vector3.forward * 0.65f + Vector3.right * 0.25f;
            else
                nameGo.transform.position = pos + Vector3.forward * 0.65f - Vector3.right * 0.25f;

        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="intValue"></param>
    public void SetGears(int intValue)
    {
        //string value = "0";

        //value = Gears.text.Replace("档", "").Trim();

        //int intValue = 0;

        //if (int.TryParse(value, out intValue))
        //{
        //    if (intValue == 1) return;

        //    --intValue;
        //    Gears.text = intValue + "档";
        //}

        SetPlcDataDTO dto = new SetPlcDataDTO();

        dto.plcDatas = new List<PLCData>();

        dto.config = "Gears";

        dto.plcDatas.Add(new PLCData() { Name = "Gears", Type = "int", Value = intValue.ToString() });

        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.SET_PLC_DATA;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    IEnumerator TriggerGearsSwitch(bool up)
    {
        SetShift(up, true);
        yield return new WaitForSeconds(0.3f);
        SetShift(up, false);
    }
    private void SetShift(bool up, bool value)
    {
        SetPlcDataDTO dto = new SetPlcDataDTO();

        dto.plcDatas = new List<PLCData>();

        dto.config =  "Gears";

        if (up)
            dto.plcDatas.Add(new PLCData() { Name = "UpShift", Type = "bool", Value = value.ToString() });
        else
            dto.plcDatas.Add(new PLCData() { Name = "DownShift", Type = "bool", Value = value.ToString() });

        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.SET_PLC_DATA;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

    public string ToString(object obj) {
        if (obj == null) return "--";
        else return obj.ToString();
    }
    public void Selected(TitleToggle toggle)
    {
        if (lastToggle != null)
        {
            lastToggle.IsOn = false;
        }

        lastToggle = toggle;
    }
    public void UnSelected(TitleToggle toggle)
    {
        toggle.IsOn = false;

        lastToggle = null;
    }

    public override void OnClose()
    {
        base.OnClose();

        //DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }

    public void OnDestroy()
    {
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
        EventCenter.Instance.UnRegisterEventHandler(EventName.ChangeModelMirror, UpdatePosition);
    }
}
