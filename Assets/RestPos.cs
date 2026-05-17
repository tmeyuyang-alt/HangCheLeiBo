using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestPos : MonoBehaviour
{
    public Vector2 oriV2;
    public Vector3 oriV3;
 
    private void Start()
    {
        oriV2 = GetComponent<RectTransform>().sizeDelta;
        oriV3 = GetComponent<RectTransform>().localPosition;
    }

    private void OnDisable()
    {
        GetComponent<RectTransform>().sizeDelta = oriV2;
        GetComponent<RectTransform>().localPosition = oriV3;
    }
}
