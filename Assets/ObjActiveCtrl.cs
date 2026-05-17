using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjActiveCtrl : MonoBehaviour
{
    
    public Button triger;
    
    bool isOpen = false;


    public GameObject[] objs;

    private void Start()
    {
        triger.onClick.AddListener(Trigger);
    }


    public void Trigger()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            foreach (var VARIABLE in objs)
            {
                VARIABLE.SetActive(true);
            }
        }
        else
        {
            foreach (var VARIABLE in objs)
            {
                VARIABLE.SetActive(false);
            }
        }
    }
}
