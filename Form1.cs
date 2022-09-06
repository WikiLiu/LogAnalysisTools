using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using CefSharp;
using CefSharp.WinForms;
using System.Threading;
using System.Web.Script.Serialization;
using System.Runtime.Remoting.Messaging;
using System.Data.SQLite;


namespace LogAnalysisTools
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser browser;
        string _filename = string.Empty;
        List<string> _curDateList = new List<string>();
        List<OutScriptCode> EntityData = new List<OutScriptCode>();
        string scriptDataFilePath = Application.StartupPath + "\\data\\data.js";

        private delegate void Delegate_LoadAnalysisData(List<OutScriptCode> data,string jsonFileName);
        private delegate void Delegate_OutMessage(string info);
        private double bsLevel = 0.0;

        public Form1()
        {
            InitializeComponent();
            searchtext.KeyUp += new KeyEventHandler(Searchtext_KeyUp);
            InitBrowser();
            //stripNolistBox.DrawMode = DrawMode.OwnerDrawFixed;
            stripNolistBox.DrawMode = DrawMode.OwnerDrawVariable;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            /* 打开窗口时不取资料
            EntityLog elog = new EntityLog();
            EntityData = elog.GetData();
            stripNolistBox.DataSource = EntityData;
            stripNolistBox.DisplayMember = "StripNo";
            stripNolistBox.ValueMember = "StripNo";
            */
        }

        public List<string> CurrentDateList {
            set { _curDateList = value; }
            get { return _curDateList; }
        }

        public static bool sortbyaccuracy = false;
        public bool zhuijia = false;

        public bool checkbutton = false;

        private void Searchtext_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Control || e.KeyCode == Keys.Enter)
            {
                QueryButton_Click(sender, null);
            }
        }
        public void InitBrowser()
        {
            if (!Cef.IsInitialized)
            {
                CefSettings settings = new CefSettings();
                settings.Locale = "zh-CN";
                //settings.CefCommandLineArgs.Add("disable-gpu", "1");
                Cef.Initialize(settings);
                //Cef.EnableHighDPISupport();
                Cef.RefreshWebPlugins();
            }
            browser = new ChromiumWebBrowser(Application.StartupPath + "\\data\\null.html");

            panel1.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// 选择日志文件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*log*)|*.log*"; //设置要选择的文件的类型
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath_text.Text = fileDialog.FileName;//返回文件的完整路径   
                _filename = fileDialog.SafeFileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string filepath = filePath_text.Text;
            if (filepath == "")
            {
                MessageBox.Show("请先选择需要分析的日志文件！", "提示信息");
                return;
            }
            EntityData = new List<OutScriptCode>();

            button2.Enabled = false;

            browser.LoadUrl(Application.StartupPath + "\\data\\wait.html");
            
            Func<List<OutScriptCode>,string, List<OutScriptCode>> Run = (i,f) =>
            {
                EntityLog readlog = new EntityLog(f);
                readlog.Analysis(); //分析log
                readlog.SaveData(); //分析值存入DB
                //取得本次log文件中的日期值
                List<StripData> cdata = readlog.CurrentAnalysisData;
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                List<string> datestr = new List<string>();
                foreach (StripData item in cdata)
                {
                    if (!datestr.Contains(item.Date))
                    {
                        datestr.Add(item.Date);
                    }
                }
                CurrentDateList = datestr;
                parameter.Add("date", "'" + string.Join("','", datestr.ToArray()) + "'");
                List< OutScriptCode > analysisRuslt = readlog.GetData(parameter);
                if (analysisRuslt == null || analysisRuslt.Count == 0)
                {
                    browser.LoadUrl(Application.StartupPath + "\\data\\nodata.html");
                }
                return analysisRuslt; //从DB取出分析结果，关联从二进制读取的内容
            };

            //异步执行
            IAsyncResult iar = Run.BeginInvoke(EntityData, filepath, CallbackWhenDone, "");
        }

        private void CallbackWhenDone(IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            Func<List<OutScriptCode>, string, List<OutScriptCode>> f = (Func<List<OutScriptCode>, string, List<OutScriptCode>>)ar.AsyncDelegate;

            Action<ListBox,Button> act = (lv,button) =>
            {
                List<OutScriptCode> sList = f.EndInvoke(iar);
                lv.DataSource = sList;
                lv.DisplayMember = "StripNo";
                lv.ValueMember = "StripNo";

                button.Enabled = true;
            };

            this.Invoke(act, stripNolistBox, button2);
        }

        private void SaveAnalysisObjectToJson(List<StripData> data, string jsonFileName)
        {
            string jsonFilePath = Application.StartupPath + "\\json_data\\"+ jsonFileName;

            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = int.MaxValue;

            string jsonstr = javaScriptSerializer.Serialize(EntityData);

            FileStream fs = new FileStream(jsonFilePath, FileMode.Create); 
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            sw.Write(jsonstr); 
            sw.Close();
            fs.Close();
        }

        private void stripNolistBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = 17;
        }

        private OutScriptCode Get_last(string stripno,string sfc, string grade)
        {
            OutScriptCode last_strip = new OutScriptCode();
            string db_path = string.Format("{0}{1}.db", Application.StartupPath, "\\data\\sqlite_db");
            string connectionString = string.Format("Data Source={0};Version=3;Pooling=true;FailIfMissing=true", db_path);

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                string sqltxt = "";

                sqltxt = @"select d.stripno,d.nameset,d.numset1,d.epsTemp,d.speed,d.delaytime1,d.targettemp1,d.targettemp2,d.targettemp3,
                                      d.thisseg,d.ValveOpenNum,d.fbkConTemp,d.New_pidCalTemp,d.Old_pidCalTemp,d.small,d.TopValveOpen,
                                      d.BotValveOpen,d.Date,d.Time,d.eps17,m.scf,m.steelGrade,m.targetThickClass,m.targetTempClass,m.fmspeedclass,
                                      m.targettemp,m.segmentno,m.actspeed,m.inheritcoeff,m.lastoutValves,m.tempindistribute 
                                   from striplogs d join stripdats m on d.stripno = m.stripNo where m.scf=1@1 and m.steelGrade='2@2' and m.rowid < (select n.rowid from stripdats n where n.stripNo='3@3') order by m.rowid desc limit 2;";


                sqltxt = sqltxt.Replace("1@1", sfc);
                sqltxt = sqltxt.Replace("2@2", grade);
                sqltxt = sqltxt.Replace("3@3", stripno);

                cmd.CommandText = sqltxt;
                SQLiteDataReader rs = cmd.ExecuteReader();
                while (rs.Read())
                {

                    last_strip.StripNo = rs["stripno"].ToString();
                    last_strip.Date = rs["Date"].ToString();
                    last_strip.Time = rs["Time"].ToString();
                    last_strip.eps17 = rs["eps17"].ToString();
                    last_strip.NameSet = rs["nameset"].ToString();
                    last_strip.NumSet1 = rs["numset1"].ToString();
                    last_strip.EpsTemp = rs["epsTemp"].ToString();
                    last_strip.Delaytime1 = rs["delaytime1"].ToString();
                    last_strip.Speed = rs["speed"].ToString();
                    last_strip.TargetTemp1 = rs["targettemp1"].ToString();
                    last_strip.TargetTemp2 = rs["targettemp2"].ToString();
                    last_strip.TargetTemp3 = rs["targettemp3"].ToString();
                    last_strip.Thisseg = rs["thisseg"].ToString();
                    last_strip.ValveOpenNum = rs["ValveOpenNum"].ToString();
                    last_strip.FbkConTemp = rs["fbkConTemp"].ToString();
                    last_strip.New_pidCalTemp = rs["New_pidCalTemp"].ToString();
                    last_strip.Old_pidCalTemp = rs["Old_pidCalTemp"].ToString();
                    last_strip.Small = rs["small"].ToString();
                    last_strip.TopValveOpen = rs["TopValveOpen"].ToString();
                    last_strip.BotValveOpen = rs["BotValveOpen"].ToString();

                    last_strip.Scf = rs["Scf"].ToString();
                    last_strip.SteelGrade = rs["SteelGrade"].ToString();
                    last_strip.TargetThickClass = rs["TargetThickClass"].ToString();
                    last_strip.TargetTempClass = rs["TargetTempClass"].ToString();
                    last_strip.FmSpeedclass = rs["FmSpeedclass"].ToString();
                    last_strip.TargetTemp = rs["TargetTemp"].ToString();
                    last_strip.Segmentno = rs["Segmentno"].ToString();
                    last_strip.ActSpeed = rs["ActSpeed"].ToString();
                    last_strip.Inheritcoeff = rs["Inheritcoeff"].ToString();
                    last_strip.LastoutValves = rs["LastoutValves"].ToString();
                    last_strip.Tempindistribute = rs["Tempindistribute"].ToString();
                    break;
                }
                rs.Close();
            }
            return last_strip;
        }


        private void stripNolistBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            object obj = stripNolistBox.Items[this.stripNolistBox.SelectedIndex];
            OutScriptCode itemdata = ((OutScriptCode)obj);
            if (!zhuijia)
            {
                FileStream fs = new FileStream(scriptDataFilePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                //log 文件数据
                sw.WriteLine("var stripNo = '" + itemdata.StripNo + "';");
                sw.WriteLine("var date = '" + itemdata.Date + "';");
                sw.WriteLine("var nameset = " + itemdata.NameSet + ";");
                sw.WriteLine("var numset1 = " + itemdata.NumSet1 + ";");
                sw.WriteLine("var epsTemp = " + itemdata.EpsTemp + ";");
                sw.WriteLine("var speed = " + itemdata.Speed + ";");
                sw.WriteLine("var delaytime1 = " + itemdata.Delaytime1 + ";");

                sw.WriteLine("var thisseg = " + itemdata.Thisseg + ";");
                sw.WriteLine("var ValveOpenNum = " + itemdata.ValveOpenNum + ";");
                sw.WriteLine("var fbkConTemp = " + itemdata.FbkConTemp + ";");
                sw.WriteLine("var New_pidCalTemp = " + itemdata.New_pidCalTemp + ";");
                sw.WriteLine("var Old_pidCalTemp = " + itemdata.Old_pidCalTemp + ";");
                sw.WriteLine("var small = " + itemdata.Small + ";");
                sw.WriteLine("var TopValveOpen = " + itemdata.TopValveOpen + ";");
                sw.WriteLine("var BotValveOpen = " + itemdata.BotValveOpen + ";");

                sw.WriteLine("var targettemp1=" + itemdata.TargetTemp1 + ";");
                sw.WriteLine("var targettemp2=" + itemdata.TargetTemp2 + ";");
                sw.WriteLine("var targettemp3=" + itemdata.TargetTemp3 + ";");

                //二进制内容
                sw.WriteLine("var scf=" + itemdata.Scf + ";");
                sw.WriteLine("var steelGrade='" + itemdata.SteelGrade + "';");
                sw.WriteLine("var targetThickClass=" + itemdata.TargetThickClass + ";");
                sw.WriteLine("var targetTempClass=" + itemdata.TargetTempClass + ";");
                sw.WriteLine("var fmSpeedClass=" + itemdata.FmSpeedclass + ";");
                sw.WriteLine("var targetTemp=" + itemdata.TargetTemp + ";");
                sw.WriteLine("var segmentNo=" + itemdata.Segmentno + ";");
                sw.WriteLine("var actSpeed=" + itemdata.ActSpeed + ";");
                sw.WriteLine("var inheritCoeff=" + itemdata.Inheritcoeff + ";");
                sw.WriteLine("var lastoutValves=" + itemdata.LastoutValves + ";");
                sw.WriteLine("var tempInDistribute=" + itemdata.Tempindistribute + ";");

                sw.Close();
                fs.Close();
            }
            if (zhuijia)
            {
                FileStream fs = new FileStream(scriptDataFilePath, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                //log 文件数据
                sw.WriteLine("var stripNo0 = '" + itemdata.StripNo + "';");
                sw.WriteLine("var date0 = '" + itemdata.Date + "';");
                sw.WriteLine("var nameset0 = " + itemdata.NameSet + ";");
                sw.WriteLine("var numset10 = " + itemdata.NumSet1 + ";");
                sw.WriteLine("var epsTemp0 = " + itemdata.EpsTemp + ";");
                sw.WriteLine("var speed0 = " + itemdata.Speed + ";");
                sw.WriteLine("var delaytime10 = " + itemdata.Delaytime1 + ";");

                sw.WriteLine("var thisseg0 = " + itemdata.Thisseg + ";");
                sw.WriteLine("var ValveOpenNum0 = " + itemdata.ValveOpenNum + ";");
                sw.WriteLine("var fbkConTemp0 = " + itemdata.FbkConTemp + ";");
                sw.WriteLine("var New_pidCalTemp0 = " + itemdata.New_pidCalTemp + ";");
                sw.WriteLine("var Old_pidCalTemp0 = " + itemdata.Old_pidCalTemp + ";");
                sw.WriteLine("var small0 = " + itemdata.Small + ";");
                sw.WriteLine("var TopValveOpen0 = " + itemdata.TopValveOpen + ";");
                sw.WriteLine("var BotValveOpen0 = " + itemdata.BotValveOpen + ";");

                sw.WriteLine("var targettemp10=" + itemdata.TargetTemp1 + ";");
                sw.WriteLine("var targettemp20=" + itemdata.TargetTemp2 + ";");
                sw.WriteLine("var targettemp30=" + itemdata.TargetTemp3 + ";");

                //二进制内容
                sw.WriteLine("var scf=0" + itemdata.Scf + ";");
                sw.WriteLine("var steelGrade0='" + itemdata.SteelGrade + "';");
                sw.WriteLine("var targetThickClass0=" + itemdata.TargetThickClass + ";");
                sw.WriteLine("var targetTempClass0=" + itemdata.TargetTempClass + ";");
                sw.WriteLine("var fmSpeedClass0=" + itemdata.FmSpeedclass + ";");
                sw.WriteLine("var targetTemp0=" + itemdata.TargetTemp + ";");
                sw.WriteLine("var segmentNo0=" + itemdata.Segmentno + ";");
                sw.WriteLine("var actSpeed0=" + itemdata.ActSpeed + ";");
                sw.WriteLine("var inheritCoeff0=" + itemdata.Inheritcoeff + ";");
                sw.WriteLine("var lastoutValves0=" + itemdata.LastoutValves + ";");
                sw.WriteLine("var tempInDistribute0=" + itemdata.Tempindistribute + ";");

                sw.Close();
                fs.Close();
                
            }
            //填入表格数据
            int length = Encoding.Default.GetByteCount(itemdata.EpsTemp);
            string[] epstemp = itemdata.EpsTemp.Remove(length - 1, 1).Remove(0, 1).Split(',');
            float[] epstemp_float = new float[epstemp.Length];

            analy.Text = "";


            for (int i = 0; i < epstemp.Length; i++)
            {
                epstemp_float[i] = Convert.ToSingle(epstemp[i]);
            }


            float count20 = 0;
            float count17 = 0;
            float count30 = 0;

            for (int i = 0; i < epstemp_float.Length; i++)
            {
                if (epstemp_float[i] <= 17 && epstemp_float[i] >= -17) { count17 = count17 + 1; }
                if (epstemp_float[i] <= 20 && epstemp_float[i] >= -20) { count20 = count20 + 1; }
                if (epstemp_float[i] <= 30 && epstemp_float[i] >= -30) { count30 = count30 + 1; }
            }
            double tempcount20 = Math.Round((count20 / epstemp_float.Length * 100), 2);
            double tempcount17 = Math.Round((count17 / epstemp_float.Length * 100), 2);
            double tempcount30 = Math.Round((count30 / epstemp_float.Length * 100), 2);



            string[,] string2 = new string[4, 6] { { "", "", "", "", "", "" }, { " 头部超", " 头部低", " 中部超", " 中部低", " 尾部超", " 尾部低" }, { " 头部略超", " 头部略低", " 中部略超", " 中部略低", " 尾部略超", " 尾部略低" }, { "头部超尖峰", " 头部低尖峰", " 中部超尖峰", " 中部低尖峰", " 尾部超尖峰", " 尾部低尖峰" } };


            int tempsta = 20;
            int headlen = 15;
            int numsta = 6;


            string string1 = "";
            int count1 = 0;
            int count0 = 0;
            float max = 0;
            float min = 0;
            for (int i = 0; i < headlen; i++)
            {

                if ((epstemp_float[i]) > tempsta) { count1 = count1 + 1; if (epstemp_float[i] > max) { max = epstemp_float[i]; } }
                if ((epstemp_float[i]) < -tempsta) { count0 = count0 + 1; if (epstemp_float[i] < min) { min = epstemp_float[i]; } }
            }
            if (count1 >= numsta) string1 = string1 + (string2[1, 0]); //正常
            if (count1 > 2 & count1 < numsta & max < 30) string1 = string1 + (string2[2, 0]); //瘦矮
            if (count1 > 2 & count1 < numsta & max >= 30) string1 = string1 + (string2[3, 0]);//瘦高

            if (count0 >= numsta) string1 = string1 + (string2[1, 1]);
            if (count0 > 2 & count0 < numsta & min > -30) string1 = string1 + (string2[2, 1]);
            if (count0 > 2 & count0 < numsta & min <= -30) string1 = string1 + (string2[3, 1]);

            count1 = 0;
            count0 = 0;
            max = 0;
            min = 0;
            for (int i = epstemp_float.Length - 2; i > epstemp_float.Length - headlen - 2; i--)
            {

                if ((epstemp_float[i]) > tempsta) { count1 = count1 + 1; if (epstemp_float[i] > max) { max = epstemp_float[i]; } }
                if ((epstemp_float[i]) < -tempsta) { count0 = count0 + 1; if (epstemp_float[i] < min) { min = epstemp_float[i]; } }

            }
            if (count1 >= numsta) string1 = string1 + (string2[1, 4]);
            if (count1 > 2 & count1 < numsta & max < 30) string1 = string1 + (string2[2, 4]);
            if (count1 > 2 & count1 < numsta & max >= 30) string1 = string1 + (string2[3, 4]);

            if (count0 >= numsta) string1 = string1 + (string2[1, 5]);
            if (count0 > 2 & count0 < numsta & min > -30) string1 = string1 + (string2[2, 5]);
            if (count0 > 2 & count0 < numsta & min <= -30) string1 = string1 + (string2[3, 5]);



            count1 = 0;
            count0 = 0;
            max = 0;
            min = 0;
            for (int i = headlen; i <= epstemp_float.Length - headlen - 2; i++)
            {
                if ((epstemp_float[i]) > tempsta) { count1 = count1 + 1; if (epstemp_float[i] > max) { max = epstemp_float[i]; } }
                if ((epstemp_float[i]) < -tempsta) { count0 = count0 + 1; if (epstemp_float[i] < min) { min = epstemp_float[i]; } }
            }
            if (count1 >= numsta) string1 = string1 + (string2[1, 2]);
            if (count1 > 2 & count1 < numsta & max < 30) string1 = string1 + (string2[2, 2]);
            if (count1 > 2 & count1 < numsta & max >= 30) string1 = string1 + (string2[3, 2]);

            if (count0 >= numsta) string1 = string1 + (string2[1, 3]);
            if (count0 > 2 & count0 < numsta & min > -30) string1 = string1 + (string2[2, 3]);
            if (count0 > 2 & count0 < numsta & min <= -30) string1 = string1 + (string2[3, 3]);

            if (string1 != "")
                string1 = string2[0, 0] + string1;



            int length1 = Encoding.Default.GetByteCount(itemdata.TopValveOpen);
            string[] fbtop = itemdata.TopValveOpen.Remove(length1 - 1, 1).Remove(0, 1).Split(',');

            float[] fb1 = new float[fbtop.Length];
            for (int i = 0; i < fbtop.Length; i++)
            {
                fb1[i] = Convert.ToSingle(fbtop[i]);
            }


            int length2 = Encoding.Default.GetByteCount(itemdata.TopValveOpen);
            string[] fbot = itemdata.TopValveOpen.Remove(length2 - 1, 1).Remove(0, 1).Split(',');

            float[] fb2 = new float[fbot.Length];
            for (int i = 0; i < fbot.Length; i++)
            {
                fb2[i] = Convert.ToSingle(fbot[i]);
            }

            float[] fb3 = new float[fbot.Length];


            for (int i = 0; i < fbot.Length; i++)
            {
                fb3[i] = fb1[i] + fb2[i];
            }
            //如果温度偏低 在偏低位置开关0
            string check = "头部低";
            string analyText = "";
            if(tempcount17<80 & string1.ToLower().Contains(check.ToLower()))
            {
                int j = 0;
                for (int i = 0; i < 15; i++)
                {
                    if (fb3[i]==0)
                    {
                        j++;
                    }
                }
                if(j>3)
                {
                    analyText=analyText+"头部反馈阀门全关";
                }

            }

            //如果温度偏高 在偏高位置全开反馈开关
            check = "头部超";
            if (tempcount17 < 80 & string1.ToLower().Contains(check.ToLower()))
            {
                int j = 0;
                
                    for (int i = 0; i < 15; i++)
                    {
                    if (fb3[i] == fb3.Max())
                    {
                        j++;
                    }
                }
                if (j > 3)
                {
                    analyText = analyText + "头部反馈阀门全开";
                }

            }


            //如果温度偏低 在偏低位置开关0
            check = "尾部低";
            if (tempcount17 < 80 & string1.ToLower().Contains(check.ToLower()))
            {
                int j = 0;
                for (int i = fbot.Length-1; i > fbot.Length - 16; i--)
                {
                    if (fb3[i] == 0)
                    {
                        j++;
                    }
                }
                if (j > 3)
                {
                    analyText = analyText + "尾部反馈阀门全关";
                }

            }

            //如果温度偏高 在偏高位置全开反馈开关
            check = "尾部超";
            if (tempcount17 < 60 & string1.ToLower().Contains(check.ToLower()))
            {
                int j = 0;
                for (int i = fbot.Length-1; i > fbot.Length - 16; i--)
                {
                    if (fb3[i] == fb3.Max())
                    {
                        j++;
                    }
                }
                if (j > 3)
                {
                    analyText = analyText + "尾部反馈阀门全开";
                }

            }

            //如果温度偏低 在偏低位置开关0
            check = "中部低";
            if (tempcount17 < 80 & string1.ToLower().Contains(check.ToLower()))
            {
                int j = 0;
                for (int i = 15; i < fbot.Length - 16; i++)
                {
                    if (fb3[i] == 0)
                    {
                        j++;
                    }
                }
                if (j > 3)
                {
                    analyText = analyText + "中部反馈阀门全关";
                }

            }

            //如果温度偏高 在偏高位置全开反馈开关
            check = "中部超";
            if (tempcount17 < 60 & string1.ToLower().Contains(check.ToLower()))
            {
                int j = 0;
                for (int i = 15; i < fbot.Length - 16; i++)
                {
                    if (fb3[i] == fb3.Max())
                    {
                        j++;
                    }
                }
                if (j > 3)
                {
                    analyText = analyText + "中部反馈阀门全开";
                }

            }
            analy.Text=analyText;
            if(analy.Text == "" | true) {

                OutScriptCode last = Get_last(itemdata.StripNo,itemdata.Scf, itemdata.SteelGrade);

                if (last.Inheritcoeff != null)
                {
                    int inheri = Encoding.Default.GetByteCount(last.Inheritcoeff);
                    string[] inheri_string = last.Inheritcoeff.Remove(inheri - 1, 1).Remove(0, 1).Split(',');
                    float[] inheri_float = new float[inheri_string.Length];
                    for (int i = 0; i < inheri_float.Length; i++)
                    {
                        inheri_float[i] = Convert.ToSingle(inheri_string[i]);
                    }


                    int lasteps = Encoding.Default.GetByteCount(last.EpsTemp);
                    string[] lasteps_string = last.EpsTemp.Remove(lasteps - 1, 1).Remove(0, 1).Split(',');
                    float[] lasteps_float = new float[lasteps_string.Length];
                    for (int i = 0; i < lasteps_float.Length; i++)
                    {
                        lasteps_float[i] = Convert.ToSingle(lasteps_string[i]);
                    }

                }

            }



            if (!zhuijia)
            {
                lb_steelgrade.Text = itemdata.SteelGrade;
                lb_date.Text = itemdata.Date;
                lb_stripno.Text = itemdata.StripNo;
                lb_sfc.Text = itemdata.Scf;
                if ((Convert.ToDecimal(itemdata.TargetTemp1) + Convert.ToDecimal(itemdata.TargetTemp2) + Convert.ToDecimal(itemdata.TargetTemp3)) == 0)
                {
                    lb_targettemp.Text = itemdata.TargetTemp;
                }
                else
                {
                    lb_targettemp.Text = itemdata.TargetTemp1 + "," + itemdata.TargetTemp2 + "," + itemdata.TargetTemp3;
                }
                lb_speedclass.Text = itemdata.FmSpeedclass;
                lb_thickclass.Text = itemdata.TargetThickClass;
                lb_tempclass.Text = itemdata.TargetTempClass;
                rank17.Text = tempcount17.ToString() + "%";
                rank20.Text = tempcount20.ToString() + "%";
                rank30.Text = tempcount30.ToString() + "%";
                fengsuostring.Text = string1;
                lb_time.Text = itemdata.Time;
            }
            if(!zhuijia)
            browser.LoadUrl(Application.StartupPath + "\\data\\chart_new.html");
            if (zhuijia)
                browser.LoadUrl(Application.StartupPath + "\\data\\chart.html");

            zhuijia = false;

        }

        private void QueryButton_Click(object sender, EventArgs e)
        {
            Dictionary<string, object> parameter = getParameter();
            if (parameter.Count() == 0)
            {
                EntityLog log = new EntityLog();
                List<string> data = log.GetDate();
                parameter.Add("date", "'"+data[0]+"'");
            }
            EntityLog elog = new EntityLog();
            EntityData = elog.GetData(parameter,sortbyaccuracy);

            stripNolistBox.DataSource = EntityData;
            stripNolistBox.DisplayMember = "StripNo";
            stripNolistBox.ValueMember = "StripNo";

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cef.Shutdown();

            //FileStream fs = new FileStream(scriptDataFilePath, FileMode.Create);
            //StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            //sw.WriteLine("var stripNoTitle = '';");
            //sw.WriteLine("var nameset = [];");
            //sw.WriteLine("var numset1 = [];");
            //sw.WriteLine("var epsTemp = [];");
            //sw.WriteLine("var speed = [];");
            //sw.WriteLine("var delaytime1 = [];");
            //sw.WriteLine("var targettemp=0;");
            //sw.Close();
            //fs.Close();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(((OutScriptCode)this.stripNolistBox.SelectedItem).StripNo);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bsLevel += 0.1;
            browser.SetZoomLevel(bsLevel);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            bsLevel += - 0.1;
            browser.SetZoomLevel(bsLevel);
        }

        private void searchtext_Enter(object sender, EventArgs e)
        {
            if (this.searchtext.Text == "=stripNo=")
            {
                this.searchtext.Text = "";
            }
        }

        private void searchtext_Leave(object sender, EventArgs e)
        {
            if (this.searchtext.Text == "")
            {
                this.searchtext.Text = "=stripNo=";
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "=steelGrade=")
            {
                this.textBox1.Text = "";
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (this.textBox1.Text == "")
            {
                this.textBox1.Text = "=steelGrade=";
            }
        }

        private void textBox2_Enter(object sender, EventArgs e)
        {
            if (this.textBox2.Text == "=sfc=")
            {
                this.textBox2.Text = "";
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (this.textBox2.Text == "")
            {
                this.textBox2.Text = "=sfc=";
            }
        }

        private void textBox3_Enter(object sender, EventArgs e)
        {

            if (this.textBox3.Text == "=ThickClass=")
            {
                this.textBox3.Text = "";
            }
        }

        private void textBox3_Leave(object sender, EventArgs e)
        {
            if (this.textBox3.Text == "")
            {
                this.textBox3.Text = "=ThickClass=";
            }
        }

        private void textBox4_Enter(object sender, EventArgs e)
        {
            if (this.textBox4.Text == "=TempClass=")
            {
                this.textBox4.Text = "";
            }
        }

        private void textBox4_Leave(object sender, EventArgs e)
        {
            if (this.textBox4.Text == "")
            {
                this.textBox4.Text = "=TempClass=";
            }
        }

        private void textBox5_Enter(object sender, EventArgs e)
        {
            if (this.textBox5.Text == "=SpeedClass=")
            {
                this.textBox5.Text = "";
            }
        }

        private void textBox5_Leave(object sender, EventArgs e)
        {
            if (this.textBox5.Text == "")
            {
                this.textBox5.Text = "=SpeedClass=";
            }
        }

        //private void textBox6_Enter(object sender, EventArgs e)
        //{
        //    if (this.textBox5.Text == "=targetTemp=")
        //    {
        //        this.textBox5.Text = "";
        //    }
        //}

        //private void textBox6_Leave(object sender, EventArgs e)
        //{
        //    if (this.textBox5.Text == "")
        //    {
        //        this.textBox5.Text = "=targetTemp=";
        //    }
        //}

        private void lb_stripno_MouseClick(object sender, MouseEventArgs e)
        {
            

            searchtext.Text = lb_stripno.Text;

            QueryButton_Click(sender, null);

        }
        private void lb_stripno_MouseDoubleClick(object sender, EventArgs e)
        {
            searchtext.Text = lb_stripno.Text;
            textBox5.Text = "=SpeedClass=";
            textBox4.Text = "=TempClass=";
            textBox1.Text = "=steelGrade=";
            textBox2.Text = "=sfc=";
            textBox7.Text = "=Date=";
            textBox3.Text = "=ThickClass=";
            textBox6.Text = "=targetTemp=";
            QueryButton_Click(sender, null);
        }



        private void lb_steelgrade_MouseClick(object sender, MouseEventArgs e)
        {
            textBox1.Text = lb_steelgrade.Text;
            QueryButton_Click(sender, null);
        }

        private void lb_steelgrade_MouseDoubleClick(object sender, EventArgs e)
        {
            textBox5.Text = "=SpeedClass=";
            textBox1.Text = lb_steelgrade.Text;
            textBox4.Text = "=TempClass=";
            searchtext.Text = "=stripNo=";
            textBox2.Text = "=sfc=";
            textBox7.Text = "=Date=";
            textBox3.Text = "=ThickClass=";
            textBox6.Text = "=targetTemp=";
            QueryButton_Click(sender, null);
        }


        private void lb_sfc_MouseClick(object sender, MouseEventArgs e)
        {
            textBox2.Text = lb_sfc.Text;
            QueryButton_Click(sender, null);
        }
        private void lb_sfc_MouseDoubelClick(object sender, EventArgs e)
        {
            textBox5.Text = "=SpeedClass=";
            textBox4.Text = "=TempClass=";
            textBox7.Text = "=Date=";
            textBox3.Text = "=ThickClass=";
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox6.Text = "=targetTemp=";
            textBox2.Text = lb_sfc.Text;
            QueryButton_Click(sender, null);
        }



        private void lb_thickclass_MouseClick(object sender, MouseEventArgs e)
        {
            textBox3.Text = lb_thickclass.Text;
            QueryButton_Click(sender, null);
        }
        private void lb_thickclass_MouseboubleClick(object sender, EventArgs e)
        {
            textBox5.Text = "=SpeedClass=";
            textBox4.Text = "=TempClass=";
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox7.Text = "=Date=";
            textBox2.Text = "=sfc=";
            textBox6.Text = "=targetTemp=";
            textBox3.Text = lb_thickclass.Text;
            QueryButton_Click(sender, null);
        }
       




        
        private void lb_tempclass_MouseClick(object sender, MouseEventArgs e)
        {
            textBox4.Text = lb_tempclass.Text;
            QueryButton_Click(sender, null);
        }
        private void lb_tempclass_MouseboubleClick(object sender, EventArgs e)
        {
            textBox5.Text = "=SpeedClass=";
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox2.Text = "=sfc=";
            textBox3.Text = "=ThickClass=";
            textBox7.Text = "=Date=";
            textBox6.Text = "=targetTemp=";
            textBox4.Text = lb_tempclass.Text;
            QueryButton_Click(sender, null);

        }




        private void lb_speedclass_MouseClick(object sender, MouseEventArgs e)
        {
            textBox5.Text = lb_speedclass.Text;
            QueryButton_Click(sender, null);
        }

        private void lb_speedclass_MousedoubleClick(object sender, EventArgs e)
        {
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox2.Text = "=sfc=";
            textBox3.Text = "=ThickClass=";
            textBox4.Text = "=TempClass=";
            textBox7.Text = "=Date=";
            textBox6.Text = "=targetTemp=";
            textBox5.Text = lb_speedclass.Text;
            QueryButton_Click(sender, null);
        }




        private void lb_targettemp_MouseClick(object sender, MouseEventArgs e)
        {
            textBox6.Text = lb_targettemp.Text;
        
        QueryButton_Click(sender, null);
        }
        private void lb_targettemp_MousedoubleClick(object sender, EventArgs e)
        {
            textBox6.Text = lb_targettemp.Text;
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox2.Text = "=sfc=";
            textBox3.Text = "=ThickClass=";
            textBox4.Text = "=TempClass=";
            textBox7.Text = "=Date=";
            textBox5.Text = "=SpeedClass=";
            QueryButton_Click(sender, null);
        }



        private void lb_date_MouseClick(object sender, MouseEventArgs e)
        {
            QueryButton_Click(sender, null);
            textBox7.Text = lb_date.Text;
        }
        private void lb_date_MousedoubleClick(object sender, EventArgs e)
        {
            textBox7.Text = lb_date.Text;
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox2.Text = "=sfc=";
            textBox3.Text = "=ThickClass=";
            textBox4.Text = "=TempClass=";
            textBox6.Text = "=targetTemp=";
            textBox5.Text = "=SpeedClass=";
            QueryButton_Click(sender, null);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FormHistory fh = new FormHistory();
            fh.Show(this);
        }

        private Dictionary<string, object> getParameter()
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();

            if (searchtext.Text.Trim() != "=stripNo=")
            {
                parameter.Add("stripno", searchtext.Text.Trim());
            }
            if (textBox1.Text.Trim() != "=steelGrade=")
            {
                parameter.Add("steelGrade", textBox1.Text.Trim());
            }
            if (textBox2.Text.Trim() != "=sfc=")
            {
                parameter.Add("scf", textBox2.Text.Trim());
            }
            if (textBox3.Text.Trim() != "=ThickClass=")
            {
                parameter.Add("targetthickclass", textBox3.Text.Trim());
            }
            if (textBox4.Text.Trim() != "=TempClass=")
            {
                parameter.Add("targettempclass", textBox4.Text.Trim());
            }
            if (textBox5.Text.Trim() != "=SpeedClass=")
            {
                parameter.Add("fmspeedclass", textBox5.Text.Trim());
            }
            if (textBox6.Text.Trim() != "=targetTemp=")
            {
                parameter.Add("targettemp", textBox6.Text.Trim());
            }
            //加入日期过滤参数
            if (textBox7.Text.Trim() == "=Date=")
            {
                if (CurrentDateList != null && CurrentDateList.Count > 0)
                {
                    parameter.Add("date", "'" + string.Join("','", CurrentDateList.ToArray()) + "'");
                }
            }
            else 
            {
                parameter.Add("date", "'" + textBox7.Text.Trim() + "'");
            }
            return parameter;
        }

        public void SeeHistoryData()
        {
            EntityLog elog = new EntityLog();
            EntityData = elog.GetData(getParameter(),sortbyaccuracy);


            stripNolistBox.DataSource = EntityData;
            //for (int i = 0; i < EntityData.Count; i++)
            //{
            //    stripNolistBox.Items.Add(EntityData[i]);

            //}
            stripNolistBox.DisplayMember = "StripNo";
            stripNolistBox.ValueMember = "StripNo";

       
      
        }

        private void updata_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(Application.StartupPath + "\\data\\readdat.exe").WaitForExit();
            browser.LoadUrl(Application.StartupPath + "\\data\\wait.html");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = Application.StartupPath + "\\data\\readdat.exe"; //"diskpart.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            browser.LoadUrl(Application.StartupPath + "\\data\\null.html");
            Dictionary<string, object> parameter = new Dictionary<string, object>();

            if (searchtext.Text.Trim() != "=stripNo=")
            {
                parameter.Add("stripno", searchtext.Text.Trim());
            }
            if (textBox1.Text.Trim() != "=steelGrade=")
            {
                parameter.Add("steelGrade", textBox1.Text.Trim());
            }
            if (textBox2.Text.Trim() != "=sfc=")
            {
                parameter.Add("scf", textBox2.Text.Trim());
            }
            if (textBox3.Text.Trim() != "=ThickClass=")
            {
                parameter.Add("targetthickclass", textBox3.Text.Trim());
            }
            if (textBox4.Text.Trim() != "=TempClass=")
            {
                parameter.Add("targettempclass", textBox4.Text.Trim());
            }
            if (textBox5.Text.Trim() != "=SpeedClass=")
            {
                parameter.Add("fmspeedclass", textBox5.Text.Trim());
            }
            if (textBox6.Text.Trim() != "=targetTemp=")
            {
                parameter.Add("targettemp", textBox6.Text.Trim());
            }
            //加入日期过滤参数
            if (textBox7.Text.Trim() == "=Date=")
            {
                if (CurrentDateList != null && CurrentDateList.Count > 0)
                {
                    parameter.Add("date", "'" + string.Join("','", CurrentDateList.ToArray()) + "'");
                }
            }
            else
            {
                parameter.Add("date", "'" + textBox7.Text.Trim() + "'");
            }
            if (parameter.Count() == 0)
            {
                EntityLog log = new EntityLog();
                List<string> data = log.GetDate();
                parameter.Add("date", "'" + data[0] + "'");
            }

            EntityLog elog = new EntityLog();
            EntityData = elog.GetData(parameter,sortbyaccuracy);

            stripNolistBox.DataSource = EntityData;
            stripNolistBox.DisplayMember = "StripNo";
            stripNolistBox.ValueMember = "StripNo";
        }

        private void clear_Click(object sender, EventArgs e)
        {
            searchtext.Text = "=stripNo=";
            textBox1.Text = "=steelGrade=";
            textBox2.Text = "=sfc=";
            textBox3.Text = "=ThickClass=";
            textBox4.Text = "=TempClass=";
            textBox5.Text = "=SpeedClass=";
            textBox6.Text = "=targetTemp=";
            textBox7.Text = "=Date=";
            Dictionary<string, object> parameter = new Dictionary<string, object>();

            if (searchtext.Text.Trim() != "=stripNo=")
            {
                parameter.Add("stripno", searchtext.Text.Trim());
            }
            if (textBox1.Text.Trim() != "=steelGrade=")
            {
                parameter.Add("steelGrade", textBox1.Text.Trim());
            }
            if (textBox2.Text.Trim() != "=sfc=")
            {
                parameter.Add("scf", textBox2.Text.Trim());
            }
            if (textBox3.Text.Trim() != "=ThickClass=")
            {
                parameter.Add("targetthickclass", textBox3.Text.Trim());
            }
            if (textBox4.Text.Trim() != "=TempClass=")
            {
                parameter.Add("targettempclass", textBox4.Text.Trim());
            }
            if (textBox5.Text.Trim() != "=SpeedClass=")
            {
                parameter.Add("fmspeedclass", textBox5.Text.Trim());
            }
            if (textBox6.Text.Trim() != "=targetTemp=")
            {
                parameter.Add("targettemp", textBox6.Text.Trim());
            }

            //加入日期过滤参数
            if (textBox7.Text.Trim() == "=Date=")
            {
                if (CurrentDateList != null && CurrentDateList.Count > 0)
                {
                    parameter.Add("date", "'" + string.Join("','", CurrentDateList.ToArray()) + "'");
                }
            }
            else
            {
                parameter.Add("date", "'" + textBox7.Text.Trim() + "'");
            }
            if (parameter.Count() == 0)
            {
                EntityLog log = new EntityLog();
                List<string> data = log.GetDate();
                parameter.Add("date", "'"+data[0]+"'");
            }
            EntityLog elog = new EntityLog();
            EntityData = elog.GetData(parameter,sortbyaccuracy);

            stripNolistBox.DataSource = EntityData;
            stripNolistBox.DisplayMember = "StripNo";
            stripNolistBox.ValueMember = "StripNo";
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel5_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void lb_date_TextChanged(object sender, EventArgs e)
        {

        }

        private void rank17_TextChanged(object sender, EventArgs e)
        {

        }
       
        //private void checkBox1_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (checkBox1.Checked)
        //    {   
        //        sortbyaccuracy = true;
        //        if (CurrentDateList.Count != 0)
        //        {
        //            QueryButton_Click(sender, e);
        //        }
        //    }
        //    if (!checkBox1.Checked)
        //    {
        //        sortbyaccuracy = false;
        //        if (CurrentDateList.Count != 0)
        //        {
        //            QueryButton_Click(sender, e);
        //        }

        //    }
        //}

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void drawLog(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1)

            {



                if((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              Color.Yellow);//Choose the color
                e.DrawBackground();
                Brush myBrush1 = Brushes.Red; //前景色
                Brush myBrush2 = Brushes.Black; //前景色
               
               
                OutScriptCode itemobj = (OutScriptCode)stripNolistBox.Items[e.Index];
                if (Convert.ToDecimal(itemobj.eps17) <= 60)
                {
                    e.Graphics.DrawString(itemobj.StripNo, e.Font, myBrush1, e.Bounds, StringFormat.GenericDefault);
                }
                else if (60 < Convert.ToDecimal(itemobj.eps17) & Convert.ToDecimal(itemobj.eps17) <= 90) {

                    e.Graphics.DrawString(itemobj.StripNo, e.Font, Brushes.Blue, e.Bounds, StringFormat.GenericDefault);

                }
                
                else
                {
                    e.Graphics.DrawString(itemobj.StripNo, e.Font, myBrush2, e.Bounds, StringFormat.GenericDefault);
                }
                e.DrawFocusRectangle();


                

                
                
            }
        }

        private void rank17_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void lb_date_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            if(CurrentDateList.Count != 0)
            zhuijia = true;
        
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
           
        }

        private void button7_Click(object sender, EventArgs e)
        {
            checkbutton = !checkbutton;
            if (checkbutton == false)
            { button7.Text = "时间排序"; sortbyaccuracy = false; }
            if (checkbutton == true)
            { button7.Text = "精度排序"; sortbyaccuracy = true; }
            if (CurrentDateList.Count != 0)
                QueryButton_Click(sender, e);
        }

        private void tableLayoutPanel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {

        }

        private void rank20_TextChanged(object sender, EventArgs e)
        {

        }

        private void fengsuostring_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
