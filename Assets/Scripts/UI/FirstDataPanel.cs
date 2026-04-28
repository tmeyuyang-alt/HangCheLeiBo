using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FirstDataPanel : MonoBehaviour
{
    public Transform[] Circles;
    public Text powerFactor;
    public Text reactivePower;
    public Text activePower;
    public Text primaryVoltage;

    private List<object> config;

    private Dictionary<string, object> datas = new Dictionary<string, object>();

    void Start()
    {
        config = GlobalInfo.m_FirstData.config;

        InvokeRepeating("UpdateUI", 0, 1/30.0f);

        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;

        UpdateUnlit();
    }
    private void SetCircle(Transform item,float value,string unlit)
    {
        item.Find("Circle").GetComponentInChildren<Image>().fillAmount = value/ 300.0f;
        var valueText = item.Find("Value").GetComponentInChildren<Text>();
        valueText.text = value.ToString();
        //valueText.transform.GetChild(0).GetComponent<Text>().text = unlit;
    }
    private void UpdateUI()
    {
        DataCenter.Instance.GetPlcData("PlcFirstDataConfig");
    }
    private Dictionary<string, ConfigDataBase> unlitDic = new Dictionary<string, ConfigDataBase>();

    private string GetUnlit(string name)
    {
        if (unlitDic.ContainsKey(name))
            return unlitDic[name].unlit;
        return "";
    }
    private void UpdateUnlit()
    {
        unlitDic.Clear();

        foreach (var item in config)
        {
            if (item is ConfigDataBase)
            {
                var cdb = item as ConfigDataBase;
                var name = cdb.name;
                if (!unlitDic.ContainsKey(name))
                    unlitDic.Add(name, cdb);
            }
        }
    }
    public void OnGetPlcDataCallback(string config, List<PLCData> plcData)
    {
        if (config != "PlcFirstDataConfig") return;

        if (plcData == null) return;

        Dictionary<string, string> tempDir = new Dictionary<string, string>();
        foreach (PLCData data in plcData)
        {
            if (!tempDir.ContainsKey(data.Name))
                tempDir.Add(data.Name, data.Value);
        }

        if (tempDir.Count == 0) return;

        if (tempDir.ContainsKey("一次电流A"))
        {
            SetCircle(Circles[0], float.Parse(tempDir["一次电流A"]), GetUnlit("一次电流A"));
        }
        if (tempDir.ContainsKey("一次电流B"))
        {
            SetCircle(Circles[1], float.Parse(tempDir["一次电流B"]),GetUnlit("一次电流B"));
        }
        if (tempDir.ContainsKey("一次电流C"))
        {
            SetCircle(Circles[2], float.Parse(tempDir["一次电流C"]),GetUnlit("一次电流C"));
        }
        if (tempDir.ContainsKey("功率因数"))
        {
            powerFactor.text = tempDir["功率因数"].ToString();
            //powerFactor.transform.GetChild(0).GetComponent<Text>().text = GetUnlit("功率因数");
        }
        if (tempDir.ContainsKey("无功功率"))
        {
            reactivePower.text = tempDir["无功功率"].ToString();
            //reactivePower.transform.GetChild(0).GetComponent<Text>().text = GetUnlit("无功功率");
        }
        if (tempDir.ContainsKey("有功功率"))
        {
            activePower.text = tempDir["有功功率"].ToString();
            //activePower.transform.GetChild(0).GetComponent<Text>().text = GetUnlit("有功功率");
        }
        if (tempDir.ContainsKey("一次电压"))
        {
            primaryVoltage.text = tempDir["一次电压"].ToString();
            //primaryVoltage.transform.GetChild(0).GetComponent<Text>().text = GetUnlit("一次电压");
        }
    }
    private void OnDestroy()
    {
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }
}
