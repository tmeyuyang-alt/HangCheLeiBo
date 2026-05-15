using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单条实时报警列表项的 UI 控件。
/// 预制体上需要挂载此组件，并在 Inspector 中拖入以下子元素。
/// </summary>
public class AlarmItemUI : MonoBehaviour
{
    [Header("显示字段")]
    public Text txtTime;           // 报警时间
    public Text txtDeviceName;     // 设备名称
    public Text txtAlarmContent;   // 报警内容

    [Header("点击交互")]
    public Button btnClick;        // 整条可点击的按钮（覆盖整行），默认 interactable=false

    public GameObject fixedObj,UnFixedObj;
    
    /// <summary>
    /// 设置显示信息。
    /// </summary>
    public void SetInfo(string time, string deviceName, string alarmContent)
    {
        if (txtTime         != null) txtTime.text         = time;
        if (txtDeviceName   != null) txtDeviceName.text   = deviceName;
        if (txtAlarmContent != null) txtAlarmContent.text  = alarmContent;
    }

    /// <summary>
    /// 设置是否可点击。报警恢复后调用此方法打开交互，并注册点击回调。
    /// </summary>
    public void SetClickable(bool clickable, Action onClick)
    {
        if (btnClick == null) return;
        
        btnClick.interactable = clickable;
        btnClick.onClick.RemoveAllListeners();

        if (clickable && onClick != null)
        {
            btnClick.onClick.AddListener(() => onClick());
        }
        
        
        fixedObj.SetActive(clickable);
        UnFixedObj.SetActive(!clickable);

        // 可选：恢复后变色提示可点击
        ColorBlock colors = btnClick.colors;
        if (clickable)
        {
            colors.normalColor = new Color(0.2f, 0.8f, 0.2f, 0.3f); // 浅绿色提示可处理
        }
        else
        {
            colors.normalColor = new Color(1f, 0.3f, 0.3f, 0.3f);   // 浅红色表示报警中
        }
        btnClick.colors = colors;
    }
}
