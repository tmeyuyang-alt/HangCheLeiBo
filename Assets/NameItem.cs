using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NameItem : MonoBehaviour
{
   public NameItemType type;
   private TextMeshProUGUI info;
  
   private void Start()
   {
      info = GetComponent<TextMeshProUGUI>();
      switch (type)
      {
         case NameItemType.LinKuang:
            info.text = NameConfig.Instance.LinKuang;
            break;
         case NameItemType.ShaoJieQiu:
            info.text = NameConfig.Instance.ShaoJieQiu;
            break;
         case NameItemType.GuiShi:
            info.text = NameConfig.Instance.GuiShi;
            break;
         case NameItemType.LengYaQiu:
            info.text = NameConfig.Instance.LengYaQiu;
            break;
         case NameItemType.Mei:
            info.text = NameConfig.Instance.Mei;
            break;
         
      }
   }
}
