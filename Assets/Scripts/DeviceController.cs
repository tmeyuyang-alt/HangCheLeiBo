using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceController : MonoBehaviour
{

    public Transform[] Electrodes;
    private Vector3[] m_DefaultPositions;


    private Dictionary<string, float> RealTimeData = new Dictionary<string, float>();
    private List<string> keys = new List<string>();

    private bool left_layout = true;

    private void Awake()
    {
        EventCenter.Instance.RegisterEventHandler(EventName.ChangeModelMirror, OnChanged);
    }
    void Start()
    {
        m_DefaultPositions = new Vector3[Electrodes.Length];
        int index = 0;
        foreach (var item in Electrodes)
        {
            m_DefaultPositions[index++] = item.position;
        }
        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }

    private void OnGetPlcDataCallback(string configName, List<PLCData> datas)
    {
        if (configName == typeof(PlcElectrodeDataConfig).Name)
        {
            RealTimeData.Clear();
            keys.Clear();

            for (int i = 0; i < datas.Count; i++)
            {

                if (datas[i].SubName != "datablock") continue;

                float value = 0;

                if (float.TryParse(datas[i].Value, out value))
                {
                    RealTimeData.Add(datas[i].Name, value);
                    keys.Add(datas[i].Name);

                }
            }

        }
    }

    public void OnChanged(object sender,EventArgs arg)
    {
        left_layout = ((BoolEventArgs)arg).value;


    }
    void Update()
    {
        int i = 0;
        foreach (var key in keys)
        {
            //Electrodes[index].position = m_DefaultPositions[index]  + Vector3.up* RealTimeData[key];


            //var tagetPos = m_DefaultPositions[index]  + Vector3.up* RealTimeData[key]*0.001f;//cm -> m
            //Electrodes[index].position = Vector3.Lerp(Electrodes[index].position, tagetPos, Time.deltaTime * 10);
            int index = 0;

            if (left_layout)
                index = i;
            else
                index = 5 - i;

            float height = m_DefaultPositions[index].y;
            var tagetPos = Electrodes[index].position;
            tagetPos.y = height + RealTimeData[key] * 0.001f;//cm -> m
            Electrodes[index].position = Vector3.Lerp(Electrodes[index].position, tagetPos, Time.deltaTime * 10);

            i++;
        }
    }

    public void OnDestroy()
    {
        EventCenter.Instance.UnRegisterEventHandler(EventName.ChangeModelMirror, OnChanged);
    }
}
