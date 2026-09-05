using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemoteCtrlDisplay : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;
    public Text btnInfo;
    public string RemoteKey,DriverKey,FarKey;
    //public string 
    void Start()
    {
        InvokeRepeating("UpdateUI", 1, 1);
    }

    public void UpdateUI()
    {
        if (plcConfigManager.GetBool(RemoteKey))
        {
            btnInfo.color = Color.green;
            btnInfo.text = "遥控器控制";
        } 
         if (plcConfigManager.GetBool(DriverKey))
        {
            btnInfo.color = Color.green;
            btnInfo.text = "驾驶控制";
        }
        if (plcConfigManager.GetBool(FarKey))
        {
            btnInfo.color = Color.white;
            btnInfo.text = "远程控制";
        }

       
    }

   
}
