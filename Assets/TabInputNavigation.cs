using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 把它挂在任意一个活动的 GameObject（UI Canvas）上即可。
/// 逻辑：在当前选中对象上找可导航的 Selectable，
///       Tab → FindSelectableOnDown()   Shift+Tab → FindSelectableOnUp()
/// </summary>
public class TabInputNavigation : MonoBehaviour
{
    public List<InputField> inputFields = new List<InputField>();
    // 当前具有焦点的输入框的索引
    public int currentIndex = 0;

    void Update()
    {
        // 检测Tab键是否被按下
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // 切换到下一个输入框
            SwitchToNextInputField();
        }
    }


    void SwitchToNextInputField()
    {
        // 失去当前输入框的焦点
        inputFields[currentIndex].DeactivateInputField();

        // 计算下一个输入框的索引
        currentIndex = (currentIndex + 1) % inputFields.Count;

        // 让下一个输入框获得焦点
        inputFields[currentIndex].Select();
        inputFields[currentIndex].ActivateInputField();
    }

}
