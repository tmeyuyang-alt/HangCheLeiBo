using Protocols;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;
using Excel;
using System.Data;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using UnityEngine.Networking;
using LitJson;
using System.Reflection;

using Newtonsoft.Json.Linq;
using System.Linq;

public class HisData
{
    public string key;
    public string data;
}







public class HistoryPanel : MonoBehaviour
{


    public string url;

    public Text StartTime;
    public Text EndTime;
   

    public UGuiTable table;
    public Pages pages;
    void Start()
    {
       
    }


    [ContextMenu("Load")]
    public void LoadConfig()
    {

        string excelPath = Application.streamingAssetsPath + "/config.xlsx";
       
        FileInfo fileInfo = new FileInfo(excelPath);
       
        using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
        {

            ExcelWorksheet workSheet = excelPackage.Workbook.Worksheets[1];

            
            List<string> dataTmp=new List<string>();
            dataTmp.Add("日期");
            dataTmp.Add("时间");
            print(workSheet.Dimension.End.Row);
            for (int i = 2; i < workSheet.Dimension.End.Row + 1; i++)
            {
                if (workSheet.Cells[i, 6].Value.ToString()=="是")
                {
                    dataTmp.Add(workSheet.Cells[i, 2].Value.ToString());
                }
            }
            table.headers = new UGuiTable.TableHeader[dataTmp.Count];
           
            for (int i = 0; i < dataTmp.Count; i++)
            {
                table.headers[i] = new UGuiTable.TableHeader(dataTmp[i].ToString(),"Label");
                //table.headers[i].name=dataTmp[i].ToString();
                //table.headers[i].type="Label";
            }
            table.Col = dataTmp.Count;



        }
    }

 

    public void UpdateData(JArray obj)
    {
   
        pages.UpdatePageNumber(table.Row - 1, obj.Count);

        //table.Clear(1);

        int maxShowNum = table.Row - 1;

        int start = pages.IndexNumber * maxShowNum;

        int count = Math.Min(maxShowNum, (obj.Count - start));

        //ShowData(start, count);

    }

    //public void ShowData(int start, int count)
    //{
    //    for (int i = 0; i < count; i++)
    //    {
    //        //table.GetItem(i + 1, 0).GetComponent<Text>().text =i.ToString();
    //        table.GetItem(i + 1, 0).GetComponent<Text>().text = datas[start + i].create_time.ToString("yyyy/MM/dd");
    //        table.GetItem(i + 1, 1).GetComponent<Text>().text = datas[start + i].create_time.ToString("HH:mm:ss");
    //        table.GetItem(i + 1, 2).GetComponent<Text>().text = datas[start + i].alarm_name.ToString();
    //        table.GetItem(i + 1, 3).GetComponent<Text>().text = datas[start + i].limit_value.ToString();
    //        //table.GetItem(i + 1, 4).GetComponent<Text>().text = datas[i].alarm_group.ToString();
    //        table.GetItem(i + 1, 4).GetComponent<Text>().text = datas[start + i].operator_name.ToString();
    //        table.GetItem(i + 1, 0).gameObject.SetActive(true);
    //        table.GetItem(i + 1, 1).gameObject.SetActive(true);
    //        table.GetItem(i + 1, 2).gameObject.SetActive(true);
    //        table.GetItem(i + 1, 3).gameObject.SetActive(true);
    //        //table.GetItem(i + 1, 4).GetComponent<Text>().text = datas[i].alarm_group.ToString();
    //        table.GetItem(i + 1, 4).gameObject.SetActive(true);
    //    }
    //    table.Clear(count + 1);
    //}
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(GetData());
        }
    }

    private IEnumerator GetFData()
    {
        using var req = UnityWebRequest.Get("http://127.0.0.1:8000/data/window?unit=hours&value=1");
        // 若你的接口需要鉴权：req.SetRequestHeader("Authorization", "Bearer ...");
        print("GET");
        yield return req.SendWebRequest();

        if (req.result is UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError($"[PlcFetcher] HTTP Error: {req.error}");
        }
        else
        {
            try
            {
                print(req.downloadHandler.text);
              //  var root = JsonMapper.ToObject<PLCD>(req.downloadHandler.text);

                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlcFetcher] JSON parse error: {ex}");
            }
        }
    }

    public void FeedData()
    {
        //for (int i = 0; i < count; i++)
        //{
        //    //table.GetItem(i + 1, 0).GetComponent<Text>().text =i.ToString();
        //    table.GetItem(i + 1, 0).GetComponent<Text>().text =
          
       
    }

    IEnumerator GetData()
    {
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }
        JObject root = JObject.Parse(req.downloadHandler.text);
        JArray rows = (JArray)root["rows"];
       // print(req.downloadHandler.text);
        if (rows.Count == 0) yield break;

        PopulateTable(rows);          // <—— 新增
       // UpdateData(rows);



        // Debug.Log($"DB2_DBD0={db1}, DB2_DBD4={db2}");
    }
    public void ShowData(JArray arg)
    {
       //
    }


    /// <summary>
    /// 根据解析后的快照数据，把内容写入 UGuiTable。<br/>
    /// headers[0] = 序号  headers[1] = 时间  headers[2…] = 配置文件里勾选为“是”的字段
    /// </summary>
    public void PopulateTable(JArray snapshots)
    {
        if (snapshots == null) return;

        // 1. 预先建立 “表头 → DB字段” 的映射，方便快速查找
        var headerToDb = ConfigManager.Instance.items
                           .ToDictionary(i => i.Name, i => i.DB);

        // 2. 先保证表格行数够用（包含表头行）
        int needRows = snapshots.Count + 1;         // +1 表头行
        while (table.Row < needRows) table.AddRowOne();

        // 3. 清空旧数据（保留表头行）
        table.Clear(1);

        // 4. 写入新数据
        for (int i = 0; i < snapshots.Count; i++)
        {
            var snap = snapshots[i];
            int rowIdx = i + 1;                     // 0 行是表头

            // -- 序号 --
            SetCell(rowIdx, 0, (i + 1).ToString());

            // -- 时间 --
            SetCell(rowIdx, 1, snap["ts"].ToString());

            // -- 其他字段 --
            for (int col = 2; col < table.headers.Length; col++)
            {
                string headerName = table.headers[col].name;

                if (!headerToDb.TryGetValue(headerName, out string dbKey)) continue;

                if (snap[dbKey] == null) continue;

                SetCell(rowIdx, col, snap[dbKey].ToString());
            }
        }
    }

    /// <summary>把值写进指定单元格，自动适配 Text / InputField</summary>
    private void SetCell(int row, int col, string value)
    {
        var cell = table.GetItem(row, col);
        if (cell == null) return;

        if (cell.TryGetComponent(out Text txt))
            txt.text = value;
        else if (cell.TryGetComponent(out InputField input))
            input.text = value;
    }

 









}
