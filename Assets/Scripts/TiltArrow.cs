using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TiltArrow : MonoBehaviour
{
    public int x=0;
    public int y=0;
    public int z=0;

    public Vector3 uifoward;

    private Text xText;
    private Text yText;
    private Text zText;
    //private void Start()
    //{
    //    var tiltUIRes = Resources.Load("TiltUI");
    //    GameObject tiltUI = GameObject.Instantiate(tiltUIRes) as GameObject;
    //    tiltUI.transform.position = transform.position - uifoward * 0.5f;
    //    tiltUI.transform.forward = uifoward;
    //    xText = tiltUI.transform.GetChild(0).GetChild(0).GetComponent<Text>();
    //    yText = tiltUI.transform.GetChild(0).GetChild(1).GetComponent<Text>();
    //    zText = tiltUI.transform.GetChild(0).GetChild(2).GetComponent<Text>();
    //}


    private void Update()
    {
        //Vector3 euler = transform.eulerAngles;

        if (x == 0 && y == 0)
        {
            transform.localScale = Vector3.zero;
        }
        else
        {
            transform.forward = new Vector3((y + 0.0001f) / 90.0f, z, (x + 0.00001f) / 90.0f).normalized;
            transform.localScale = Vector3.one;
        }

    }
    public void OnReceiveData(PLCData data)
    {
        try
        {
            if (data.SubName == "datablock")
            {
                x = int.Parse(data.Value);
                xText.text = "X:"+data.Value;
            }
            else if (data.SubName == "y_datablock")
            {
                y = int.Parse(data.Value);
                yText.text = "Y:" + data.Value;
            }
            else if (data.SubName == "z_datablock")
            {
                z = int.Parse(data.Value);
                zText.text = "Z:" + data.Value;
            }
        }
        catch { 
        
        }
    }
}
