using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CangWeiHeightDisplay : MonoBehaviour
{
    public float maxZ= 0.8f, minZ=0;
    public float MaxHeight=4;

    public float tmpZ = 0;
    public void SetHeight(float arg)
    {
        if (arg>MaxHeight)
        {
            arg = MaxHeight;
        }
        tmpZ = (arg/MaxHeight)*(maxZ-minZ);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, tmpZ);
    } 
}
