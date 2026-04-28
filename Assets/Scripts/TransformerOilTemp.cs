using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransformerOilTemp : MonoBehaviour
{
    public Text text;
    void Start()
    {
        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }

    private void OnGetPlcDataCallback(string arg1, List<PLCData> arg2)
    {
        //if (arg1 == "±äÑ¹Æ÷ÓÍÎÂ")
        //{

        //}
        if (arg1 == typeof(PlcElecFurnaceDataConfig).Name)
        {
            foreach (PLCData data in arg2)
            {
                if (data.Name == "±äÑ¹Æ÷ÓÍÎÂ" && data.SubName == "datablock")
                {
                    text.text = string.Format("{0}£º<color=#ffc97a>{1}</color>¡æ", data.Name, data.Value);
                }
            }
        }

    }

    public void OnDestroy()
    {
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }
}
