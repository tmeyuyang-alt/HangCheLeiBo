using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

public class DeviceSignalConfigEditorWindow : EditorWindow
{
    [Serializable]
    private class DeviceSignalConfigFile
    {
        public string deviceName;
        public List<DeviceSignalPoint> points = new List<DeviceSignalPoint>();
    }

    [Serializable]
    private class LegacyDeviceSignalConfigFile
    {
        public string deviceName;
        public string sharedIpAddress;
        public List<DeviceSignalPoint> points = new List<DeviceSignalPoint>();
    }

    [Serializable]
    private class DeviceSignalConfigProjectFile
    {
        public string sharedIpAddress;
        public List<DeviceSignalConfigFile> devices = new List<DeviceSignalConfigFile>();
    }

    [Serializable]
    private class ImportedConfigPayload
    {
        public string sharedIpAddress;
        public List<DeviceSignalConfigFile> devices = new List<DeviceSignalConfigFile>();
    }

    [Serializable]
    private class UploadConfigRequest
    {
        public string plcId;
        public DeviceSignalConfigProjectFile config;
    }

    [Serializable]
    private class UploadConfigResponse
    {
        public int imported_devices;
        public int imported_points;
    }

    [Serializable]
    private class ConfigSnapshotResponse
    {
        public string ts;
        public string plc_id;
        public DeviceSignalConfigProjectFile config_json;
        public string source;
    }

    private const string DefaultConfigAssetSearchFolder = "Assets/方案1";
    private const string DefaultNameLibraryPath = "Assets/DeviceSignalNameLibrary.asset";
    private const string DefaultProjectConfigExcelRelativePath = "Config/deviceConfig.xlsx";
    private const string NameLibraryPrefsKey = "DeviceSignalConfigEditorWindow.NameLibraryGuid";
    private const string ConfigAssetPrefsKey = "DeviceSignalConfigEditorWindow.ConfigAssetGuid";
    private const string ConfigAssetSearchFolderPrefsKey = "DeviceSignalConfigEditorWindow.ConfigAssetSearchFolder";
    private const string UploadServerUrlPrefsKey = "DeviceSignalConfigEditorWindow.UploadServerUrl";
    private const string UploadPlcIdPrefsKey = "DeviceSignalConfigEditorWindow.UploadPlcId";
    private const string AddressIncrementValuePrefsKey = "DeviceSignalConfigEditorWindow.AddressIncrementValue";
    private static readonly string[] AlarmLevelOptions = { "无", "1级", "2级", "3级" };

    private DeviceSignalConfigAsset configAsset;
    private DeviceSignalNameLibrary nameLibrary;
    private Vector2 scrollPosition;
    private Vector2 assetListScrollPosition;
    private List<DeviceSignalConfigAsset> allConfigAssets = new List<DeviceSignalConfigAsset>();
    private List<bool> pointFoldouts = new List<bool>();
    private List<int> pointNamePageIndices = new List<int>();
    private List<bool> pointCustomNameModes = new List<bool>();
    private string pendingDeviceName = string.Empty;
    private string pendingSharedIpAddress = string.Empty;
    private string configAssetSearchFolder = DefaultConfigAssetSearchFolder;
    private string uploadServerUrl = "http://132.232.253.32:8000";
    private string uploadPlcId = "plc01";
    private string addressIncrementValue = "0.1";
    private const int NameOptionsPerPage = 20;

    [MenuItem("Tools/配置编辑器/设备功能点配置")]
    private static void OpenWindow()
    {
        GetWindow<DeviceSignalConfigEditorWindow>("设备配置编辑器");
    }

    [MenuItem("Assets/Create/Config/Device Signal Name Library", priority = 201)]
    private static void CreateNameLibraryAsset()
    {
        CreateAssetAtSelectionPath(new DeviceSignalNameLibrary(), "DeviceSignalNameLibrary.asset");
    }

    [MenuItem("Assets/Create/Config/Device Signal Config", priority = 202)]
    private static void CreateConfigAsset()
    {
        CreateAssetAtSelectionPath(new DeviceSignalConfigAsset(), "DeviceSignalConfig.asset");
    }

    private DeviceSignalConfigAsset CreateConfigAssetInConfigFolder()
    {
        string searchFolder = GetConfigAssetSearchFolder();
        EnsureAssetFolder(searchFolder);

        DeviceSignalConfigAsset asset = CreateInstance<DeviceSignalConfigAsset>();
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{searchFolder}/DeviceSignalConfig.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        return asset;
    }

    private static void CreateAssetAtSelectionPath(UnityEngine.Object asset, string fileName)
    {
        string basePath = "Assets";
        UnityEngine.Object selectedObject = Selection.activeObject;
        if (selectedObject != null)
        {
            string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                basePath = AssetDatabase.IsValidFolder(selectedPath)
                    ? selectedPath
                    : Path.GetDirectoryName(selectedPath)?.Replace("\\", "/");
            }
        }

        if (string.IsNullOrEmpty(basePath))
        {
            basePath = "Assets";
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{basePath}/{fileName}");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    private static void EnsureAssetFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return;
        }

