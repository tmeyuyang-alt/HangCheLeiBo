using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloadDisplay : MonoBehaviour
{
  public TextMeshProUGUI info;
  public string key;

  private void Start()
  {
    InvokeRepeating("UpdateUI",1,1);
  }

  public void UpdateUI()
  {
    info.text = PLCConfigManager.Instance.GetFloatValue(key).ToString("F2");
  }
}
