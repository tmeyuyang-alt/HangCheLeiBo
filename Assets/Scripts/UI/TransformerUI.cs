using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransformerUI : MonoBehaviour
{
    public Text Gears;
    void Start()
    {
        DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;
    }
    public void OnGetPlcDataCallback(string configName, List<PLCData> plcData)
    {
        //if (configName == "Gears")
        //{
        //    if (plcData.Count > 0)
        //        Gears.text = plcData[0].Value + "µµ";
        //}

        if (configName == "Gears" || configName == GlobalInfo.otherPlcDataConfig)
        {
            foreach (var item in plcData)
            {
                if (item.Name == "Gears")
                    Gears.text = item.Value + "µ²";
            }

        }
    }

    public void OnDestroy()
    {
        DataHandler.getInstance.OnGetPlcDataCallback -= OnGetPlcDataCallback;
    }
}
