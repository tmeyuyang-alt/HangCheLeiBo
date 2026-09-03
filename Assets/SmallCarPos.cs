using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallCarPos : MonoBehaviour
{
    public bool isNegative=true;
    public string carKey;
    public PLCValueSource valueSource = PLCValueSource.ActiveCrane;

    public float maxLimit;
    //public float minLimit;
    
    public float maxCarPos;
    
    [Tooltip("Lerp平滑时间，值越小跟随越快")]
    public float smoothTime = 0.15f;

    public float curr;

    public void Update()
    {
        curr = PLCConfigManager.Instance.GetFloatValue(carKey, valueSource);
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
