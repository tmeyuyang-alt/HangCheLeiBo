using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class QueryHistoryDataDTO
{
    /// <summary>
    /// 开始时间  如2022/8/1 15:42:00
    /// </summary>
    public string startTime;
    /// <summary>
    /// 时间长度（单位为秒） 
    /// </summary>
    public long timeLength;
    /// <summary>
    /// 时间间隔（单位为秒）
    /// </summary>
    public int intervalTime;
    /// <summary>
    /// 返回结果
    /// </summary>
    public List<string> fields;
    public List<string> results;
}