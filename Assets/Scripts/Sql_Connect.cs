using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sql_Connect : MonoBehaviour
{
    //static string connStr = string.Format("server=127.0.0.1;user id = lipengfei; password=123456; database=test;pooling=false; charset=utf8;");
    static string connStr = string.Format("server=127.0.0.1;user id = lipengfei; password=123456; database=huanglin_dianlu;pooling=false; charset=utf8;");

    private MySqlConnection sqlConnection = null;


    private static Sql_Connect _Instance;
    public static Sql_Connect Instance
    {
        get
        {
            if (_Instance == null)
            {
                GameObject ins = new GameObject("SqlConManager");
                _Instance = ins.AddComponent<Sql_Connect>();
            }
            return _Instance;
        }
    }

    void Awake()
    {
        //sqlConnection = new MySqlConnection(connStr);
        //sqlConnection.Open();

        //if (Login("12345", "123456") != null)
        //{
        //    Debug.Log("登录成功");
        //}

        //for (int i = 0; i < 5000; i++)
        //{
        //    HistoryData history = new HistoryData();


        //    history.a1_second_curr = Random.Range(0, 5000.0f);
        //    history.b1_second_curr = Random.Range(0, 7000.0f);
        //    history.c1_second_curr = Random.Range(0, 5000.0f);

        //    history.a2_second_curr = Random.Range(0, 5000.0f);
        //    history.b2_second_curr = Random.Range(0, 8000.0f);
        //    history.c2_second_curr = Random.Range(0, 5000.0f);

        //    history.a1_ground_vol = Random.Range(0, 5000.0f);
        //    history.b1_ground_vol = Random.Range(0, 4000.0f);
        //    history.c1_ground_vol = Random.Range(0, 5000.0f);

        //    history.a2_ground_vol = Random.Range(0, 5000.0f);
        //    history.b2_ground_vol = Random.Range(0, 5000.0f);
        //    history.c2_ground_vol = Random.Range(0, 5000.0f);

        //    history.transformer_gear = Random.Range(0, 2);

        //    history.power = Random.Range(0, 5000.0f);

        //    history.primary_voltage = Random.Range(0, 5000.0f);

        //    history.active_power = Random.Range(0, 5000.0f);

        //    SetHistoryData(history);
        //}
        //GetHistoryData();
    }

    public void Command(string cmd)
    { 
    
    }
    /// <summary>
    /// 登录 返回空为登录失败
    /// </summary> 
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Account Login(string account, string password)
    {

        string table = "huanglin_dianlu.yonghu";


        string sqlstr = string.Format("SELECT * FROM {0} where account='{1}' and password ='{2}'", table, account, password);

       
        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        var reader = cmd.ExecuteReader();

        if (reader.HasRows)
        {
            while (reader.Read())
            {
                //获取账号信息
                Account acc = new Account();
                acc.account = account;
                acc.accountType = reader.GetInt32("account_type");
                reader.Close();
                return acc;
            }
        }

        reader.Close();
        //登录失败
        return null;

    }

    /// <summary>
    /// 添加账号
    /// </summary>
    public bool AddAccount(string account, string password, Account.AccountType type)
    {
        string table = "huanglin_dianlu.yonghu";

        string sqlstr = string.Format("INSERT INTO {0} (`account`, `password`,`account_type`) VALUES('{1}', '{2}','{3}')", table, account, password, (int)type);

        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        int num = cmd.ExecuteNonQuery();

        return num > 0;
    }
    /// <summary>
    /// 更新账号
    /// </summary>
    /// <param name="account"></param>
    /// <param name="password"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool UpdateAccount(string account, string password,int type)
    {
        string table = "huanglin_dianlu.yonghu";

        string sqlstr = "update {0} SET `password` = '{1}',`account_type`= '{2}' where (`account` = '{3}' )";

        sqlstr = string.Format(sqlstr, table,password, (int)type, account);

        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        int num = cmd.ExecuteNonQuery();

        return num > 0;
    }
    /// <summary>
    /// 历史数据数据
    /// </summary>
    public List<HistoryData> GetHistoryData()
    {
        List<HistoryData> datas = new List<HistoryData>();

        string table = "huanglin_dianlu.history_data";

        string sqlstr = string.Format("SELECT * FROM {0} Limit 0, 100", table);

        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            HistoryData data        = new HistoryData();

            data.create_time        = reader.GetDateTime("create_time");
            data.a1_second_curr     = reader.GetDouble("a1_second_curr");
            data.b1_second_curr     = reader.GetDouble("b1_second_curr");
            data.c1_second_curr     = reader.GetDouble("c1_second_curr");
            data.a2_second_curr     = reader.GetDouble("a2_second_curr");
            data.b2_second_curr     = reader.GetDouble("b2_second_curr");
            data.c2_second_curr     = reader.GetDouble("c2_second_curr");
            data.a1_ground_vol      = reader.GetDouble("a1_ground_vol");
            data.b1_ground_vol      = reader.GetDouble("b1_ground_vol");
            data.c1_ground_vol      = reader.GetDouble("c1_ground_vol");
            data.a2_ground_vol      = reader.GetDouble("a2_ground_vol");
            data.b2_ground_vol      = reader.GetDouble("b2_ground_vol");
            data.c2_ground_vol      = reader.GetDouble("c2_ground_vol");
            data.transformer_gear   = reader.GetInt32("transformer_gear");
            data.power              = reader.GetDouble("power");
            data.primary_voltage    = reader.GetDouble("primary_voltage");
            data.active_power       = reader.GetDouble("active_power");

            datas.Add(data);
        }
        reader.Close();
        return datas;
    }
    public bool SetHistoryData(HistoryData data)
    {
        //string table = "huanglin_dianlu.warning_data";


        string sqlstr = "INSERT INTO `huanglin_dianlu`.`history_data` (`a1_second_curr`, `b1_second_curr`, `c1_second_curr`, `a2_second_curr`, `b2_second_curr`, `c2_second_curr`, `a1_ground_vol`, `b1_ground_vol`, `c1_ground_vol`, `a2_ground_vol`, `b2_ground_vol`, `c2_ground_vol`,`transformer_gear`,`power`,`primary_voltage`,`active_power`) " +
                                                            "VALUES(@a1_second_curr, @b1_second_curr, @c1_second_curr, @a2_second_curr, @b2_second_curr, @c2_second_curr, @a1_ground_vol, @b1_ground_vol, @c1_ground_vol, @a2_ground_vol, @b2_ground_vol, @c2_ground_vol,@transformer_gear,@power,@primary_voltage,@active_power)";

 
        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        cmd.Parameters.AddRange(new MySqlParameter[] {
            new MySqlParameter("@a1_second_curr", MySqlDbType.Double) { Value = data.a1_second_curr},
            new MySqlParameter("@b1_second_curr", MySqlDbType.Double) { Value = data.b1_second_curr },
            new MySqlParameter("@c1_second_curr", MySqlDbType.Double) { Value = data.c1_second_curr },
            new MySqlParameter("@a2_second_curr", MySqlDbType.Double) { Value = data.a2_second_curr },
            new MySqlParameter("@b2_second_curr", MySqlDbType.Double) { Value = data.b2_second_curr },
            new MySqlParameter("@c2_second_curr", MySqlDbType.Double) { Value = data.c2_second_curr },
            new MySqlParameter("@a1_ground_vol", MySqlDbType.Double) { Value = data.a1_ground_vol },
            new MySqlParameter("@b1_ground_vol", MySqlDbType.Double) { Value = data.b1_ground_vol },
            new MySqlParameter("@c1_ground_vol", MySqlDbType.Double) { Value = data.c1_ground_vol },
            new MySqlParameter("@a2_ground_vol", MySqlDbType.Double) { Value = data.a2_ground_vol },
            new MySqlParameter("@b2_ground_vol", MySqlDbType.Double) { Value = data.b2_ground_vol },
            new MySqlParameter("@c2_ground_vol", MySqlDbType.Double) { Value = data.c2_ground_vol },
            new MySqlParameter("@transformer_gear", MySqlDbType.Int32) { Value = data.transformer_gear },
            new MySqlParameter("@power", MySqlDbType.Double) { Value = data.power },
            new MySqlParameter("@primary_voltage", MySqlDbType.Double) { Value = data.primary_voltage },
            new MySqlParameter("@active_power", MySqlDbType.Double) { Value = data.active_power },
        }
        );
        var num = cmd.ExecuteNonQuery();

        return num > 0;
    }

    public List<WarningData> GetWarningData()
    {
        List<WarningData> datas = new List<WarningData>();

        string table = "huanglin_dianlu.warning_data";

        string sqlstr = string.Format("SELECT * FROM {0} Limit 0, 100", table);

        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            WarningData data = new WarningData();

            data.create_time = reader.GetDateTime("create_time");
            data.alarm_name = reader.GetString("alarm_name");
            data.limit_value = reader.GetInt32("limit_value");
            data.alarm_group = reader.GetInt32("alarm_group");
            data.operator_name = reader.GetString("operator_name");
            datas.Add(data);
        }

        reader.Close();
        return datas;
    }
    /// <summary>
    /// 历史报警
    /// </summary>
    public bool SetHistoryWarning(WarningData data)
    {
        string sqlstr = "INSERT INTO `huanglin_dianlu`.`warning_data` (`bit_number`, `label`, `value`, `limit_value`, `level`, `alarm_group`, `confirm`, `operator_name`) " +
                                                                        "VALUES(@bit_number,@label, @value, @limit_value, @level, @alarm_group, @confirm, @operator_name)";
        MySqlCommand cmd = new MySqlCommand(sqlstr, sqlConnection);

        cmd.Parameters.AddRange(new MySqlParameter[] {
            new MySqlParameter("@alarm_name", MySqlDbType.String) { Value = data.alarm_name },
            new MySqlParameter("@limit_value", MySqlDbType.Double) { Value = data.limit_value },
            new MySqlParameter("@alarm_group", MySqlDbType.Int32) { Value = data.alarm_group },
            new MySqlParameter("@operator_name", MySqlDbType.String) { Value = data.operator_name } 
        }
        );
        var num = cmd.ExecuteNonQuery();

        return num > 0;
    }

    public void OnDestroy()
    {
        if (sqlConnection != null)
            sqlConnection.Close();
    }
}
