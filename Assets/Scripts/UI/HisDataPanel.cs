using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Excel;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XCharts.Runtime;

public class HisDataPanel : UIPanel
{
    [Serializable]
    private class HistoryQueryRequest
    {
        public string start_at;
        public int duration_value = 1;
        public string duration_unit = "hours";
        public int interval_value = 1;
        public string interval_unit = "minutes";
        public string aggregate = "last";
        public string plc_id = "plc01";
        public string device_name_contains;
        public string[] devices;     // 勾选的设备名列表，匹配 device_name（OR）
        public string[] conditions;  // 勾选的指标名列表，匹配 display_name（OR）
    }
    public float columnWidth = 150f;
    public UGuiTable table;
    public bool inital = false;
    public List<string> datas = new List<string>();
    public List<string> fields = new List<string>();
    public Pages pages;
    public GameObject QueryPanel;
    public Button queryBtn;
    public Button confirmBtn;
    public InputField timeLength;
    public InputField timeInterval;
    public Dropdown timeLengthDB;
    public Dropdown timeIntervalDB;
    public Text startTime;
    public string baseUrl = "http://127.0.0.1:8000";
    public string plcId = "plc01";
    public string aggregate = "last";
    public string deviceNameContains = string.Empty;

    public HistoryQueryCondition condition;

    [Header("图表（可选）：填入后可调用 ApplyDataToChart() 渲染折线图")]
    public LineChart mChart;

    [Header("图表 Serie 切换（可选）：用于动态生成每条折线的开关项")]
    public Transform lineToggleContainer;
    public GameObject lineTogglePrefab;

    private int hiddenFieldNum = 0;

    // 缓存最近一次查询结果，供手动调用 ApplyDataToChart() 使用
    private JArray _cachedColumns;
    private JArray _cachedRows;

    

    public void Start()
    {
        string savedBaseUrl = DataUtil.Deserializer<string>(Application.streamingAssetsPath + "/ServerIP.config");
        if (!string.IsNullOrWhiteSpace(savedBaseUrl))
        {
            baseUrl = NormalizeServerBaseUrl(savedBaseUrl);
        }

        queryBtn.onClick.AddListener(() => { QueryPanel.SetActive(true); });
        confirmBtn.onClick.AddListener(() =>
        {
            QueryPanel.SetActive(false);
            QueryHistory();
        });

        pages.OnPageNumChanged += index =>
        {
            int maxShowNum = table.Row - 1;
            int start = index * maxShowNum;
            int fieldCount = table.Col + hiddenFieldNum;
            int lineNum = fieldCount <= 0 ? 0 : datas.Count / fieldCount;
            int count = Math.Min(maxShowNum, Math.Max(0, lineNum - start));
            ShowData(start, count);
        };

        GameObject.FindObjectOfType<TitlePanel>().gameObject.transform.Find("Items").gameObject.SetActive(false);
    }

    private void ShowData(int start, int count)
    {
        if (datas == null || fields == null || fields.Count == 0)
        {
            table.Clear(1);
            return;
        }

        int col = table.Col;
        int fieldCount = col + hiddenFieldNum;
        for (int i = 0; i < count; i++)
        {
            for (int x = 0; x < col; x++)
            {
                string dataText = datas[(start + i) * fieldCount + x + hiddenFieldNum];
                if (string.IsNullOrWhiteSpace(dataText))
                {
                    dataText = "--";
                }

                table.GetItem(i + 1, x).GetComponent<Text>().text = dataText;
            }
        }

        table.Show(1, count + 1);
        table.Clear(count + 1);
    }

    private void ApplyQueryResult(JArray columnsToken, JArray rowsToken)
    {
        // 缓存原始结果，供手动调用 ApplyDataToChart() 使用
        _cachedColumns = columnsToken;
        _cachedRows    = rowsToken;
        ApplyQueryResultInternal(columnsToken, rowsToken);
    }

