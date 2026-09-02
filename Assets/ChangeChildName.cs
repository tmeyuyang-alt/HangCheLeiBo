using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeChildName : MonoBehaviour
{
    [ContextMenu("ChangeName")]
    public void ChangeName()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).name = (i+1).ToString();
        }
    }
}
