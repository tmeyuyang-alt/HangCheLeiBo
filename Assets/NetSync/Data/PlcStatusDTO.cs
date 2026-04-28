using System;
using System.Collections.Generic;


public class PlcStatusDTO
{
    public string configName;
    public Dictionary<string, string> connectStatus = new Dictionary<string, string>();
}