    private void ApplyQueryResultInternal(JArray columnsToken, JArray rowsToken)
    {
        if (columnsToken == null || columnsToken.Count == 0)
        {
            fields = new List<string>();
            datas = new List<string>();
            pages.UpdatePageNumber(table.Row - 1, 0);
            table.Clear(1);
            return;
        }

        List<string> columnLabels = new List<string>();
        List<string> fieldKeys = new List<string>();
        foreach (JToken columnToken in columnsToken)
        {
            string field = columnToken["field"]?.ToString();
            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            string label = columnToken["label"]?.ToString();
            if (string.IsNullOrWhiteSpace(label))
            {
                label = field;
            }

            if (string.Equals(field, "ts", StringComparison.OrdinalIgnoreCase))
            {
                label = "时间";
            }

            fieldKeys.Add(field);
            columnLabels.Add(label);
        }

        fields = columnLabels;
        datas = new List<string>();
        if (rowsToken != null)
        {
            foreach (JToken row in rowsToken)
            {
                for (int i = 0; i < fieldKeys.Count; i++)
                {
                    string fieldKey = fieldKeys[i];
                    string value = row[fieldKey]?.ToString();
                    if (string.Equals(fieldKey, "ts", StringComparison.OrdinalIgnoreCase))
                    {
                        value = FormatTimestamp(value);
                    }

                    datas.Add(string.IsNullOrWhiteSpace(value) ? "--" : value);
                }
            }
        }

        EnsureTableInitialized(columnLabels);

        RectTransform rectTable = table.GetComponent<RectTransform>();
        Vector2 sizeDelta = rectTable.sizeDelta;
        sizeDelta.x = columnWidth* table.Col;
        rectTable.sizeDelta = sizeDelta;
        rectTable.anchoredPosition = new Vector2(columnWidth* table.Col * 0.5f, 0);

        int lineNum = table.Col == 0 ? 0 : datas.Count / table.Col;
        pages.UpdatePageNumber(table.Row - 1, lineNum);

        if (lineNum == 0)
        {
            table.Clear(1);
            return;
        }

        ShowData(0, Mathf.Min(table.Row - 1, lineNum));
    }

    private void EnsureTableInitialized(List<string> columnNames)
    {
        UGuiTable.TableHeader[] headers = new UGuiTable.TableHeader[columnNames.Count];
        for (int i = 0; i < headers.Length; i++)
        {
            headers[i] = new UGuiTable.TableHeader(columnNames[i], "Label");
        }

        table.headers = headers;
        bool colChanged = table.Col != headers.Length;
        table.Col = headers.Length;
        if (table.Row < 23)
        {
            table.Row = 23;
        }

        // 首次初始化，或列数发生变化时，强制重建表格
        // Inital() 只追加不清除，必须先 ResetTable() 销毁旧帧，否则旧帧错位导致 header 丢失和黑色字
        if (!inital || colChanged)
        {
            table.ResetTable();
            table.Inital();
            inital = true;
            return;
        }

        for (int i = 0; i < headers.Length; i++)
        {
            table.GetItem(0, i).GetComponent<Text>().text = headers[i].name;
        }
    }

    public void QueryHistory()
    {
        StopAllCoroutines();
        ClearTableData();
        StartCoroutine(QueryHistoryCoroutine());
    }

    /// <summary>发起新查询前清空表格、缓存及图表，避免旧数据残留导致报错。</summary>
    private void ClearTableData()
    {
        _cachedColumns = null;
        _cachedRows    = null;
        datas          = new List<string>();
        fields         = new List<string>();
        pages.UpdatePageNumber(table.Row - 1, 0);
        table.Clear(1);

        // 同步清空图表
        if (mChart != null)
            mChart.RemoveData();

        // 清空切换项列表
        if (lineToggleContainer != null)
        {
            for (int i = lineToggleContainer.childCount - 1; i >= 0; i--)
                Destroy(lineToggleContainer.GetChild(i).gameObject);
        }
    }

