using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class QueryHistoryWarningDTO
{
    public string startTime;
    public string endTime;
    public int warningGroup;
    public long timeInterval;

    public List<WarningData> results;
}
