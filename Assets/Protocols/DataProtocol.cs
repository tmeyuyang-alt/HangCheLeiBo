using System;

namespace Protocols
{
    public class DataProtocol
    {
        /// <summary>
        /// 更新
        /// </summary>
        public const int UPDATE = 0;
        /// <summary>
        /// 上传
        /// </summary>
        public const int UPLOAD = 1;
        /// <summary>
        /// 删除
        /// </summary>
        public const int DELETE = 2;
        /// <summary>
        /// 查询
        /// </summary>
        public const int QUERY = 3;
        /// <summary>
        /// 搜索
        /// </summary>
        public const int SEARCH = 4;
        /// <summary>
        /// 连接PLC
        /// </summary>
        public const int PLC_CONNECT = 5;
        /// <summary>
        /// 断开PLC
        /// </summary>
        public const int PLC_DISCONNECT = 6;
        /// <summary>
        /// 获取连接状态
        /// </summary>
        public const int PLC_CONNECT_STATUS = 8;
        /// <summary>
        /// 获取PLC数据
        /// </summary>
        public const int GET_PLC_DATA = 7;
        /// <summary>
        /// 设置PLC数据
        /// </summary>
        public const int SET_PLC_DATA = 9;
        /// <summary>
        /// 更新配置文件
        /// </summary>
        public const int UPDATE_CONFIG = 10;

        /// <summary>
        /// 删除配置某一项
        /// </summary>
        public const int DEL_CONFIG_ITEM = 11;

        /// <summary>
        /// 添加一项
        /// </summary>
        public const int ADD_CONFIG_ITEM = 12;
        /// <summary>
        /// 重命名配置某一项
        /// </summary>
        public const int RENAME_CONFIG_ITEM = 13;

        /// <summary>
        /// 重命名配置某一项的数据 （可能用不上）
        /// </summary>
        public const int UPDATE_CONFIG_ITEM_DATA = 14;

        /// <summary>
        /// 查询历史警告数据
        /// </summary>
        public const int QUERY_HISOTRY_WARNING = 15;
        /// <summary>
        /// 查询历史数据
        /// </summary>
        public const int QUERY_HISOTRY_DATA = 16;


        /// <summary>
        /// 获取PLC其他配置信息
        /// </summary>
        public const int GET_OTHER_PLC_CONFIG = 17;

        /// <summary>
        /// 获取正在报警的列表
        /// </summary>
        public const int Get_WARNINGDAT_LIST = 18;

        /// <summary>
        /// 连接所有
        /// </summary>
        public const int CONNECT_ALL = 19;

        /// <summary>
        /// 获取所有连接状态
        /// </summary>
        public const int GET_CONNECT_ALL_STATUS = 20;
    }
}
