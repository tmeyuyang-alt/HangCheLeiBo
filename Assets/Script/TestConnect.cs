using S7.Net.Types;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConnect : MonoBehaviour
{

    PLCConnect plcCon = new PLCConnect();

    List<DataItem> list = new List<DataItem>();
    // Start is called before the first frame update
    void Start()
    {
        plcCon.ipaddrees = "192.168.1.50";
        plcCon.port = 102;
        plcCon.rack = 0;
        plcCon.slot = 0;

        plcCon.Open();


        //List<DataItem> list = new List<DataItem>();

        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));
        list.Add(DataItem.FromAddress("DB5.DBD32"));

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 5 == 0)
        {

            var lastTime = System.DateTime.Now;

            plcCon.GetPlc().ReadMultipleVars(list);

            for (int i = 0; i < list.Count; i++)
            {
                Debug.Log(list[i].Value);
            }
        }
        //Debug.Log((System.DateTime.Now - lastTime).TotalSeconds);

        //PLCConfigManager.Instance.GetValue(DataBlockKey.ForwardLimit);
        //PLCConfigManager.Instance.GetValue(DataBlockKey.ForwardLimit);

    }
}
