using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
   public TMP_InputField  inputField;
   public Button setButton;

   public string limitKey;
   public string biaoDingKey;
   public string settingPrefsKey;

   private void Start()
   {
      LoadLastSetting();
      InvokeRepeating("UpdateUI",1,15);
      if (setButton != null)
      {
         setButton.onClick.AddListener(SetBiaoDing);
      }
   }

   public void UpdateUI()
   {
      if (inputField.isFocused)
      {
         return;
      }
      inputField.text = PLCConfigManager.Instance.GetFloatValue(limitKey).ToString("F2");
   }

   public void SetLimit()
   {
      if (inputField == null)
      {
         return;
      }

      PLCConfigManager.Instance.SetValue(limitKey, Convert.ToSingle(inputField.text));
      SaveLastSetting(inputField.text);
   }

   public void SetBiaoDing()
   {
      PLCConfigManager.Instance.SetPulseBool(biaoDingKey,true);
   }

   public void LoadLastSetting()
   {
      if (inputField == null)
      {
         return;
      }

      string prefsKey = GetSettingPrefsKey();
      if (!PlayerPrefs.HasKey(prefsKey))
      {
         return;
      }

      inputField.text = PlayerPrefs.GetString(prefsKey);
   }

   private void SaveLastSetting(string value)
   {
      string prefsKey = GetSettingPrefsKey();
      PlayerPrefs.SetString(prefsKey, value);
      PlayerPrefs.Save();
   }

   private string GetSettingPrefsKey()
   {
      if (!string.IsNullOrWhiteSpace(settingPrefsKey))
      {
         return settingPrefsKey;
      }

      return "SettingPanel.LastSetting." + limitKey;
   }
}
