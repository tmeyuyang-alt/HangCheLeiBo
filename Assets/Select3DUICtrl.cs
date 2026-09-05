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

    [Header("1号行车：隐藏并禁止点击的料仓编号")]
    [SerializeField] private List<int> crane1DisabledNumbers = new List<int> { 152, 153, 154, 155 };

    [Header("2号行车：隐藏并禁止点击的料仓编号")]
    [SerializeField] private List<int> crane2DisabledNumbers = new List<int> { 8, 9, 10, 11 };

    private readonly HashSet<Select3DUIItem> hiddenByCrane = new HashSet<Select3DUIItem>();

    private void Start()
    {
        keyCurrTotal = "任务任务总数量";
        RefreshCraneVisibility();
        InvokeRepeating("UpdateUI",1,1);
        
    }


    private void OnEnable()
    {
        PLCConfigManager.OnActiveCraneChanged += OnActiveCraneChanged;
        RefreshCraneVisibility();
    }

    private void OnDisable()
    {
        PLCConfigManager.OnActiveCraneChanged -= OnActiveCraneChanged;
    }

    private void RefreshCraneVisibility()
    {
        if (PLCConfigManager.Instance != null)
        {
            OnActiveCraneChanged(PLCConfigManager.Instance.activeCraneIndex);
        }
    }

    private void OnActiveCraneChanged(int craneIndex)
    {
        if (items == null)
        {
            return;
        }

        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (!int.TryParse(item.name, out int number))
            {
                continue;
            }

            // 行车索引从 0 开始；停用整个对象，同时隐藏显示并禁用碰撞点击。
            bool hidden = (craneIndex == 0 && crane1DisabledNumbers != null && crane1DisabledNumbers.Contains(number)) ||
                          (craneIndex == 1 && crane2DisabledNumbers != null && crane2DisabledNumbers.Contains(number));
            if (hidden)
            {
                if (item.gameObject.activeSelf)
                {
                    hiddenByCrane.Add(item);
                    item.gameObject.SetActive(false);
                }
            }
            else if (hiddenByCrane.Remove(item))
            {
                // 只恢复本组件隐藏的对象，也支持从配置列表中移除编号后恢复。
                item.gameObject.SetActive(true);
            }
        }
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
            if (VARIABLE == null || !VARIABLE.gameObject.activeInHierarchy)
            {
                continue;
            }
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
                    if (VARIABLE == null || !VARIABLE.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    
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
            if (VARIABLE == null || !VARIABLE.gameObject.activeInHierarchy)
            {
                continue;
            }
            if (tmpInt2.ToString()==VARIABLE.name)
            {
                VARIABLE.SetCurrent();
            }
        }
     
        
    }
}
