using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GaiBanDisplayCtrl : MonoBehaviour
{

   
    public string openKey;
    public string closeKey;
    public string stopKey;
    public string eleKey;
    public string farStateKey;

    public Text stateImg;
    
    public Button openButton,StopButton;
    //public Button closeButton;
    public TextMeshProUGUI info;

    public string openStateKey;
    public string closeStateKey;
    
    public bool lastOpenState;
    public bool lastCloseState;

    public int index;
    public bool tmpOpen;
    public bool tmpClose;

    
    
    bool isOpen=false;
    
  
   
    private void Start()
    {
        openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"关";
        openButton.onClick.AddListener(SendOpenCmd);
        StopButton.onClick.AddListener(SendStopKey);
        //closeButton.onClick.AddListener(SendCloseCmd);
        
        openKey=gameObject.name+"开";
        closeKey = gameObject.name + "关";
        stopKey = gameObject.name + "停";
        eleKey = gameObject.name + "电流";

        openStateKey = gameObject.name + "盖板开到位"+index;
        closeStateKey =gameObject.name + "盖板关到位"+index;
        farStateKey=gameObject.name + "远程反馈";
        
        
        
        InvokeRepeating("UpdateState",.2f,.2f);
        
    }

 
    [ContextMenu("Open")]
    public void Open()
    {
        openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"关";
    }

    [ContextMenu("Close")]
    public void Close()
    {
      
        openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"开";
     
    }

    public void SendStopKey()
    {
        PLCConfigManager.Instance.SetPulseBool(stopKey,true);
    }




    public void SendOpenCmd()
    {
        if (lastOpenState)
        {
            PLCConfigManager.Instance.SetPulseBool(closeKey,true);
        }

        if (lastCloseState)
        {
            PLCConfigManager.Instance.SetPulseBool(openKey,true);
            
        }
       
    }
    public void OpenQuick()
    {

        openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"关";
    }

    public void CloseQuick()
    {
        openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"开";
    }


    public void UpdateState()
    {
        tmpOpen = PLCConfigManager.Instance.GetBool(openStateKey);
        tmpClose = PLCConfigManager.Instance.GetBool(closeStateKey);
      
        info.text = PLCConfigManager.Instance.GetFloatValue(eleKey).ToString("F2");
        
        bool tmpFar=PLCConfigManager.Instance.GetBool(farStateKey);
        
        stateImg.text = tmpFar ? "远程" : "就地";
  
        lastOpenState=tmpOpen;
        lastCloseState=tmpClose;
        if (tmpOpen==false && tmpClose==false)
        {
            if (lastOpenState)
            {
                Close();
            }
            if (lastCloseState)
            {
                Open();
            }
        }

        
        if (tmpOpen)
        {
            OpenQuick();
            return;
        }

        if (tmpClose)
        {
            CloseQuick();
            return;
        }
    }



  
   
}
