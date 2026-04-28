using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShouDongCtrl : MonoBehaviour
{
    public string GlobalDianDongKey;
    
    public PLCConfigManager plcConfigManager;

    private void OnEnable()
    {
        plcConfigManager.SetValueNoNotify(GlobalDianDongKey,true);
    }
}
