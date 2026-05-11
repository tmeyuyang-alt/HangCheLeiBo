using System;
using System.Collections;
using UnityEngine;

public class GaiBanCtrl : MonoBehaviour
{


    public string openKey;
    public string closeKey;

    public string openStateKey;
    public string closeStateKey;
    
    public bool lastOpenState;
    public bool lastCloseState;

    public int index;
    public bool tmpOpen;
    public bool tmpClose;
    
    [Tooltip("盖板开合动画时间（秒）")]
    public float animationTime = 1f;

    private Coroutine _rotateCoroutine;
    [ContextMenu("Open")]
    public void Open()
    {
        RotateTo(0f);
    }

    [ContextMenu("Close")]
    public void Close()
    {
        RotateTo(-90f);
    }
    

    private void Start()
    {
        openKey=gameObject.name+"开";
        closeKey = gameObject.name + "关";

        openStateKey = gameObject.name + "盖板开到位"+index;
        closeStateKey =gameObject.name + "盖板关到位"+index;
        
        InvokeRepeating("UpdateState",.2f,.2f);
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        if (_rotateCoroutine != null)
        {
            StopCoroutine(_rotateCoroutine);
            _rotateCoroutine = null;
        }
    }

    public void UpdateState()
    {
        if (!CanRunCoroutine())
        {
            return;
        }
        tmpOpen = PLCConfigManager.Instance.GetBool(openStateKey);
        tmpClose = PLCConfigManager.Instance.GetBool(closeStateKey);
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
        else
        {
            if (tmpOpen)
            {
                Open();
            }

            if (tmpClose)
            {
                Close();
            }
        }
        lastOpenState=tmpOpen;
        lastCloseState=tmpClose;
     
    }

    private void RotateTo(float targetX)
    {
        if (!CanRunCoroutine())
        {
            return;
        }

        if (_rotateCoroutine != null)
        {
            StopCoroutine(_rotateCoroutine);
        }

        _rotateCoroutine = StartCoroutine(RotateXCoroutine(targetX));
    }

    private bool CanRunCoroutine()
    {
        return isActiveAndEnabled && gameObject.activeInHierarchy;
    }

    private IEnumerator RotateXCoroutine(float targetX)
    {
        
        float duration = Mathf.Max(0.0001f, animationTime);
        float elapsed = 0f;
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(targetX, transform.localEulerAngles.y, transform.localEulerAngles.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.localRotation = targetRotation;
        _rotateCoroutine = null;
    }
}
