using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigCarPos : MonoBehaviour
{
    public bool isDebug=false;
    public string carKey;

    public float maxLimit;
    //public float minLimit;
    
    public float maxCarPos;
    
    [Tooltip("Lerp平滑时间，值越小跟随越快")]
    public float smoothTime = 0.15f;

    public float curr;
    public bool isNegative=false;

    public void Update()
    {
       
        if (!isDebug)
        {
            curr = PLCConfigManager.Instance.GetFloatValue(carKey);
        }
        float tmp = (curr / maxCarPos) * maxLimit;

        if (isNegative)
        {
            tmp = -tmp;
        }
        Vector3 targetPos = new Vector3(tmp,transform.localPosition.y, transform.localPosition.z);
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, t);
    }
}
