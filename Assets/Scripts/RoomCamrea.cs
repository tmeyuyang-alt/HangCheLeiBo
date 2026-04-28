using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomCamrea : MonoBehaviour
{
    private Vector3 DefaultEuler;

    public float vertical = 10f;
    public float horizontal = 10f;
    public float MoveSpeed = 5;
    private float curH =0;
    private float curV =0;

    void Start()
    {
        DefaultEuler = this.transform.localEulerAngles;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            curH += Input.GetAxis("Mouse X")* MoveSpeed;
            curV -= Input.GetAxis("Mouse Y")* MoveSpeed;
        }
        if (Mathf.Abs(curH) <= horizontal)
        {
            transform.eulerAngles = DefaultEuler +new Vector3(curV, curH);
        }
        else
        {
            curH = curH > 0 ? horizontal : -horizontal;

            //curH = Mathf.Lerp(curH, curH > 0 ? horizontal : -horizontal, Time.deltaTime * 3);
        }

        if (Mathf.Abs(curV) <= vertical)
        {
            transform.eulerAngles = DefaultEuler + new Vector3(curV, curH);
        }
        else
        {
            curV = curV > 0 ? vertical : -vertical;

            //curV = Mathf.Lerp(curV, curV > 0 ? vertical : -vertical, Time.deltaTime * 3);
        }
    }

    internal void RestParam()
    {
        transform.eulerAngles = DefaultEuler;
    }
}