        string[] parts = assetFolderPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private void OnEnable()
    {
        nameLibrary = LoadPersistedAsset<DeviceSignalNameLibrary>(NameLibraryPrefsKey)
            ?? AssetDatabase.LoadAssetAtPath<DeviceSignalNameLibrary>(DefaultNameLibraryPath);
        configAssetSearchFolder = NormalizeAssetFolderPath(EditorPrefs.GetString(ConfigAssetSearchFolderPrefsKey, DefaultConfigAssetSearchFolder));
        uploadServerUrl = EditorPrefs.GetString(UploadServerUrlPrefsKey, "http://132.232.253.32:8000");
        uploadPlcId = EditorPrefs.GetString(UploadPlcIdPrefsKey, "plc01");
        addressIncrementValue = EditorPrefs.GetString(AddressIncrementValuePrefsKey, "0.1");
        RefreshConfigAssets();
        PersistSelectedNameLibrary();
        PersistSelectedConfigAsset();
        PersistUploadSettings();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawConfigAssetList();
            GUILayout.Space(10);
            DrawEditorPanel();
        }
    }

    private void DrawConfigAssetList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
        {
            EditorGUILayout.LabelField("设备列表", EditorStyles.boldLabel);
            DrawConfigAssetSearchFolderField();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新", GUILayout.Height(24)))
                {
                    RefreshConfigAssets();
                }

                if (GUILayout.Button("新建", GUILayout.Height(24)))
                {
                    configAsset = CreateConfigAssetInConfigFolder();
                    RefreshConfigAssets();
                }
            }

            assetListScrollPosition = EditorGUILayout.BeginScrollView(assetListScrollPosition, "box");
            if (allConfigAssets.Count == 0)
            {
                EditorGUILayout.HelpBox($"指定目录中还没有设备配置资产：{GetConfigAssetSearchFolder()}", MessageType.Info);
            }
            else
            {
                foreach (DeviceSignalConfigAsset asset in allConfigAssets)
                {
                    if (asset == null)
                    {
                        continue;
                    }

                    bool isSelected = asset == configAsset;
                    GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton);
                    buttonStyle.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Toggle(isSelected, asset.name, buttonStyle, GUILayout.Height(24)) && !isSelected)
                    {
                        configAsset = asset;
                        pendingDeviceName = configAsset != null ? configAsset.name : string.Empty;
                        pendingSharedIpAddress = GetCurrentSharedIpAddress();
                        ResetPointEditorState();
                        PersistSelectedConfigAsset();
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawConfigAssetSearchFolderField()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            DefaultAsset currentFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(GetConfigAssetSearchFolder());

            EditorGUI.BeginChangeCheck();
            DefaultAsset selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("检索目录", currentFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck() && selectedFolder != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedFolder);
                if (AssetDatabase.IsValidFolder(selectedPath))
                {
                    SetConfigAssetSearchFolder(selectedPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("目录无效", "请选择 Project 窗口中的文件夹。", "确定");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(GetConfigAssetSearchFolder(), EditorStyles.miniLabel, GUILayout.Height(18));

                if (GUILayout.Button("选择", GUILayout.Width(48), GUILayout.Height(20)))
                {
                    string absolutePath = EditorUtility.OpenFolderPanel("选择设备配置检索目录", Application.dataPath, string.Empty);
                    string assetPath = AbsolutePathToAssetPath(absolutePath);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        SetConfigAssetSearchFolder(assetPath);
                    }
                }
            }
        }
    }

    private void DrawEditorPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            DrawAssetSection();
            EditorGUILayout.Space(8);

            if (configAsset == null)
            {
                EditorGUILayout.HelpBox("请先从左侧选择或创建一个设备配置。", MessageType.Info);
                return;
            }

            if (!DrawToolbar())
            {
                return;
            }
            EditorGUILayout.Space(8);
            DrawPointList();
        }
    }

    private void DrawAssetSection()
    {
        EditorGUILayout.LabelField("基础配置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        nameLibrary = (DeviceSignalNameLibrary)EditorGUILayout.ObjectField("名称库资产", nameLibrary, typeof(DeviceSignalNameLibrary), false);
        if (EditorGUI.EndChangeCheck())
        {
            PersistSelectedNameLibrary();
            Repaint();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新建名称库", GUILayout.Height(24)))
            {
                CreateNameLibraryAsset();
                nameLibrary = Selection.activeObject as DeviceSignalNameLibrary;
                PersistSelectedNameLibrary();
            }

            if (GUILayout.Button("导入 Excel 生成名称库", GUILayout.Height(24)))
            {
                ImportNameLibraryFromExcel();
            }
        }

        if (string.IsNullOrEmpty(pendingDeviceName) && configAsset != null)
        {
            pendingDeviceName = configAsset.name;
        }

        pendingDeviceName = EditorGUILayout.TextField("设备名称", pendingDeviceName);
        pendingSharedIpAddress = EditorGUILayout.TextField("共享IP地址", pendingSharedIpAddress);
        EditorGUI.BeginChangeCheck();
        addressIncrementValue = EditorGUILayout.TextField("地址递增值", addressIncrementValue);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(AddressIncrementValuePrefsKey, addressIncrementValue ?? string.Empty);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("服务器上传", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        uploadServerUrl = EditorGUILayout.TextField("服务器地址", uploadServerUrl);
        uploadPlcId = EditorGUILayout.TextField("PLC 标识", uploadPlcId);
        if (EditorGUI.EndChangeCheck())
        {
            PersistUploadSettings();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("一键上传配置到服务器", GUILayout.Height(28)))
            {
                UploadConfigToServer();
            }

            if (GUILayout.Button("从服务器下载配置", GUILayout.Height(28)))
            {
                DownloadConfigFromServer();
            }
        }
    }

    private bool DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("添加功能点", GUILayout.Height(28)))
            {
                if (configAsset == null)
                {
                    return false;
                }

                Undo.RecordObject(configAsset, "Add Device Signal Point");
                if (configAsset.points == null)
                {
                    configAsset.points = new List<DeviceSignalPoint>();
                }

                DeviceSignalPoint previousPoint = configAsset.points.Count > 0
                    ? configAsset.points[configAsset.points.Count - 1]
                    : null;

                configAsset.points.Add(CreatePointFromPrevious(previousPoint));
                MarkConfigDirty();
            }

            if (GUILayout.Button("保存配置", GUILayout.Height(28)))
            {
                SaveConfigAsset();
                GUIUtility.ExitGUI();
                return false;
            }

            if (GUILayout.Button("导入 JSON", GUILayout.Height(28)))
            {
                ImportJson();
                GUIUtility.ExitGUI();
                return false;
            }

            if (GUILayout.Button("导入设备表", GUILayout.Height(28)))
            {
                ImportProjectConfigFromExcel();
                GUIUtility.ExitGUI();
                return false;
            }

            if (GUILayout.Button("导出 JSON", GUILayout.Height(28)))
            {
                ExportJson();
            }
        }

        return true;
    }

    private void RefreshConfigAssets()
    {
        allConfigAssets.Clear();

        string searchFolder = GetConfigAssetSearchFolder();
        EnsureAssetFolder(searchFolder);
        string[] guids = AssetDatabase.FindAssets("t:DeviceSignalConfigAsset", new[] { searchFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DeviceSignalConfigAsset asset = AssetDatabase.LoadAssetAtPath<DeviceSignalConfigAsset>(path);
            if (asset != null)
            {
                allConfigAssets.Add(asset);
            }
        }

        allConfigAssets.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));

        if (configAsset == null || !allConfigAssets.Contains(configAsset))
        {
            configAsset = LoadPersistedAsset<DeviceSignalConfigAsset>(ConfigAssetPrefsKey);
            if (configAsset == null || !allConfigAssets.Contains(configAsset))
            {
                configAsset = allConfigAssets.Count > 0 ? allConfigAssets[0] : AssetDatabase.LoadAssetAtPath<DeviceSignalConfigAsset>($"{searchFolder}/DeviceSignalConfig.asset");
            }
        }

        pendingDeviceName = configAsset != null ? configAsset.name : string.Empty;
        pendingSharedIpAddress = GetCurrentSharedIpAddress();
        PersistSelectedConfigAsset();
    }

    private void RenameConfigAsset(string newDeviceName)
    {
        if (configAsset == null)
        {
            return;
        }

        string trimmedName = string.IsNullOrWhiteSpace(newDeviceName) ? configAsset.name : newDeviceName.Trim();
        if (trimmedName == configAsset.name)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(configAsset);
        string errorMessage = AssetDatabase.RenameAsset(assetPath, trimmedName);
        if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorUtility.DisplayDialog("重命名失败", errorMessage, "确定");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshConfigAssets();
    }

    private void DrawPointList()
    {
        EditorGUILayout.LabelField("功能点列表", EditorStyles.boldLabel);

        if (configAsset == null)
        {
            return;
        }

        if (configAsset.points == null)
        {
            configAsset.points = new List<DeviceSignalPoint>();
        }

        if (configAsset.points.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有功能点，点击“添加功能点”开始配置。", MessageType.None);
            return;
        }

        EnsurePointFoldouts();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < configAsset.points.Count; i++)
        {
            DrawPointItem(i, configAsset.points[i]);
            EditorGUILayout.Space(6);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawPointItem(int index, DeviceSignalPoint point)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EnsurePointFoldouts();

            using (new EditorGUILayout.HorizontalScope())
            {
                string pointTitle = string.IsNullOrWhiteSpace(point.displayName)
                    ? $"功能点 {index + 1}"
                    : $"功能点 {index + 1} - {point.displayName}";

                pointFoldouts[index] = EditorGUILayout.Foldout(pointFoldouts[index], pointTitle, true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("删除", GUILayout.Width(64)))
                {
                    Undo.RecordObject(configAsset, "Remove Device Signal Point");
                    configAsset.points.RemoveAt(index);
                    if (index >= 0 && index < pointFoldouts.Count)
                    {
                        pointFoldouts.RemoveAt(index);
                    }
                    if (index >= 0 && index < pointCustomNameModes.Count)
                    {
                        pointCustomNameModes.RemoveAt(index);
                    }
                    MarkConfigDirty();
                    GUIUtility.ExitGUI();
                }
            }

            if (!pointFoldouts[index])
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            string[] options = GetNameOptions();
            int nameIndex = GetSelectedNameIndex(point.displayName, options);
            bool useCustomName = GetPointCustomNameMode(index, options, nameIndex);
            int selectedIndex = useCustomName ? options.Length - 1 : nameIndex;
            bool hasSelectionChanged = DrawPagedNameSelector(index, options, selectedIndex, out int newSelectedIndex);
            if (hasSelectionChanged)
            {
                string selectedName = options[Mathf.Clamp(newSelectedIndex, 0, options.Length - 1)];
                useCustomName = selectedName == CustomNameOption;
                SetPointCustomNameMode(index, useCustomName);
                if (!useCustomName)
                {
                    point.displayName = selectedName;
                }
            }

            if (useCustomName)
            {
                point.displayName = EditorGUILayout.TextField("自定义名称", point.displayName);
            }

            point.dataType = (DeviceSignalDataType)EditorGUILayout.EnumPopup("数据类型", point.dataType);
            using (new EditorGUILayout.HorizontalScope())
            {
                point.address = EditorGUILayout.TextField("地址", point.address);
                GUI.enabled = index > 0;
                if (GUILayout.Button("递增", GUILayout.Width(64)))
                {
                    IncrementAddressFromPrevious(index, point);
                }
                GUI.enabled = true;
            }
            point.isWrite = EditorGUILayout.Toggle("是否写", point.isWrite);
            point.isPulse = EditorGUILayout.Toggle("是否脉冲", point.isPulse);
            int alarmLevelIndex = Mathf.Clamp((int)point.alarmLevel, 0, AlarmLevelOptions.Length - 1);
            alarmLevelIndex = EditorGUILayout.Popup("报警等级", alarmLevelIndex, AlarmLevelOptions);
            point.alarmLevel = (DeviceSignalAlarmLevel)alarmLevelIndex;
            point.isHistoryData = EditorGUILayout.Toggle("是否历史数据", point.isHistoryData);
            if (EditorGUI.EndChangeCheck())
            {
                MarkConfigDirty();
            }
        }
    }

    private void EnsurePointFoldouts()
    {
        if (configAsset == null || configAsset.points == null)
        {
            pointFoldouts.Clear();
            pointNamePageIndices.Clear();
            pointCustomNameModes.Clear();
            return;
        }

        while (pointFoldouts.Count < configAsset.points.Count)
        {
            pointFoldouts.Add(true);
        }

        while (pointFoldouts.Count > configAsset.points.Count)
        {
            pointFoldouts.RemoveAt(pointFoldouts.Count - 1);
        }

        while (pointNamePageIndices.Count < configAsset.points.Count)
        {
            pointNamePageIndices.Add(-1);
        }

        while (pointNamePageIndices.Count > configAsset.points.Count)
        {
            pointNamePageIndices.RemoveAt(pointNamePageIndices.Count - 1);
        }

        while (pointCustomNameModes.Count < configAsset.points.Count)
        {
            pointCustomNameModes.Add(false);
        }

        while (pointCustomNameModes.Count > configAsset.points.Count)
        {
            pointCustomNameModes.RemoveAt(pointCustomNameModes.Count - 1);
        }
    }

    private const string CustomNameOption = "自定义...";

    private bool GetPointCustomNameMode(int pointIndex, string[] options, int selectedIndex)
    {
        bool isLibraryName = selectedIndex >= 0 && selectedIndex < options.Length - 1;
        if (!isLibraryName)
        {
            return true;
        }

        return pointIndex >= 0 && pointIndex < pointCustomNameModes.Count && pointCustomNameModes[pointIndex];
    }

    private void SetPointCustomNameMode(int pointIndex, bool isCustom)
    {
        EnsurePointFoldouts();
        if (pointIndex >= 0 && pointIndex < pointCustomNameModes.Count)
        {
            pointCustomNameModes[pointIndex] = isCustom;
        }
    }

    private void ResetPointEditorState()
    {
        pointFoldouts.Clear();
        pointNamePageIndices.Clear();
        pointCustomNameModes.Clear();
    }

    private string[] GetNameOptions()
    {
        List<string> options = new List<string>();
        if (nameLibrary != null && nameLibrary.names != null)
        {
            foreach (string item in nameLibrary.names)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    options.Add(item);
                }
            }
        }

        if (options.Count == 0)
        {
            options.Add("启动信号");
            options.Add("停止信号");
            options.Add("拉闸信号");
        }

        options.Add(CustomNameOption);
        return options.ToArray();
    }

    private int GetSelectedNameIndex(string currentName, string[] options)
    {
        if (string.IsNullOrEmpty(currentName))
        {
            return 0;
        }

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == currentName)
            {
                return i;
            }
        }

        return options.Length - 1;
    }

    private bool DrawPagedNameSelector(int pointIndex, string[] options, int selectedIndex, out int newSelectedIndex)
    {
        newSelectedIndex = selectedIndex;
        if (options == null || options.Length == 0)
        {
            return false;
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(options.Length / (float)NameOptionsPerPage));
        int currentPageIndex = pointIndex < pointNamePageIndices.Count ? pointNamePageIndices[pointIndex] : 0;
        bool hasLibrarySelection = selectedIndex >= 0 && selectedIndex < options.Length - 1;
        if (currentPageIndex < 0 && hasLibrarySelection)
        {
            currentPageIndex = selectedIndex / NameOptionsPerPage;
        }

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel("名称");

            GUI.enabled = currentPageIndex > 0;
            if (GUILayout.Button("上一页", GUILayout.Width(56)))
            {
                currentPageIndex--;
            }

            GUI.enabled = currentPageIndex < totalPages - 1;
            if (GUILayout.Button("下一页", GUILayout.Width(56)))
            {
                currentPageIndex++;
            }
            GUI.enabled = true;

            GUILayout.Label($"{currentPageIndex + 1}/{totalPages}", GUILayout.Width(40));
        }

        if (pointIndex < pointNamePageIndices.Count)
        {
            pointNamePageIndices[pointIndex] = currentPageIndex;
        }

        int startIndex = currentPageIndex * NameOptionsPerPage;
        int count = Mathf.Min(NameOptionsPerPage, options.Length - startIndex);
        string[] pageOptions = new string[count];
        Array.Copy(options, startIndex, pageOptions, 0, count);

        int localSelectedIndex = 0;
        if (hasLibrarySelection && selectedIndex >= startIndex && selectedIndex < startIndex + count)
        {
            localSelectedIndex = selectedIndex - startIndex;
        }
        else if (selectedIndex == options.Length - 1 && pageOptions.Length > 0 && pageOptions[pageOptions.Length - 1] == CustomNameOption)
        {
            localSelectedIndex = pageOptions.Length - 1;
        }

        EditorGUI.BeginChangeCheck();
        int newLocalIndex = EditorGUILayout.Popup(GUIContent.none, localSelectedIndex, pageOptions);
        bool hasChanged = EditorGUI.EndChangeCheck();
        if (hasChanged)
        {
            newSelectedIndex = startIndex + newLocalIndex;
        }

        return hasChanged;
    }

    private void IncrementAddressFromPrevious(int index, DeviceSignalPoint point)
    {
        if (configAsset == null || configAsset.points == null || index <= 0 || index >= configAsset.points.Count)
        {
            return;
        }

        string previousAddress = configAsset.points[index - 1]?.address;
        if (!TryIncrementPlcAddress(previousAddress, addressIncrementValue, out string incrementedAddress, out string errorMessage))
        {
            EditorUtility.DisplayDialog("地址递增失败", errorMessage, "确定");
            return;
        }

        Undo.RecordObject(configAsset, "Increment Device Signal Address");
        point.address = incrementedAddress;
        MarkConfigDirty();
        GUI.FocusControl(null);
    }

    private bool TryIncrementPlcAddress(string address, string incrementText, out string incrementedAddress, out string errorMessage)
    {
        incrementedAddress = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(address))
        {
            errorMessage = "上一条功能点地址为空，无法递增。";
            return false;
        }

        if (TryParseDbxAddress(address, out int dbNumber, out int startByte, out int bitNumber))
        {
            if (!TryParseDbxIncrement(incrementText, out int bitIncrement, out errorMessage))
            {
                return false;
            }

            int totalBits = startByte * 8 + bitNumber + bitIncrement;
            if (totalBits < 0)
            {
                errorMessage = "递增后的 DBX 地址不能小于 0。";
                return false;
            }

            incrementedAddress = $"DB{dbNumber}.DBX{totalBits / 8}.{totalBits % 8}";
            return true;
        }

        if (TryParseDbByteAddress(address, out dbNumber, out string dataType, out startByte))
        {
            if (!TryParseByteIncrement(incrementText, out int byteIncrement, out errorMessage))
            {
                return false;
            }

            int nextByte = startByte + byteIncrement;
            if (nextByte < 0)
            {
                errorMessage = "递增后的 DB 地址不能小于 0。";
                return false;
            }

            incrementedAddress = $"DB{dbNumber}.{dataType}{nextByte}";
            return true;
        }

        errorMessage = $"无法识别上一条地址：{address}\n支持格式示例：DB1.DBX0.2、DB1.DBW0、DB1.DBD0、DB1.DBB0。";
        return false;
    }

    private bool TryParseDbxAddress(string address, out int dbNumber, out int startByte, out int bitNumber)
    {
        dbNumber = 0;
        startByte = 0;
        bitNumber = 0;

        string normalizedAddress = address.Trim();
        int dbxIndex = normalizedAddress.IndexOf(".DBX", StringComparison.OrdinalIgnoreCase);
        if (!normalizedAddress.StartsWith("DB", StringComparison.OrdinalIgnoreCase) || dbxIndex <= 2)
        {
            return false;
        }

        string dbNumberText = normalizedAddress.Substring(2, dbxIndex - 2);
        string bitAddressText = normalizedAddress.Substring(dbxIndex + 4);
        string[] parts = bitAddressText.Split('.');
        return parts.Length == 2
            && int.TryParse(dbNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out dbNumber)
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out startByte)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out bitNumber)
            && bitNumber >= 0
            && bitNumber <= 7;
    }

    private bool TryParseDbByteAddress(string address, out int dbNumber, out string dataType, out int startByte)
    {
        dbNumber = 0;
        dataType = string.Empty;
        startByte = 0;

        string normalizedAddress = address.Trim();
        if (!normalizedAddress.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] supportedTypes = { ".DBB", ".DBW", ".DBD" };
        foreach (string token in supportedTypes)
        {
            int typeIndex = normalizedAddress.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (typeIndex <= 2)
            {
                continue;
            }

            string dbNumberText = normalizedAddress.Substring(2, typeIndex - 2);
            string startByteText = normalizedAddress.Substring(typeIndex + token.Length);
            if (startByteText.Contains("."))
            {
                return false;
            }

            if (int.TryParse(dbNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out dbNumber)
                && int.TryParse(startByteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out startByte))
            {
                dataType = token.Substring(1).ToUpperInvariant();
                return true;
            }
        }

        return false;
    }

    private bool TryParseDbxIncrement(string incrementText, out int bitIncrement, out string errorMessage)
    {
        bitIncrement = 0;
        errorMessage = string.Empty;
        string normalizedIncrement = string.IsNullOrWhiteSpace(incrementText) ? "0.1" : incrementText.Trim();

        if (normalizedIncrement.Contains("."))
        {
            string[] parts = normalizedIncrement.Split('.');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int byteIncrement)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int extraBitIncrement))
            {
                errorMessage = "DBX 地址递增值格式错误，请输入 0.1、0.2、1.0 这类 byte.bit 格式。";
                return false;
            }

            bitIncrement = byteIncrement * 8 + extraBitIncrement;
        }
        else if (int.TryParse(normalizedIncrement, NumberStyles.Integer, CultureInfo.InvariantCulture, out int wholeByteIncrement))
        {
            bitIncrement = wholeByteIncrement * 8;
        }
        else
        {
            errorMessage = "DBX 地址递增值格式错误，请输入 0.1、0.2、1.0 这类 byte.bit 格式。";
            return false;
        }

        if (bitIncrement <= 0)
        {
            errorMessage = "地址递增值必须大于 0。";
            return false;
        }

        return true;
    }

    private bool TryParseByteIncrement(string incrementText, out int byteIncrement, out string errorMessage)
    {
        byteIncrement = 0;
        errorMessage = string.Empty;
        string normalizedIncrement = string.IsNullOrWhiteSpace(incrementText) ? "1" : incrementText.Trim();

        if (!decimal.TryParse(normalizedIncrement, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal increment)
            && !decimal.TryParse(normalizedIncrement, NumberStyles.Number, CultureInfo.CurrentCulture, out increment))
        {
            errorMessage = "非 DBX 地址递增值必须是整数字节数，例如 1、2、4。";
            return false;
        }

        if (increment != decimal.Truncate(increment))
        {
            errorMessage = "非 DBX 地址递增值必须是整数字节数，例如 1、2、4。";
            return false;
        }

        byteIncrement = (int)increment;
        if (byteIncrement <= 0)
        {
            errorMessage = "地址递增值必须大于 0。";
            return false;
        }

        return true;
    }

    private DeviceSignalPoint CreatePointFromPrevious(DeviceSignalPoint previousPoint)
    {
        if (previousPoint != null)
        {
            return new DeviceSignalPoint
            {
                displayName = previousPoint.displayName,
                dataType = previousPoint.dataType,
                address = previousPoint.address,
                isWrite = previousPoint.isWrite,
                isPulse = previousPoint.isPulse,
                alarmLevel = previousPoint.alarmLevel,
                isHistoryData = previousPoint.isHistoryData
            };
        }

        string[] options = GetNameOptions();
        string defaultName = options.Length > 0 ? options[0] : "新功能点";
        if (defaultName == CustomNameOption)
        {
            defaultName = "新功能点";
        }

        return new DeviceSignalPoint
        {
            displayName = defaultName,
            dataType = DeviceSignalDataType.BOOL,
            address = string.Empty,
            isWrite = false,
            isPulse = false,
            alarmLevel = DeviceSignalAlarmLevel.None,
            isHistoryData = false
        };
    }

    private void SaveConfigAsset()
    {
        ApplyPendingDeviceName();
        ApplySharedIpAddressToAllConfigs(pendingSharedIpAddress);
        SaveAssetIfNeeded(nameLibrary);
        SaveAssetIfNeeded(configAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshConfigAssets();
        PersistSelectedNameLibrary();
        PersistSelectedConfigAsset();
        ShowNotification(new GUIContent("配置已保存"));
    }

    private void SaveAssetIfNeeded(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
    }

    private void ImportNameLibraryFromExcel()
    {
        string excelPath = EditorUtility.OpenFilePanel("选择 Excel 文件", Application.dataPath, "xlsx");
        if (string.IsNullOrEmpty(excelPath))
        {
            return;
        }

        if (nameLibrary == null)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("保存名称库", "DeviceSignalNameLibrary", "asset", "请选择名称库保存位置");
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            nameLibrary = CreateInstance<DeviceSignalNameLibrary>();
            AssetDatabase.CreateAsset(nameLibrary, assetPath);
            PersistSelectedNameLibrary();
        }

        List<string> importedNames;
        try
        {
            importedNames = ReadUniqueNamesFromExcel(excelPath);
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("导入失败", $"Excel 读取失败：{exception.Message}", "确定");
            return;
        }

        if (importedNames.Count == 0)
        {
            EditorUtility.DisplayDialog("导入失败", "Excel 第二列没有可用内容。", "确定");
            return;
        }

        Undo.RecordObject(nameLibrary, "Import Device Signal Name Library");
        nameLibrary.names = importedNames;
        EditorUtility.SetDirty(nameLibrary);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        PersistSelectedNameLibrary();
        ShowNotification(new GUIContent($"名称库已更新，共 {importedNames.Count} 条"));
    }

    private List<string> ReadUniqueNamesFromExcel(string excelPath)
    {
        List<string> results = new List<string>();
        HashSet<string> uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (FileStream stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (ExcelPackage package = new ExcelPackage(stream))
        {
            if (package.Workbook.Worksheets.Count == 0)
            {
                return results;
            }

            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
            if (worksheet.Dimension == null)
            {
                return results;
            }

            int endRow = worksheet.Dimension.End.Row;
            for (int row = 1; row <= endRow; row++)
            {
                string rawValue = worksheet.Cells[row, 2].Text;
                string normalizedValue = string.IsNullOrWhiteSpace(rawValue) ? string.Empty : rawValue.Trim();
                if (string.IsNullOrEmpty(normalizedValue) || !uniqueNames.Add(normalizedValue))
                {
                    continue;
                }

                results.Add(normalizedValue);
            }
        }

        return results;
    }

    private void ImportProjectConfigFromExcel()
    {
        string excelPath = GetDefaultProjectConfigExcelPath();
        if (!File.Exists(excelPath))
        {
            EditorUtility.DisplayDialog("导入失败", $"未找到配置表：{excelPath}", "确定");
            return;
        }

        ImportedConfigPayload payload;
        try
        {
            payload = ReadProjectConfigFromExcel(excelPath);
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("导入失败", $"Excel 读取失败：{exception.Message}", "确定");
            return;
        }

        if (payload == null || payload.devices == null || payload.devices.Count == 0)
        {
            EditorUtility.DisplayDialog("导入失败", "Excel 中没有可导入的点位。", "确定");
            return;
        }

        ApplyImportedConfigs(payload.devices, null);
        SaveAssetIfNeeded(nameLibrary);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshConfigAssets();
        PersistSelectedConfigAsset();

        if (configAsset == null && allConfigAssets.Count > 0)
        {
            configAsset = allConfigAssets[0];
            PersistSelectedConfigAsset();
        }

        pendingDeviceName = configAsset != null ? configAsset.name : string.Empty;
        pendingSharedIpAddress = GetCurrentSharedIpAddress();

        int importedPointCount = 0;
        foreach (DeviceSignalConfigFile device in payload.devices)
        {
            if (device?.points == null)
            {
                continue;
            }

            importedPointCount += device.points.Count;
        }

        EditorUtility.DisplayDialog(
            "导入完成",
            $"已从 deviceConfig.xlsx 导入 {payload.devices.Count} 个设备，{importedPointCount} 个点位。",
            "确定");
    }

    private string GetDefaultProjectConfigExcelPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(projectRoot, DefaultProjectConfigExcelRelativePath);
    }

    private ImportedConfigPayload ReadProjectConfigFromExcel(string excelPath)
    {
        Dictionary<string, DeviceSignalConfigFile> deviceMap = new Dictionary<string, DeviceSignalConfigFile>(StringComparer.OrdinalIgnoreCase);
        string currentDeviceName = string.Empty;
        using (FileStream stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (ExcelPackage package = new ExcelPackage(stream))
        {
            if (package.Workbook.Worksheets.Count == 0)
            {
                return new ImportedConfigPayload();
            }

            ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
            if (worksheet.Dimension == null || worksheet.Dimension.End.Row < 3)
            {
                return new ImportedConfigPayload();
            }

            int endRow = worksheet.Dimension.End.Row;
            for (int row = 3; row <= endRow; row++)
            {
                string rawDeviceName = NormalizeExcelCellText(worksheet.Cells[row, 1].Text);
                if (!string.IsNullOrEmpty(rawDeviceName))
                {
                    currentDeviceName = rawDeviceName;
                }

                string deviceName = currentDeviceName;
                string pointName = NormalizeExcelCellText(worksheet.Cells[row, 2].Text);
                string dataTypeText = NormalizeExcelCellText(worksheet.Cells[row, 3].Text);
                string address = NormalizeExcelCellText(worksheet.Cells[row, 4].Text);
                string readTypeText = NormalizeExcelCellText(worksheet.Cells[row, 5].Text);
                string pulseText = NormalizeExcelCellText(worksheet.Cells[row, 6].Text);
                string skipText = NormalizeExcelCellText(worksheet.Cells[row, 7].Text);
                string alarmLevelText = NormalizeExcelCellText(worksheet.Cells[row, 8].Text);
                string historyText = NormalizeExcelCellText(worksheet.Cells[row, 9].Text);

                if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(pointName) || string.IsNullOrEmpty(address))
                {
                    continue;
                }

                if (skipText.Contains("没有"))
                {
                    continue;
                }

                if (!deviceMap.TryGetValue(deviceName, out DeviceSignalConfigFile deviceConfig))
                {
                    deviceConfig = new DeviceSignalConfigFile
                    {
                        deviceName = deviceName,
                        points = new List<DeviceSignalPoint>()
                    };
                    deviceMap.Add(deviceName, deviceConfig);
                }

                deviceConfig.points.Add(new DeviceSignalPoint
                {
                    displayName = pointName,
                    dataType = ParseExcelDataType(dataTypeText),
                    address = address,
                    isWrite = ParseExcelIsWrite(readTypeText),
                    isPulse = ParseExcelIsPulse(pulseText),
                    alarmLevel = ParseExcelAlarmLevel(alarmLevelText),
                    isHistoryData = ParseExcelIsHistory(historyText)
                });
            }
        }

        ImportedConfigPayload payload = new ImportedConfigPayload();
        foreach (DeviceSignalConfigFile device in deviceMap.Values)
        {
            payload.devices.Add(device);
        }

        return payload;
    }

    private string NormalizeExcelCellText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private DeviceSignalDataType ParseExcelDataType(string value)
    {
        string normalizedValue = NormalizeExcelCellText(value).Replace(" ", string.Empty).ToUpperInvariant();
        if (normalizedValue.Contains("LINT") || normalizedValue.Contains("LONG"))
        {
            return DeviceSignalDataType.LINT;
        }

        if (normalizedValue.Contains("DINT") || normalizedValue.Contains("DWORD"))
        {
            return DeviceSignalDataType.DINT;
        }

        if (normalizedValue.Contains("REAL") || normalizedValue.Contains("FLOAT"))
        {
            return DeviceSignalDataType.REAL;
        }

        if (normalizedValue.Contains("INT") || normalizedValue.Contains("WORD"))
        {
            return DeviceSignalDataType.INT;
        }

        return DeviceSignalDataType.BOOL;
    }

    private bool ParseExcelIsWrite(string value)
    {
        string normalizedValue = NormalizeExcelCellText(value);
        if (string.IsNullOrEmpty(normalizedValue))
        {
            return false;
        }

        string upperValue = normalizedValue.ToUpperInvariant();
        return normalizedValue.Contains("写")
            || normalizedValue.Contains("读写")
            || upperValue.Contains("RW")
            || upperValue.Contains("WRITE");
    }

    private bool ParseExcelIsPulse(string value)
    {
        string normalizedValue = NormalizeExcelCellText(value);
        return normalizedValue.Contains("脉冲500ms") || normalizedValue.Contains("脉冲");
    }

    private DeviceSignalAlarmLevel ParseExcelAlarmLevel(string value)
    {
        string normalizedValue = NormalizeExcelCellText(value);
        if (normalizedValue.Contains("3级") || normalizedValue.Contains("三级"))
        {
            return DeviceSignalAlarmLevel.Level3;
        }

        if (normalizedValue.Contains("2级") || normalizedValue.Contains("二级"))
        {
            return DeviceSignalAlarmLevel.Level2;
        }

        if (normalizedValue.Contains("1级") || normalizedValue.Contains("一级"))
        {
            return DeviceSignalAlarmLevel.Level1;
        }

        return DeviceSignalAlarmLevel.None;
    }

    private bool ParseExcelIsHistory(string value)
    {
        string normalizedValue = NormalizeExcelCellText(value);
        if (string.IsNullOrEmpty(normalizedValue))
        {
            return false;
        }

        string upperValue = normalizedValue.ToUpperInvariant();
        return normalizedValue == "是"
            || normalizedValue == "Y"
            || normalizedValue == "y"
            || upperValue == "YES"
            || upperValue == "TRUE"
            || normalizedValue == "1";
    }

    private void ExportJson()
    {
        DeviceSignalConfigProjectFile projectFile = BuildProjectConfigPayload();
        if (projectFile.devices.Count == 0)
        {
            return;
        }

        string path = EditorUtility.SaveFilePanel("导出工程设备配置", Application.dataPath, "DeviceSignalConfigs", "json");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string json = JsonUtility.ToJson(projectFile, true);
        File.WriteAllText(path, json, Encoding.UTF8);
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("JSON 已导出"));
    }

    private DeviceSignalConfigProjectFile BuildProjectConfigPayload()
    {
        RefreshConfigAssets();
        DeviceSignalConfigProjectFile projectFile = new DeviceSignalConfigProjectFile
        {
            sharedIpAddress = GetCurrentSharedIpAddress()
        };

        foreach (DeviceSignalConfigAsset asset in allConfigAssets)
        {
            if (asset == null)
            {
                continue;
            }

            projectFile.devices.Add(new DeviceSignalConfigFile
            {
                deviceName = asset.name,
                points = ClonePoints(asset.points)
            });
        }

        return projectFile;
    }

    private void UploadConfigToServer()
    {
        SaveConfigAsset();
        PersistUploadSettings();

        string normalizedServerUrl = string.IsNullOrWhiteSpace(uploadServerUrl)
            ? string.Empty
            : uploadServerUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedServerUrl))
        {
            EditorUtility.DisplayDialog("上传失败", "请先填写服务器地址。", "确定");
            return;
        }

        string normalizedPlcId = string.IsNullOrWhiteSpace(uploadPlcId)
            ? "plc01"
            : uploadPlcId.Trim();
        DeviceSignalConfigProjectFile projectFile = BuildProjectConfigPayload();
        if (projectFile.devices.Count == 0)
        {
            EditorUtility.DisplayDialog("上传失败", "当前没有可上传的设备配置。", "确定");
            return;
        }

        UploadConfigRequest requestPayload = new UploadConfigRequest
        {
            plcId = normalizedPlcId,
            config = projectFile
        };

        string requestJson = JsonUtility.ToJson(requestPayload, true);
        string endpoint = normalizedServerUrl + "/api/v1/config/import-json";
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(requestJson);

        UnityEngine.Debug.Log($"[UploadConfig] POST {endpoint}");
        UnityEngine.Debug.Log($"[UploadConfig] Body size: {bodyBytes.Length} bytes");
        UnityEngine.Debug.Log($"[UploadConfig] Body preview: {(requestJson.Length > 500 ? requestJson.Substring(0, 500) + "..." : requestJson)}");

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Accept = "application/json";
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;
            request.ServicePoint.Expect100Continue = false;
            request.UserAgent = "UnityEditor";
            request.ContentLength = bodyBytes.Length;
            request.KeepAlive = false;
            request.ProtocolVersion = System.Net.HttpVersion.Version10;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string responseText = reader.ReadToEnd();
                UnityEngine.Debug.Log($"[UploadConfig] Response {(int)response.StatusCode}: {responseText}");
                UploadConfigResponse result = JsonUtility.FromJson<UploadConfigResponse>(responseText);
                int importedDevices = result != null ? result.imported_devices : 0;
                int importedPoints = result != null ? result.imported_points : 0;
                EditorUtility.DisplayDialog(
                    "上传成功",
                    $"已上传到 {normalizedServerUrl}\n设备数: {importedDevices}\n点位数: {importedPoints}",
                    "确定");
            }
        }
        catch (WebException ex)
        {
            string message = ex.Message;
            string statusCode = "";
            string responseBody = "";
            string responseHeaders = "";

            if (ex.Response is HttpWebResponse httpResp)
            {
                statusCode = $"{(int)httpResp.StatusCode} {httpResp.StatusDescription}";
                responseHeaders = httpResp.Headers?.ToString() ?? "";
                using (StreamReader reader = new StreamReader(httpResp.GetResponseStream()))
                {
                    responseBody = reader.ReadToEnd();
                }
            }

            UnityEngine.Debug.LogError($"[UploadConfig] WebException: {ex.Message}");
            UnityEngine.Debug.LogError($"[UploadConfig] Status: {ex.Status}");
            UnityEngine.Debug.LogError($"[UploadConfig] HTTP Status: {statusCode}");
            UnityEngine.Debug.LogError($"[UploadConfig] Response Headers:\n{responseHeaders}");
            UnityEngine.Debug.LogError($"[UploadConfig] Response Body:\n{responseBody}");

            string dialogMsg = $"错误: {message}\n\nHTTP状态: {statusCode}\n\n响应内容: {(string.IsNullOrWhiteSpace(responseBody) ? "(空)" : responseBody)}\n\n请查看Console面板获取完整日志";
            EditorUtility.DisplayDialog("上传失败", dialogMsg, "确定");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[UploadConfig] Unexpected error: {ex}");
            EditorUtility.DisplayDialog("上传失败", $"未知错误: {ex.Message}", "确定");
        }
    }

    private void DownloadConfigFromServer()
    {
        string normalizedServerUrl = string.IsNullOrWhiteSpace(uploadServerUrl)
            ? string.Empty
            : uploadServerUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedServerUrl))
        {
            EditorUtility.DisplayDialog("下载失败", "请先填写服务器地址。", "确定");
            return;
        }

        string normalizedPlcId = string.IsNullOrWhiteSpace(uploadPlcId)
            ? "plc01"
            : uploadPlcId.Trim();

        string endpoint = normalizedServerUrl + "/api/v1/config/latest?plc_id=" + Uri.EscapeDataString(normalizedPlcId);

        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = "GET";
            request.Timeout = 15000;
            request.ServicePoint.Expect100Continue = false;
            request.UserAgent = "UnityEditor";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                string responseText = reader.ReadToEnd();
                ConfigSnapshotResponse snapshot = JsonUtility.FromJson<ConfigSnapshotResponse>(responseText);
                if (snapshot == null || snapshot.config_json == null || snapshot.config_json.devices == null || snapshot.config_json.devices.Count == 0)
                {
                    EditorUtility.DisplayDialog("下载失败", "服务器返回的配置为空。", "确定");
                    return;
                }

                if (!EditorUtility.DisplayDialog(
                    "确认导入",
                    $"从服务器下载到 {snapshot.config_json.devices.Count} 个设备配置\n" +
                    $"PLC: {snapshot.plc_id}\n来源: {snapshot.source}\n时间: {snapshot.ts}\n\n是否导入？（会覆盖当前同名设备配置）",
                    "导入", "取消"))
                {
                    return;
                }

                ApplyImportedConfigs(snapshot.config_json.devices, snapshot.config_json.sharedIpAddress);
                SaveAssetIfNeeded(nameLibrary);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RefreshConfigAssets();
                PersistSelectedConfigAsset();

                if (configAsset == null && allConfigAssets.Count > 0)
                {
                    configAsset = allConfigAssets[0];
                    PersistSelectedConfigAsset();
                }

                pendingDeviceName = configAsset != null ? configAsset.name : string.Empty;
                pendingSharedIpAddress = GetCurrentSharedIpAddress();
                ShowNotification(new GUIContent("已从服务器下载配置"));
            }
        }
        catch (WebException ex)
        {
            string message = ex.Message;
            if (ex.Response != null)
            {
                using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                {
                    string responseText = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(responseText))
                    {
                        message = responseText;
                    }
                }
            }

            EditorUtility.DisplayDialog("下载失败", message, "确定");
        }
    }

    private void ImportJson()
    {
        string path = EditorUtility.OpenFilePanel("导入工程设备配置", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string json = File.ReadAllText(path, Encoding.UTF8);
        ImportedConfigPayload payload = ParseImportedConfigFiles(json);
        if (payload == null || payload.devices == null || payload.devices.Count == 0)
        {
            EditorUtility.DisplayDialog("导入失败", "JSON 内容无效。", "确定");
            return;
        }

        ApplyImportedConfigs(payload.devices, payload.sharedIpAddress);
        SaveAssetIfNeeded(nameLibrary);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshConfigAssets();
        PersistSelectedConfigAsset();

        if (configAsset == null && allConfigAssets.Count > 0)
        {
            configAsset = allConfigAssets[0];
            PersistSelectedConfigAsset();
        }

        pendingDeviceName = configAsset != null ? configAsset.name : string.Empty;
        pendingSharedIpAddress = GetCurrentSharedIpAddress();
        ShowNotification(new GUIContent("JSON 已导入"));
    }

    private ImportedConfigPayload ParseImportedConfigFiles(string json)
    {
        DeviceSignalConfigProjectFile projectFile = JsonUtility.FromJson<DeviceSignalConfigProjectFile>(json);
        if (projectFile != null && projectFile.devices != null && projectFile.devices.Count > 0)
        {
            return new ImportedConfigPayload
            {
                sharedIpAddress = projectFile.sharedIpAddress,
                devices = projectFile.devices
            };
        }

        LegacyDeviceSignalConfigFile legacySingleFile = JsonUtility.FromJson<LegacyDeviceSignalConfigFile>(json);
        if (legacySingleFile != null && (!string.IsNullOrWhiteSpace(legacySingleFile.deviceName) || legacySingleFile.points != null))
        {
            return new ImportedConfigPayload
            {
                sharedIpAddress = legacySingleFile.sharedIpAddress,
                devices = new List<DeviceSignalConfigFile>
                {
                    new DeviceSignalConfigFile
                    {
                        deviceName = legacySingleFile.deviceName,
                        points = legacySingleFile.points ?? new List<DeviceSignalPoint>()
                    }
                }
            };
        }

        return null;
    }

    private void ApplyImportedConfigs(List<DeviceSignalConfigFile> importedFiles, string sharedIpAddress)
    {
        RefreshConfigAssets();

        Dictionary<string, DeviceSignalConfigAsset> assetMap = new Dictionary<string, DeviceSignalConfigAsset>(StringComparer.OrdinalIgnoreCase);
        foreach (DeviceSignalConfigAsset asset in allConfigAssets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.name))
            {
                continue;
            }

            assetMap[asset.name.Trim()] = asset;
        }

        foreach (DeviceSignalConfigFile file in importedFiles)
        {
            if (file == null)
            {
                continue;
            }

            string deviceName = string.IsNullOrWhiteSpace(file.deviceName) ? "DeviceSignalConfig" : file.deviceName.Trim();
            if (!assetMap.TryGetValue(deviceName, out DeviceSignalConfigAsset asset) || asset == null)
            {
                asset = CreateConfigAssetForImport(deviceName);
                assetMap[deviceName] = asset;
                allConfigAssets.Add(asset);
            }

            Undo.RecordObject(asset, "Import Device Signal Config");
            asset.points = ClonePoints(file.points);
            EditorUtility.SetDirty(asset);
        }

        if (sharedIpAddress != null)
        {
            ApplySharedIpAddressToAllConfigs(sharedIpAddress);
        }
    }

    private DeviceSignalConfigAsset CreateConfigAssetForImport(string deviceName)
    {
        string targetDirectory = GetConfigAssetSearchFolder();
        EnsureAssetFolder(targetDirectory);
        if (configAsset != null)
        {
            string currentAssetPath = AssetDatabase.GetAssetPath(configAsset);
            if (!string.IsNullOrEmpty(currentAssetPath))
            {
                string currentDirectory = Path.GetDirectoryName(currentAssetPath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(currentDirectory) && IsPathInsideConfigAssetSearchFolder(currentDirectory))
                {
                    targetDirectory = currentDirectory;
                }
            }
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{targetDirectory}/{deviceName}.asset");
        DeviceSignalConfigAsset newAsset = CreateInstance<DeviceSignalConfigAsset>();
        newAsset.sharedIpAddress = GetCurrentSharedIpAddress();
        newAsset.points = new List<DeviceSignalPoint>();
        AssetDatabase.CreateAsset(newAsset, assetPath);
        return newAsset;
    }

    private bool IsPathInsideConfigAssetSearchFolder(string assetPath)
    {
        string searchFolder = GetConfigAssetSearchFolder();
        return assetPath.Equals(searchFolder, StringComparison.OrdinalIgnoreCase)
            || assetPath.StartsWith(searchFolder + "/", StringComparison.OrdinalIgnoreCase);
    }

    private string GetConfigAssetSearchFolder()
    {
        configAssetSearchFolder = NormalizeAssetFolderPath(configAssetSearchFolder);
        return configAssetSearchFolder;
    }

    private void SetConfigAssetSearchFolder(string assetFolderPath)
    {
        string normalizedPath = NormalizeAssetFolderPath(assetFolderPath);
        if (normalizedPath == GetConfigAssetSearchFolder())
        {
            return;
        }

        configAssetSearchFolder = normalizedPath;
        EditorPrefs.SetString(ConfigAssetSearchFolderPrefsKey, configAssetSearchFolder);
        configAsset = null;
        RefreshConfigAssets();
        Repaint();
    }

    private static string NormalizeAssetFolderPath(string assetFolderPath)
    {
        string normalizedPath = string.IsNullOrWhiteSpace(assetFolderPath)
            ? DefaultConfigAssetSearchFolder
            : assetFolderPath.Trim().Replace("\\", "/").TrimEnd('/');

        if (string.IsNullOrEmpty(normalizedPath)
            || (!normalizedPath.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                && !normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)))
        {
            return DefaultConfigAssetSearchFolder;
        }

        return normalizedPath;
    }

    private static string AbsolutePathToAssetPath(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return string.Empty;
        }

        string normalizedAbsolutePath = absolutePath.Replace("\\", "/").TrimEnd('/');
        string normalizedDataPath = Application.dataPath.Replace("\\", "/").TrimEnd('/');

        if (normalizedAbsolutePath.Equals(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets";
        }

        if (!normalizedAbsolutePath.StartsWith(normalizedDataPath + "/", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("目录无效", "检索目录必须位于当前工程的 Assets 目录下。", "确定");
            return string.Empty;
        }

        return "Assets" + normalizedAbsolutePath.Substring(normalizedDataPath.Length);
    }

    private List<DeviceSignalPoint> ClonePoints(List<DeviceSignalPoint> sourcePoints)
    {
        List<DeviceSignalPoint> clonedPoints = new List<DeviceSignalPoint>();
        if (sourcePoints == null)
        {
            return clonedPoints;
        }

        foreach (DeviceSignalPoint point in sourcePoints)
        {
            if (point == null)
            {
                continue;
            }

            clonedPoints.Add(new DeviceSignalPoint
            {
                displayName = point.displayName,
                dataType = point.dataType,
                address = point.address,
                isWrite = point.isWrite,
                isPulse = point.isPulse,
                alarmLevel = point.alarmLevel,
                isHistoryData = point.isHistoryData
            });
        }

        return clonedPoints;
    }

    private string GetCurrentSharedIpAddress()
    {
        if (configAsset != null && !string.IsNullOrWhiteSpace(configAsset.sharedIpAddress))
        {
            return configAsset.sharedIpAddress.Trim();
        }

        foreach (DeviceSignalConfigAsset asset in allConfigAssets)
        {
            if (asset != null && !string.IsNullOrWhiteSpace(asset.sharedIpAddress))
            {
                return asset.sharedIpAddress.Trim();
            }
        }

        return string.Empty;
    }

    private void ApplySharedIpAddressToAllConfigs(string sharedIpAddress)
    {
        string normalizedIpAddress = string.IsNullOrWhiteSpace(sharedIpAddress) ? string.Empty : sharedIpAddress.Trim();
        HashSet<DeviceSignalConfigAsset> updatedAssets = new HashSet<DeviceSignalConfigAsset>();

        foreach (DeviceSignalConfigAsset asset in allConfigAssets)
        {
            if (asset == null || updatedAssets.Contains(asset))
            {
                continue;
            }

            Undo.RecordObject(asset, "Update Shared Device IP Address");
            asset.sharedIpAddress = normalizedIpAddress;
            EditorUtility.SetDirty(asset);
            updatedAssets.Add(asset);
        }

        if (configAsset != null && !updatedAssets.Contains(configAsset))
        {
            Undo.RecordObject(configAsset, "Update Shared Device IP Address");
            configAsset.sharedIpAddress = normalizedIpAddress;
            EditorUtility.SetDirty(configAsset);
        }

        pendingSharedIpAddress = normalizedIpAddress;
    }

    private void ApplyPendingDeviceName()
    {
        if (configAsset == null)
        {
            return;
        }

        string targetName = string.IsNullOrWhiteSpace(pendingDeviceName) ? configAsset.name : pendingDeviceName.Trim();
        RenameConfigAsset(targetName);
        pendingDeviceName = configAsset != null ? configAsset.name : targetName;
    }

    private void MarkConfigDirty()
    {
        if (configAsset != null)
        {
            EditorUtility.SetDirty(configAsset);
        }
    }

    private T LoadPersistedAsset<T>(string prefsKey) where T : UnityEngine.Object
    {
        string guid = EditorPrefs.GetString(prefsKey, string.Empty);
        if (string.IsNullOrEmpty(guid))
        {
            return null;
        }

        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    private void PersistSelectedNameLibrary()
    {
        PersistAssetGuid(NameLibraryPrefsKey, nameLibrary);
    }

    private void PersistSelectedConfigAsset()
    {
        PersistAssetGuid(ConfigAssetPrefsKey, configAsset);
    }

    private void PersistUploadSettings()
    {
        EditorPrefs.SetString(UploadServerUrlPrefsKey, uploadServerUrl ?? string.Empty);
        EditorPrefs.SetString(UploadPlcIdPrefsKey, uploadPlcId ?? string.Empty);
    }

    private void PersistAssetGuid(string prefsKey, UnityEngine.Object asset)
    {
        if (asset == null)
        {
            EditorPrefs.DeleteKey(prefsKey);
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(assetPath))
        {
            EditorPrefs.DeleteKey(prefsKey);
            return;
        }

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid))
        {
            EditorPrefs.DeleteKey(prefsKey);
            return;
        }

        EditorPrefs.SetString(prefsKey, guid);
    }
}

