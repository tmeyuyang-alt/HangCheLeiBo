using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlarmStatusPanel3D : MonoBehaviour
{
    public Text text;
    public void SetName(string strName,bool value= false)
    {
        if (value)
        {
            this.text.text = string.Format("{0}:\t<color=#ff5454>异常</color>", strName);
        }
        else
        {
            this.text.text = string.Format("{0}:\t<color=#fffe9a>正常</color>", strName);
        }
    }
}
