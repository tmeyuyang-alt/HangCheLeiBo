using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddMainPanelToggle : MonoBehaviour
{
    public TitleToggle toggle;
    public int index = -1;
    // Start is called before the first frame update
    void Start()
    {
        if (toggle == null) { 
            toggle = GetComponent<TitleToggle>();
        }
        index = GameObject.FindObjectOfType<MainPanel>().Bind(toggle);
    }
}
