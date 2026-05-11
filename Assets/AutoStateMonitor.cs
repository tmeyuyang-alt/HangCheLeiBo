using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoStateMonitor : MonoBehaviour
{
   // public static AutoStateMonitor instance;
    
    public PLCConfigManager plcConfigManager;

    public bool isAuto=false;

    public string key;
    
    private void Update()
    {
        isAuto = !plcConfigManager.GetBool(key);
    }


   
}
