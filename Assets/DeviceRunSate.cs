using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeviceRunSate : MonoBehaviour
{
   public PLCConfigManager plcConfigManager;
   
   public string key;
   public string errorKey;
   public string DianLiuKey, PingLvKey;

   public Text info,DianliuInfo,PingLvInfo;

   private void Start()
   {
       InvokeRepeating("UpdateUI",1,1);
   }

   public void UpdateUI()
   {
      
       if (plcConfigManager.GetFloatValue(DianLiuKey)>0)
       {
           info.text = "<color=Green>运行</color>";
       }
       else
       {
           info.text =  "<color=White>未运行</color>";
       }
       if (plcConfigManager.GetBool(errorKey))
       {
           info.text ="<color=Red>运行故障</color>";  //info.text = plcConfigManager.GetBool(key) ? "<color=Green>运行</color>" : "<color=White>未运行</color>";
       }
      
       
       DianliuInfo.text = plcConfigManager.GetFloatValue(DianLiuKey).ToString("F0")+" A";
       PingLvInfo.text = plcConfigManager.GetFloatValue(PingLvKey).ToString("F0")+" Hz";
       
   }

}
