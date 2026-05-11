using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AddTask : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;

    public string FangLiaoKey;
    public string ZhuaLiaoKey;
    public string ZhuaDouNumKey;

    public string ConfirmKey;

    //public InputField mInputFangLiao;
    public InputField mInputZhuaDou;
    
    public Dropdown mDropFangLiao;
    public Dropdown mDropZhuaLiao;


    public void OnOpen(int arg)
    {
        gameObject.SetActive(true);
        mDropZhuaLiao.value=arg-1;
    }
    public void SetValue()
    {
        plcConfigManager.SetValue(FangLiaoKey, mDropFangLiao.value+1);
        plcConfigManager.SetValue(ZhuaLiaoKey, mDropZhuaLiao.value+1);
        plcConfigManager.SetValue(ZhuaDouNumKey, Convert.ToInt32(mInputZhuaDou.text));
        plcConfigManager.SetValue(ConfirmKey, true);
        gameObject.SetActive(false);
    }
    
    
}
