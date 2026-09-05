using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TruckMonitor : MonoBehaviour
{
    public string truckKey, ResetKey;

    public GameObject warningPanel;
    public Button btn;

    private void Start()
    {
        btn.onClick.AddListener(ClearBtn);
        InvokeRepeating("UpdateUI",1,1);
    }

    public void UpdateUI()
    {
        if (PLCConfigManager.Instance.GetBool(truckKey))
        {
            warningPanel.SetActive(true);
        }
        else
        {
            warningPanel.SetActive(false);
        }
    }

    public void ClearBtn()
    {
        PLCConfigManager.Instance.SetPulseBool(ResetKey,true);
    }
}
