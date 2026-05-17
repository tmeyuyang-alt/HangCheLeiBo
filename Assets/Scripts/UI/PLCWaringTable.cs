using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 报警记录查询面板。
///
/// Inspector 配置：
///   - table            : UGuiTable（7 列固定表头）
///   - pages            : Pages 分页组件
///   - inputDeviceName  : InputField，输入设备名进行精确过滤（留空 = 不过滤）
///   - inputStartTime   : InputField，开始时间，格式 yyyy-MM-dd HH:mm:ss
///   - inputEndTime     : InputField，结束时间，留空则取当前时间
///   - dropdownOrder    : Dropdown，选项 0=倒序（最新在前）/ 1=正序（最早在前）
///   - queryBtn         : Button，点击触发查询
///   - baseUrl / plcId  : 服务器地址
/// </summary>
public class PLCWaringTable : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector 字段
    // ------------------------------------------------------------------

    [Header("表格 & 分页")]
    public UGuiTable table;
    public Pages pages;
    public float columnWidth = 160f;

    [Header("查询条件")]
    public InputField inputDeviceName;   // 设备名（精确匹配，留空不过滤）
    public Text inputStartTime;    // 开始时间 yyyy-MM-dd HH:mm:ss
    public Text inputEndTime;      // 结束时间（留空取当前）
    public Dropdown   dropdownOrder;     // 0 = 倒序(desc)  1 = 正序(asc)
    public Button     queryBtn;

    [Header("服务器")]
    public string baseUrl  = "http://127.0.0.1:8000";
    public string plcId    = "plc01";
    public int    pageSize = 50;

    // ------------------------------------------------------------------
    // 固定表头（7 列）
    // ------------------------------------------------------------------

    private static readonly string[] ColumnHeaders = new[]
    {
        "报警时间", "恢复时间", "设备名", "报警内容", "报警级别", "处理方法", "操作人员"
    };

    // 对应 JSON 字段名（与 AlarmRecordItem 一致）
    private static readonly string[] ColumnFields = new[]
    {
        "trigger_time", "recovery_time", "device_name",
        "alarm_content", "alarm_level", "handling_method", "operator_name"
    };

    // ------------------------------------------------------------------
    // 内部状态
    // ------------------------------------------------------------------

    private bool _tableInited   = false;
    private int  _totalRecords  = 0;
    private int  _currentPage   = 1;

    // 当前页数据（每行是一个按列顺序排列的字符串数组）
    private readonly List<string[]> _rows = new List<string[]>();

    // ------------------------------------------------------------------
    // 请求体（JsonUtility 序列化）
    // ------------------------------------------------------------------

    [Serializable]
    private class AlarmHistoryRequest
    {
        public string start_at;
        public string end_at;        // 可为 null，但 JsonUtility 不支持 null，留空时不填
        public string device_name;   // 精确匹配，留空不过滤
        public string plc_id;
        public string order;         // "desc" | "asc"
        public int    page;
        public int    page_size;
    }

    // ------------------------------------------------------------------
    // 生命周期
    // ------------------------------------------------------------------

    private void Start()
    {
        // 读取服务器地址
        string saved = DataUtil.Deserializer<string>(
            Application.streamingAssetsPath + "/ServerIP.config");
        if (!string.IsNullOrWhiteSpace(saved))
            baseUrl = NormalizeBaseUrl(saved);

        // 初始化下拉菜单选项（倒序 / 正序）
        if (dropdownOrder != null && dropdownOrder.options.Count == 0)
        {
            dropdownOrder.options.Add(new Dropdown.OptionData("倒序（最新在前）"));
            dropdownOrder.options.Add(new Dropdown.OptionData("正序（最早在前）"));
            dropdownOrder.value = 0;
            dropdownOrder.RefreshShownValue();
        }

        // 查询按钮
        if (queryBtn != null)
            queryBtn.onClick.AddListener(OnQueryClicked);

        // 分页回调
        if (pages != null)
        {
            pages.OnPageNumChanged += pageIndex =>
            {
                _currentPage = pageIndex + 1; // Pages 从 0 开始
                StartCoroutine(QueryCoroutine());
            };
        }

        // 初始化表格（列固定，只建一次）
        InitTable();
    }

    // ------------------------------------------------------------------
    // 表格初始化（固定 7 列）
    // ------------------------------------------------------------------

    private void InitTable()
    {
        if (_tableInited) return;

        UGuiTable.TableHeader[] headers =
            new UGuiTable.TableHeader[ColumnHeaders.Length];
        for (int i = 0; i < headers.Length; i++)
            headers[i] = new UGuiTable.TableHeader(ColumnHeaders[i], "Label");

        table.headers = headers;
        table.Col     = headers.Length;
        if (table.Row < 23) table.Row = 23;

        // 每页请求量不能超过表格可显示行数，否则 ShowRows 越界
        pageSize = Mathf.Min(pageSize, table.Row - 1);

        table.ResetTable();
        table.Inital();

        // 调整宽度
        RectTransform rt = table.GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector2 sd = rt.sizeDelta;
            sd.x = columnWidth * table.Col;
            rt.sizeDelta = sd;
            rt.anchoredPosition = new Vector2(sd.x * 0.5f, 0);
        }

        _tableInited = true;
    }

    // ------------------------------------------------------------------
    // 查询入口
    // ------------------------------------------------------------------

    public void OnQueryClicked()
    {
        _currentPage = 1;
        StopAllCoroutines();
        StartCoroutine(QueryCoroutine());
    }

    // ------------------------------------------------------------------
    // HTTP 查询协程
    // ------------------------------------------------------------------

    private IEnumerator QueryCoroutine()
    {
        string endpoint = NormalizeBaseUrl(baseUrl) + "/api/v1/alarm/history";

        AlarmHistoryRequest payload = BuildPayload();
        string json = BuildJson(payload);

        Debug.Log($"[PLCWaringTable] 请求地址: {endpoint}");
        Debug.Log($"[PLCWaringTable] 请求参数: {json}");

        using (UnityWebRequest req = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PLCWaringTable] 查询失败: {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            try
            {
                ParseAndDisplay(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PLCWaringTable] 解析响应失败: {ex}");
            }
        }
    }

    // ------------------------------------------------------------------
    // 构造请求体
    // ------------------------------------------------------------------

    private AlarmHistoryRequest BuildPayload()
    {
        string deviceName = inputDeviceName != null ? inputDeviceName.text.Trim() : "";
        string startText  = inputStartTime  != null ? inputStartTime.text.Trim()  : "";
        string endText    = inputEndTime    != null ? inputEndTime.text.Trim()     : "";
        string order      = (dropdownOrder != null && dropdownOrder.value == 1) ? "asc" : "desc";

        // 起始时间默认最近 7 天
        if (string.IsNullOrWhiteSpace(startText))
            startText = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd HH:mm:ss");

        return new AlarmHistoryRequest
        {
            start_at    = ParseToLocalString(startText),
            end_at      = string.IsNullOrWhiteSpace(endText) ? null : ParseToLocalString(endText),
            device_name = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName,
            plc_id      = string.IsNullOrWhiteSpace(plcId) ? "plc01" : plcId.Trim(),
            order       = order,
            page        = _currentPage,
            page_size   = pageSize,
        };
    }

    /// <summary>
    /// JsonUtility 不支持 null 字段，手动拼装 JSON 以处理可选字段。
    /// </summary>
    private string BuildJson(AlarmHistoryRequest p)
    {
        var sb = new StringBuilder("{");
        sb.Append($"\"start_at\":\"{p.start_at}\",");
        if (!string.IsNullOrEmpty(p.end_at))
            sb.Append($"\"end_at\":\"{p.end_at}\",");
        if (!string.IsNullOrEmpty(p.device_name))
            sb.Append($"\"device_name\":\"{EscapeJson(p.device_name)}\",");
        sb.Append($"\"plc_id\":\"{EscapeJson(p.plc_id)}\",");
        sb.Append($"\"order\":\"{p.order}\",");
        sb.Append($"\"page\":{p.page},");
        sb.Append($"\"page_size\":{p.page_size}");
        sb.Append("}");
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // 解析响应并显示
    // ------------------------------------------------------------------

    private void ParseAndDisplay(string responseText)
    {
        JObject root    = JObject.Parse(responseText);
        _totalRecords   = root["total"]?.Value<int>() ?? 0;
        int page        = root["page"]?.Value<int>() ?? 1;
        int retPageSize = root["page_size"]?.Value<int>() ?? pageSize;

        _rows.Clear();
        JArray records = root["records"] as JArray;
        if (records != null)
        {
            foreach (JToken rec in records)
            {
                string[] row = new string[ColumnFields.Length];
                for (int i = 0; i < ColumnFields.Length; i++)
                {
                    string val = rec[ColumnFields[i]]?.ToString() ?? "";
                    // 时间字段格式化
                    if (i == 0 || i == 1)
                        val = FormatTimestamp(val);
                    row[i] = string.IsNullOrWhiteSpace(val) ? "--" : val;
                }
                _rows.Add(row);
            }
        }

        // 更新分页（Pages 分页基于【每页条数】和【总条数】）
        int lineNum = _totalRecords;
        if (pages != null)
            pages.UpdatePageNumber(retPageSize, lineNum, _currentPage - 1);

        ShowRows();

        Debug.Log($"[PLCWaringTable] 查询完成: 共 {_totalRecords} 条，第 {page} 页，本页 {_rows.Count} 条");
    }

    // ------------------------------------------------------------------
    // 将当前页数据写入表格
    // ------------------------------------------------------------------

    private void ShowRows()
    {
        // 每页最多能显示 table.Row - 1 行（第 0 行是表头）
        int maxVisible = table.Row - 1;
        int count = Mathf.Min(_rows.Count, maxVisible);
        int cols  = ColumnFields.Length;

        for (int i = 0; i < count; i++)
        {
            for (int c = 0; c < cols; c++)
            {
                Text cell = table.GetItem(i + 1, c)?.GetComponent<Text>();
                if (cell != null)
                    cell.text = _rows[i][c];
            }
        }

        table.Show(1, count + 1);
        table.Clear(count + 1);
    }

    // ------------------------------------------------------------------
    // 辅助方法
    // ------------------------------------------------------------------

    /// <summary>
    /// 把任意格式的时间字符串统一转成本地时间字符串 "yyyy-MM-dd HH:mm:ss"。
    /// TDengine 按本地时间存储字符串，插入和查询必须保持一致，不能用 UTC。
    /// </summary>
    private string ParseToLocalString(string raw)
    {
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal, out DateTime dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return raw;
    }

    private string FormatTimestamp(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "--";
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTime dt))
        {
            return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
        return raw;
    }

    private string NormalizeBaseUrl(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "http://127.0.0.1:8000";
        string s = raw.Trim().TrimEnd('/');
        int idx = s.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) s = s.Substring(0, idx);
        idx = s.IndexOf("/data", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) s = s.Substring(0, idx);
        return s.TrimEnd('/');
    }

    private static string EscapeJson(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
