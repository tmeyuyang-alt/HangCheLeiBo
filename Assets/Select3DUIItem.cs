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
    public Color currentColor;
    private Color defaultColor;
    
    public string keyName;
    private string currKeyName;
    
    bool isSelected = false;

    private void Update()
    {
      //  transform.LookAt(Camera.main.transform);
    }


    public void SetSelected()
    {
        GetComponent<Image>().color = selectedColor;
            //print(gameObject.name+" set");
    }

    public void SetCurrent()
    {
        GetComponent<Image>().color = currentColor;
    }

    public void SetDefault()
    {
        GetComponent<Image>().color = defaultColor;
    }
    
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
       
    }
   
    [ContextMenu("C")]
    public void ChangeName()
    {
        myName =transform.name;
        transform.GetChild(0).GetComponent<TextMeshProUGUI>().text=myName;
    }
    
}
