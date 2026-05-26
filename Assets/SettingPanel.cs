using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
   private const string DefaultTiShengHeightConfigName = "TiShengHeight.config";
   private const float DefaultTiShengHeight = 13f;

   public TMP_InputField  inputField;
   public Button setButton;

   public TMP_InputField tiShengHeightInputField;
   public Button setTiShengHeightButton;

   public string limitKey;
   public string biaoDingKey;
   public string settingPrefsKey;
   public string tiShengHeightConfigName = DefaultTiShengHeightConfigName;
   public float defaultTiShengHeight = DefaultTiShengHeight;

   private void Start()
   {
      LoadLastSetting();
      LoadTiShengHeightSetting();
      InvokeRepeating("UpdateUI",1,15);
      if (setButton != null)
      {
         setButton.onClick.AddListener(SetBiaoDing);
      }

      if (setTiShengHeightButton != null)
      {
         setTiShengHeightButton.onClick.AddListener(SetTiShengHeight);
      }
   }

   public void UpdateUI()
   {
      if (inputField == null)
      {
         return;
      }

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

   public void LoadTiShengHeightSetting()
   {
      float height = GetTiShengHeight(tiShengHeightConfigName, defaultTiShengHeight);

      if (tiShengHeightInputField != null)
      {
         tiShengHeightInputField.text = FormatFloat(height);
      }
   }

   public void SetTiShengHeight()
   {
      if (tiShengHeightInputField == null)
      {
         return;
      }

      if (!TryParseFloat(tiShengHeightInputField.text, out float height))
      {
         height = defaultTiShengHeight;
      }

      SaveTiShengHeight(height, tiShengHeightConfigName);
      tiShengHeightInputField.text = FormatFloat(height);
   }

   public static float GetTiShengHeight()
   {
      return GetTiShengHeight(DefaultTiShengHeightConfigName, DefaultTiShengHeight);
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

   private static float GetTiShengHeight(string configName, float defaultValue)
   {
      string path = GetTiShengHeightConfigPath(configName);

      if (!File.Exists(path))
      {
         SaveTiShengHeight(defaultValue, configName);
         return defaultValue;
      }

      try
      {
         string value = ReadFirstValue(path);
         if (TryParseFloat(value, out float height))
         {
            return height;
         }
      }
      catch (Exception ex)
      {
         Debug.LogWarning("[SettingPanel] Read ti sheng height config failed: " + ex.Message);
      }

      SaveTiShengHeight(defaultValue, configName);
      return defaultValue;
   }

   private static void SaveTiShengHeight(float height, string configName)
   {
      string path = GetTiShengHeightConfigPath(configName);
      string directory = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
         Directory.CreateDirectory(directory);
      }

      File.WriteAllText(path, FormatFloat(height));
   }

   private static string GetTiShengHeightConfigPath(string configName)
   {
      if (string.IsNullOrWhiteSpace(configName))
      {
         configName = DefaultTiShengHeightConfigName;
      }

      return Path.Combine(Application.streamingAssetsPath, configName);
   }

   private static string ReadFirstValue(string path)
   {
      string[] lines = File.ReadAllLines(path);
      foreach (string line in lines)
      {
         string value = line.Trim();
         if (!string.IsNullOrEmpty(value) && value != "...")
         {
            return value;
         }
      }

      return string.Empty;
   }

   private static bool TryParseFloat(string value, out float result)
   {
      if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
      {
         return true;
      }

      return float.TryParse(value, out result);
   }

   private static string FormatFloat(float value)
   {
      return value.ToString("0.##", CultureInfo.InvariantCulture);
   }
}
