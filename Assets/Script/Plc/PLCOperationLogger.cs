using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// PLC 写操作日志记录器（全局单例，DontDestroyOnLoad）。
///
/// 服务器地址自动从 StreamingAssets/ServerIP.config 读取，
/// 与项目其他模块（PLCWaringTable、WarningNotify 等）保持一致。
/// 文件不存在或内容为空时回退到 Inspector 中填写的 fallbackUrl。
///
/// 使用方式：
///   PLCOperationLogger.Instance?.Log(address, value, description);
///
///   - address     : PLC 地址，如 "DB1.DBX0.0"
///   - value       : 写入的值（字符串）
///   - description : 可读描述，如 "1#称 启动"（可不填）
///
/// 操作人员自动从 LoginManager.Instance.CurrentUser.name 获取。
/// 时间戳由客户端本地时间生成，与 TDengine 本地时区保持一致。
/// HTTP 请求 fire-and-forget，不阻塞主线程，失败时仅打印 Warning。
///
/// Inspector 配置：
///   fallbackUrl  : 当 ServerIP.config 不存在时使用的备用地址
///   plcId        : PLC 编号，默认 plc01
///   enableLogging: 是否开启日志（调试期可关闭）
/// </summary>
public class PLCOperationLogger : MonoBehaviour
{
    // ------------------------------------------------------------------
    // 单例
    // ------------------------------------------------------------------
    public static PLCOperationLogger Instance { get; private set; }

    // ------------------------------------------------------------------
    // Inspector 配置
    // ------------------------------------------------------------------
    [Header("服务器（地址优先从 ServerIP.config 读取）")]
    [Tooltip("StreamingAssets/ServerIP.config 不存在时的备用地址")]
    public string fallbackUrl   = "http://127.0.0.1:8000";
    public string plcId         = "plc01";
    public bool   enableLogging = true;

    // ------------------------------------------------------------------
    // 运行时服务器地址（Awake 时确定，不再变动）
    // ------------------------------------------------------------------
    public string _serverUrl;

    // ------------------------------------------------------------------
    // 单调时间戳：保证并发写入时每条日志的 ts 唯一，避免 TDengine 主键冲突
    // ------------------------------------------------------------------
    private static long _lastTimestampMs = 0;
    private static readonly object _tsLock = new object();

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
      print("Load");
        _serverUrl = LoadServerUrl();
        Debug.Log($"[PLCOperationLogger] 服务器地址: {_serverUrl}");
    }

    // ------------------------------------------------------------------
    // 公开接口
    // ------------------------------------------------------------------

    /// <summary>
    /// 记录一次 PLC 写操作。Fire-and-forget，不等待服务器响应。
    /// </summary>
    /// <param name="address">PLC 地址，如 "DB1.DBX0.0"</param>
    /// <param name="value">写入的值（字符串形式）</param>
    /// <param name="description">可读描述，如 "1#称 启动"</param>
    public void Log(string address, string value, string description = "")
    {
        if (!enableLogging) return;
        StartCoroutine(PostLog(address, value, description));
    }

    // ------------------------------------------------------------------
    // 读取 ServerIP.config
    // ------------------------------------------------------------------

    private string LoadServerUrl()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "ServerIP.config");
        try
        {
            if (File.Exists(configPath))
            {
                string raw = File.ReadAllText(configPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    // 规范化：去除末尾斜杠和多余路径，只保留 scheme://host:port
                    return NormalizeUrl(raw);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PLCOperationLogger] 读取 ServerIP.config 失败: {ex.Message}，使用备用地址");
        }

        return NormalizeUrl(fallbackUrl);
    }

    /// <summary>
    /// 规范化 URL：去掉末尾斜杠，只保留 scheme://host:port 部分。
    /// 例：http://43.136.176.84:8000/ → http://43.136.176.84:8000
    /// </summary>
    private static string NormalizeUrl(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        raw = raw.Trim().TrimEnd('/');

        // 若文件里多写了路径（如 http://host:8000/api/...）只取前三段
        try
        {
            var uri = new Uri(raw);
            return $"{uri.Scheme}://{uri.Authority}";
        }
        catch
        {
            return raw;
        }
    }

    // ------------------------------------------------------------------
    // 内部 HTTP 上传
    // ------------------------------------------------------------------

    private IEnumerator PostLog(string address, string value, string description)
    {
        // 操作人员：从 LoginManager 获取，未登录则记录"未知"
        string operatorName = "未知";
        if (LoginManager.Instance?.CurrentUser != null)
            operatorName = LoginManager.Instance.CurrentUser.name;

        // 单调时间戳：毫秒精度，同一毫秒内连续写入时自动 +1ms，确保 TDengine 主键不冲突
        string opTime = NextUniqueTimestamp();

        // 手动拼 JSON
        var sb = new StringBuilder();
        sb.Append("{");
        sb.AppendFormat("\"op_time\":\"{0}\",",      EscapeJson(opTime));
        sb.AppendFormat("\"operator_name\":\"{0}\",", EscapeJson(operatorName));
        sb.AppendFormat("\"address\":\"{0}\",",       EscapeJson(address));
        sb.AppendFormat("\"value\":\"{0}\",",         EscapeJson(value));
        sb.AppendFormat("\"description\":\"{0}\",",   EscapeJson(description));
        sb.AppendFormat("\"plc_id\":\"{0}\"",         EscapeJson(plcId));
        sb.Append("}");

        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

        using var req = new UnityWebRequest($"{_serverUrl}/api/v1/operation/log", "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[PLCOperationLogger] 日志上传失败 | {address}={value} | {req.error}");
        }
    }

    // ------------------------------------------------------------------
    // 单调时间戳生成
    // ------------------------------------------------------------------

    /// <summary>
    /// 返回本地时间字符串（毫秒精度）。
    /// 若当前毫秒 ≤ 上一次已用毫秒，则自动 +1ms，保证每次调用返回的值严格递增，
    /// 从而避免同一 ts 在 TDengine 中覆盖前一条记录。
    /// </summary>
    private static string NextUniqueTimestamp()
    {
        long nowMs;
        lock (_tsLock)
        {
            nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs <= _lastTimestampMs)
                nowMs = _lastTimestampMs + 1;
            _lastTimestampMs = nowMs;
        }
        // 转为本地时间字符串（与 TDengine 本地时区一致）
        DateTime local = DateTimeOffset.FromUnixTimeMilliseconds(nowMs).LocalDateTime;
        return local.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    // ------------------------------------------------------------------

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
    }
}
