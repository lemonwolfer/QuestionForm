using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;   //引用MySql

namespace QuestionForm
{
    public partial class MainForm : Form
    {
        int pageSize = 10;
        String connectMysql = "server=127.0.0.1;port=3306;user=root;password=lichao12; database=mysql;";
        DataRowCollection hotRows;
        int currentRowNo;
        int hitCount = 0;
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            // server=127.0.0.1/localhost 代表本机，端口号port默认是3306可以不写
            MySqlConnection conn = new MySqlConnection(connectMysql);
            conn.Open();
            //创建mysql数据库表
            CreatedMysqlTable();
            ReadFromFile();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            this.checkBox1.Visible = false;
            this.checkBox2.Visible = false;
            this.checkBox3.Visible = false;
            this.checkBox4.Visible = false;
            this.checkBox5.Visible = false;
            var sql = "SELECT * FROM questions ORDER BY RAND() LIMIT  "+pageSize;
            DataSet dataSet=MySqlHelper.ExecuteDataset(connectMysql, sql);
            currentRowNo = 0;
            hotRows = dataSet.Tables[0].Rows;
            this.button4.Visible = true;
            this.button4.Text = "下一题";
            refreshQuestion();

        }

        private void refreshQuestion()
        {
            this.textBox1.Text = hotRows[currentRowNo].ItemArray[1].ToString();
            this.textBox1.Text += Environment.NewLine;
            this.textBox1.Text += Environment.NewLine;
            this.textBox1.Text += Environment.NewLine;
            String options = hotRows[currentRowNo].ItemArray[2].ToString();
            this.textBox1.Text += options;
            this.checkBox1.Visible = true;
            this.checkBox2.Visible = true;
            this.checkBox3.Visible = true;
            this.checkBox4.Visible = true;
            this.checkBox1.CheckState = CheckState.Unchecked;
            this.checkBox2.CheckState = CheckState.Unchecked;
            this.checkBox3.CheckState = CheckState.Unchecked;
            this.checkBox4.CheckState = CheckState.Unchecked;
            if (options.IndexOf('E') > 0)
            {
                this.checkBox5.Visible = true;
            }
            else {
                this.checkBox5.Visible = false;
            }
            this.checkBox5.CheckState = CheckState.Unchecked;

        }

        private void button4_Click(object sender, EventArgs e)
        {
            currentRowNo++;
            if (currentRowNo == pageSize - 1)
            {
                checkAnswer(currentRowNo-1);
                this.button4.Text = "查看结果";
            }
            else if (currentRowNo == pageSize)
            {
                this.textBox1.Text += "本次答题完毕！";
                this.textBox1.Text += Environment.NewLine;
                this.textBox1.Text += Environment.NewLine;
                this.textBox1.Text += "一共题目" + pageSize + ",答对 " + hitCount + " 题";
                this.button4.Visible = false;
                this.checkBox1.Visible = false;
                this.checkBox2.Visible = false;
                this.checkBox3.Visible = false;
                this.checkBox4.Visible = false;
                this.checkBox5.Visible = false;
            }
            else {
                checkAnswer(currentRowNo-1);
                refreshQuestion();
            }
                
        }

        private void checkAnswer(int index)
        {
            String correctAnswers= hotRows[index].ItemArray[3].ToString();
            String filledAnswers = String.Empty;
            if (this.checkBox1.Checked)
                filledAnswers+="A";
            if (this.checkBox2.Checked)
                filledAnswers += "B";
            if (this.checkBox3.Checked)
                filledAnswers += "C";
            if (this.checkBox4.Checked)
                filledAnswers += "D";
            if (this.checkBox5.Checked)
                filledAnswers += "E";
            if (correctAnswers.Equals(filledAnswers))
                hitCount += 1;
        }

