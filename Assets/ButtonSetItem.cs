using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSetItem : MonoBehaviour
{
    public string key;
    public Button btn;

    private void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(Set);
    }

    public void Set()
    {
        PLCConfigManager.Instance.SetPulseBool(key,true);
    }
}
