using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneKeyOpenCtrl : MonoBehaviour
{
    public string openKey;
    public string closeKey;

    public void OneKeyOpen()
    {
        PLCConfigManager.Instance.SetPulseBool(openKey,true);
    }

    public void OneKeyClose()
    {
        PLCConfigManager.Instance.SetPulseBool(closeKey, true);
    }
}
