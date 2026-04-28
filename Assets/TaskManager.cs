using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public Text totalInfo;
    public PLCConfigManager plcConfigManager;
    public List<TaskItem> taskList ;

    public TaskItem mCurrItem;

    public string ComplatedKey,EndKey,TotalNumKey,TaskStartKey,TaskStopKey;
    
    public Text StatuText;

    private void Start()
    {
        InvokeRepeating("UpdateUI", 0.1f, 0.1f);
    }

    public void UpdateUI()
    {
        
        if (plcConfigManager.GetBool(ComplatedKey))
        {
            StatuText.text = "完成";
        }
        else
        {
            StatuText.text = "未完成";
        }
        
     

        int totalTmp = plcConfigManager.GetIntValue(TotalNumKey);
        totalInfo.text=totalTmp.ToString();
       // print(totalTmp);

         if (totalTmp >0)
         {
             for (int i = 0; i < taskList.Count; i++)
             {
                 if (i>totalTmp-1)
                 {
                     taskList[i].gameObject.SetActive(false);
                 }
                 else
                 {
                     taskList[i].gameObject.SetActive(true);
                 }
             }
         }
         else
         {
             foreach (var VARIABLE in taskList)
             {
                 VARIABLE.gameObject.SetActive(false);
             }
         }
    }

    public void TaskStart()
    {
        plcConfigManager.SetValue(TaskStartKey,true);
    }

    public void TaskStop()
    {
        plcConfigManager.SetValue(TaskStopKey,true);
    }
    
    public void DelectCurrTask()
    {
        mCurrItem.Delete();
    }

    public string LastChoice;

    public void Delect()
    {
        foreach (TaskItem item in taskList)
        {
            if (item.isSelect.isOn)
            {
                item.Delete();
            }
        }
    }

    public void MoveUp()
    {
        print("UP");
        foreach (TaskItem item in taskList)
        {
            if (item.isSelect.isOn)
            {
                LastChoice = item.XiaLiaoNameInfo.text;
                item.MoveUp();
            }
        }
        RefreshUI();
        
    }

    public async void RefreshUI()
    {
        await Task.Delay(1000);
        foreach (var VARIABLE in taskList)
        {
            if (VARIABLE.XiaLiaoNameInfo.text==LastChoice)
            {
                VARIABLE.isSelect.isOn = true;
            }
        }
    }

    public void MoveDown()
    {
        foreach (TaskItem item in taskList)
        {
            if (item.isSelect.isOn)
            {
                LastChoice = item.XiaLiaoNameInfo.text;
                item.MoveDown();
            }
        }
         RefreshUI();
    }
   

}
