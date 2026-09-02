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
    public Text mDropZhuaLiao;

    public int tmparg;


    public void OnOpen(int arg)
    {
        Debug.Log("oPEN!!!!!!!!!");
        gameObject.SetActive(true);
        mDropZhuaLiao.text=(arg).ToString();
        tmparg=arg;
    }
    public void SetValue()
    {
        plcConfigManager.SetValue(FangLiaoKey, mDropFangLiao.value+1);
        plcConfigManager.SetValue(ZhuaLiaoKey, tmparg);
        plcConfigManager.SetValue(ZhuaDouNumKey, Convert.ToInt32(mInputZhuaDou.text));
        plcConfigManager.SetValue(ConfirmKey, true);
        gameObject.SetActive(false);
    }
    
    
}
