using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelShowControl : MonoBehaviour
{
    public Transform[] GroupA;
    public Transform[] GroupB;
    public Transform[] GroupC;

    public static ModelShowControl Instance;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    public void SetValueA(bool active)
    {
        for (int i = 0; i < GroupA.Length; i++)
        {
            GroupA[i].gameObject.SetActive(active);
        }
    }

    public void SetValueB(bool active)
    {
        for (int i = 0; i < GroupB.Length; i++)
        {
            GroupB[i].gameObject.SetActive(active);
        }
    }
    public void SetValueC(bool active)
    {
        for (int i = 0; i < GroupC.Length; i++)
        {
            GroupC[i].gameObject.SetActive(active);
        }
    }

    public void SetFreeMode()
    {
        SetValueC(false);
        SetValueB(true);
        SetValueA(true);
    }

    public void SetWorkMode()
    {
        SetValueC(true);
        SetValueB(false);
        SetValueA(false);
    }
}
