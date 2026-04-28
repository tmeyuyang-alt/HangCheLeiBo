using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataList : MonoBehaviour
{

    public Transform Group;

    private List<object> config;

    public string PlcConfig = "";

    public int StartIndex = 0;

    public int maxShow = 15;
    private int intervalTime = 5;
    private System.DateTime lastShowTime;
    private int currentPageIndex = 0;
    private int lastCount = 0;

    public System.Func<Transform, Text> getNameTextFunc;
    public System.Func<Transform, Text> getValueTextFunc;

    public System.Action<Transform, object> onValueChanged;

    //private PLC_Connect plc;

    private List<PLCData> datas = new List<PLCData>(15);

    private Dictionary<string,ConfigDataBase> unlitDic = new Dictionary<string, ConfigDataBase>();

    private string GetUnlit(string name)
    {
        if(unlitDic.ContainsKey(name))
            return unlitDic[name].unlit;
        return "";
    }

    public void Start()
    {
        //plc = DataCenter.Instance.GetPLCConnect(PlcConfig);

        InvokeRepeating("UpdateData", 0, 1/30.0f);
        lastShowTime = System.DateTime.Now.AddDays(-1);
        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;

        EventCenter.Instance.RegisterEventHandler(EventName.PlcSettingsPanelSave, OnUpdateConfig);
    }

    public void OnUpdateConfig(object sender, System.EventArgs args)
    {
        UpdateUnlit();
    }
    public void SetConfig(List<object> list)
    {
        config = list;

        UpdateUnlit();
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

    private void UpdateData()
    {
        DataCenter.Instance.GetPlcData(PlcConfig);

        //UpdateUnlit();
    }

    private void UpdatePage(int Count)
    {
        int pageNum = Mathf.CeilToInt(Count * 1.0f / maxShow);


        UpdateUIShow(currentPageIndex);

        ++currentPageIndex;

        if (currentPageIndex >= pageNum)
        {
            currentPageIndex = 0;
        }
    }

    public void OnGetPlcDataCallback(string config, List<PLCData> plcData)
    {
        if (config == null) return;

        if (config != PlcConfig) return;

        if (plcData.Count == 0) return;

        datas.Clear();

        for (int i = 0; i < plcData.Count; i++)
        {
            if (plcData[i].SubName == "datablock")
            {
                datas.Add(plcData[i]);
            }
        }


        if ((System.DateTime.Now - lastShowTime).TotalSeconds > intervalTime)
        {
            UpdatePage(datas.Count);
            lastShowTime = System.DateTime.Now;
        }


        /*
        for (int i = StartIndex; i < Group.childCount; i++)
        {
            Group.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < datas.Count; i++)
        {
            string name = datas[i].Name;
            string value = datas[i].Value;

            if (i +StartIndex>= Group.childCount)
            {
                GameObject.Instantiate(Group.GetChild(StartIndex).gameObject, Group);
            }
            var item = Group.GetChild(i + StartIndex);

            item.gameObject.SetActive(true);

            getNameTextFunc(item).text = name;

            if (value != null)
                getValueTextFunc(item).text = value;

            onValueChanged?.Invoke(item, value);
        }
        */


        if (lastCount != datas.Count)
        {
            currentPageIndex = 0;
        }
        lastCount = datas.Count;

    }

    public void UpdateUIShow(int pageIndex = 0)
    {
        //0 1 2 3  4 5 6 7 8 9

        ClearShow();

        int istart = pageIndex * maxShow;

        int length = Mathf.Min(datas.Count, istart + maxShow);

        int index = 0;

        for (int i = istart; i < length; i++)
        {
            string name = datas[i].Name;
            string value = datas[i].Value;

            if (index + StartIndex >= Group.childCount)
            {
                GameObject.Instantiate(Group.GetChild(StartIndex).gameObject, Group);
            }
            var item = Group.GetChild(index + StartIndex);

            item.gameObject.SetActive(true);

            getNameTextFunc(item).text = name;

            if (value != null)
            {
                var valueText = getValueTextFunc(item);
                valueText.text = value;

                if (valueText.transform.childCount > 0)
                {
                    //通过名字获取单位
                    valueText.transform.GetChild(0).GetComponent<Text>().text = GetUnlit(name);
                }
            }
            onValueChanged?.Invoke(item, value);

            index++;
        }
    }

    public void ClearShow()
    {
        for (int i = StartIndex; i < Group.childCount; i++)
        {
            Group.GetChild(i).gameObject.SetActive(false);
        }

    }

    public void BindGetTextFunc(System.Func<Transform, Text> nameFunc, System.Func<Transform, Text> valueFunc)
    {
        //return func(null);
        getNameTextFunc = nameFunc;
        getValueTextFunc = valueFunc;
    }

    public void OnDestroy()
    {
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;

        EventCenter.Instance.UnRegisterEventHandler(EventName.PlcSettingsPanelSave, OnUpdateConfig);
        //DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }
}
