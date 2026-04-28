using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Extra;
using LitJson;

/// <summary>
/// PLCHistoryTable: reads historical PLC data from SQLite and renders it in a table.
/// </summary>
public class PLCHistoryTable : TableAdapter<TableParams, TupleViewsHolder, TupleViewsHolder>
{
    private const string IndexColumnName = "\u5E8F\u53F7";
    private const string TimeColumnName = "\u65F6\u95F4";
    private const string HistoryDataName = "PLC\u5386\u53F2\u6570\u636E";

    public Button mConfirm;
    public Text mStartTime;
    public Text mEndTime;
    public HistoryDataSaver historyDataSaver;

    private IntPtr dbConnection;
    private List<HistoryRecord> _cachedHistoryRecords;
    private BasicTableColumns _columns;
    private List<string> _columnNames;

    private const int PLACEHOLDER_ROWS = 50;
    private const int CHUNK_SIZE = 500;
    private const int SQLITE_ROW = 100;
    private const int SQLITE_OK = 0;
    private const int MAX_DATA_LIMIT = 10000;

    private static readonly List<string> DefaultPlcColumns = new List<string>
    {
        "\u5236\u52A8\u7535\u963B\u7BB11\u5F53\u524D\u6E29\u5EA6",
        "\u5236\u52A8\u7535\u963B\u7BB12\u5F53\u524D\u6E29\u5EA6",
        "\u63D0\u5347\u53D8\u9891\u9891\u7387",
        "\u63D0\u5347\u53D8\u9891\u7535\u6D41",
        "\u6293\u6597\u53D8\u9891\u9891\u7387",
        "\u6293\u6597\u53D8\u9891\u7535\u6D41",
        "\u5C0F\u8F66\u53D8\u9891\u9891\u7387",
        "\u5C0F\u8F66\u53D8\u9891\u7535\u6D41",
        "\u5927\u8F66\u53D8\u9891\u9891\u7387",
        "\u5927\u8F66\u53D8\u9891\u7535\u6D41",
        "\u63D0\u5347\u53D8\u9891\u9891\u7387\u7ED9\u5B9A",
        "\u6293\u6597\u53D8\u9891\u9891\u7387\u7ED9\u5B9A",
        "\u5C0F\u8F66\u53D8\u9891\u9891\u7387\u7ED9\u5B9A",
        "\u5927\u8F66\u53D8\u9891\u9891\u7387\u7ED9\u5B9A",
        "\u5927\u8F66\u5F53\u524D\u4F4D\u7F6E",
        "\u5C0F\u8F66\u5F53\u524D\u4F4D\u7F6E",
        "\u6293\u6597\u5F53\u524D\u9AD8\u5EA6",
        "\u6293\u6597\u5F53\u524D\u5F00\u5EA6",
        "\u6599\u4ED3\u6599\u4F4D[1]",
        "\u6599\u4ED3\u6599\u4F4D[2]",
        "\u6599\u4ED3\u6599\u4F4D[3]",
        "\u6599\u4ED3\u6599\u4F4D[4]",
        "\u6599\u4ED3\u6599\u4F4D[5]",
        "\u6599\u4ED3\u6599\u4F4D[6]"
    };

    public class HistoryRecord
    {
        public long id;
        public string timestamp;
        public Dictionary<string, double> values;
    }

    protected override void Start()
    {
        base.Start();
        if (mConfirm != null)
        {
            mConfirm.onClick.AddListener(GetMyData);
        }

        InitializeSQLiteConnection();
        DrawPlaceholders();
    }

    private void InitializeSQLiteConnection()
    {
        if (historyDataSaver == null)
        {
            Debug.LogWarning("[PLCHistoryTable] HistoryDataSaver is not assigned.");
            DrawPlaceholders();
            return;
        }

        string dbPath = historyDataSaver.dbPath;
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("[PLCHistoryTable] Database path is empty.");
            DrawPlaceholders();
            return;
        }

