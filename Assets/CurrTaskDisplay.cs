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
        xiaLiao.text = PLCConfigManager.Instance.GetIntValue("执行中放料仓号").ToString();
        zhuaLiao.text=PLCConfigManager.Instance.GetIntValue("执行中取料仓号").ToString();
        yiZhua.text=PLCConfigManager.Instance.GetIntValue("执行中已抓斗数").ToString();
        shengYu.text=PLCConfigManager.Instance.GetIntValue("执行中剩余斗数").ToString();
    }
}
