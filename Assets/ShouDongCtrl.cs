using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ShouDongCtrl : MonoBehaviour
{
    public string GlobalDianDongKey;
    
    public PLCConfigManager plcConfigManager;

    private void OnEnable()
    {
        plcConfigManager.SetValueNoNotify(GlobalDianDongKey,true);
        print("true");
    }

    private void OnDisable()
    {
        plcConfigManager.SetValueNoNotify(GlobalDianDongKey,false);
    }

   
   
    
}