        try
        {
            Debug.Log($"[PLCHistoryTable] Opening database: {dbPath}");
            int result = SQLite3.sqlite3_open(dbPath, out dbConnection);
            if (result != SQLITE_OK)
            {
                Debug.LogError($"[PLCHistoryTable] Failed to open database, code: {result}");
                DrawPlaceholders();
                return;
            }

            Debug.Log("[PLCHistoryTable] SQLite connection ready.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PLCHistoryTable] Database init failed: {e.Message}");
            DrawPlaceholders();
        }
    }

    private void DrawPlaceholders()
    {
        List<string> columns = BuildColumnsFromConfig();
        var columnInfoList = new List<BasicColumnInfo>
        {
            new BasicColumnInfo(IndexColumnName, TableValueType.STRING),
            new BasicColumnInfo(TimeColumnName, TableValueType.STRING)
        };

        foreach (string col in columns)
        {
            columnInfoList.Add(new BasicColumnInfo(col, TableValueType.STRING));
        }

        var infos = new BasicTableColumns(columnInfoList);
        Columns = infos;

        var tuples = new ITuple[PLACEHOLDER_ROWS];
        for (int i = 0; i < PLACEHOLDER_ROWS; i++)
        {
            var tuple = TableViewUtil.CreateTupleWithEmptyValues<BasicTuple>(infos.ColumnsCount);
            for (int c = 0; c < infos.ColumnsCount; c++)
            {
                tuple.SetValue(c, " ");
            }

            tuples[i] = tuple;
        }

        Tuples = new BasicTableData(infos, tuples, false);
        ResetTableWithCurrentData();
    }

    private List<string> BuildColumnsFromConfig()
    {
        List<string> columns = new List<string>();

        if (historyDataSaver != null)
        {
            var dataColumns = historyDataSaver.GetColumnNames();
            if (dataColumns != null)
            {
                foreach (string col in dataColumns)
                {
                    if (!string.IsNullOrEmpty(col) && !columns.Contains(col))
                    {
                        columns.Add(col);
                    }
                }
            }
        }

        if (columns.Count == 0)
        {
            columns.AddRange(DefaultPlcColumns);
        }

        return columns;
    }

    public void GetMyData()
    {
        string startTime = ExtractDateTimeText(mStartTime != null ? mStartTime.text : null, false);
        string endTime = ExtractDateTimeText(mEndTime != null ? mEndTime.text : null, true);

        if (string.IsNullOrEmpty(startTime) && string.IsNullOrEmpty(endTime))
        {
            Debug.LogWarning("[PLCHistoryTable] No time range specified.");
            return;
        }

        StartCoroutine(LoadFromDatabase(startTime, endTime));
    }

    private string ExtractDateTimeText(string rawText, bool isEndTime)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        string trimmed = rawText.Trim();
        Match match = Regex.Match(trimmed, "\\d{4}[-/]\\d{1,2}[-/]\\d{1,2}(?:\\s+\\d{1,2}:\\d{1,2}:\\d{1,2})?");
        if (!match.Success)
        {
            return null;
        }

        string value = match.Value;
        string normalizedValue = value.Replace('/', '-');
        string[] formats =
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-M-d H:m:s",
            "yyyy-MM-dd",
            "yyyy-M-d"
        };

        if (!DateTime.TryParseExact(normalizedValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
        {
            return null;
        }

        if (!normalizedValue.Contains(":"))
        {
            return parsed.ToString(
                isEndTime ? "yyyy-MM-dd 23:59:59" : "yyyy-MM-dd 00:00:00",
                CultureInfo.InvariantCulture
            );
        }

        return parsed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private IEnumerator LoadFromDatabase(string startTime, string endTime)
    {
        _cachedHistoryRecords = new List<HistoryRecord>();
        string sql = "SELECT id, timestamp, data_name, data_value FROM history_data";
        bool hasStartTime = !string.IsNullOrEmpty(startTime);
        bool hasEndTime = !string.IsNullOrEmpty(endTime);

        if (hasStartTime || hasEndTime)
        {
            sql += " WHERE 1=1";
            if (hasStartTime)
            {
                sql += " AND REPLACE(timestamp, '/', '-') >= ?";
            }

            if (hasEndTime)
            {
                sql += " AND REPLACE(timestamp, '/', '-') <= ?";
            }
        }

        sql += " ORDER BY timestamp DESC LIMIT " + MAX_DATA_LIMIT;
        Debug.Log($"[PLCHistoryTable] Query SQL: {sql}, start={startTime ?? "null"}, end={endTime ?? "null"}");

        IntPtr stmt = IntPtr.Zero;
        try
        {
            int prepareResult = SQLite3.sqlite3_prepare_v2(dbConnection, sql, sql.Length, out stmt, IntPtr.Zero);
            if (prepareResult != SQLITE_OK)
            {
                Debug.LogError($"[PLCHistoryTable] Failed to prepare SELECT, code: {prepareResult}");
                yield break;
            }

            int bindIndex = 1;
            if (hasStartTime)
            {
                SQLite3.sqlite3_bind_text(stmt, bindIndex++, startTime, -1, SQLite3.SQLITE_TRANSIENT);
            }

            if (hasEndTime)
            {
                SQLite3.sqlite3_bind_text(stmt, bindIndex++, endTime, -1, SQLite3.SQLITE_TRANSIENT);
            }

            while (SQLite3.sqlite3_step(stmt) == SQLITE_ROW)
            {
                var record = new HistoryRecord
                {
                    id = SQLite3.sqlite3_column_int64(stmt, 0),
                    values = new Dictionary<string, double>()
                };

                record.timestamp = ReadSqliteText(stmt, 1);
                string dataName = ReadSqliteText(stmt, 2);
                string dataValue = ReadSqliteText(stmt, 3);

                if (!string.IsNullOrEmpty(dataValue) && LooksLikeJson(dataValue))
                {
                    record.values = ParseJsonData(dataValue);
                }

                _cachedHistoryRecords.Add(record);
            }

            Debug.Log($"[PLCHistoryTable] Loaded {_cachedHistoryRecords.Count} records.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PLCHistoryTable] Failed to read history data: {e.Message}");
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }

        if (_cachedHistoryRecords.Count > 0)
        {
            yield return null;
        }

        GroupAndSortRecords();
        PrepareDataForDisplay();
    }

    private Dictionary<string, double> ParseJsonData(string jsonData)
    {
        Dictionary<string, double> result = new Dictionary<string, double>();
        if (string.IsNullOrEmpty(jsonData) || jsonData == HistoryDataName)
        {
            return result;
        }

        try
        {
            JsonData json = JsonMapper.ToObject(jsonData);
            if (!json.IsObject)
            {
                return result;
            }

            foreach (string key in json.Keys)
            {
                JsonData value = json[key];
                if (value == null)
                {
                    continue;
                }

                if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                {
                    result[key] = parsed;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PLCHistoryTable] Failed to parse JSON: {e.Message}. JSON: {jsonData}");
        }

        return result;
    }

    private bool LooksLikeJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        return trimmed.StartsWith("{") && trimmed.EndsWith("}");
    }

    private string ReadSqliteText(IntPtr stmt, int columnIndex)
    {
        IntPtr textPtr = SQLite3.sqlite3_column_text(stmt, columnIndex);
        if (textPtr == IntPtr.Zero)
        {
            return string.Empty;
        }

        int byteCount = SQLite3.sqlite3_column_bytes(stmt, columnIndex);
        if (byteCount <= 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[byteCount];
        Marshal.Copy(textPtr, buffer, 0, byteCount);
        return Encoding.UTF8.GetString(buffer);
    }

    private void GroupAndSortRecords()
    {
        if (_cachedHistoryRecords == null || _cachedHistoryRecords.Count == 0)
        {
            return;
        }

        Dictionary<string, List<HistoryRecord>> timestampGroups = new Dictionary<string, List<HistoryRecord>>();
        foreach (HistoryRecord record in _cachedHistoryRecords)
        {
            if (!timestampGroups.ContainsKey(record.timestamp))
            {
                timestampGroups[record.timestamp] = new List<HistoryRecord>();
            }

            timestampGroups[record.timestamp].Add(record);
        }

        _cachedHistoryRecords = timestampGroups.Values.SelectMany(list => list).ToList();
    }

    private void PrepareDataForDisplay()
    {
        _columnNames = BuildColumnsFromConfig();
        var columnInfoList = new List<BasicColumnInfo>
        {
            new BasicColumnInfo(IndexColumnName, TableValueType.STRING),
            new BasicColumnInfo(TimeColumnName, TableValueType.STRING)
        };

        foreach (string col in _columnNames)
        {
            columnInfoList.Add(new BasicColumnInfo(col, TableValueType.STRING));
        }

        _columns = new BasicTableColumns(columnInfoList);
        Columns = _columns;
        var tuples = new ITuple[_cachedHistoryRecords.Count];
        for (int i = 0; i < _cachedHistoryRecords.Count; i++)
        {
            tuples[i] = CreateTupleForRecord(i, _cachedHistoryRecords[i], _columns.ColumnsCount);
        }

        Tuples = new BasicTableData(_columns, tuples, false);
        ResetTableWithCurrentData();
    }

    private void ReadRecordsInto(BasicTuple[] into, int firstItemIndex, int countToRead, Action onDone)
    {
        var cols = _columns;
        for (int i = 0; i < countToRead; i++)
        {
            int globalIdx = firstItemIndex + i;
            into[i] = globalIdx < _cachedHistoryRecords.Count
                ? CreateTupleForRecord(globalIdx, _cachedHistoryRecords[globalIdx], cols.ColumnsCount)
                : TableViewUtil.CreateTupleWithEmptyValues<BasicTuple>(cols.ColumnsCount);
        }

        onDone?.Invoke();
    }

    private BasicTuple CreateTupleForRecord(int recordIndex, HistoryRecord record, int columnsCount)
    {
        var tuple = TableViewUtil.CreateTupleWithEmptyValues<BasicTuple>(columnsCount);
        tuple.SetValue(0, (recordIndex + 1).ToString());
        tuple.SetValue(1, record != null ? (record.timestamp ?? string.Empty) : string.Empty);

        for (int c = 2; c < columnsCount; c++)
        {
            string columnName = GetColumnName(c);
            string displayValue = string.Empty;
            if (record != null && record.values != null && record.values.TryGetValue(columnName, out double value))
            {
                displayValue = value.ToString(CultureInfo.InvariantCulture);
            }

            tuple.SetValue(c, displayValue);
        }

        return tuple;
    }

    private string GetColumnName(int columnIdx)
    {
        if (columnIdx == 0)
        {
            return IndexColumnName;
        }

        if (columnIdx == 1)
        {
            return TimeColumnName;
        }

        int dataColumnIndex = columnIdx - 2;
        if (_columnNames != null && dataColumnIndex >= 0 && dataColumnIndex < _columnNames.Count)
        {
            return _columnNames[dataColumnIndex];
        }

        return string.Empty;
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
