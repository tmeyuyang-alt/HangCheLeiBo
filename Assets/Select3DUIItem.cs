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
    
    public Image targetImage;
    public TextMeshProUGUI targetText;
    
    
    bool isSelected = false;

    private void Update()
    {
      //  transform.LookAt(Camera.main.transform);
    }


    public void SetSelected()
    {
        targetImage.gameObject.SetActive(true); 
        targetText.gameObject.SetActive(true);
        targetImage.color = selectedColor;
     
    }

    public void SetCurrent()
    {
        targetImage.gameObject.SetActive(true); 
        targetText.gameObject.SetActive(true);
        targetImage.color = currentColor;
    }

    public void SetDefault()
    {
        targetImage.gameObject.SetActive(false); 
        targetText.gameObject.SetActive(false);
    }
    
    void Start()
    {
        myName=gameObject.name;
        //ChangeName();
       
        GetComponent<Clickable3DObject>().onClick.AddListener(BeenSelected);
        
        targetImage=transform.GetChild(0).transform.GetChild(0).GetComponent<Image>();
        targetText=transform.GetChild(0).transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        defaultColor =targetImage.color;
      
    }

    public void BeenSelected()
    {
       isSelected=!isSelected;
       SetSelected();
       TaskListManager.Instance.addTaskPanel.OnOpen((Convert.ToInt32(myName)));
       
    }
   
    [ContextMenu("C")]
    public void ChangeName()
    {
        myName =transform.name;
        transform.GetChild(0).GetComponent<TextMeshProUGUI>().text=myName;
    }
    
}
