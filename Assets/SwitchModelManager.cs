using System.Collections;
using System.Collections.Generic;
using UMP.Wrappers;
using UnityEngine;

public class SwitchModelManager : MonoBehaviour
{
   public GameObject[] models1,models2,models3;
   
   public string model1Json;
   public string model2Json;
   public string model3Json;
   public string plcid;
   
   public PLCConfigManager plcConfig;
   public WarningNotify warningNotify;
   public HisDataPanel hisDataPanel;
   public PLCWaringTable plcWarningTable;
   //public WarningNotify warningNotify2;

   
   [ContextMenu("Switch Models1")]
   public void SwitchModel1()
   {
      plcConfig.deviceSignalJsonName = model1Json;
      warningNotify.configName=plcConfig.deviceSignalJsonName;
      warningNotify.plcId = plcid;
      hisDataPanel.plcId = plcid;
      plcWarningTable.plcId = plcid;
      
      foreach (var VARIABLE in models1)
      {
         VARIABLE.SetActive(true);
      }
      foreach (var VARIABLE in models3)
      {
         VARIABLE.SetActive(false);
      }

      foreach (var VARIABLE in models2)
      {
         VARIABLE.SetActive(false);
      }
   }
   [ContextMenu("Switch Models2")]
   public void SwitchModel2()
   {
      plcConfig.deviceSignalJsonName = model2Json;
      warningNotify.configName=plcConfig.deviceSignalJsonName;
      hisDataPanel.plcId = plcid;
      warningNotify.plcId = plcid;
      plcWarningTable.plcId = plcid;
      
      foreach (var VARIABLE in models1)
      {
         VARIABLE.SetActive(false);
      }
      foreach (var VARIABLE in models3)
      {
         VARIABLE.SetActive(false);
      }

      foreach (var VARIABLE in models2)
      {
         VARIABLE.SetActive(true);
      }
   }
   [ContextMenu("Switch Models3")]
   public void SwitchModel3()
   {
      plcConfig.deviceSignalJsonName = model3Json;
      warningNotify.configName=plcConfig.deviceSignalJsonName;
      hisDataPanel.plcId = plcid;
      warningNotify.plcId = plcid;
      plcWarningTable.plcId = plcid;
      
      foreach (var VARIABLE in models1)
      {
         VARIABLE.SetActive(false);
      }

      foreach (var VARIABLE in models2)
      {
         VARIABLE.SetActive(false);
      }
      foreach (var VARIABLE in models3)
      {
         VARIABLE.SetActive(true);
      }
   }
   
   
   
}
