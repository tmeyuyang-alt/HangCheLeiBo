using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CangWeiSetting : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;
    public List<CangWeiSettingItem> cangWeiSettingItems;


    public float timr = 0;  
    bool isTyping = false;
    private void Start()
    {
        InvokeRepeating("UpdateValue",0.2f,0.5f);
    }

    private void Update()
    {
        if (isTyping)
        {
            timr += Time.deltaTime;
            if (timr >= 60)
            {
                timr = 0;
                isTyping=false;
            }
        }

        foreach (var VARIABLE in cangWeiSettingItems)
        {
            foreach (var VARIABLE2 in VARIABLE.InputField)
            {
                if (VARIABLE2.isFocused)
                {
                    isTyping=true;
                    timr = 0;
                }
            }
        }
        
        
    }

    public void SetValue()
    {
        foreach (CangWeiSettingItem cangWeiSetting in cangWeiSettingItems)
        {
          
            plcConfigManager.SetValue(cangWeiSetting.QueLiaoHeightKey,Convert.ToSingle(cangWeiSetting.InputField[1].text));
            plcConfigManager.SetValue(cangWeiSetting.BuliaoHeightKey,Convert.ToSingle(cangWeiSetting.InputField[2].text));
            plcConfigManager.SetValue(cangWeiSetting.ManLiaoHeightKey,Convert.ToSingle(cangWeiSetting.InputField[3].text));
        }
    }
    
    public void UpdateValue()
    {
        foreach (CangWeiSettingItem cangWeiSetting in cangWeiSettingItems)
        {
            cangWeiSetting.HeightInfo.text = plcConfigManager.GetFloatValue(cangWeiSetting.CurrHeightKey).ToString("F2");
           // cangWeiSetting.DropZhuaLiao.text= plcConfigManager.GetFloatValue(cangWeiSetting.QueLiaoHeightKey).ToString("F2");
            switch (plcConfigManager.GetIntValue(cangWeiSetting.QuLiaoNum))   
            {
                case 0:
                    cangWeiSetting.DropZhuaLiao.text = "0无效";
                    break;
                case 1:
                    cangWeiSetting.DropZhuaLiao.text = "1白煤";
                    break;
                case 2:
                    cangWeiSetting.DropZhuaLiao.text = "2硅石";
                    break;
                case 3:
                    cangWeiSetting.DropZhuaLiao.text= "3硅石";
                    break;
                case 4:
                    cangWeiSetting.DropZhuaLiao.text = "4磷矿";
                    break;
                case 5:
                    cangWeiSetting.DropZhuaLiao.text = "5磷矿";
                    break;
            }
        }

        if (!isTyping)
        {
            foreach (CangWeiSettingItem cangWeiSetting in cangWeiSettingItems)
            {

            
                cangWeiSetting.InputField[1].text= plcConfigManager.GetFloatValue(cangWeiSetting.QueLiaoHeightKey).ToString("F2");
                cangWeiSetting.InputField[2].text= plcConfigManager.GetFloatValue(cangWeiSetting.BuliaoHeightKey).ToString("F2");
                cangWeiSetting.InputField[3].text= plcConfigManager.GetFloatValue(cangWeiSetting.ManLiaoHeightKey).ToString("F2");
                // plcConfigManager.SetValue(cangWeiSetting.QueLiaoHeightKey,Convert.ToInt32(cangWeiSetting.InputField[1].text));
                // plcConfigManager.SetValue(cangWeiSetting.BuliaoHeightKey,Convert.ToInt32(cangWeiSetting.InputField[2].text));
                // plcConfigManager.SetValue(cangWeiSetting.ManLiaoHeightKey,Convert.ToInt32(cangWeiSetting.InputField[3].text));
            }
        }
   
    }
}