        private void ReadFromFile()
        {
            String filePath = "D:\\test.xls";
            String tablename = getTableName(filePath);
            //括号中为表格地址  
            DataSet ds = ExcelToDataSet(filePath, tablename);
            var sql = "INSERT IGNORE INTO questions(content,options,answers) VALUE(?content,?options,?answers)";
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                string content = ds.Tables[tablename].Rows[i]["试题内容"].ToString();  //Rows[i]["col1"]表示i行"col1"字段  
                string options = ds.Tables[tablename].Rows[i]["试题选项"].ToString();
                string answers = ds.Tables[tablename].Rows[i]["答案"].ToString();
                int answers_len = answers.Length;
                string pattern = @"[(（]\s*[ABCDE]+\s*[）)]";
                foreach (Match match in Regex.Matches(content, pattern))
                {
                    // Execute the query  
                    foreach (char c in match.Value)
                    {
                        if (!"ABCDE".Contains(c))
                            continue;
                        content = content.Replace(c, ' ');
                        if (answers_len < 1)
                        {
                            answers += c;
                        }
                    }
                    
                }
                MySqlHelper.ExecuteNonQuery(connectMysql, sql, new MySqlParameter("content", content), 
                     new MySqlParameter("options", options),
                     new MySqlParameter("answers", answers));
            }
            int a = 1;
        }

        private bool CreatedMysqlTable()
        {
            #region 创建mysql数据库表

            using (var Conn = new MySqlConnection(connectMysql))
            {
                Conn.Open();

                String createStatement = @" CREATE TABLE `questions` (                  `Id` int(4) NOT NULL AUTO_INCREMENT,                  `content` varchar(255) COLLATE utf8_bin DEFAULT NULL,                  `options` varchar(255) COLLATE utf8_bin DEFAULT NULL,                  `answers` varchar(10) COLLATE utf8_bin DEFAULT NULL,                  PRIMARY KEY (`Id`)                ) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin ";
                using (MySqlCommand cmd = new MySqlCommand(createStatement, Conn))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
                #endregion
            }
        }

        //函数用来读取一个excel文件到DataSet集中  
        public static DataSet ExcelToDataSet(string filename, string tableName)
        {
            OleDbConnection myConn = getConn(filename);
            myConn.Open();
            tableName = getTableName(filename);
            string strCom = " SELECT * FROM [" + tableName + "]";
            //获取Excel指定Sheet表中的信息
            OleDbDataAdapter myCommand = new OleDbDataAdapter(strCom, myConn);
            DataSet ds;
            ds = new DataSet();

            myCommand.Fill(ds, tableName);
            myConn.Close();
            return ds;
        }

        private static OleDbConnection getConn(String filename)
        {
            //获取文件扩展名
            string strExtension = System.IO.Path.GetExtension(filename);
            OleDbConnection myConn = null;
            switch (strExtension)
            {
                case ".xls":
                    myConn = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filename + ";" + "Extended Properties=\"Excel 8.0;HDR=yes;IMEX=1;\"");
                    break;
                case ".xlsx":
                    myConn = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filename + ";" + "Extended Properties=\"Excel 12.0;HDR=yes;IMEX=1;\"");
                    //此连接可以操作.xls与.xlsx文件 (支持Excel2003 和 Excel2007 的连接字符串) 
                    //"HDR=yes;"是说Excel文件的第一行是列名而不是数，"HDR=No;"正好与前面的相反。"IMEX=1 "如果列中的数据类型不一致，使用"IMEX=1"可必免数据类型冲突。 
                    break;
                default:
                    myConn = null;
                    break;
            }
            if (myConn == null)
            {
                return null;
            }
            return myConn;
        }

        private static string getTableName(String fileName)
        {
            OleDbConnection myConn = getConn(fileName);
            //读取文件
            DataTable dataTab = new DataTable();
            myConn.Open();
            //获取表名的一种方式
            dataTab = myConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });//获得表头
            String tableName = dataTab.Rows[0]["Table_Name"].ToString();
            myConn.Close();
            return tableName;
        }

        
    }
}
