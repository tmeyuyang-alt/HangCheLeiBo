using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallCarPos : MonoBehaviour
{
    public bool isNegative=true;
    public string carKey;
    public PLCValueSource valueSource = PLCValueSource.ActiveCrane;
    public int craneNumber = 0;

    public float maxLimit;
    //public float minLimit;
    public bool IsDebug=false;
    public float maxCarPos;
    
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
        if (!IsDebug)
        {
            curr = PLCConfigManager.Instance.GetFloatValue(carKey, valueSource);
        }
        float tmp=0;
        if (isNegative)
        { 
            tmp = -(curr/ maxCarPos) * maxLimit;  
        }
        else
        {
             tmp = (curr/ maxCarPos) * maxLimit;
        }       
        
        Vector3 targetPos = new Vector3(tmp, transform.localPosition.y, transform.localPosition.z);
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, t);
    }
}
