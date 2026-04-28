using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiaoHeightDispla : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;

    public string DeviceName;

    public string dbName;
    public Text info;

    private void Start()
    {
        dbName = "料仓设置料仓"+DeviceName+"_料仓料位";
        info =transform.GetChild(0).GetComponent<Text>();
        InvokeRepeating("UpdateUI", 0.2f, 1f);
        
    }

    public void UpdateUI()
    {
        info.text = plcConfigManager.GetFloatValue(dbName).ToString("F2")+"m";
    }
}
