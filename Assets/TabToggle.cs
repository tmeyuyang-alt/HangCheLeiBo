using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabToggle : MonoBehaviour
{

    public List<Toggle> mToggles;

    public List<GameObject> mAll;

    private void Start()
    {
        foreach (var item in mToggles)
        {
            item.onValueChanged.AddListener(OnChange);
        }
    }

    public void OnChange(bool arg)
    {
        foreach (var item in mToggles)
        {
            if (item.isOn)
            {
                foreach (var item2 in mAll)
                {
                    if (item.name == item2.name)
                    {
                        item2.gameObject.SetActive(true);
                    }else
                    {
                        item2.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
