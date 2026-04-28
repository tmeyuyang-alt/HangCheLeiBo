using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleToggle:MonoBehaviour,IPointerClickHandler
{
    public Sprite normal;
    public Sprite selected;

    public Image context;

    protected bool m_IsOn = false;

    public int id = 0;
    public virtual bool IsOn
    {
        set
        {
            m_IsOn = value;
            if (context==null)
            {
                context = GetComponent<Image>();
                context.sprite = m_IsOn ? selected : normal;
            }
            else
            {
                context.sprite = m_IsOn ? selected : normal;
            }
        }
        get { return m_IsOn; }
    }

    public System.Action OnClick;
    public void OnPointerClick(PointerEventData eventData)
    {
        IsOn = true;
        OnClick?.Invoke();
    }
}
