using System;
using System.Collections;
using HighlightPlus;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Clickable3DObject : MonoBehaviour
{
    
    private float doubleClickInterval = 0.1f;
    private bool ignoreClickWhenPointerOverUI = false;

    public UnityEvent onClick;
    public UnityEvent onDoubleClick;
    public UnityEvent onRightClick;

    private float lastLeftClickTime = -1f;
    private Coroutine singleClickCoroutine;
    private Color colorHover = new Color(1.0f, 1f, 1.0f, 1f);

    private float timr = 2;
    bool isHovering= false;

    private void Update()
    {
        if (isHovering=true)
        {
            timr-=Time.deltaTime;
            if (timr<=0)
            {
                isHovering = false;
                timr = 2;
            }
        }
    }

    private void OnMouseExit()
    {
        if (GetComponent<HighlightEffect>()!=null)
        {
            GetComponent<HighlightEffect>().highlighted = false;
        }
    }

    private void OnMouseOver()
    {
        if (IsPointerOverUI())
        {
            return;
        }

        timr = 2;
        if (GetComponent<HighlightEffect>()!=null)
        {
            GetComponent<HighlightEffect>().glow= 1;
            GetComponent<HighlightEffect>().overlay = 0;
            
            GetComponent<HighlightEffect>().highlighted = true;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (!LoginManager.Instance.isAdmin)
            {
                return;
            }
            onRightClick?.Invoke();
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        float currentTime = Time.unscaledTime;
        bool isDoubleClick = lastLeftClickTime >= 0f && currentTime - lastLeftClickTime <= doubleClickInterval;

        if (isDoubleClick)
        {
            if (singleClickCoroutine != null)
            {
                StopCoroutine(singleClickCoroutine);
                singleClickCoroutine = null;
            }

            lastLeftClickTime = -1f;
            onDoubleClick?.Invoke();
            return;
        }

        lastLeftClickTime = currentTime;
        if (singleClickCoroutine != null)
        {
            StopCoroutine(singleClickCoroutine);
        }

        singleClickCoroutine = StartCoroutine(InvokeSingleClickDelayed());
    }

    private IEnumerator InvokeSingleClickDelayed()
    {
        yield return new WaitForSecondsRealtime(doubleClickInterval);
        singleClickCoroutine = null;
        onClick?.Invoke();
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
