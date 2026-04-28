using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElectrodeParamPanel : MonoBehaviour
{

    public Text Title;
    public Text LowLimitValue;
    public Text HighLimitValue;
    public Text AngleValue;
    public string electrodeName;

    private Transform mainCameraTransform;

    public Vector3 initalPos = Vector3.zero;
    public Vector3 centerPos = Vector3.zero;
    private LineRenderer lineRenderer;
    private GameObject electrode_obj = null;
    void Start()
    {
        //Title.text = string.Format("{0}高度：<color=#ffc97a>{1}</color>  cm", electrodeName,100);
        Title.text = string.Format("{0} 高度：<color=#ffc97a><size=28>{1}</size></color> <size=16> mm</size>", electrodeName, 100);

        electrode_obj = GameObject.Find("dianji");

        mainCameraTransform = Camera.main.transform;

        initalPos = transform.position;
        centerPos = electrode_obj.transform.position;
        centerPos.y = initalPos.y;

        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Inital()
    {
        initalPos = transform.position;
        centerPos.y = initalPos.y;
    }

    public void OnReceiveData(List<PLCData> plcDatas,bool IsTiltData)
    {
        if (!IsTiltData)
        {
            string height = "";
            string lowlimit = "";
            string highlimit = "";
            for (int i = 0; i < plcDatas.Count; i++)
            {
                if (plcDatas[i].Name == electrodeName)
                {
                    switch (plcDatas[i].SubName)
                    {
                        case "datablock":
                            height = plcDatas[i].Value;
                            break;
                        case "highlimit_datablock":
                            highlimit = plcDatas[i].Value;
                            break;
                        case "lowlimit_datablock":
                            lowlimit = plcDatas[i].Value;
                            break;
                    }
                }
            }

            UpdateData(height, lowlimit, highlimit);
        }
        else
        {

            for (int i = 0; i < plcDatas.Count; i++)
            {
                //if (plcDatas[i].Name == electrodeName)
                if (plcDatas[i].Name == "电极" + electrodeName)
                {
                    var data = plcDatas[i];
                    float x = 0;
                    float y = 0;
                    float z = 0;

                    try
                    {
                        if (data.SubName == "datablock")
                        {
                            x = int.Parse(data.Value);
                        }
                        else if (data.SubName == "y_datablock")
                        {
                            y = int.Parse(data.Value);
                        }
                        else if (data.SubName == "z_datablock")
                        {
                            z = int.Parse(data.Value);
                        }
                    }
                    catch
                    {

                    }


                    //this.AngleValue.text = string.Format("X:{0}\t\tY:{1}", x, y);
                }
            }
        }
    }

    public void UpdateData(string height, string lowlimit, string highlimit)
    {
        //Title.text = string.Format("{0}电极高度：<color=#ffc97a>{1}</color>  cm", electrodeName, height);
        Title.text = string.Format("{0} 高度：<color=#ffc97a><size=28>{1}</size></color> <size=16> mm</size>", electrodeName, height);
        LowLimitValue.text = string.Format("{0}电极低限：<color=#77c2e4>{1}</color>", electrodeName, lowlimit);
        HighLimitValue.text = string.Format("{0}电极低限：<color=#77c2e4>{1}</color>", electrodeName, highlimit);
    }

    public void Update()
    {
        //var vec = initalPos -  mainCameraTransform.position;

        //var distance  = vec.magnitude;

        //float mindis = 20;
        //if (distance < mindis)
        //{
        //    var tempValue = (distance / mindis);
        //    tempValue = Mathf.Pow(tempValue, 5);
        //    transform.position = initalPos + Vector3.down*tempValue;
        //}
        //else
        //    transform.position = initalPos;

        //if(electrode_obj!=null)
        //centerPos = electrode_obj.transform.position;

        var vec = initalPos - centerPos;
        var vec2 = mainCameraTransform.position - centerPos;

        var angle =  Vector3.SignedAngle(vec.normalized, Vector3.forward,Vector3.up);
        var offset =  Vector3.SignedAngle(vec2.normalized, Vector3.forward,Vector3.up);

        offset = (int)(offset / 60.0f) * 60.0f;

        angle =  angle - offset;

        var targetPos = Vector3.zero;
        //var cheight = 0.8f;
        if (28 < Mathf.Abs(angle) && Mathf.Abs(angle) < 32f)
        {
            targetPos = initalPos + Vector3.down*1.5f;
            //cheight = 1.5f;
        }
        else if (88 < Mathf.Abs(angle) && Mathf.Abs(angle) < 92)
        {
            targetPos = initalPos + Vector3.down*0.5f;
        }
        else
        {
            targetPos = initalPos + Vector3.down*0.2f ;// Vector3.up*0.2f;
        }
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5);

        //var connectPos = centerPos + vec.normalized * 2.5f + Vector3.down * cheight;


        ////找到最近的点
        //lineRenderer.SetPosition(0, FindMinDistance(centerPos));


        //lineRenderer.SetPosition(1, connectPos);

    }
    private Vector3[] rect = new Vector3[4];
    public Vector3 FindMinDistance(Vector3 target)
    {
        float x = 1.826613f;
        float y = 0.8753976f;

        var t = Camera.main.WorldToScreenPoint(target);
        rect[0] = Camera.main.WorldToScreenPoint(transform.position);
        rect[1] = Camera.main.WorldToScreenPoint(transform.position +transform.right*x);
        rect[2] = Camera.main.WorldToScreenPoint(transform.position + transform.right * x + transform.up*y);
        rect[3] = Camera.main.WorldToScreenPoint(transform.position+ transform.up * y);

        float min = (rect[0] - t).sqrMagnitude;
        int index = 0;
        for (int i = 1; i < rect.Length; i++)
        {
            float d = (rect[i] - t).sqrMagnitude;
            if (d < min)
            {
                min = d;
                index = i;
            }
        }

        switch (index)
        {
            case 0:
                return transform.position;
            case 1:
                return transform.position + transform.right * x;
            case 2:
                return transform.position + transform.right * x + transform.up * y;
            case 3:
                return transform.position + transform.up * y;
        }

        return Vector3.zero;
    }
}
