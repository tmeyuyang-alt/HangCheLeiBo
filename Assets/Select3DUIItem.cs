using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Select3DUIItem : MonoBehaviour
{
    // Start is called before the first frame update
    public string myName;
    public Color selectedColor;
    private Color defaultColor;
    
    bool isSelected = false;
    void Start()
    {
        ChangeName();
        defaultColor = gameObject.GetComponent<Image>().color;
        GetComponent<Button>().onClick.AddListener(BeenSelected);
    }

    public void BeenSelected()
    {
       isSelected=!isSelected;
       
       TaskListManager.Instance.addTaskPanel.OnOpen((Convert.ToInt32(myName)));
       
       // if (isSelected)
       // {
       //     GetComponent<Image>().color = selectedColor;
       // }
       // else
       // {
       //     GetComponent<Image>().color =defaultColor;
       // }
    }
   
    [ContextMenu("C")]
    public void ChangeName()
    {
        myName =transform.name;
        transform.GetChild(0).GetComponent<TextMeshProUGUI>().text=myName;
        
    }
    
}
