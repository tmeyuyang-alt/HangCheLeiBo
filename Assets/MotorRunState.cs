using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DisplayItem
{
    public string Key;
    public Text info;
    
}
public class MotorRunState : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;
    
    public List<DisplayItem> displayItems;


    private void Start()
    {
        
        InvokeRepeating("UpdateUI",1,1);
    }

    public void UpdateUI()
    {
        foreach (DisplayItem item in displayItems)
        {
           item.info.text= plcConfigManager.GetFloatValue(item.Key).ToString("F1");
        }
    }
}