    private IEnumerator QueryHistoryCoroutine()
    {
        string endpoint = NormalizeServerBaseUrl(baseUrl) + "/api/v1/history/query";
        
        print(endpoint);
        HistoryQueryRequest payload = BuildRequestPayload();
        string json = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogErrorFormat("[HisDataPanel] 历史数据查询失败: {0}\n{1}", request.error, request.downloadHandler.text);
                yield break;
            }

            JObject root;
            try
            {
                root = JObject.Parse(request.downloadHandler.text);
                print(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("[HisDataPanel] 历史数据解析失败: {0}", ex);
                yield break;
            }

            ApplyQueryResult(root["columns"] as JArray, root["rows"] as JArray);
        }
    }

    public override void OnClose()
    {
        base.OnClose();
        StopAllCoroutines();
        GameObject.FindObjectOfType<TitlePanel>().gameObject.transform.Find("Items").gameObject.SetActive(true);
    }

    public void SaveExcel()
    {
        if (fields == null || datas == null || fields.Count == 0 || datas.Count == 0)
        {
            Debug.LogWarning("[HisDataPanel] 当前没有可导出的历史数据。");
            return;
        }

        OpenFileName ofn = new OpenFileName();
        ofn.structSize = Marshal.SizeOf(ofn);
        ofn.filter = "Excel Files(*.xlsx)\0*.xlsx\0";
        ofn.file = new string(new char[256]);
        ofn.maxFile = ofn.file.Length;
        ofn.fileTitle = new string(new char[64]);
        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.initialDir = Application.dataPath;
        ofn.title = "打开Excel";
        ofn.defExt = "xlsx";
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;

        if (!OpenFileDialog.GetSaveFileName(ofn))
        {
            return;
        }

        ofn.file = ofn.file.Replace("\\", "/");
        FileInfo newFile = new FileInfo(ofn.file);
        if (newFile.Exists)
        {
            newFile.Delete();
            newFile = new FileInfo(ofn.file);
        }

        using (ExcelPackage package = new ExcelPackage(newFile))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");
            for (int i = 0; i < fields.Count; i++)
            {
                worksheet.DefaultColWidth = 25;
                worksheet.Cells[1, i + 1].Value = fields[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int fieldNum = fields.Count + hiddenFieldNum;
            int lineNum = fieldNum == 0 ? 0 : datas.Count / fieldNum;
            for (int i = 0; i < lineNum; i++)
            {
                for (int x = 0; x < fieldNum - hiddenFieldNum; x++)
                {
                    worksheet.Cells[i + 2, x + 1].Value = datas[i * fieldNum + x + hiddenFieldNum];
                }
            }

            package.Save();
        }
    }

    private HistoryQueryRequest BuildRequestPayload()
    {
        string[] selectedDevices    = GetSelectedValues(condition != null ? condition.devices    : null);
        string[] selectedConditions = GetSelectedValues(condition != null ? condition.conditions : null);
        bool hasFilter = selectedDevices.Length > 0 || selectedConditions.Length > 0;

        HistoryQueryRequest payload = new HistoryQueryRequest();
        payload.start_at       = ParseStartAt(startTime != null ? startTime.text : string.Empty);
        payload.duration_value = ParsePositiveInt(timeLength != null ? timeLength.text : string.Empty, 1);
        payload.duration_unit  = MapDurationUnit(timeLengthDB != null && timeLengthDB.captionText != null ? timeLengthDB.captionText.text : string.Empty);
        payload.interval_value = ParsePositiveInt(timeInterval != null ? timeInterval.text : string.Empty, 1);
        payload.interval_unit  = MapIntervalUnit(timeIntervalDB != null && timeIntervalDB.captionText != null ? timeIntervalDB.captionText.text : string.Empty);
        payload.aggregate      = string.IsNullOrWhiteSpace(aggregate) ? "last" : aggregate.Trim();
        payload.plc_id         = string.IsNullOrWhiteSpace(plcId) ? "plc01" : plcId.Trim();
        payload.device_name_contains = hasFilter ? null : (string.IsNullOrWhiteSpace(deviceNameContains) ? null : deviceNameContains.Trim());
        payload.devices    = selectedDevices.Length    > 0 ? selectedDevices    : null;
        payload.conditions = selectedConditions.Length > 0 ? selectedConditions : null;

        Debug.Log($"[HisDataPanel] devices={string.Join(",", selectedDevices)} conditions={string.Join(",", selectedConditions)}");
        return payload;
    }

    /// <summary>收集指定 Toggle 列表中已勾选项的 GameObject 名字。</summary>
    private string[] GetSelectedValues(List<HistoryToggleItem> items)
    {
        if (items == null || items.Count == 0)
            return new string[0];

        List<string> values = new List<string>();
        foreach (HistoryToggleItem item in items)
        {
            if (item == null || item.toggle == null || !item.toggle.isOn)
                continue;

            string value = item.gameObject.name.Trim();
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
                values.Add(value);
        }

        return values.ToArray();
    }

    private string GetConditionItemValue(HistoryToggleItem item)
    {
        if (item.transform.childCount > 1)
        {
            Text text = item.transform.GetChild(1).GetComponent<Text>();
            if (text != null && !string.IsNullOrWhiteSpace(text.text))
            {
                return text.text.Trim();
            }
        }

        return item.gameObject.name.Trim();
    }

    private int ParsePositiveInt(string text, int defaultValue)
    {
        int value;
        if (int.TryParse(text, out value) && value > 0)
        {
            return value;
        }

        return defaultValue;
    }

    private string ParseStartAt(string rawText)
    {
        string normalized = string.IsNullOrWhiteSpace(rawText) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : rawText.Trim();
        DateTime parsed;
        if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            return parsed.ToUniversalTime().ToString("o");
        }

        return normalized;
    }

    private string FormatTimestamp(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return "--";
        }

        DateTime parsed;
        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
        {
            return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        return rawValue;
    }

    private string MapDurationUnit(string unitText)
    {
        switch (unitText)
        {
            case "年":
                return "years";
            case "日":
                return "days";
            case "时":
                return "hours";
            case "分":
                return "minutes";
            case "秒":
                return "seconds";
            default:
                return "hours";
        }
    }

    private string MapIntervalUnit(string unitText)
    {
        switch (unitText)
        {
            case "年":
                return "years";
            case "日":
                return "days";
            case "时":
                return "hours";
            case "分":
                return "minutes";
            case "秒":
                return "seconds";
            default:
                return "minutes";
        }
    }

    private string NormalizeServerBaseUrl(string rawUrl)
    {
        string normalized = string.IsNullOrWhiteSpace(rawUrl) ? "http://127.0.0.1:8000" : rawUrl.Trim();
        normalized = normalized.TrimEnd('/');

        int dataIndex = normalized.IndexOf("/data", StringComparison.OrdinalIgnoreCase);
        if (dataIndex >= 0)
        {
            normalized = normalized.Substring(0, dataIndex);
        }

        int apiIndex = normalized.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (apiIndex >= 0)
        {
            normalized = normalized.Substring(0, apiIndex);
        }

        return normalized.TrimEnd('/');
    }

    // -------------------------------------------------------------------------
    // 图表填充（手动调用，切换到 Chart 页面后调用此方法）
    // -------------------------------------------------------------------------

    /// <summary>
    /// 将最近一次查询结果渲染到 mChart（LineChart）。
    /// 每个数据点位为一条 Serie，X 轴为时间戳。
    /// 切换到图表页面后手动调用此方法。
    /// </summary>
    public void ApplyDataToChart()
    {
        if (mChart == null)
        {
            Debug.LogWarning("[HisDataPanel] mChart 未赋值，无法渲染图表。");
            return;
        }

        if (_cachedColumns == null || _cachedRows == null || _cachedRows.Count == 0)
        {
            Debug.LogWarning("[HisDataPanel] 没有可用的查询数据，请先执行查询。");
            return;
        }

        // 收集非 ts 列（每列对应一个 Serie）
        var serieLabels = new List<string>();
        var serieFields = new List<string>();
        foreach (JToken col in _cachedColumns)
        {
            string field = col["field"]?.ToString();
            if (string.IsNullOrEmpty(field) || string.Equals(field, "ts", StringComparison.OrdinalIgnoreCase))
                continue;
            serieFields.Add(field);
            string label = col["label"]?.ToString();
            serieLabels.Add(string.IsNullOrWhiteSpace(label) ? field : label);
        }

        if (serieFields.Count == 0)
        {
            Debug.LogWarning("[HisDataPanel] 没有可绘制的数据列。");
            return;
        }

        // 清空旧数据
        mChart.RemoveData();

        // 添加 Serie（每个点位一条折线）
        for (int i = 0; i < serieLabels.Count; i++)
        {
            mChart.AddSerie<Line>(serieLabels[i]);
        }

        // 逐行填充 X 轴时间戳和各 Serie 数据
        foreach (JToken row in _cachedRows)
        {
            string ts = row["ts"]?.ToString() ?? string.Empty;
            mChart.AddXAxisData(FormatTimestamp(ts));

            for (int i = 0; i < serieFields.Count; i++)
            {
                double val = 0;
                JToken valToken = row[serieFields[i]];
                if (valToken != null && valToken.Type != JTokenType.Null)
                    double.TryParse(valToken.ToString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out val);
                mChart.AddData(i, val);
            }
        }

        // 生成每条 Serie 对应的 LineToggleItem
        RefreshLineToggles(serieLabels);

        Debug.Log($"[HisDataPanel] 图表已刷新：{serieFields.Count} 条 Serie，{_cachedRows.Count} 个时间点。");
    }

    /// <summary>
    /// 清空 lineToggleContainer 中的旧子项，再为每条 Serie 实例化一个 LineToggleItem。
    /// Toggle 控制对应 Serie 的显示/隐藏；infoColor 显示该 Serie 在主题中的颜色。
    /// </summary>
    private void RefreshLineToggles(List<string> serieLabels)
    {
       print("1");
        if (lineToggleContainer == null || lineTogglePrefab == null)
            return;
        print("2");
        // 清空旧的切换项
        for (int i = lineToggleContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(lineToggleContainer.GetChild(i).gameObject);
        }
        print("3");
        for (int i = 0; i < serieLabels.Count; i++)
        {
            int serieIndex = i; // 闭包捕获
            GameObject go = Instantiate(lineTogglePrefab, lineToggleContainer);
            LineToggleItem item = go.GetComponent<LineToggleItem>();
            if (item == null)
                continue;

            // 设置文字标签（同时确保所在 GameObject 处于激活状态）
            if (item.info != null)
            {
                item.info.gameObject.SetActive(true);
                item.info.text = serieLabels[i].Split('.',StringSplitOptions.None)[1];
                item.info.enabled = true;
               
            }

            // 获取 XCharts 主题颜色并赋给色块（同时确保所在 GameObject 处于激活状态）
            Color serieColor = mChart.theme.GetColor(i);
            if (item.infoColor != null)
            {
                item.infoColor.gameObject.SetActive(true);
                item.infoColor.color = serieColor;
                item.infoColor.enabled = true;
            }

            // Toggle 默认全部开启（确保 GameObject 和组件本身均处于激活/启用状态）
            if (item.toggle != null)
            {
                item.toggle.gameObject.SetActive(true);
                item.toggle.enabled = true;
                
                item.toggle.isOn = true;
                item.transform.GetChild(0).GetComponent<Image>().enabled = true;
                item.toggle.onValueChanged.AddListener(isOn =>
                {
                    var serie = mChart.GetSerie(serieIndex);
                    if (serie != null)
                    {
                        serie.show = isOn;
                        mChart.RefreshChart();
                    }
                });
            }
        }
    }
}
