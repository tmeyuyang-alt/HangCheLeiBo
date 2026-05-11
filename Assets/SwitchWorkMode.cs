using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SwitchWorkMode : MonoBehaviour
{
    public bool isWorkMode = false;

    public GameObject[] WorkModes;
    
    public GameObject[] ActiveModes;

    public void SwitchToWork()
    {
        foreach (var VARIABLE in WorkModes)
        {
            VARIABLE.SetActive(false);
        }

        foreach (var VARIABLE in ActiveModes)
        {
            VARIABLE.SetActive(true);
        }
    }

    public void SwitchToDefault()
    {
        foreach (var VARIABLE in WorkModes)
        {
            VARIABLE.SetActive(true);
        }
        foreach (var VARIABLE in ActiveModes)
        {
            VARIABLE.SetActive(false);
        }
    }
}
