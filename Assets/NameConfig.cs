using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum NameItemType
{
    LinKuang,
    GuiShi,
    Mei,
    ShaoJieQiu,
    LengYaQiu
}
public class NameConfig : MonoBehaviour
{
    public static NameConfig Instance;
   
    public Dictionary<string,string> nameConfig = new Dictionary<string, string>();

    public string LinKuang,GuiShi,Mei,ShaoJieQiu,LengYaQiu;

    private void Awake()
    {
        Instance=this;
        string configPath = Application.streamingAssetsPath + "/name.config";
        nameConfig=DataUtil.Deserializer<Dictionary<string,string>>(configPath);
        nameConfig.TryGetValue("磷矿",out LinKuang);
        nameConfig.TryGetValue("硅石", out GuiShi);
        nameConfig.TryGetValue("煤", out Mei);
        nameConfig.TryGetValue("烧结球", out ShaoJieQiu);
        nameConfig.TryGetValue("冷压球", out LengYaQiu);
    }

    void Start()
    {
       
        
    }
    [ContextMenu("Spwan")]
    public void SpwanConfig()
    {
        string configPath = Application.streamingAssetsPath + "/name.config";
        nameConfig = new Dictionary<string, string>();
        nameConfig.Add("磷矿","磷矿");
        nameConfig.Add("硅石","硅石");
        nameConfig.Add("煤","煤");
        nameConfig.Add("烧结球","烧结球");
        nameConfig.Add("冷压球","冷压球");
        DataUtil.Serializer<Dictionary<string, string>>(configPath, nameConfig);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
