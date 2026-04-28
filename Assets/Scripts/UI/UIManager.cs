using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    public Transform NormalUI;
    public Transform PopupUI;
    public Transform TopUI;
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            return instance;
        }
    }
    private void Awake()
    {
        instance = this;
    }

    public Stack<UIPanel> UIStack = new Stack<UIPanel>();

    public T GetPanel<T>() where T : UIPanel
    {
        foreach (var ui in UIStack)
        {
            if (ui.GetType().Name == typeof(T).Name)
            {
                return ui as T;
            }
        }
        return null;
    }
    public UIPanel OpenPanel<T>(object param) where T : UIPanel
    {
        Debug.Log("Open Panel:" + typeof(T).Name);

        GameObject panel = GameObject.Instantiate(Resources.Load(typeof(T).Name)) as GameObject;

        panel.transform.SetParent(NormalUI, false);

        var uipanel = panel.GetComponent<UIPanel>();
        
        uipanel.OnEnter(param);

        UIStack.Push(uipanel);

        return uipanel;
    }
    public UIPanel OpenPanel<T>(object param,string type) where T : UIPanel
    {
        Debug.Log("Open Panel:" + typeof(T).Name);

        GameObject panel = GameObject.Instantiate(Resources.Load(typeof(T).Name)) as GameObject;


        var uipanel = panel.GetComponent<UIPanel>();

        switch (type)
        {
            case "Normal":
                panel.transform.SetParent(NormalUI, false);
                UIStack.Push(uipanel);
                break;
            case "Popup":
                panel.transform.SetParent(PopupUI, false);
                break;
            case "Top":
                panel.transform.SetParent(TopUI, false);
                break;
        }


        uipanel.OnEnter(param);

        return uipanel;
    }
    public void ClosePanel<T>() where T : UIPanel
    { 
    
    }

    public void PopOnePanel<T>() where T : UIPanel
    {
        UIPanel panel = UIStack.Peek();

        if (panel.GetType().Name == typeof(T).Name)
        {
            UIStack.Pop();
        }
        else
        {
            Debug.LogError(string.Format(" Pop {0} Failed!", typeof(T).Name));
        }
    }
    public void PopPanel()
    {
        if (UIStack.Count < 2) return;

        UIPanel panel = UIStack.Pop();
        panel.OnClose();
    }
}
