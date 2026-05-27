using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GaiBanCtrl : MonoBehaviour
{


    public string openKey;
    public string closeKey;
    
    //public Button openButton;
    //public Button closeButton;

    public string openStateKey;
    public string closeStateKey;
    
    public bool lastOpenState;
    public bool lastCloseState;

    public int index;
    public bool tmpOpen;
    public bool tmpClose;

    public float OpenArg=-180f;
    public float CloseArg=-90f;
    
    bool isOpen=false;
    
    [Tooltip("盖板开合动画时间（秒）")]
    public float animationTime = 1f;

    private Tween _rotateTween;
    private float _rotateTargetX;
    private bool _hasRotateTarget;
   
    private void Start()
    {
        //openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"关";
        //openButton.onClick.AddListener(SendOpenCmd);
        //closeButton.onClick.AddListener(SendCloseCmd);
        
        openKey=gameObject.name+"开";
        closeKey = gameObject.name + "关";

        openStateKey = gameObject.name + "盖板开到位"+index;
        closeStateKey =gameObject.name + "盖板关到位"+index;
        
        InvokeRepeating("UpdateState",.2f,.2f);
    }

    private void OnEnable()
    {
        
    }
    [ContextMenu("Open")]
    public void Open()
    {
        RotateTo(OpenArg);
        //openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"关";
        //closeButton.gameObject.SetActive(true);
        //openButton.gameObject.SetActive(false);
    }

    [ContextMenu("Close")]
    public void Close()
    {
        RotateTo(CloseArg);
        //openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"开";
        //closeButton.gameObject.SetActive(false);
        //openButton.gameObject.SetActive(true);
    }

    public void OpenQuick()
    {
        KillRotateTween();
        transform.localRotation=Quaternion.Euler(new Vector3(OpenArg,transform.localRotation.eulerAngles.y,transform.localRotation.eulerAngles.z));
       // openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"关";
    }

    public void CloseQuick()
    {
        KillRotateTween();
        transform.localRotation=Quaternion.Euler(new Vector3(CloseArg,transform.localRotation.eulerAngles.y,transform.localRotation.eulerAngles.z));
        //openButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = gameObject.name+"开";
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
    public void SendCloseCmd()
    {
       
    }

    private void OnDisable()
    {
        KillRotateTween();
    }

    public void UpdateState()
    {
        tmpOpen = PLCConfigManager.Instance.GetBool(openStateKey);
        tmpClose = PLCConfigManager.Instance.GetBool(closeStateKey);
        
        if (!CanRunCoroutine())
        {
            return;
        }
       
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
        // else
        // {
        //     if (tmpOpen)
        //     {
        //         Open();
        //     }
        //
        //     if (tmpClose)
        //     {
        //         Close();
        //     }
        // }
        lastOpenState=tmpOpen;
        lastCloseState=tmpClose;
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

    private void RotateTo(float targetX)
    {
        if (!CanRunCoroutine())
        {
            return;
        }

        if (_rotateTween != null && _rotateTween.IsActive() && _rotateTween.IsPlaying() &&
            _hasRotateTarget && Mathf.Abs(Mathf.DeltaAngle(_rotateTargetX, targetX)) < 0.01f)
        {
            return;
        }

        float currentX = NormalizeAngle(transform.localEulerAngles.x);
        if (Mathf.Abs(Mathf.DeltaAngle(currentX, targetX)) < 0.01f)
        {
            KillRotateTween();
            return;
        }

        KillRotateTween();

        _rotateTargetX = targetX;
        _hasRotateTarget = true;
        Vector3 targetEuler = new Vector3(targetX, transform.localEulerAngles.y, transform.localEulerAngles.z);
        _rotateTween = transform
            .DOLocalRotate(targetEuler, Mathf.Max(0.0001f, animationTime), RotateMode.Fast)
            .SetEase(Ease.Linear)
            .OnKill(() =>
            {
                _rotateTween = null;
                _hasRotateTarget = false;
            });
    }

    private bool CanRunCoroutine()
    {
        return isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    private void KillRotateTween()
    {
        if (_rotateTween != null)
        {
            _rotateTween.Kill();
            _rotateTween = null;
        }

        _hasRotateTarget = false;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
    }
}
