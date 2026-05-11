using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemCommandCtrl : MonoBehaviour
{

    public PLCConfigManager plcConfigManager;
   
    
    
    //设置手动
    public string mShouDongKey;
    //设置自动
    public string mAutoKey;

   

    public GameObject AuToBtn,ShouDongBtn;
    
    
    //故障复位
    public string ErrorResetKey;
    
    public string ErrorComfirmKey;

    public AutoStateMonitor auto;
    

    //急停
    public string ForceStopKey;

    public void ErrorReset()
    {
        plcConfigManager.SetPulseBool(ErrorResetKey,true);
    }

    public void ErrorComfirm()
    {
        plcConfigManager.SetPulseBool(ErrorComfirmKey,true);
    }
    
    public void ForceStop()
    {
        plcConfigManager.SetPulseBool(ForceStopKey, true);
    }
  
    public void SetShouDongCommand()
    {
        plcConfigManager.SetBool(mShouDongKey, true);
      
    }
    public void SetAutoCommand()
    {
        plcConfigManager.SetBool(mShouDongKey, false);
      
    }
    
    

    private void Update()
    {
        if (auto.isAuto)
        {
            AuToBtn.gameObject.SetActive(true);
            ShouDongBtn.gameObject.SetActive(false);
        }
        else
        {
            AuToBtn.gameObject.SetActive(false);
            ShouDongBtn.gameObject.SetActive(true);
        }
    }
}
