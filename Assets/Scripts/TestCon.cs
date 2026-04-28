using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCon : MonoBehaviour
{
    private void Start()
    {
        string connStr = string.Format("server=127.0.0.1;user id = lipengfei; password=123456; database=test;pooling=false; charset=utf8;");


        var sql = new MySqlConnection(connStr);

        sql.Open();

        //string c = string.Format("INSERT INTO `test`.`my_table` (`id`, `time`, `context`) VALUES ('{0}', '{1}', '{2}')", "123456789", "2024/10/10 00:00:00", "Unity upload");
        //MySqlCommand cmd = new MySqlCommand(c, sql);
        //cmd.ExecuteNonQuery();


        string c = string.Format("select* from test.my_table limit 1,50000");

        MySqlCommand cmd = new MySqlCommand(c, sql);

        var reader = cmd.ExecuteReader();

        System.DateTime lastTime = System.DateTime.Now;
        while (reader.Read())
        {
          string output = string.Format("{0} {1} {2}", reader.GetInt32(0), reader.GetDateTime(1), reader.GetString(2));

          //Debug.Log(output);
        }

        Debug.Log((System.DateTime.Now - lastTime).TotalMilliseconds);
    }


}
