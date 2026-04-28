using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BaseEventHandler
{
    public BaseEventHandler(EventHandler handler, int sort)
    {
        this.handler = handler;
        this.sort = sort;
    }
    public EventHandler handler;
    public int sort = 0;
}

public class EventCenter : MonoBehaviour
{
    public static EventCenter Instance;

    private Dictionary<string, List<BaseEventHandler>> events = new Dictionary<string, List<BaseEventHandler>>();
    void Awake()
    {
        Instance = this;
    }

    public void RegisterEventHandler(string eventName, EventHandler handler, int sort = 0)
    {
        if (events.ContainsKey(eventName))
        {
            //events[eventName].Add(handler);
            var handlerList = events[eventName];
            for (int i = 0; i < handlerList.Count; i++)
            {
                if (handlerList[i].sort >= sort)
                {
                    handlerList.Insert(i, new BaseEventHandler(handler, sort));
                    return;
                }
            }
            //²åÈëÄ©Î²
            handlerList.Add( new BaseEventHandler(handler, sort));
        }
        else
        {
            events.Add(eventName, new List<BaseEventHandler>() { new BaseEventHandler(handler, sort) });
        }
    }

    public bool UnRegisterEventHandler(string eventName, EventHandler handler)
    {
        if (events.ContainsKey(eventName))
        {
            var list = events[eventName];
            //return list.Remove(handler);

            int index = list.FindIndex(delegate (BaseEventHandler bHandler) { return bHandler.handler.Equals(handler); });

            if (index != -1)
            {
                list.RemoveAt(index);
                return true;
            }
        }
        return false;
    }

    public void TriggerEvent(string eventName, object sender, EventArgs args)
    {
        if (events.ContainsKey(eventName))
        {
            var list = events[eventName];
            foreach (var item in list)
            {
                if (item != null && item.handler != null)
                    item.handler(sender, args);
            }
        }
    }
}