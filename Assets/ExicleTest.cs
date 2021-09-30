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

[Serializable]
public struct PlayerData
{
    public string name_use;//用户名
    public string number_phone;//手机号
    public string number_IDCard;//身份证号码
    public string password;//用户密码
}

public class ExicleTest : MonoBehaviour
{
    public PlayerData[] playerDatas;
    public Text[] texts_input;
    public Text text_print;
    private string path_playerData;
    
    // Start is called before the first frame update
    void Start()
    {
        path_playerData = Application.streamingAssetsPath + "/PlayerData.xlsx";//Excle表和位置,Application.dataPath不能在创建成员变量时使用
        try
        {
            string path_json = Application.dataPath + "/Json";
            if (Directory.Exists(path_json) == false)
            {
                Directory.CreateDirectory(path_json);
                print("creatPath");
            }
        }
        catch (Exception ex)
        {
            print("创建文件异常:" + ex);
            //throw;
        }

        try
        {
            //print(playerDatas.Length);
            for (int i = 0; i < playerDatas.Length; i++)//将PData所填写的数据先写入Json然后用文件流写入Excle表(如果要改写 playerData现有的数据，要删除对应的json文件,增加长度则不用)，其实这里没必要，只是我多写了一步Json的读写
            {
                if (File.Exists(Application.dataPath + "/Json/user" + i + ".json") == false)
                {
                    FileStream fs = new FileStream(Application.dataPath + "/Json/user" + i + ".json", FileMode.Create);//创建json

                    byte[] bytes_write = System.Text.Encoding.GetEncoding("GB2312").GetBytes(JsonMapper.ToJson(playerDatas[i]));
                    fs.Write(bytes_write, 0, bytes_write.Length);
                    fs.Close();
                    fs.Dispose();//文件流销毁
                    
                    print("creatJson");
                }
            }
        }
        catch (Exception ex)
        {
            print("Json创建异常:" + ex);
            //throw;
        }

        WriteExicle();//写入exicle表
        ExicleToDataTable("Sheet1",true);//从exicle写入系统表中
    }


    /// /// <summary>
    /// 写入Exicle的方法，如果要重新制定表格的布局，那么就要修改结构体和写入规则
    /// </summary>
    void WriteExicle()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath+"/Json");//在Json目录下
        FileInfo[] fileInfos = directoryInfo.GetFiles("*.json",SearchOption.TopDirectoryOnly);//在指向目录的顶层目录下获取所有的json文件
        List<PlayerData> list_playerData = new List<PlayerData>(fileInfos.Length);
        for (int i = 0; i < fileInfos.Length; i++)//把每个Json文件读取出来放入数据列表list_playerData
        {
            FileStream fileS = fileInfos[i].OpenRead();
            byte[] bytes_read = new byte[fileS.Length];
            fileS.Read(bytes_read,0,bytes_read.Length);
            fileS.Close();
            fileS.Dispose();
            list_playerData.Add(JsonMapper.ToObject<PlayerData>(System.Text.Encoding.UTF8.GetString(bytes_read)));//把json解析成结构体添加进列表中
        }

        HSSFWorkbook hssfw = new HSSFWorkbook();
        ISheet sheet = hssfw.CreateSheet("Sheet1");
        sheet.SetColumnWidth(0,512*10);//设置列宽
        sheet.SetColumnWidth(1, 521 * 15);
        sheet.SetColumnWidth(2, 512 * 20);
        sheet.SetColumnWidth(3, 512 * 15);

        IRow row;//行类
        ICell cell;//单元格类        

        for (int i = 0; i <= list_playerData.Count; i++)//用第一行来取标题，所以《=最大长度
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

                if(i == 0)//第一行取标题
                {
                    switch (j)
                    {
                        case 0:
                            cell.SetCellValue("用户名");
                            break;
                        case 1:
                            cell.SetCellValue("手机号");
                            break;
                        case 2:
                            cell.SetCellValue("身份证号码");
                            break;
                        case 3:
                            cell.SetCellValue("用户密码");
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
                            cell.SetCellValue(list_playerData[i-1].name_use);//i需要减1，保证不超出范围
                            break;
                        case 1:
                            cell.SetCellValue(list_playerData[i-1].number_phone);
                            break;
                        case 2:
                            cell.SetCellValue(list_playerData[i-1].number_IDCard);
                            break;
                        case 3:
                            cell.SetCellValue(list_playerData[i-1].password);
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

        FileStream fs = new FileStream(path_playerData, FileMode.OpenOrCreate);
        hssfw.Write(fs);
        //byte[] bytes = new byte[fs.Length];
        //fs.Read(bytes,0,bytes.Length);
        //print(System.Text.Encoding.UTF8.GetString(bytes));     
        fs.Close();
        fs.Dispose();
        //ExicleToDataTable("Sheet1", true);
    }//写入exicle中

    DataTable dataTable = new DataTable();
    FileStream fs;
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
                            print(callValue);//列的名字
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
                            print(row.GetCell(j).ToString());//获取单元格的文本内容
                        }
                        
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }
            print(dataTable.Rows.Count);
            return dataTable;
        }
        catch (Exception ex)
        {
            print("捕获异常：" + ex);
            return null;
        }        
    }

    public void OnClick()
    {
        int a = int.Parse(texts_input[0].text)-1, b = int.Parse(texts_input[1].text)-1;//为了让数组和输入的数值匹配
        print(a+"," + b);
        print(dataTable.Rows.Count);
        text_print.text = dataTable.Rows[a][b].ToString();
    }//点击按钮查询Excle表中的数据

    void OnApplicationQuit()
    {
        print("结束运行");
        fs.Close();
        fs.Dispose();
        dataTable.Clear();        
    }   
}
