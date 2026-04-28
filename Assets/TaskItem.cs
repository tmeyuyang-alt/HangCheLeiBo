using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    
    public PLCConfigManager plcConfigManager;
    
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
        
        InvokeRepeating("UpdateUI",1,1);
    }

    public void BuildKey()
    {
        XiaLiaoCangNameKey = "任务" + DeviceName + "放料仓号";
        ZhuaLiaoCangNameKey =  "任务" + DeviceName + "取料仓号";
        
        ZhuaDouNumKey=  "任务" + DeviceName + "抓料斗数";
        // XiaLiaoXKey = "任务" + DeviceName + "放料位X";
        // XiaLiaoYKey = "任务" + DeviceName + "放料位Y";
        // XiaLiaoZKey = "任务" + DeviceName + "放料位Z";
        // ZhuaLiaoXKey = "任务" + DeviceName + "取料位X";
        // ZhuaLiaoYKey = "任务" + DeviceName + "取料位Y";
        // ZhuaLiaoZKey = "任务" + DeviceName + "取料位Z";
        
        
        MoveUpKey = "任务" + DeviceName + "前移";
        MoveDownKey = "任务" + DeviceName + "后移";
        DeleteKey = "任务" + DeviceName + "删除";
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
                XiaLiaoNameInfo.text = "1号磷矿";
                break;
            case 2:
                XiaLiaoNameInfo.text = "2号硅石";
                break;
            case 3:
                XiaLiaoNameInfo.text = "3号白煤";
                break;
            case 4:
                XiaLiaoNameInfo.text = "4号球/磷矿";
                break;
            case 5:
                XiaLiaoNameInfo.text = "5号仓";
                break;
            case 6:
                XiaLiaoNameInfo.text = "6号磷矿";
                break;
            case 7:
                XiaLiaoNameInfo.text = "7号硅石";
                break;
            case 8:
                XiaLiaoNameInfo.text = "8号白煤";
                break;
             case 9:
                XiaLiaoNameInfo.text = "9号球/磷矿";
                break;
              case 10:
                XiaLiaoNameInfo.text = "10号仓";
                break;
        }
        switch (plcConfigManager.GetIntValue(ZhuaLiaoCangNameKey))
        {
            case 0:
               ZhuaLiaoNameInfo.text = "--";
                break;
            case 1:
                ZhuaLiaoNameInfo.text = "1白煤";
                break;
            case 2:
                ZhuaLiaoNameInfo.text = "2硅石";
                break;
            case 3:
                ZhuaLiaoNameInfo.text = "3硅石";
                break;
            case 4:
                ZhuaLiaoNameInfo.text = "4磷矿";
                break;
            case 5:
                ZhuaLiaoNameInfo.text = "5磷矿";
                break;
            case 6:
                ZhuaLiaoNameInfo.text = "6磷矿";
                break;
        }

        ZhuaDouNumInfo.text = plcConfigManager.GetIntValue(ZhuaDouNumKey).ToString();





        // if (XiaLiaoXInfo!=null)
        // {
        //     XiaLiaoXInfo.text = plcConfigManager.GetFloatValue(XiaLiaoXKey).ToString("F1");
        // }
        // if (XiaLiaoYInfo!=null)
        // {
        //     XiaLiaoYInfo.text = plcConfigManager.GetFloatValue(XiaLiaoYKey).ToString("F1");
        // }
        // if (XiaLiaoZInfo!=null)
        // {
        //     XiaLiaoZInfo.text = plcConfigManager.GetFloatValue(XiaLiaoZKey).ToString("F1");
        // }
        //
        //
        // if (ZhuaLiaoXInfo!=null)
        // {
        //     ZhuaLiaoXInfo.text = plcConfigManager.GetFloatValue(ZhuaLiaoXKey).ToString("F1");
        // }
        // if (ZhuaLiaoYInfo!=null)
        // {
        //     ZhuaLiaoYInfo.text = plcConfigManager.GetFloatValue(ZhuaLiaoYKey).ToString("F1");
        // }
        // if (ZhuaLiaoZInfo!=null)
        // {
        //     ZhuaLiaoZInfo.text = plcConfigManager.GetFloatValue(ZhuaLiaoZKey).ToString("F1");
        // }

    }
    public void MoveUp()
    {
        plcConfigManager.SetValue(MoveUpKey, true);
        isSelect.isOn = false;
    }

    public void MoveDown()
    {
        plcConfigManager.SetValue(MoveDownKey, true);
        isSelect.isOn = false;
    }

    public void Delete()
    {
        plcConfigManager.SetValue(DeleteKey, true);
        isSelect.isOn = false;
    }
    
}
