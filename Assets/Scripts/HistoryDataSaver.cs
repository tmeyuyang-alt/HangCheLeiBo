using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;

/// <summary>
/// 原生 SQLite3 函数声明
/// </summary>
internal static class SQLite3
{
    public const int SQLITE_OK = 0;
    public const int SQLITE_ROW = 100;
    public const int SQLITE_DONE = 101;
    public const int SQLITE_ERROR = 1;
    public const int SQLITE_MISUSE = 21;

    // Open flags
    public const int SQLITE_OPEN_READONLY = 0x01;
    public const int SQLITE_OPEN_READWRITE = 0x02;
    public const int SQLITE_OPEN_CREATE = 0x04;
    public const int SQLITE_OPEN_URI = 0x40;
    public const int SQLITE_OPEN_MEMORY = 0x80;
    public const int SQLITE_OPEN_NOMUTEX = 0x20000;

    public const int SQLITE_INTEGER = 1;
    public const int SQLITE_FLOAT = 2;
    public const int SQLITE_TEXT = 3;
    public const int SQLITE_BLOB = 4;
    public const int SQLITE_NULL = 5;
    public static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);

    [DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int sqlite3_open_v2(string filename, out IntPtr db, int flags, IntPtr zVfs);

    [DllImport("sqlite3", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int sqlite3_open(string filename, out IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_close(IntPtr db);

    [DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int sqlite3_prepare_v2(IntPtr db, string zSql, int nByte, out IntPtr ppStmt, IntPtr pzTail);

    [DllImport("sqlite3", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_finalize(IntPtr pStmt);

    [DllImport("sqlite3", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_step(IntPtr pStmt);

    [DllImport("sqlite3", EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_reset(IntPtr pStmt);

    [DllImport("sqlite3", EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_int(IntPtr pStmt, int index, int value);

    [DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_int64(IntPtr pStmt, int index, long value);

    [DllImport("sqlite3", EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_double(IntPtr pStmt, int index, double value);

    [DllImport("sqlite3", EntryPoint = "sqlite3_bind_text", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int sqlite3_bind_text(IntPtr pStmt, int index, string value, int n, IntPtr Destructor);

    [DllImport("sqlite3", EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_bind_null(IntPtr pStmt, int index);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_count(IntPtr pStmt);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
    public static extern IntPtr sqlite3_column_name(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_type(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_int(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
    public static extern long sqlite3_column_int64(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
    public static extern double sqlite3_column_double(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr sqlite3_column_text(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr sqlite3_column_blob(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
    public static extern int sqlite3_column_bytes(IntPtr pStmt, int i);

    [DllImport("sqlite3", EntryPoint = "sqlite3_errmsg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr sqlite3_errmsg(IntPtr db);
}

/// <summary>
/// 历史数据保存类 - 使用原生 SQLite3
/// 每分钟保存一次配置表中标记为"是"的PLC地址数据
/// 使用JSON格式存储多个点位数据到一行
/// </summary>
public class HistoryDataSaver : MonoBehaviour
{
    private const string HistoryDataName = "PLC历史数据";
    private const string WarningTableName = "waring_data";
    private const string WarningSignalPrefix = "故障信号显示";
    public PLCConfigManager plcConfigManager;
   // public static HistoryDataSaver Instance { get; private set; }

    private IntPtr dbConnection;
    public string dbPath;
    private float saveInterval = 60f;
    private float saveTimer = 0f;
    private readonly Dictionary<string, bool> warningSignalStates = new Dictionary<string, bool>();

    private void Awake()
    {
        LoadHistoryConfig();
        // if (Instance == null)
        // {
        //   
        //     Instance = this;
        //     DontDestroyOnLoad(gameObject);
        //     
        // }
        // else
        // {
        //     Destroy(gameObject);
        // }
    }

    private void Start()
    {
        InitializeDatabase();
        saveTimer = saveInterval;
    }

    private void Update()
    {
        MonitorWarningSignals();
        saveTimer += Time.deltaTime;

        if (saveTimer >= saveInterval)
        {
            saveTimer = 0f;
            SaveCurrentData();
        }
    }

    /// <summary>
    /// 初始化SQLite数据库
    /// </summary>
    private void InitializeDatabase()
    {
        string dllPath = Path.Combine(Application.dataPath, "Plugins/sqlite3.dll");
        if (!File.Exists(dllPath))
        {
            Debug.LogError("[HistoryDataSaver] SQLite DLL不存在: " + dllPath);
            return;
        }

        Debug.Log("[HistoryDataSaver] SQLite DLL找到: " + dllPath);

        string persistentPath = Application.persistentDataPath;

        if (!Directory.Exists(persistentPath))
        {
            try
            {
                Directory.CreateDirectory(persistentPath);
                Debug.Log("[HistoryDataSaver] 创建目录: " + persistentPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[HistoryDataSaver] 创建目录失败: " + e.Message);
                return;
            }
        }

        dbPath = Path.Combine(persistentPath, "history_data.db");

        try
        {
            Debug.Log("[HistoryDataSaver] 正在打开数据库: " + dbPath);
            Debug.Log("[HistoryDataSaver] persistentDataPath: " + persistentPath);
            Debug.Log("[HistoryDataSaver] 目录存在: " + Directory.Exists(persistentPath));

            IntPtr db = IntPtr.Zero;
            int result = SQLite3.sqlite3_open(dbPath, out db);

            if (result != SQLite3.SQLITE_OK)
            {
                Debug.LogWarning("[HistoryDataSaver] sqlite3_open 失败，错误码: " + result + "，尝试 sqlite3_open_v2");
                result = SQLite3.sqlite3_open_v2(dbPath, out db, 0, IntPtr.Zero);
            }

            if (result != SQLite3.SQLITE_OK)
            {
                Debug.LogError("[HistoryDataSaver] 数据库打开失败，错误码: " + result);
                Debug.LogError("[HistoryDataSaver] 可能的原因: 1)路径无效 2)DLL不兼容 3)权限问题");
                return;
            }

            dbConnection = db;
            CreateHistoryTable();
            CreateWarningTable();
            InitializeWarningSignalStates();

            Debug.Log("[HistoryDataSaver] SQLite数据库初始化成功: " + dbPath);
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 数据库初始化失败: " + e.Message + "\n" + e.StackTrace);
        }
    }

    /// <summary>
    /// 创建历史数据表
    /// 表结构：id, timestamp, data_name, data_value
    /// 每次保存一行，data_value字段存储JSON格式的所有点位数据
    /// </summary>
    private void CreateHistoryTable()
    {
        try
        {
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS history_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT NOT NULL,
                    data_name TEXT NOT NULL,
                    data_value TEXT NOT NULL
                )";

            Debug.Log("[HistoryDataSaver] 创建表 SQL:\n" + createTableSql);

            ExecuteSQL(createTableSql);

            Debug.Log("[HistoryDataSaver] 历史数据表创建/验证成功");
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 创建表失败: " + e.Message);
        }
    }

    private void CreateWarningTable()
    {
        try
        {
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS waring_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    alarm_time TEXT NOT NULL,
                    alarm_name TEXT NOT NULL
                )";

            ExecuteSQL(createTableSql);
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 创建报警表失败: " + e.Message);
        }
    }

    private void InitializeWarningSignalStates()
    {
        warningSignalStates.Clear();

        if (ConfigManager.Instance == null || ConfigManager.Instance.items == null)
        {
            return;
        }

        foreach (DBItem item in ConfigManager.Instance.items)
        {
            if (!IsWarningSignalItem(item))
            {
                continue;
            }

            warningSignalStates[item.Name] = ReadBoolValue(item.Name);
        }
    }

    private void MonitorWarningSignals()
    {
        if (dbConnection == IntPtr.Zero || plcConfigManager == null)
        {
            return;
        }

        if (ConfigManager.Instance == null || ConfigManager.Instance.items == null || ConfigManager.Instance.items.Count == 0)
        {
            return;
        }

        if (warningSignalStates.Count == 0)
        {
            InitializeWarningSignalStates();
        }

        foreach (DBItem item in ConfigManager.Instance.items)
        {
            if (!IsWarningSignalItem(item))
            {
                continue;
            }

            bool currentValue = ReadBoolValue(item.Name);
            bool lastValue = warningSignalStates.ContainsKey(item.Name) && warningSignalStates[item.Name];

            if (!lastValue && currentValue)
            {
                SaveWarningRecord(GetWarningDisplayName(item));
            }

            warningSignalStates[item.Name] = currentValue;
        }
    }

    private bool IsWarningSignalItem(DBItem item)
    {
        return item != null
            && !string.IsNullOrEmpty(item.Name)
            && !string.IsNullOrEmpty(item.DisplayName)
            && item.DisplayName.StartsWith(WarningSignalPrefix);
    }

    private bool ReadBoolValue(string key)
    {
        object rawValue = plcConfigManager.GetValue(key);
        if (rawValue is bool boolValue)
        {
            return boolValue;
        }

        if (rawValue is int intValue)
        {
            return intValue != 0;
        }

        if (rawValue is float floatValue)
        {
            return Math.Abs(floatValue) > float.Epsilon;
        }

        if (rawValue is double doubleValue)
        {
            return Math.Abs(doubleValue) > double.Epsilon;
        }

        return false;
    }

    private string GetWarningDisplayName(DBItem item)
    {
        string displayName = item.DisplayName.Trim();
        if (displayName.StartsWith(WarningSignalPrefix))
        {
            displayName = displayName.Substring(WarningSignalPrefix.Length).Trim();
        }

        return string.IsNullOrEmpty(displayName) ? item.Name : displayName;
    }

    private void SaveWarningRecord(string alarmName)
    {
        IntPtr stmt = IntPtr.Zero;
        try
        {
            string insertSql = "INSERT INTO waring_data (alarm_time, alarm_name) VALUES (?, ?)";
            int result = SQLite3.sqlite3_prepare_v2(dbConnection, insertSql, insertSql.Length, out stmt, IntPtr.Zero);
            if (result != SQLite3.SQLITE_OK)
            {
                Debug.LogError("[HistoryDataSaver] 报警记录 INSERT 准备失败，错误码: " + result);
                return;
            }

            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SQLite3.sqlite3_bind_text(stmt, 1, currentTime, -1, SQLite3.SQLITE_TRANSIENT);
            SQLite3.sqlite3_bind_text(stmt, 2, alarmName, -1, SQLite3.SQLITE_TRANSIENT);

            result = SQLite3.sqlite3_step(stmt);
            if (result != SQLite3.SQLITE_DONE)
            {
                Debug.LogError("[HistoryDataSaver] 报警记录 INSERT 执行失败，错误码: " + result);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 保存报警记录失败: " + e.Message);
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }
    }

    /// <summary>
    /// 执行SQL语句（不返回结果）
    /// </summary>
    private void ExecuteSQL(string sql)
    {
        IntPtr stmt = IntPtr.Zero;
        try
        {
            int result = SQLite3.sqlite3_prepare_v2(dbConnection, sql, sql.Length, out stmt, IntPtr.Zero);
            if (result != SQLite3.SQLITE_OK)
            {
                Debug.LogError("[HistoryDataSaver] SQL准备失败，错误码: " + result + "，SQL: " + sql);
                return;
            }

            result = SQLite3.sqlite3_step(stmt);
            if (result != SQLite3.SQLITE_DONE && result != SQLite3.SQLITE_ROW)
            {
                Debug.LogError("[HistoryDataSaver] SQL执行失败，错误码: " + result + "，SQL: " + sql);
            }
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }
    }

    /// <summary>
    /// 从ConfigManager加载需要保存历史的配置
    /// </summary>
    private void LoadHistoryConfig()
    {
        if (ConfigManager.Instance == null)
        {
            Debug.LogWarning("[HistoryDataSaver] ConfigManager未找到，等待初始化...");
            return;
        }
    }

    /// <summary>
    /// 将PLC数据打包成JSON字符串
    /// 格式：{"点位A":12.5,"点位B":1500.0,...}
    /// </summary>
    private string PackDataToJson(Dictionary<string, double> data)
    {
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");

        bool first = true;
        foreach (var kvp in data)
        {
            if (!first)
            {
                jsonBuilder.Append(",");
            }
            first = false;

            jsonBuilder.Append($"\"{kvp.Key}\":{kvp.Value}");
        }

        jsonBuilder.Append("}");
        return jsonBuilder.ToString();
    }

    /// <summary>
    /// 保存当前PLC数据到数据库（一行包含多个点位）
    /// data_value字段存储JSON格式的所有点位数据
    /// </summary>
    public void SaveCurrentData()
    {
        if (dbConnection == IntPtr.Zero)
        {
            Debug.LogWarning("[HistoryDataSaver] 数据库未连接");
            return;
        }

        if (ConfigManager.Instance == null ||
            ConfigManager.Instance.items == null ||
            ConfigManager.Instance.items.Count == 0 ||
            ConfigManager.Instance.mTitles == null ||
            ConfigManager.Instance.mTitles.Count == 0)
        {
            Debug.LogWarning("[HistoryDataSaver] ConfigManager未就绪");
            return;
        }

        IntPtr stmt = IntPtr.Zero;
        try
        {
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 收集所有点位数据
            Dictionary<string, double> dataMap = new Dictionary<string, double>();

            foreach (string title in ConfigManager.Instance.mTitles)
            {
                var item = ConfigManager.Instance.items.Find(i =>
                    !string.IsNullOrEmpty(i.DisplayName) && i.DisplayName == title);

                double value = 0.0;

                if (item != null && plcConfigManager != null)
                {
                    object rawValue = plcConfigManager.GetValue(item.Name);

                    if (rawValue != null)
                    {
                        if (rawValue is float f)
                            value = (double)f;
                        else if (rawValue is int i)
                            value = (double)i;
                        else if (rawValue is bool b)
                            value = b ? 1.0 : 0.0;
                        else if (rawValue is double d)
                            value = d;
                    }
                }

                dataMap[title] = value;
            }

            // 将数据打包成JSON
            string jsonData = PackDataToJson(dataMap);

            Debug.Log($"[HistoryDataSaver] 打包数据，共{dataMap.Count}个点位");
            Debug.Log($"[HistoryDataSaver] JSON: {jsonData}");

            // 准备INSERT语句
            string insertSql = "INSERT INTO history_data (timestamp, data_name, data_value) VALUES (?, ?, ?)";

            Debug.Log($"[HistoryDataSaver] 准备执行 SQL (长度={insertSql.Length}):\n{insertSql}");

            int result = SQLite3.sqlite3_prepare_v2(dbConnection, insertSql, insertSql.Length, out stmt, IntPtr.Zero);
            if (result != SQLite3.SQLITE_OK)
            {
                Debug.LogError("[HistoryDataSaver] INSERT准备失败，错误码: " + result);
                return;
            }

            // 绑定参数
            // 1. 时间戳
            SQLite3.sqlite3_bind_text(stmt, 1, currentTime, -1, SQLite3.SQLITE_TRANSIENT);

            // 2. 数据名称（固定值"PLC历史数据"）
            SQLite3.sqlite3_bind_text(stmt, 2, "PLC历史数据", -1, IntPtr.Zero);

            // 3. JSON数据
            SQLite3.sqlite3_bind_text(stmt, 3, jsonData, -1, SQLite3.SQLITE_TRANSIENT);
            SQLite3.sqlite3_bind_text(stmt, 2, HistoryDataName, -1, SQLite3.SQLITE_TRANSIENT);

            // 执行
            result = SQLite3.sqlite3_step(stmt);
            if (result != SQLite3.SQLITE_DONE)
            {
                Debug.LogError("[HistoryDataSaver] INSERT执行失败，错误码: " + result);
            }
            else
            {
                Debug.Log($"[HistoryDataSaver] 数据保存成功 - 时间: {currentTime}，点位数: {dataMap.Count}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 保存数据失败: " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }
    }

    /// <summary>
    /// 从数据库读取历史数据
    /// </summary>
    public List<Dictionary<string, object>> GetHistoryData(string startTime = null, string endTime = null, int limit = 1000)
    {
        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

        if (dbConnection == IntPtr.Zero)
        {
            Debug.LogWarning("[HistoryDataSaver] 数据库未连接");
            return result;
        }

        IntPtr stmt = IntPtr.Zero;
        try
        {
            string sql = "SELECT id, timestamp, data_name, data_value FROM history_data";

            if (!string.IsNullOrEmpty(startTime) || !string.IsNullOrEmpty(endTime))
            {
                sql += " WHERE 1=1";

                if (!string.IsNullOrEmpty(startTime))
                    sql += " AND timestamp >= '" + startTime + "'";

                if (!string.IsNullOrEmpty(endTime))
                    sql += " AND timestamp <= '" + endTime + "'";
            }

            sql += " ORDER BY timestamp DESC LIMIT " + limit;

            Debug.Log($"[HistoryDataSaver] 查询SQL: {sql}");

            int prepareResult = SQLite3.sqlite3_prepare_v2(dbConnection, sql, sql.Length, out stmt, IntPtr.Zero);
            if (prepareResult != SQLite3.SQLITE_OK)
            {
                Debug.LogError("[HistoryDataSaver] SELECT准备失败，错误码: " + prepareResult);
                return result;
            }

            int columnCount = SQLite3.sqlite3_column_count(stmt);
            List<string> columnNames = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                IntPtr namePtr = SQLite3.sqlite3_column_name(stmt, i);
                string name = namePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(namePtr) : "";
                columnNames.Add(name);
            }

            while (SQLite3.sqlite3_step(stmt) == SQLite3.SQLITE_ROW)
            {
                Dictionary<string, object> row = new Dictionary<string, object>();

                for (int i = 0; i < columnCount; i++)
                {
                    string columnName = columnNames[i];
                    int columnType = SQLite3.sqlite3_column_type(stmt, i);

                    object value = null;
                    switch (columnType)
                    {
                        case SQLite3.SQLITE_INTEGER:
                            value = SQLite3.sqlite3_column_int64(stmt, i);
                            break;
                        case SQLite3.SQLITE_FLOAT:
                            value = SQLite3.sqlite3_column_double(stmt, i);
                            break;
                        case SQLite3.SQLITE_TEXT:
                            IntPtr textPtr = SQLite3.sqlite3_column_text(stmt, i);
                            value = Marshal.PtrToStringAnsi(textPtr);
                            break;
                        case SQLite3.SQLITE_BLOB:
                            IntPtr blobPtr = SQLite3.sqlite3_column_blob(stmt, i);
                            int blobSize = SQLite3.sqlite3_column_bytes(stmt, i);
                            byte[] blobData = new byte[blobSize];
                            Marshal.Copy(blobPtr, blobData, 0, blobSize);
                            value = blobData;
                            break;
                        case SQLite3.SQLITE_NULL:
                            value = null;
                            break;
                    }

                    row[columnName] = value;
                }

                result.Add(row);
            }

            Debug.Log($"[HistoryDataSaver] 查询历史数据成功，返回 {result.Count} 条记录");
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 读取历史数据失败: " + e.Message);
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有列名
    /// </summary>
    public List<string> GetColumnNames()
    {
        List<string> columns = new List<string>();

        if (dbConnection == IntPtr.Zero)
        {
            return columns;
        }

        IntPtr stmt = IntPtr.Zero;
        try
        {
            string sql = "PRAGMA table_info(history_data)";

            int prepareResult = SQLite3.sqlite3_prepare_v2(dbConnection, sql, sql.Length, out stmt, IntPtr.Zero);
            if (prepareResult != SQLite3.SQLITE_OK)
            {
                Debug.LogError("[HistoryDataSaver] PRAGMA准备失败，错误码: " + prepareResult);
                return columns;
            }

            while (SQLite3.sqlite3_step(stmt) == SQLite3.SQLITE_ROW)
            {
                IntPtr namePtr = SQLite3.sqlite3_column_text(stmt, 1);
                string columnName = namePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(namePtr) : "";
                if (columnName != "id" && columnName != "timestamp" && columnName != "data_name" && columnName != "data_value")
                {
                    columns.Add(columnName);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[HistoryDataSaver] 获取列名失败: " + e.Message);
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }

        return columns;
    }

    private void OnDestroy()
    {
        if (dbConnection != IntPtr.Zero)
        {
            SQLite3.sqlite3_close(dbConnection);
            dbConnection = IntPtr.Zero;
        }
    }
}
