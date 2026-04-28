using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toggle3D : TitleToggle
{

    public OutlineObj outline;

    private void Awake()
    {
        outline = GetComponent<OutlineObj>();
    }
    public override bool IsOn
    {
        set
        {
            m_IsOn = value;

            //显示选中和未选中
            outline.enabled = m_IsOn;
        }
        get { return m_IsOn; }

    }


    private void OnMouseDown()
    {
        if(GlobalInfo.user.permission!=1)
        if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            IsOn = true;
            OnClick?.Invoke();
        }
    }
}
