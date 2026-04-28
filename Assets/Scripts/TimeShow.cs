using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeShow : MonoBehaviour
{
    public Text text;
    private void Start()
    {
        text = GetComponent<Text>();
    }
    void Update()
    {
        if (Time.frameCount % 30 == 0)
        {
            text.text = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }
    }
}
