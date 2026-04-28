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
        plcConfigManager.SetValue(ErrorResetKey,true);
    }

    public void ErrorComfirm()
    {
        plcConfigManager.SetValue(ErrorComfirmKey,true);
    }
    
    public void ForceStop()
    {
        plcConfigManager.SetValue(ForceStopKey, true);
    }
  
    public void SetShouDongCommand()
    {
        plcConfigManager.SetValue(mShouDongKey, true);
       // plcConfigManager.SetValue(mAutoKey, false);
    }
    public void SetAutoCommand()
    {
        plcConfigManager.SetValue(mAutoKey, true);
       // PLCConfigManager.Instance.SetValue(mShouDongKey, false);
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
