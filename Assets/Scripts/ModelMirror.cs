using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelMirror : MonoBehaviour
{
    void Awake()
    {
        EventCenter.Instance.RegisterEventHandler(EventName.ChangeModelMirror, OnChanged,-1);
    }

    public void OnChanged(object sender, System.EventArgs args)
    {
        if (args is BoolEventArgs)
        {
            var x = ((BoolEventArgs)args).value ? 1 : -1;
            transform.localScale = new Vector3(x, 1, 1); 
        }
    }

    private void OnDestroy()
    {
        EventCenter.Instance.UnRegisterEventHandler(EventName.ChangeModelMirror, OnChanged);
    }
}
