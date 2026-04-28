using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalConfigGroup : MonoBehaviour
{

    public Button leftBtn;
    public Button rightBtn;

    public InputField nameInput;
    
    public GlobalConfig config;

    public Sprite normalTex;
    public Sprite selectedTex;

    void Start()
    {
        //TODO:读取配置   
        var path = Application.streamingAssetsPath + "/globalConfig.config";
        if (System.IO.File.Exists(path))
        {
            var json = System.IO.File.ReadAllText(path);

            config = LitJson.JsonMapper.ToObject<GlobalConfig>(json);

            if (config == null)
            {
                config = new GlobalConfig();
            }
        }
        else
        {
            config = new GlobalConfig();
        }

        nameInput.onValueChanged.AddListener((text) =>
        {
            config.Name = text;
            Save();
        });
        leftBtn.onClick.AddListener(() =>
        {
            config.ModelLeft = true;
            rightBtn.GetComponent<Image>().sprite = normalTex;
            leftBtn.GetComponent<Image>().sprite = selectedTex;


            EventCenter.Instance.TriggerEvent(EventName.ChangeModelMirror, this, new BoolEventArgs { value = config.ModelLeft });
            Save();
        });

        rightBtn.onClick.AddListener(() =>
        {
            config.ModelLeft = false;
            leftBtn.GetComponent<Image>().sprite = normalTex;
            rightBtn.GetComponent<Image>().sprite = selectedTex;

            EventCenter.Instance.TriggerEvent(EventName.ChangeModelMirror, this, new BoolEventArgs { value = config.ModelLeft });
            Save();
        });


        EventCenter.Instance.TriggerEvent(EventName.ChangeModelMirror, this, new BoolEventArgs { value = config.ModelLeft });
        leftBtn.GetComponent<Image>().sprite = config.ModelLeft ? selectedTex : normalTex;
        rightBtn.GetComponent<Image>().sprite = !config.ModelLeft ? selectedTex : normalTex;
        nameInput.text = config.Name;

        //3是超级管理员
        if (GlobalInfo.user.permission == 3)
        {
            leftBtn.gameObject.SetActive(true);
            rightBtn.gameObject.SetActive(true);
            nameInput.readOnly = false;
        }
        else
        {
            leftBtn.gameObject.SetActive(false);
            rightBtn.gameObject.SetActive(false);
            nameInput.readOnly = true;
        }
    }

    public void Save()
    {
        var path = Application.streamingAssetsPath + "/globalConfig.config";

        System.IO.File.WriteAllText(path, LitJson.JsonMapper.ToJson(config));
    }
}
