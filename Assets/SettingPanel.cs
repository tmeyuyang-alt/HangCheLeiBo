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
   private const string DefaultResistanceBoxTempHighLimitKey = "\u7535\u963B\u6E29\u5EA6\u8BBE\u7F6E\u5236\u52A8\u7535\u963B\u7BB1\u6E29\u5EA6\u4E0A\u9650";
   private const string DefaultResistanceBoxTempLowLimitKey = "\u7535\u963B\u6E29\u5EA6\u8BBE\u7F6E\u5236\u52A8\u7535\u963B\u7BB1\u6E29\u5EA6\u4E0B\u9650";

   public TMP_InputField  inputField;
   public Button setButton;

   public TMP_InputField tiShengHeightInputField;
   public Button setTiShengHeightButton;

   public TMP_InputField resistanceBoxTempHighLimitInputField;
   public TMP_InputField resistanceBoxTempLowLimitInputField;
   public Button setResistanceBoxTempHighLimitButton;
   public Button setResistanceBoxTempLowLimitButton;
   public Button setResistanceBoxTempLimitButton;

   public string limitKey;
   public string biaoDingKey;
   public string settingPrefsKey;
   public string tiShengHeightConfigName = DefaultTiShengHeightConfigName;
   public float defaultTiShengHeight = DefaultTiShengHeight;
   public string resistanceBoxTempHighLimitKey = DefaultResistanceBoxTempHighLimitKey;
   public string resistanceBoxTempLowLimitKey = DefaultResistanceBoxTempLowLimitKey;
   public string resistanceBoxTempHighLimitPrefsKey;
   public string resistanceBoxTempLowLimitPrefsKey;

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

      if (setResistanceBoxTempHighLimitButton != null)
      {
         setResistanceBoxTempHighLimitButton.onClick.AddListener(SetResistanceBoxTempHighLimit);
      }

      if (setResistanceBoxTempLowLimitButton != null)
      {
         setResistanceBoxTempLowLimitButton.onClick.AddListener(SetResistanceBoxTempLowLimit);
      }

      if (setResistanceBoxTempLimitButton != null)
      {
         setResistanceBoxTempLimitButton.onClick.AddListener(SetResistanceBoxTempLimits);
      }
   }

   public void UpdateUI()
   {
      UpdatePlcInput(inputField, limitKey);
      UpdatePlcInput(resistanceBoxTempHighLimitInputField, resistanceBoxTempHighLimitKey);
      UpdatePlcInput(resistanceBoxTempLowLimitInputField, resistanceBoxTempLowLimitKey);
   }

   public void SetLimit()
   {
      SetPlcFloatLimit(inputField, limitKey, GetSettingPrefsKey());
   }

   public void SetBiaoDing()
   {
      PLCConfigManager.Instance.SetPulseBool(biaoDingKey,true);
   }

   public void LoadLastSetting()
   {
      LoadLastSetting(inputField, GetSettingPrefsKey());
      LoadLastSetting(resistanceBoxTempHighLimitInputField, GetResistanceBoxTempHighLimitPrefsKey());
      LoadLastSetting(resistanceBoxTempLowLimitInputField, GetResistanceBoxTempLowLimitPrefsKey());
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

   public void SetResistanceBoxTempHighLimit()
   {
      SetPlcFloatLimit(resistanceBoxTempHighLimitInputField, resistanceBoxTempHighLimitKey, GetResistanceBoxTempHighLimitPrefsKey());
   }

   public void SetResistanceBoxTempLowLimit()
   {
      SetPlcFloatLimit(resistanceBoxTempLowLimitInputField, resistanceBoxTempLowLimitKey, GetResistanceBoxTempLowLimitPrefsKey());
   }

   public void SetResistanceBoxTempLimits()
   {
      SetResistanceBoxTempHighLimit();
      SetResistanceBoxTempLowLimit();
   }

   public static float GetTiShengHeight()
   {
      return GetTiShengHeight(DefaultTiShengHeightConfigName, DefaultTiShengHeight);
   }

   private void SaveLastSetting(string value, string prefsKey)
   {
      if (string.IsNullOrWhiteSpace(prefsKey))
      {
         return;
      }

      PlayerPrefs.SetString(prefsKey, value);
      PlayerPrefs.Save();
   }

   private void LoadLastSetting(TMP_InputField targetInputField, string prefsKey)
   {
      if (targetInputField == null || string.IsNullOrWhiteSpace(prefsKey) || !PlayerPrefs.HasKey(prefsKey))
      {
         return;
      }

      targetInputField.text = PlayerPrefs.GetString(prefsKey);
   }

   private void UpdatePlcInput(TMP_InputField targetInputField, string key)
   {
      if (targetInputField == null || targetInputField.isFocused || PLCConfigManager.Instance == null || string.IsNullOrWhiteSpace(key))
      {
         return;
      }

      targetInputField.text = PLCConfigManager.Instance.GetFloatValue(key).ToString("F2");
   }

   private void SetPlcFloatLimit(TMP_InputField targetInputField, string key, string prefsKey)
   {
      if (targetInputField == null || PLCConfigManager.Instance == null || string.IsNullOrWhiteSpace(key))
      {
         return;
      }

      if (!TryParseFloat(targetInputField.text, out float value))
      {
         Debug.LogWarning("[SettingPanel] Invalid float setting: " + targetInputField.text);
         return;
      }

      PLCConfigManager.Instance.SetValue(key, value);
      SaveLastSetting(targetInputField.text, prefsKey);
   }

   private string GetSettingPrefsKey()
   {
      if (!string.IsNullOrWhiteSpace(settingPrefsKey))
      {
         return settingPrefsKey;
      }

      return "SettingPanel.LastSetting." + limitKey;
   }

   private string GetResistanceBoxTempHighLimitPrefsKey()
   {
      if (!string.IsNullOrWhiteSpace(resistanceBoxTempHighLimitPrefsKey))
      {
         return resistanceBoxTempHighLimitPrefsKey;
      }

      return "SettingPanel.LastSetting." + resistanceBoxTempHighLimitKey;
   }

   private string GetResistanceBoxTempLowLimitPrefsKey()
   {
      if (!string.IsNullOrWhiteSpace(resistanceBoxTempLowLimitPrefsKey))
      {
         return resistanceBoxTempLowLimitPrefsKey;
      }

      return "SettingPanel.LastSetting." + resistanceBoxTempLowLimitKey;
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
