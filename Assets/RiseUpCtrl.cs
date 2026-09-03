using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiseUpCtrl : MonoBehaviour
{
    public string carKey;
    public PLCValueSource valueSource = PLCValueSource.ActiveCrane;
    public int craneNumber = 0;

    public float offset = 2;
    
    public float maxLimit;
    
    public float downLimit=-10;
    //public float minLimit;
    
    public float maxCarPos;
    public bool isDebug=false;
    
    [Tooltip("Lerp平滑时间，值越小跟随越快")]
    public float smoothTime = 0.15f;

    public float curr;

    private void OnEnable()
    {
        PLCConfigManager.OnActiveCraneChanged += OnActiveCraneChanged;
        ApplyCraneValueSource();
    }

    private void Start()
    {
        ApplyCraneValueSource();
    }

    private void OnDisable()
    {
        PLCConfigManager.OnActiveCraneChanged -= OnActiveCraneChanged;
    }

    private void OnActiveCraneChanged(int craneIndex)
    {
        ApplyCraneValueSource();
    }

    private void ApplyCraneValueSource()
    {
        if (craneNumber <= 0 || PLCConfigManager.Instance == null)
        {
            return;
        }

        valueSource = PLCConfigManager.Instance.GetValueSourceForCraneNumber(craneNumber);
    }

    public void Update()
    {
        if (!isDebug)
        {
            curr = PLCConfigManager.Instance.GetFloatValue(carKey, valueSource);
        }
        float tmp = -(curr / maxCarPos) * maxLimit;
        if (tmp+offset<=downLimit)
        {
            tmp = downLimit-offset;
        }
        Vector3 targetPos = new Vector3(transform.localPosition.x,transform.localPosition.y , tmp+offset);
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, t);
    }
}
