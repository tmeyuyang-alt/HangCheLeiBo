using OfficeOpenXml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class DBItem
{
    public string Name { get; set; }
    public string DB { get; set; }
    
    public string DisplayName { get; set; }
    
}

public class ConfigManager : MonoBehaviour
{



    public static ConfigManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public List<DBItem> items = new List<DBItem>();

    public List<string> mTitles;
    public List<string> mDBs;

    private void Start()
    {
        mTitles=new List<string>();
        mDBs=new List<string>();
        string excelPath = Application.streamingAssetsPath + "/config.xlsx";

        FileInfo fileInfo = new FileInfo(excelPath);
        // mTitles.Add("时间");
        // mDBs.Add("ts");
        using (ExcelPackage excelPackage = new ExcelPackage(fileInfo))
        {
            ExcelWorksheet workSheet = excelPackage.Workbook.Worksheets[1];
            //print(workSheet.Dimension.End.Row);
            for (int i = 2; i < workSheet.Dimension.End.Row + 1; i++)
            {
                //print(workSheet.Cells[i,2].Value.ToString());
                if (workSheet.Cells[i,6].Value.ToString().Equals("是"))
                {
                    mTitles.Add(workSheet.Cells[i, 2].Value.ToString());
                    string tmp = workSheet.Cells[i, 3].Value.ToString();
                    string[] tmpBu=tmp.Split('.');
                    string tmp2=tmpBu[0]+"_"+tmpBu[1];
                    mDBs.Add(tmp2);
                    //print(workSheet.Cells[i, 3].Value.ToString());
                }
                items.Add(new DBItem() {Name=workSheet.Cells[i,1].Value.ToString()+workSheet.Cells[i, 2].Value.ToString(), DB= workSheet.Cells[i,3].Value.ToString() ,DisplayName = workSheet.Cells[i,7].Value.ToString()});
                
            }
           
        }
    }



}
