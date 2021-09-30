using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Data;
using LitJson;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine.UI;
using NaughtyAttributes;
using System.Linq;

public class MergeSameOrder : MonoBehaviour {
    public Text m_text;
    public Text debugText;
    
    private Dictionary<string, string> m_table = new Dictionary<string, string>();
    private DataTable dataTable = new DataTable();
    private FileStream fs;
    private string path_playerData;

    [Button]
    public void LoadExcel()
    {
        path_playerData = Application.streamingAssetsPath + "/" + m_text.text;//Excle表和位置,Application.dataPath不能在创建成员变量时使用
        ExicleToDataTable("Sheet1", true);//从exicle写入系统表中
    }

    [Button]
    public void MatchSameOrderId()
    {
        print(dataTable.Rows.Count);
        for (int i = 0; i < dataTable.Rows.Count; i++)
        {
            if (!m_table.ContainsKey(dataTable.Rows[i].ItemArray[0].ToString()))
            {
                m_table.Add(dataTable.Rows[i].ItemArray[0].ToString(), dataTable.Rows[i].ItemArray[1].ToString());
            }
            else {
                string _key = dataTable.Rows[i].ItemArray[0].ToString();
                m_table[_key] = m_table[_key] +"|" +dataTable.Rows[i].ItemArray[1].ToString();//Add(dataTable.Rows[i].ItemArray[0].ToString(), dataTable.Rows[i].ItemArray[1].ToString());
            }
        }
        WriteExcel();
        
    }


	// Use this for initialization
	void Start () {
        

    }
    void WriteExcel()
    {
        HSSFWorkbook hssfw = new HSSFWorkbook();
        ISheet sheet = hssfw.CreateSheet("Sheet1");
        sheet.SetColumnWidth(0, 512 * 10);//设置列宽
        sheet.SetColumnWidth(1, 521 * 15);
        sheet.SetColumnWidth(2, 512 * 20);
        sheet.SetColumnWidth(3, 512 * 15);

        IRow row;//行类
        ICell cell;//单元格类        

        for (int i = 0; i <= m_table.Count; i++)//用第一行来取标题，所以《=最大长度
        {
            row = sheet.CreateRow(i);
            for (int j = 0; j < 4; j++)
            {
                cell = row.CreateCell(j);
                //设置表格的样式
                ICellStyle style = hssfw.CreateCellStyle();
                style.BorderBottom = BorderStyle.Thin;
                style.BorderLeft = BorderStyle.Thin;
                style.BorderRight = BorderStyle.Thin;
                style.BorderTop = BorderStyle.Thin;
                style.Alignment = HorizontalAlignment.Left;
                cell.CellStyle = style;

                if (i == 0)//第一行取标题
                {
                    switch (j)
                    {
                        case 0:
                            cell.SetCellValue("ID");
                            break;
                        case 1:
                            cell.SetCellValue("匹配项");
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (j)
                    {
                        case 0:
                            cell.SetCellValue(m_table.ElementAt(i-1).Key);//i需要减1，保证不超出范围
                            break;
                        case 1:
                            cell.SetCellValue(m_table.ElementAt(i - 1).Value);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        if (Directory.Exists(Application.dataPath + "/StreamingAssets") == false)
        {
            Directory.CreateDirectory(Application.dataPath + "/StreamingAssets");
            print("streamingAssets创建成功");
        }

        FileStream fs = new FileStream(Application.dataPath + "/StreamingAssets/OutPut.xlsx", FileMode.OpenOrCreate);
        hssfw.Write(fs);
        //byte[] bytes = new byte[fs.Length];
        //fs.Read(bytes,0,bytes.Length);
        //print(System.Text.Encoding.UTF8.GetString(bytes));     
        fs.Close();
        fs.Dispose();
        //ExicleToDataTable("Sheet1", true);
        debugText.text = "输出成功";
    }

    /// <summary>
    /// 将Exicle数据转存入DataTable中
    /// </summary>
    /// <param name="sheelName">工作薄的名字</param>
    /// <param name="isFirstRowColumn">第一行是否是DataTable的列名</param>
    /// <returns>返回的dataTable</returns>
    DataTable ExicleToDataTable(string sheelName, bool isFirstRowColumn)
    {
        dataTable.Clear();
        ISheet sheet = null;
        int startRow = 0;//开始行

        try
        {
            fs = new FileStream(path_playerData, FileMode.Open, FileAccess.Read);

            IWorkbook workbook = null;

            if (path_playerData.IndexOf(".xlsx") > 0)//2007
            {
                print("2007版本的Exicle");
                workbook = new HSSFWorkbook(fs);
                fs.Close();
                fs.Dispose();
            }
            else if (path_playerData.IndexOf(".xls") > 0)//2003版本的
            {
                //print(path_playerData.IndexOf(".xls"));
                print("2003版本的Exicle");
                workbook = new HSSFWorkbook(fs);
                fs.Close();
                fs.Dispose();
            }


            if (sheelName != null)
            {
                sheet = workbook.GetSheet(sheelName);//找到对应的工作单赋值
                if (sheet == null)//如果该名称的工作单不存在
                {
                    sheet = workbook.GetSheetAt(0);//赋给第一张工作单
                }
            }
            else//如果该名称为空
            {
                sheet = workbook.GetSheetAt(0);//赋给第一张工作单
            }

            if (sheet != null)
            {
                IRow firstRow = sheet.GetRow(0);
                int count_cell = firstRow.LastCellNum;//一行最后一个cell的编号 即总的列数

                if (isFirstRowColumn)
                {
                    for (int i = firstRow.RowNum; i < count_cell; i++)//处理第一行的所有单元格
                    {
                        ICell cell = firstRow.GetCell(i);
                        if (cell != null)
                        {
                            string callValue = cell.StringCellValue;//到这里已经能取到表格的数据了
                            //print(callValue);//列的名字
                            if (callValue != null)//把第一行的所有标题添加进dataTable中
                            {
                                DataColumn column = new DataColumn(callValue);
                                dataTable.Columns.Add(column);
                            }
                        }
                    }
                    startRow = sheet.FirstRowNum + 1;//由于第一行是数据类型，所以数据要从第二行开始算
                }
                else//如果不需要第一行菜单栏
                {
                    startRow = sheet.FirstRowNum;
                }


                int count_row = sheet.LastRowNum;
                for (int i = startRow; i <= count_row; i++)//添加所有的表格数据
                {
                    DataRow dataRow = dataTable.NewRow();//new一个数据行

                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                    {
                        if (row.GetCell(j) != null)
                        {
                            dataRow[j] = row.GetCell(j).ToString();
                            //print(row.GetCell(j).ToString());//获取单元格的文本内容
                        }

                    }

                    dataTable.Rows.Add(dataRow);
                }
            }
            print(dataTable.Rows.Count);
            debugText.text = "加载成功：共" + dataTable.Rows.Count+"项";
            return dataTable;
        }
        catch (Exception ex)
        {
            print("捕获异常：" + ex);
            debugText.text = "捕获异常：" + ex;
            return null;
        }
    }
}
