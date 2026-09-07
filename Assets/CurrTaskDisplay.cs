using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrTaskDisplay : MonoBehaviour
{
    public Text xiaLiao, zhuaLiao, yiZhua, shengYu;

    private void Start()
    {
        InvokeRepeating("UpdateUI", 0.2f, 0.2f);
    }

    public void UpdateUI()
    {
        //xiaLiao.text = PLCConfigManager.Instance.GetIntValue("执行中放料仓号").ToString();
        
        
        switch (PLCConfigManager.Instance.GetIntValue("执行中放料仓号"))
        {
            case 0:
                xiaLiao.text = "--";
                break;
            case 1:
                xiaLiao.text = "1-1白煤";
                break;
            case 2:
                xiaLiao.text = "1-2硅石";
                break;
            case 3:
                xiaLiao.text= "1-3磷矿";
                break;
            case 4:
                xiaLiao.text = "1-4焦炭";
                break;
            case 5:
                xiaLiao.text = "1-5磷矿";
                break;
            case 6:
                xiaLiao.text = "1-6磷矿";
                break;
            case 7:
                xiaLiao.text = "2-1白煤";
                break;
            case 8:
                xiaLiao.text = "2-2硅石";
                break;
            case 9:
                xiaLiao.text = "2-3磷矿";
                break;
            case 10:
                xiaLiao.text = "2-4焦炭";
                break;
            case 11:
                xiaLiao.text = "2-5磷矿";
                break;
            case 12:
                xiaLiao.text = "2-6磷矿";
                break;
           
        }
        
        
        
        zhuaLiao.text=PLCConfigManager.Instance.GetIntValue("执行中取料仓号").ToString();
        yiZhua.text=PLCConfigManager.Instance.GetIntValue("执行中已抓斗数").ToString();
        shengYu.text=PLCConfigManager.Instance.GetIntValue("执行中剩余斗数").ToString();
    }
}
