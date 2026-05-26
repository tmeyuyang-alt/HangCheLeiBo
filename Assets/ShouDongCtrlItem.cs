using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShouDongCtrlItem : MonoBehaviour
{
    
    public PLCConfigManager plcConfigManager;
    
    public string MoveKey1,MoveKey2;
    
   // public string MoveKey3;

   // public string DianDongKey;

    // string Stopkey;

    public void Move1()
    {
        plcConfigManager.SetBool(MoveKey1,true);
    }

    public void Stop1()
    {
        plcConfigManager.SetBool(MoveKey1,false);
    }

    public void Move2()
    {
        plcConfigManager.SetBool(MoveKey2,true);
    }

    public void Stop2()
    {
        plcConfigManager.SetBool(MoveKey2,false);
    }
    


    // public void SetDianDong()
    // {
    //     PLCConfigManager.Instance.SetValue(DianDongKey,true);
    // }

    public void SetStopkey()
    {
        //plcConfigManager.SetPulseBool(Stopkey,true);
    }
}
