using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TiShengDisplay : MonoBehaviour
{
    public TextMeshProUGUI info;
    public string key;
    private float tmp;

    private void Start()
    {
        InvokeRepeating("UpdateUI",1,1);
    }

    public void UpdateUI()
    {
        tmp = PLCConfigManager.Instance.GetFloatValue(key);
        if (tmp > SettingPanel.GetTiShengHeight())
        {
            info.text = "此区域无料";
        }
        else
        {
            info.text =tmp.ToString("F2");
        }
       
    }
}
