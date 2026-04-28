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

public class HisWarningPanel : UIPanel
{
    private Sql_Connect connect;

    private List<WarningData> datas;


    public Text StartTime;
    public Text EndTime;
    public InputField WarningGroupInput;

    public UGuiTable table;
    public Pages pages;
    void Start()
    {
        connect = Sql_Connect.Instance;

        pages.OnPageNumChanged += (index) =>
        {
            int maxShowNum = table.Row - 1;

            int start = index * maxShowNum;

            int count = Math.Min(maxShowNum, (datas.Count - start));

            ShowData(start, count);
        };

        DataHandler.getInstance.OnQueryHisWarningDataCallback += UpdateData;

        QueryHistory();

        GameObject.FindObjectOfType<TitlePanel>().gameObject.transform.Find("Items").gameObject.SetActive(false);
    }

    //private void QueryHisDataCallback(List<WarningData> obj)
    //{
    //    foreach (var item in obj)
    //    {
    //        Debug.Log(item["create_time"]);
    //        Debug.Log(item["alarm_name"]);
    //    }
    //}

    public void QueryHistory()
    {
        QueryHistoryWarningDTO dto = new QueryHistoryWarningDTO();

        dto.startTime = StartTime.text;
        dto.endTime = EndTime.text;

        if (WarningGroupInput.text != "")
            dto.warningGroup = int.Parse(WarningGroupInput.text);
        else
            dto.warningGroup = int.MinValue;

        SocketModel model = new SocketModel();

        model.type = Protocol.Data;

        model.command = DataProtocol.QUERY_HISOTRY_WARNING;

        model.message = SerializeTool.Encode2Str(dto);

       // model.senderID = GlobalInfo.user.uid.ToString();

        model.token = GlobalInfo.user.token;

        ClientManager.getInstance.SendServer(model);
    }
    public void UpdateData(List<WarningData> obj)
    {
        datas = obj;

        //for (int i = 0; i < table.Col - 1 - datas.Count; i++)
        //{
        //    for (int j = 0; j < table.Col; j++)
        //    {
        //        table.GetItem(i + 1, j).GetComponent<Text>().text = "";
        //    }
        //}
        pages.UpdatePageNumber(table.Row - 1, obj.Count);

        //table.Clear(1);

        int maxShowNum = table.Row - 1;

        int start = pages.IndexNumber * maxShowNum;

        int count = Math.Min(maxShowNum, (datas.Count - start));

        ShowData(start, count);

    }

    public void ShowData(int start, int count)
    {
        for (int i = 0; i < count; i++)
        {
            //table.GetItem(i + 1, 0).GetComponent<Text>().text =i.ToString();

            table.GetItem(i + 1, 0).GetComponent<Text>().text = datas[start + i].create_time.ToString("yyyy/MM/dd");
            table.GetItem(i + 1, 1).GetComponent<Text>().text = datas[start + i].create_time.ToString("HH:mm:ss");
            table.GetItem(i + 1, 2).GetComponent<Text>().text = datas[start + i].alarm_name.ToString();
            table.GetItem(i + 1, 3).GetComponent<Text>().text = datas[start + i].limit_value.ToString();
            //table.GetItem(i + 1, 4).GetComponent<Text>().text = datas[i].alarm_group.ToString();
            table.GetItem(i + 1, 4).GetComponent<Text>().text = datas[start + i].operator_name.ToString();

            table.GetItem(i + 1, 0).gameObject.SetActive(true);
            table.GetItem(i + 1, 1).gameObject.SetActive(true);
            table.GetItem(i + 1, 2).gameObject.SetActive(true);
            table.GetItem(i + 1, 3).gameObject.SetActive(true);
            //table.GetItem(i + 1, 4).GetComponent<Text>().text = datas[i].alarm_group.ToString();
            table.GetItem(i + 1, 4).gameObject.SetActive(true);
        }
        //
        table.Clear(count + 1);
    }

