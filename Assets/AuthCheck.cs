using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AuthCheck : MonoBehaviour
{
    public UnityEvent NOAuthEvent;
    private void OnEnable()
    {
        if (!LoginManager.Instance.isAdmin)
        {
            print("NOAUTH");
            NOAuthEvent.Invoke();
        }
    }
}
