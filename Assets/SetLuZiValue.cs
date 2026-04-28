using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XCharts.Runtime;

public class SetLuZiValue : MonoBehaviour
{
    public ProgressBar mBar;
    public Text mValueInfo;
    public Text mInfo;

    private float mMax = 1500;
    
     public float Curr;

    void Start()
    {
       // mInfo.text = gameObject.name;
    }
    public void SetValue(float arg)
    {
        
        Curr=arg/mMax;
        

       
        if (Curr > 1)
        {
           Curr = 1;
        }

        if (Curr <= 0)
        {
            Curr = 0;
        }

        mBar.value = Curr;
        mValueInfo.text = arg.ToString("F2");
        mBar.RefreshGraph();
    }
}
