using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonMirror : MonoBehaviour
{
    public Transform[] buttons;
    private Vector3[] button_pos0;
    private Vector3[] button_pos1;
    void Awake()
    {
        EventCenter.Instance.RegisterEventHandler(EventName.ChangeModelMirror, OnChangedMirror);

        button_pos0 = new Vector3[buttons.Length];
        button_pos1 = new Vector3[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            button_pos0[i] = buttons[i].position;
        }

        var temp0 = button_pos0[0];
        var temp1 = button_pos0[1];

        button_pos1[0] = new Vector3(temp0.x,temp1.y, temp1.z);
        button_pos1[1] = new Vector3(temp1.x,temp0.y, temp0.z);

    }

    void OnChangedMirror(object sender, System.EventArgs args)
    {
        var leftMirror = ((BoolEventArgs)args).value;

        if (leftMirror)
        {
            for (int i = 0; i < button_pos0.Length; i++)
            {
                buttons[i].position = button_pos0[i];
            }
        }
        else
        {
            for (int i = 0; i < button_pos1.Length; i++)
            {
                buttons[i].position = button_pos1[i];
            }
        }
    }
}
