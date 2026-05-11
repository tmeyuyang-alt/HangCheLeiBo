using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    
    public PLCConfigManager plcConfigManager;

    public int myIndex;
    
    public string DeviceName;
    //下料仓号
    public string XiaLiaoCangNameKey;
   //取料仓号
    public string ZhuaLiaoCangNameKey;
    // public string XiaLiaoXKey,XiaLiaoYKey,XiaLiaoZKey;
    // public string ZhuaLiaoXKey,ZhuaLiaoYKey,ZhuaLiaoZKey;
    public string ZhuaDouNumKey;
    public string MoveUpKey;
    public string MoveDownKey;
    public string DeleteKey;


    public Toggle isSelect;
    public Text XiaLiaoNameInfo,ZhuaLiaoNameInfo;
    public Text ZhuaDouNumInfo;
      //  ,XiaLiaoXInfo,XiaLiaoYInfo,XiaLiaoZInfo,ZhuaLiaoXInfo,ZhuaLiaoYInfo,ZhuaLiaoZInfo;

      [ContextMenu("C")]
      public void ChangeName()
      {
          gameObject.name = DeviceName;
      }
    private void Start()
    {
        BuildKey();
        
        InvokeRepeating("UpdateUI",0.1f,0.1f);
        //isSelect.onValueChanged.AddListener(BeenSelected);
    }

   

    public void BuildKey()
    {
        XiaLiaoCangNameKey = "排队中" + DeviceName + "放料仓号";
        ZhuaLiaoCangNameKey =  "排队中" + DeviceName + "取料仓号";
        
        ZhuaDouNumKey=  "排队中" + DeviceName + "抓料斗数";
        // XiaLiaoXKey = "任务" + DeviceName + "放料位X";
        // XiaLiaoYKey = "任务" + DeviceName + "放料位Y";
        // XiaLiaoZKey = "任务" + DeviceName + "放料位Z";
        // ZhuaLiaoXKey = "任务" + DeviceName + "取料位X";
        // ZhuaLiaoYKey = "任务" + DeviceName + "取料位Y";
        // ZhuaLiaoZKey = "任务" + DeviceName + "取料位Z";
        
        
        MoveUpKey = "排队中" + DeviceName + "前移";
        MoveDownKey = "排队中" + DeviceName + "后移";
        DeleteKey = "排队中" + DeviceName + "删除";
    }

    private void Update()
    {
        if (isSelect != null)
        {
             if (isSelect.isOn)
        {
            transform.GetChild(4).GetComponent<Button>().interactable = true;
            transform.GetChild(5).GetComponent<Button>().interactable = true;
            transform.GetChild(6).GetComponent<Button>().interactable = true;
        }
        else
        {
            transform.GetChild(4).GetComponent<Button>().interactable = false;
            transform.GetChild(5).GetComponent<Button>().interactable = false;
            transform.GetChild(6).GetComponent<Button>().interactable = false;
        }  
            
        }
     
    }

    public void UpdateUI()
    {
        
        
      //  ZhuaLiaoNameInfo.text = plcConfigManager.GetIntValue(ZhuaLiaoCangNameKey).ToString();
       // XiaLiaoNameInfo.text = plcConfigManager.GetIntValue(XiaLiaoCangNameKey).ToString();

        switch (plcConfigManager.GetIntValue(XiaLiaoCangNameKey))
        {
            case 0:
                XiaLiaoNameInfo.text = "--";
                break;
            case 1:
                XiaLiaoNameInfo.text = "1";
                break;
            case 2:
                XiaLiaoNameInfo.text = "2";
                break;
            case 3:
                XiaLiaoNameInfo.text = "3";
                break;
           
        }
        switch (plcConfigManager.GetIntValue(ZhuaLiaoCangNameKey))
        {
            case 0:
               ZhuaLiaoNameInfo.text = "--";
                break;
            case 1:
                ZhuaLiaoNameInfo.text = "1";
                break;
            case 2:
                ZhuaLiaoNameInfo.text = "2";
                break;
            case 3:
                ZhuaLiaoNameInfo.text = "3";
                break;
            case 4:
                ZhuaLiaoNameInfo.text = "4";
                break;
            case 5:
                ZhuaLiaoNameInfo.text = "5";
                break;
            case 6:
                ZhuaLiaoNameInfo.text = "6";
                break;
            case 7:
                ZhuaLiaoNameInfo.text = "7";
                break;
            case 8:
                ZhuaLiaoNameInfo.text = "8";
                break;
            case 9:
                ZhuaLiaoNameInfo.text = "9";
                break;
            case 10:
                ZhuaLiaoNameInfo.text = "10";
                break;
            case 11:
                ZhuaLiaoNameInfo.text = "11";
                break;
            case 12:
                ZhuaLiaoNameInfo.text = "12";
                break;
            
        }

        ZhuaDouNumInfo.text = plcConfigManager.GetIntValue(ZhuaDouNumKey).ToString();

    }
    public void MoveUp()
    {
        plcConfigManager.SetPulseBool(MoveUpKey, true);
        
        TaskListManager.Instance.MoveUp(Convert.ToInt32(gameObject.name));
    }

    public void MoveDown()
    {
        plcConfigManager.SetPulseBool(MoveDownKey, true);
        TaskListManager.Instance.MoveDown(Convert.ToInt32(gameObject.name));
    }

    public void Delete()
    {
        plcConfigManager.SetPulseBool(DeleteKey, true);
        isSelect.isOn = false;
    }
    
}
