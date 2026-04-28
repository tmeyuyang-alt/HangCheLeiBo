using System;
using System.Collections;
using System.Collections.Generic;
using HedgehogTeam.EasyTouch;
using HighlightPlus;
using UnityEngine;

public class TogglePanel : MonoBehaviour
{
    public List<QuickTap> allTaps;

    public List<GameObject> allPanels;

    private void Start()
    {
        foreach (QuickTap t in allTaps)
        {
            t.onTap.AddListener((x) => { ShowMyPanel(t.gameObject.name); });
        }
    }

    public void ShowMyPanel(string arg)
    {
        print(arg);
        foreach (GameObject panel in allPanels)
        {
            if (panel.name == arg)
            {
                panel.SetActive(true);
            }
            else
            {
                panel.SetActive(false);
            }
        }

        foreach (QuickTap t in allTaps)
        {
            if (t.gameObject.name == arg)
            {
                t.GetComponent<HighlightEffect>().highlighted = true;
            }
            else
            {
                t.GetComponent<HighlightEffect>().highlighted = false;
            }
        }
    }
}
