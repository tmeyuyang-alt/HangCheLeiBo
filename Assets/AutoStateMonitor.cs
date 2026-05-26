using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoStateMonitor : MonoBehaviour
{
   // public static AutoStateMonitor instance;
    
    public PLCConfigManager plcConfigManager;


    public GameObject[] shoudongItems;
    public GameObject[] ziDongItems;
    public Button shoudongBtn;

    public bool isAuto=false;

    public string key;
    
    private void Update()
    {
        isAuto = !plcConfigManager.GetBool(key);



        if (isAuto)
        {
            foreach (var VARIABLE in shoudongItems)
            {
                VARIABLE.SetActive(false);
            }

            foreach (var VARIABLE in ziDongItems)
            {
                VARIABLE.SetActive(true);
            }
            shoudongBtn.interactable = false;
        }
        else
        {
            foreach (var VARIABLE in shoudongItems)
            {
                VARIABLE.SetActive(true);
            }

            foreach (var VARIABLE in ziDongItems)
            {
                VARIABLE.SetActive(false);
            }
            shoudongBtn.interactable = true;
        }
        
    }


   
}
