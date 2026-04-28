using System.Collections.Generic;


public class PLCParamData
{
    public string PlcType;
    public string IPAddress;
    public int rack;
    public int slot;

    public class ConfigData
    {
        public string name;
        public string type;
        public string DB;
        public int[] start;
        public int high_Limit;
    }

    public PLCParamData()
    {
       config = new List<PLCParamData.ConfigData>(); 
    }
    public List<ConfigData> config;
}
