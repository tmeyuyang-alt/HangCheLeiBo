using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XCharts;
using XCharts.Runtime;

public class ElectricityDataPanel : MonoBehaviour
{

    public LineChart lineChart;

    private PLC_Connect connect;


    private Dictionary<string, ElectricityConfigData> datas = new Dictionary<string, ElectricityConfigData>();

    public Transform Items;
    public Transform Values;

    private List<string> keys = new List<string>();

    Dictionary<string, Dictionary<string, string>> plcDatasDir;

    private void Start()
    {
        InvokeRepeating("UpdateData", 0, 1/30.0f);
        var config = GlobalInfo.m_ElectricityData.config;

        //为了保证顺序 实用保存键的顺序
        for (int i = 0; i < config.Count; i++)
        {
            var cdb = DataUtil.GetConfigData<ElectricityConfigData>(config, i);

            keys.Add(cdb.name);
        }

        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;

        InitData();

        for (int i = 0; i < keys.Count; i++)
        {
            lineChart.UpdateXAxisData(i, keys[i]);
        }
    }

    private void UpdateKey()
    {
        var config = GlobalInfo.m_ElectricityData.config;
        keys.Clear();
        //为了保证顺序 实用保存键的顺序
        for (int i = 0; i < config.Count; i++)
        {
            var cdb = DataUtil.GetConfigData<ElectricityConfigData>(config, i);

            keys.Add(cdb.name);
        }
    }
    public void OnGetPlcDataCallback(string config, List<PLCData> plcDatas)
    {
        if (config != "PlcElectricityDataConfig") return;


        UpdateKey();

        Dictionary<string, Dictionary<string, string>> tempDir = new Dictionary<string, Dictionary<string, string>>();

        foreach (var data in plcDatas)
        {
            if (!tempDir.ContainsKey(data.Name))
            {
                tempDir.Add(data.Name, new Dictionary<string, string>());
                tempDir[data.Name].Add(data.DB, data.Value);
            }
            else
            {
                if (!tempDir[data.Name].ContainsKey(data.DB))
                    tempDir[data.Name].Add(data.DB, data.Value);
            }
        }
        plcDatasDir = tempDir;

        UpdateUI(datas);
    }
    private void InitData()
    {

        var config = GlobalInfo.m_ElectricityData.config;

        for (int i = 0; i < config.Count; i++)
        {
            var cdb = DataUtil.GetConfigData<ElectricityConfigData>(config, i);

            if (cdb == null) continue;
            if (!datas.ContainsKey(cdb.name))
                datas.Add(cdb.name, cdb);
        }
    }
    private void UpdateData()
    {
        DataCenter.Instance.GetPlcData("PlcElectricityDataConfig");
    }
    private void UpdateUI(Dictionary<string, ElectricityConfigData> objs)
    {
        int index = 0;
        foreach (var key in keys)
        {
            if (plcDatasDir.ContainsKey(key) && plcDatasDir[key] != null)
            {

                string datablock = objs[key].datablock;
                string oper_datablock = objs[key].oper_datablock;
                string bedding_datablock = objs[key].bedding_datablock;
                string up_datablock = objs[key].up_datablock;
                string down_datablock = objs[key].down_datablock;
                string highlimit_alarm_datablock = objs[key].highlimit_alarm_datablock;
                string lowlimit_alarm_datablock = objs[key].lowlimit_alarm_datablock;
                string electricity_alarm_datablock = objs[key].electricity_alarm_datablock;


                float value = 0;
                if (plcDatasDir[key].ContainsKey(datablock))
                    float.TryParse(plcDatasDir[key][datablock], out value);

                bool oper = false;
                if (plcDatasDir[key].ContainsKey(oper_datablock))
                    bool.TryParse(plcDatasDir[key][oper_datablock], out oper);

                bool bedding = false;
                if (plcDatasDir[key].ContainsKey(bedding_datablock))
                    bool.TryParse(plcDatasDir[key][bedding_datablock], out bedding);

                bool up = false;
                if (plcDatasDir[key].ContainsKey(up_datablock))
                    bool.TryParse(plcDatasDir[key][up_datablock], out up);

                bool down = false;
                if (plcDatasDir[key].ContainsKey(down_datablock))
                    bool.TryParse(plcDatasDir[key][down_datablock], out down);

                bool highlimit_alarm = false;
                if (plcDatasDir[key].ContainsKey(highlimit_alarm_datablock))
                    bool.TryParse(plcDatasDir[key][highlimit_alarm_datablock], out highlimit_alarm);

                bool lowlimit_alarm = false;
                if (plcDatasDir[key].ContainsKey(lowlimit_alarm_datablock))
                    bool.TryParse(plcDatasDir[key][lowlimit_alarm_datablock], out lowlimit_alarm);

                bool electricity_alarm = false;
                if (plcDatasDir[key].ContainsKey(electricity_alarm_datablock))
                    bool.TryParse(plcDatasDir[key][electricity_alarm_datablock], out electricity_alarm);

                Items.GetChild(index).GetChild(0).GetComponent<Text>().text = oper ? "自动" : "手动";
                //rgb(122, 191, 255)
                //Items.GetChild(index).GetChild(1).GetComponent<Text>().color = bedding ? new Color(122.0f / 255.0f, 191.0f / 255.0f, 255 / 255.0f)
                //                                                                      : new Color(102.0f / 255.0f, 102.0f / 255.0f, 102.0f / 255.0f);
                Items.GetChild(index).GetChild(1).GetComponent<Text>().enabled = bedding;
                if (up == down && up == false)
                {

                    Items.GetChild(index).GetChild(2).localScale = Vector3.zero;
                }
                else if (up == down && up == true)
                {
                    //TODO异常
                }
                else
                {
                    Items.GetChild(index).GetChild(2).localScale = up ? new Vector3(1, 1, 1) : new Vector3(1, -1, 1);
                    //Items.GetChild(index).GetChild(2).GetComponent<Image>().color = up ? new Color(1, 0.788f, 0.4784f) : new Color(1, 0.2784f, 0.2784f);
                }

                if (highlimit_alarm || lowlimit_alarm)
                {
                    Items.GetChild(index).GetChild(2).localScale = Vector3.zero;
                    Items.GetChild(index).GetChild(3).gameObject.SetActive(true);

                    Items.GetChild(index).GetChild(3).GetComponent<Text>().text = highlimit_alarm ? "高限" : "低限";
                }
                else
                {
                    Items.GetChild(index).GetChild(3).gameObject.SetActive(false);
                }

                var valueText = Values.GetChild(index).GetComponent<Text>();

                valueText.text = value.ToString();

                valueText.color = electricity_alarm ? Color.red : Color.white;


                lineChart.UpdateXAxisData(index, key);
                lineChart.UpdateData(0, index++, value);
            }

        }
    }

    private void OnDestroy()
    {
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }
}
