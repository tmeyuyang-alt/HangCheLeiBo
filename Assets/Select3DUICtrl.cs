using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Select3DUICtrl : MonoBehaviour
{
    
    public List<Select3DUIItem> items;
    public string keyCurrTotal;
    public int totalNum;
    private int tmpInt,tmpInt2;

    private void Start()
    {
        keyCurrTotal = "任务任务总数量";
        InvokeRepeating("UpdateUI",1,1);
        
    }

    private void UpdateUI()
    {

        if (TaskListManager.Instance.addTaskPanel.isActiveAndEnabled)
        {
            return;
        }
        
        totalNum = PLCConfigManager.Instance.GetIntValue(keyCurrTotal);
        
        foreach (var VARIABLE in items)
        {
            VARIABLE.SetDefault();
        }
       
        
     
        if (totalNum>0)
        {
            
            for (int i = 1; i <= totalNum; i++)
            {
                
                tmpInt=PLCConfigManager.Instance.GetIntValue("排队中"+i.ToString()+"取料仓号");
               
                
                //print(i+"---"+tmpInt+"---"+tmpInt2);
                
                foreach (var VARIABLE in items)
                {
                    
                    if (VARIABLE.name == tmpInt.ToString())
                    {
                        //print(VARIABLE.name+"@");
                        VARIABLE.SetSelected();
                    }

                    if (tmpInt2.ToString()==VARIABLE.name)
                    {
                        VARIABLE.SetCurrent();
                    }

                  
                }
            }

        }
        
        tmpInt2=PLCConfigManager.Instance.GetIntValue("执行中取料仓号");
        
        
        foreach (var VARIABLE in items)
        {
            if (tmpInt2.ToString()==VARIABLE.name)
            {
                VARIABLE.SetCurrent();
            }
        }
     
        
    }
}
