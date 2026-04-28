using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic;
using Com.ForbiddenByte.OSA.Core;

public class PLCWarningTable : TableAdapter<TableParams, TupleViewsHolder, TupleViewsHolder>
{
    private const string IndexColumnName = "序号";
    private const string TimeColumnName = "报警时间";
    private const string NameColumnName = "报警名称";
    private const string WarningTableName = "waring_data";
    private const int PLACEHOLDER_ROWS = 30;
    private const int SQLITE_OK = 0;
    private const int SQLITE_ROW = 100;
    private const int MAX_DATA_LIMIT = 10000;

    public Button mConfirm;
    public Text mStartTime;
    public Text mEndTime;
    public HistoryDataSaver historyDataSaver;

    private IntPtr dbConnection;
    private List<WarningRecord> _cachedWarningRecords;

    private class WarningRecord
    {
        public long id;
        public string alarmTime;
        public string alarmName;
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
            Debug.LogWarning("[PLCWarningTable] HistoryDataSaver is not assigned.");
            DrawPlaceholders();
            return;
        }

        string dbPath = historyDataSaver.dbPath;
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("[PLCWarningTable] Database path is empty.");
            DrawPlaceholders();
            return;
        }

        try
        {
            int result = SQLite3.sqlite3_open(dbPath, out dbConnection);
            if (result != SQLITE_OK)
            {
                Debug.LogError($"[PLCWarningTable] Failed to open database, code: {result}");
                DrawPlaceholders();
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PLCWarningTable] Database init failed: {e.Message}");
            DrawPlaceholders();
        }
    }

    private void DrawPlaceholders()
    {
        var columns = CreateColumns();
        Columns = columns;

        var tuples = new ITuple[PLACEHOLDER_ROWS];
        for (int i = 0; i < PLACEHOLDER_ROWS; i++)
        {
            var tuple = TableViewUtil.CreateTupleWithEmptyValues<BasicTuple>(columns.ColumnsCount);
            for (int c = 0; c < columns.ColumnsCount; c++)
            {
                tuple.SetValue(c, " ");
            }

            tuples[i] = tuple;
        }

        Tuples = new BasicTableData(columns, tuples, false);
        ResetTableWithCurrentData();
    }

    public void GetMyData()
    {
        string startTime = ExtractDateTimeText(mStartTime != null ? mStartTime.text : null, false);
        string endTime = ExtractDateTimeText(mEndTime != null ? mEndTime.text : null, true);

        if (string.IsNullOrEmpty(startTime) && string.IsNullOrEmpty(endTime))
        {
            Debug.LogWarning("[PLCWarningTable] No time range specified.");
            return;
        }

        StartCoroutine(LoadFromDatabase(startTime, endTime));
    }

    private IEnumerator LoadFromDatabase(string startTime, string endTime)
    {
        _cachedWarningRecords = new List<WarningRecord>();

        string sql = $"SELECT id, alarm_time, alarm_name FROM {WarningTableName}";
        bool hasStartTime = !string.IsNullOrEmpty(startTime);
        bool hasEndTime = !string.IsNullOrEmpty(endTime);

        if (hasStartTime || hasEndTime)
        {
            sql += " WHERE 1=1";
            if (hasStartTime)
            {
                sql += " AND REPLACE(alarm_time, '/', '-') >= ?";
            }

            if (hasEndTime)
            {
                sql += " AND REPLACE(alarm_time, '/', '-') <= ?";
            }
        }

        sql += " ORDER BY alarm_time DESC LIMIT " + MAX_DATA_LIMIT;
        Debug.Log($"[PLCWarningTable] Query SQL: {sql}, start={startTime ?? "null"}, end={endTime ?? "null"}");

        IntPtr stmt = IntPtr.Zero;
        try
        {
            int prepareResult = SQLite3.sqlite3_prepare_v2(dbConnection, sql, sql.Length, out stmt, IntPtr.Zero);
            if (prepareResult != SQLITE_OK)
            {
                Debug.LogError($"[PLCWarningTable] Failed to prepare SELECT, code: {prepareResult}");
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
                _cachedWarningRecords.Add(new WarningRecord
                {
                    id = SQLite3.sqlite3_column_int64(stmt, 0),
                    alarmTime = ReadSqliteText(stmt, 1),
                    alarmName = ReadSqliteText(stmt, 2)
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PLCWarningTable] Failed to read warning data: {e.Message}");
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SQLite3.sqlite3_finalize(stmt);
            }
        }

        if (_cachedWarningRecords.Count > 0)
        {
            yield return null;
        }

        PrepareDataForDisplay();
    }

    private void PrepareDataForDisplay()
    {
        var columns = CreateColumns();
        Columns = columns;

        var tuples = new ITuple[_cachedWarningRecords.Count];
        for (int i = 0; i < _cachedWarningRecords.Count; i++)
        {
            tuples[i] = CreateTupleForRecord(i, _cachedWarningRecords[i], columns.ColumnsCount);
        }

        Tuples = new BasicTableData(columns, tuples, false);
        ResetTableWithCurrentData();
    }

    private BasicTableColumns CreateColumns()
    {
        return new BasicTableColumns(new List<BasicColumnInfo>
        {
            new BasicColumnInfo(IndexColumnName, TableValueType.STRING),
            new BasicColumnInfo(TimeColumnName, TableValueType.STRING),
            new BasicColumnInfo(NameColumnName, TableValueType.STRING)
        });
    }

    private BasicTuple CreateTupleForRecord(int index, WarningRecord record, int columnsCount)
    {
        var tuple = TableViewUtil.CreateTupleWithEmptyValues<BasicTuple>(columnsCount);
        tuple.SetValue(0, (index + 1).ToString());
        tuple.SetValue(1, record != null ? (record.alarmTime ?? string.Empty) : string.Empty);
        tuple.SetValue(2, record != null ? (record.alarmName ?? string.Empty) : string.Empty);
        return tuple;
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

        string normalizedValue = match.Value.Replace('/', '-');
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

    private void OnDestroy()
    {
        if (dbConnection != IntPtr.Zero)
        {
            SQLite3.sqlite3_close(dbConnection);
            dbConnection = IntPtr.Zero;
        }
    }
}
