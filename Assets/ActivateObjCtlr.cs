using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateObjCtlr : MonoBehaviour
{
   public GameObject[] target;


   public void SetToggle()
   {
      foreach (GameObject go in target)
      {
         go.SetActive(!go.activeSelf);
      }
     // target.SetActive(!target.activeSelf);
   }
}
