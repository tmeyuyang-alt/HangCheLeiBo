using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TemperaturePanel3d : MonoBehaviour
{
    public Text text;
    public Image AlarmIcon;

    public string myName;
    //void Start()
    //{
    //    DataHandler.getInstance.OnGetPlcDataCallback += OnGetPlcDataCallback;

    //}

    public void SetName(string n)
    {
        this.myName = n;
        text.text = string.Format("{0}£º<color=#ffc97a>{1}</color>  ¡æ", this.myName, "--");
    }
    //void OnGetPlcDataCallback(string config, List<PLCData> datas)
    //{

    //}

    public void OnDataUpdate(string subName, string value)
    {

        if (subName == "highlimit_datablock")
        {
            text.text = string.Format("{0}¸ßÏÞ£º<color=#ffc97a>{1}</color>  ¡æ", this.myName, value);
        }
        else if (subName == "alarm_datablock")
        {
            if (value.ToLower() == "true")
            {
                AlarmIcon.enabled = true;
            }
            else
            {
                AlarmIcon.enabled = false;
            }
        }
    }
}
