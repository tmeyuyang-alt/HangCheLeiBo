using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CangWeiSettingItem : MonoBehaviour
{
    
    
    public string DeviceName;
    public string QuLiaoNum;
    public string QueLiaoHeightKey;
    public string BuliaoHeightKey;
    public string ManLiaoHeightKey;
    public string CurrHeightKey;


    public Text HeightInfo;
    public Text DropZhuaLiao;
    
    public List<InputField> InputField;

    private void Start()
    {
        BuildKey();
        foreach (var VARIABLE in InputField)
        {
            VARIABLE.contentType = UnityEngine.UI.InputField.ContentType.DecimalNumber;
        }
    }

   
    
    [ContextMenu("BUILD")]
    public void BuildKey()
    {
        QuLiaoNum = "料仓设置" + DeviceName + "_取料仓号";
        QueLiaoHeightKey = "料仓设置" + DeviceName + "_缺料高度";
        BuliaoHeightKey = "料仓设置" + DeviceName + "_补料高度";
        ManLiaoHeightKey = "料仓设置" + DeviceName + "_满料高度";
        CurrHeightKey = "料仓设置" + DeviceName + "_料仓料位";
    }

   
  
}
