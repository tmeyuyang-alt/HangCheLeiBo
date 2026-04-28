using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabCtrl : MonoBehaviour
{
   public List<Button> mAllButtons;
   public List<GameObject> mAllObjects;

   private void Start()
   {
      foreach (Button button in mAllButtons)
      {
         button.onClick.AddListener(() =>
         {
            foreach (var VARIABLE in mAllObjects)
            {
               if (VARIABLE.name == button.name)
               {
                  VARIABLE.SetActive(true);
               }
               else
               {
                  VARIABLE.SetActive(false);
               }
            }
            
         });
      }
   }
}