    public void OpenConfigPanel()
    {
        UIManager.Instance.OpenPanel<HisWarningConfigPanel>(null, "Popup");
    }

    public override void OnClose()
    {
        base.OnClose();

        GameObject.FindObjectOfType<TitlePanel>().gameObject.transform.Find("Items").gameObject.SetActive(true);
        DataHandler.getInstance.OnQueryHisWarningDataCallback -= UpdateData;
    }

    public void SaveExcel()
    {
        OpenFileName ofn = new OpenFileName();

        ofn.structSize = Marshal.SizeOf(ofn);

        //ofn.filter = "All Files\0*.*\0\0";
        //ofn.filter = "Image Files(*.jpg;*.png)\0*.jpg;*.png\0";
        //ofn.filter = "Txt Files(*.txt)\0*.txt\0";

        //ofn.filter = "Word Files(*.docx)\0*.docx\0";
        //ofn.filter = "Word Files(*.doc)\0*.doc\0";
        //ofn.filter = "Word Files(*.doc:*.docx)\0*.doc:*.docx\0";

        //ofn.filter = "Excel Files(*.xls)\0*.xls\0";
        ofn.filter = "Excel Files(*.xlsx)\0*.xlsx\0";  //指定打开格式
        //ofn.filter = "Excel Files(*.xls:*.xlsx)\0*.xls:*.xlsx\0";
        //ofn.filter = "Excel Files(*.xlsx:*.xls)\0*.xlsx:*.xls\0";

        ofn.file = new string(new char[256]);

        ofn.maxFile = ofn.file.Length;

        ofn.fileTitle = new string(new char[64]);

        ofn.maxFileTitle = ofn.fileTitle.Length;

        ofn.initialDir = UnityEngine.Application.dataPath;//默认路径

        ofn.title = "打开Excel";

        ofn.defExt = "xlsx";

        //注意 一下项目不一定要全选 但是0x00000008项不要缺少
        ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR

        if (OpenFileDialog.GetSaveFileName(ofn))
        {
            ofn.file = ofn.file.Replace("\\", "/");

            FileInfo newFile = new FileInfo(ofn.file);
            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
                newFile = new FileInfo(ofn.file);
            }
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");

                //worksheet.Cells["A1"].Value = "学生信息"; //显示
                //worksheet.Cells["A1"].Style.Font.Size = 16; //字体大小
                //worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; //对其方式
                //worksheet.Cells["A1"].Style.Border.BorderAround(ExcelBorderStyle.Thin); //表格边框
                worksheet.DefaultColWidth = 25;
                worksheet.Cells["A1"].Value = "日期";
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells["B1"].Value = "时间";
                worksheet.Cells["B1"].Style.Font.Bold = true;
                worksheet.Cells["B1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells["C1"].Value = "报警名称";
                worksheet.Cells["C1"].Style.Font.Bold = true;
                worksheet.Cells["C1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells["D1"].Value = "限值";
                worksheet.Cells["D1"].Style.Font.Bold = true;
                worksheet.Cells["D1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells["E1"].Value = "操作员";
                worksheet.Cells["E1"].Style.Font.Bold = true;
                worksheet.Cells["E1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                for (int i = 0; i < datas.Count; i++)
                {
                    worksheet.Cells["A"+(i+2)].Value = datas[i].create_time.ToString("yyyy/MM/dd");
         
                    
                    worksheet.Cells["B"+(i+2)].Value = datas[i].create_time.ToString("HH:mm:ss");
       
                    
                    worksheet.Cells["C"+(i+2)].Value = datas[ i].alarm_name.ToString();
       

                    worksheet.Cells["D"+(i+2)].Value = datas[i].limit_value.ToString();
            

                    worksheet.Cells["E"+(i+2)].Value = datas[i].operator_name.ToString();

                }

                package.Save();
            }
        }
    }
}
