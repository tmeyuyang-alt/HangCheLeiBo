using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SelectItem
{
    public string SName;

    public List<GameObject> Activates;
   // public List<GameObject> Deactivates;
}

public class SelectManager : MonoBehaviour
{
    public List<SelectItem> items;
    public void Switch(string arg)
    {
        foreach (var VARIABLE in items)
        {
            if (arg == VARIABLE.SName)
            {
                foreach (var VARIABLE2 in VARIABLE.Activates)
                {
                    VARIABLE2.SetActive(true);
                }
            }
            else
            {
                foreach (var VARIABLE2 in VARIABLE.Activates)
                {
                    VARIABLE2.SetActive(false);
                }
            }
        }
    }
}
