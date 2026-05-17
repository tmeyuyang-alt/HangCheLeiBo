using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchModelManager : MonoBehaviour
{
   public GameObject[] models1,models2;
   
   public string model1Json;
   public string model2Json;
   public string plcid;
   
   public PLCConfigManager plcConfig;
   public WarningNotify warningNotify;
   public HisDataPanel hisDataPanel;
   public PLCWaringTable plcWarningTable;

   
   [ContextMenu("Switch Models1")]
   public void SwitchModel1()
   {
      plcConfig.deviceSignalJsonName = model1Json;
      warningNotify.configName=plcConfig.deviceSignalJsonName;
      hisDataPanel.plcId = plcid;
      plcWarningTable.plcId = plcConfig.deviceSignalJsonName;
      
      foreach (var VARIABLE in models1)
      {
         VARIABLE.SetActive(true);
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
      plcWarningTable.plcId = plcConfig.deviceSignalJsonName;
      plcConfig.deviceSignalJsonName = model2Json;
      foreach (var VARIABLE in models1)
      {
         VARIABLE.SetActive(false);
      }

      foreach (var VARIABLE in models2)
      {
         VARIABLE.SetActive(true);
      }
   }
   
}
