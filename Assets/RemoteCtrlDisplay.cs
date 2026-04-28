using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemoteCtrlDisplay : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;
    public Text btnInfo;
    public string key;
    void Start()
    {
        InvokeRepeating("UpdateUI", 1, 1);
    }

    public void UpdateUI()
    {
        if (plcConfigManager.GetBool(key))
        {
            btnInfo.color = Color.green;
        }
        else
        {
            btnInfo.color = Color.white;
        }
    }

   
}
