using System;
using System.Collections;
using System.Collections.Generic;
using HedgehogTeam.EasyTouch;
using HighlightPlus;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(HighlightEffect))]
public class HoverEvents : MonoBehaviour
{
    
    public HighlightEffect highlightEffect;
    public UnityEvent OnEnter;
    public UnityEvent OnExit;
    public GameObject activateObject;

    private QuickTap tap;

    private void Start()
    {
        highlightEffect = GetComponent<HighlightEffect>();
        tap= GetComponent<QuickTap>();
        if (tap!=null)
        {
            tap.onTap.AddListener((x) => { OnMouseExit(); });
        }
    }

    void OnMouseEnter()
    {
        // 1️⃣ 先让 UI 拦截：如果鼠标正指向任何 UI Graphic，则直接返回
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
              // 光标从 3D 移到 UI，立刻还原
            return;
        }
        OnEnter.Invoke();
        if (activateObject != null)
        {
            activateObject.SetActive(true);
        }
        if (highlightEffect!=null)
        {
            highlightEffect.highlighted = true;
        }
    }
    
        // 当鼠标离开 Collider 时触发
        void OnMouseExit()
        {
            // 1️⃣ 先让 UI 拦截：如果鼠标正指向任何 UI Graphic，则直接返回
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // 光标从 3D 移到 UI，立刻还原
                return;
            }
            OnExit.Invoke();
            if (highlightEffect!=null)
            {
                highlightEffect.highlighted = false;
            }
            if (activateObject != null)
            {
                activateObject.SetActive(false);
            }
        }
    
}
