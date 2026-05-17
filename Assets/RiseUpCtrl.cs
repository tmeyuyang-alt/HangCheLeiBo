using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiseUpCtrl : MonoBehaviour
{
    public string carKey;

    public float offset = 2;
    
    public float maxLimit;
    
    public float downLimit=-10;
    //public float minLimit;
    
    public float maxCarPos;
    
    [Tooltip("Lerp平滑时间，值越小跟随越快")]
    public float smoothTime = 0.15f;

    public float curr;
    public void Update()
    {
        curr = PLCConfigManager.Instance.GetFloatValue(carKey);
        float tmp = -(curr / maxCarPos) * maxLimit;
        if (tmp<=downLimit)
        {
            tmp = downLimit;
        }
        Vector3 targetPos = new Vector3(transform.localPosition.x, transform.localPosition.y, tmp+offset);
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, t);
    }
}
