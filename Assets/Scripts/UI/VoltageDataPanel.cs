using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XCharts;
using XCharts.Runtime;

public class VoltageDataPanel : MonoBehaviour
{

    public BarChart barChart ;

    public Transform Values;
    void Start()
    {
        InvokeRepeating("UpdateData", 0, 1/30.0f);

        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }
    private List<string> keys = new List<string>();
    private Dictionary<string, float> datas = new Dictionary<string, float> ();

    private void OnGetPlcDataCallback(string configName, List<PLCData> plcDatas)
    {
        if (configName != "PlcVoltageDataConfig") return;

        Dictionary<string, string> tempValues = new Dictionary<string, string>();
        foreach (var item in plcDatas)
        {
            if (!tempValues.ContainsKey(item.DB))
            {
                tempValues.Add(item.DB, item.Value);
            }
        }

        keys.Clear();

        var config = GlobalInfo.m_VoltageData.config;

        for (int i = 0; i < config.Count; i++)
        {
            var cdb = DataUtil.GetConfigData<ConfigDataBase>(config, i);

            if (cdb == null) continue;

            if (keys.Count < config.Count)
            {
                keys.Add(cdb.name);
            }

            if (tempValues.ContainsKey(cdb.datablock))
            {
                var value = tempValues[cdb.datablock];

                if (datas.ContainsKey(cdb.name))
                {
                    if (value != null)
                        datas[cdb.name] = float.Parse(value.ToString());
                }
                else
                {
                    if (value != null)
                        datas.Add(cdb.name, float.Parse(value.ToString()));
                }
            }
        }

        UpdateUI();
    }
    private void UpdateData()
    {

        DataCenter.Instance.GetPlcData("PlcVoltageDataConfig");
    }

    private void UpdateUI()
    {
        int index = 0;
        foreach (var key in keys)
        {
            if (datas.ContainsKey(key))
            {
                Values.GetChild(index).GetComponent<Text>().text = datas[key].ToString();
                barChart.UpdateXAxisData(index, key);
                barChart.UpdateData(0, index++, datas[key]);
            }

        }
    }

    private void OnDestroy()
    {

        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }
}
