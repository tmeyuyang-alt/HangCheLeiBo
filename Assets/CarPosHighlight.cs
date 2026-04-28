using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;

[Serializable]
public class PosItem
{
    public string name;
    public float posX;
    public GameObject highLightObj;
    
}
public class CarPosHighlight : MonoBehaviour
{

    public List<PosItem> posItems;

    [ContextMenu("Chang")]
    public void ChangeName()
    {
        foreach (var item in posItems)
        {
            item.highLightObj.name = item.name;
        }
    }

    public string tmpName="";
    public void SetPos(float arg)
    {
        //PosItem tmp=new PosItem();
        float tmpValue=-1;
      
        foreach (var item in posItems)
        {
            // if (tmpValue < 0)
            // {
            //     tmpValue = item.posX;
            //     tmpName = item.name;
            // }
         float tmpValue2= Mathf.Abs(item.posX - arg);
         if (tmpValue < 0)
         {
             tmpValue = tmpValue2;
             tmpName=item.name;
             continue;
         }
         if (tmpValue2 <= tmpValue)
         {
             tmpName = item.name;
             tmpValue=tmpValue2;
         }
        }

        foreach (var VARIABLE in posItems)
        {
            if (VARIABLE.name==tmpName)
            {
                VARIABLE.highLightObj.SetActive(true);
                //VARIABLE.highLightObj.GetComponent<HighlightEffect>().highlighted = true;
            }
            else
            {
                VARIABLE.highLightObj.SetActive(false);
               // VARIABLE.highLightObj.GetComponent<HighlightEffect>().highlighted = false;
            }
        }
        
        
        
    }
}
