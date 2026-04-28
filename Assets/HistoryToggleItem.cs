using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoryToggleItem : MonoBehaviour
{
    private Text label;
    
    public Toggle toggle;
    

    private void Start()
    {
        toggle=GetComponent<Toggle>();
        // label=
        // label.text=gameObject.name;
    }
    [ContextMenu("C")]
    public void Change()
    {
        label=transform.GetChild(1).GetComponent<Text>();
        label.text=gameObject.name;
    }
}
