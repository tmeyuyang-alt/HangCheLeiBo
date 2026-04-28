using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class NameItemText : MonoBehaviour
{
    public NameItemType type;
    private Text info;
    public string houzui;
  
    private void Start()
    {
        info = GetComponent<Text>();
        switch (type)
        {case NameItemType.LinKuang:
                info.text = NameConfig.Instance.LinKuang;
                break;
            case NameItemType.ShaoJieQiu:
                info.text = NameConfig.Instance.ShaoJieQiu;
                break;
            case NameItemType.GuiShi:
                info.text = NameConfig.Instance.GuiShi;
                break;
            case NameItemType.LengYaQiu:
                info.text = NameConfig.Instance.LengYaQiu;
                break;
            case NameItemType.Mei:
                info.text = NameConfig.Instance.Mei;
                break;
         
        }

        if (!string.IsNullOrEmpty(houzui))
        {
            info.text += houzui;
        }
    }
}
