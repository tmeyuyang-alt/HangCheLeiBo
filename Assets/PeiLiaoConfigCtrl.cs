using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;




[Serializable]
public class PeiLiaoItem
{
    public string mMat01A;
    public string mMat01B;
    public string mMat02A;
    public string mMat02B;
    public string mMat03A;
    public string mMat03B;
    public string mMat04A;
    public string mMat04B;
    public string mMat05A;
    public string mMat05B;
}

public class LuZiPeiFang
{
    public float Mei;
    public float LingKuang;
    public float GuiShi;
    public float ShaoJieQiu;
    public float LengYaQiu;
}



public class PeiLiaoConfigCtrl : MonoBehaviour
{

    public InputField m01,m02,m03,m04,m05,m06,m07,m08,m09,m10;

    public List<Toggle> mToggles;

    public PeiLiaoItem mLuZi01;
    public PeiLiaoItem mLuZi02;
    


    public Button mConfirm;

    private void Start()
    {
        mConfirm.onClick.AddListener(SetValue);
    }
    
    [ContextMenu("Spwan")]
    public void SpwanConfig()
    {
        string configPath = Application.streamingAssetsPath + "/PeiFang.config";
        List<LuZiPeiFang>  list = new List<LuZiPeiFang>();
        
        list.Add(new LuZiPeiFang(){Mei = 45,LingKuang = 45,GuiShi = 45,ShaoJieQiu = 45,LengYaQiu = 45});
        list.Add(new LuZiPeiFang(){Mei = 55,LingKuang = 55,GuiShi = 55,ShaoJieQiu = 55,LengYaQiu = 55});
        
        DataUtil.Serializer<List<LuZiPeiFang>>(configPath,list);
    }


    public void SetValue()
    {
        string SelectIndex = "1";
        foreach (Toggle toggle in mToggles)
        {
            if (toggle.isOn)
            {
                SelectIndex = toggle.gameObject.name;
            }
        }

        switch (SelectIndex)
        {
            
        }
    }
}
