using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Data.SQLite;

namespace LogAnalysisTools
{
    public class EntityLog
    {
        string _filePath = string.Empty;
        string _errorMsg = string.Empty;
        private string root = null;
        private List<StripData> _data = new List<StripData>();
        string db_path = "";
        string connectionString = "";

        public EntityLog()
        {
            db_path = string.Format("{0}{1}.db", Application.StartupPath, "\\data\\sqlite_db");
            connectionString = string.Format("Data Source={0};Version=3;Pooling=true;FailIfMissing=true", db_path);
        }
        public EntityLog(string filepath)
        {
            db_path = string.Format("{0}{1}.db", Application.StartupPath, "\\data\\sqlite_db");
            connectionString = string.Format("Data Source={0};Version=3;Pooling=true;FailIfMissing=true", db_path);
            _filePath = filepath;
        }

        public List<StripData> CurrentAnalysisData
        {
            get { return _data; }
        }
        public void Analysis()
        {
            //string begenTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string patternStripNo = @"curStrip_stripNo=\d+";

            string stripNo = string.Empty;
            Dictionary<string, object> colVals = new Dictionary<string, object>();
            StripData mData = new StripData();

            SegNoData segNoData = new SegNoData();
            List<SegNoData> listSegNoData = new List<SegNoData>();

            StreamReader sr = new StreamReader(_filePath, Encoding.Default);
            while (true)
            {
                string row = sr.ReadLine();
                if (row == null)
                {
                    break;
                }
                else
                {
                    string[] vals = row.Split('|');
                    Match m = Regex.Match(row, patternStripNo);
                    if (m.Success)
                    {
                        stripNo = Regex.Match(m.Groups[0].Value, @"\d+").Groups[0].Value;

                        if (!string.IsNullOrEmpty(mData.StripNo) && mData.StripNo != stripNo)
                        {
                            mData.Items = listSegNoData;
                            _data.Add(mData);
                            mData = new StripData();
                            listSegNoData = new List<SegNoData>();
                        }

                        segNoData = new SegNoData();

                        mData.StripNo = stripNo;
                        mData.Date = vals[0].Substring(0, 10);
                        mData.Time = vals[0].Substring(11, 8);

                        Match segno_match = Regex.Match(row, @".segNo=(\d+)");
                        if (segno_match.Success)
                        {
                            segNoData.segNo = Convert.ToDecimal(Regex.Match(segno_match.Groups[0].Value, @"\d+").Groups[0].Value);
                        }
                    }
                    else if (!string.IsNullOrEmpty(stripNo))
                    {
                        Match coiltemp_match = Regex.Match(row, @"  cmpCoilTemp=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (coiltemp_match.Success)
                        {
                            segNoData.cmpCoilTemp = Convert.ToDecimal(Regex.Match(coiltemp_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                        }
                        Match epstemp_match = Regex.Match(row, @", epsTemp=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (epstemp_match.Success)
                        {
                            segNoData.epsTemp = Convert.ToDecimal(Regex.Match(epstemp_match.Groups[0].Value, @"-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?").Groups[0].Value);
                        }

                        Match temptarget3_match = Regex.Match(row, @"TempTarget: Head, mid, tail=(.+),Target LastSeg=");
                        if (temptarget3_match.Success)
                        {
                            string f = temptarget3_match.Value.Substring(28, 11);
                            string[] garray = Regex.Split(f, ",", RegexOptions.IgnoreCase);
                            segNoData.targetTemp1 = Convert.ToDecimal(garray[0]);
                            segNoData.targetTemp2 = Convert.ToDecimal(garray[1]);
                            segNoData.targetTemp3 = Convert.ToDecimal(garray[2]);
                        }

                        Match thisseg_match = Regex.Match(row, @"thisSeg=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (thisseg_match.Success)
                        {
                            segNoData.thisseg = Convert.ToDecimal(Regex.Match(thisseg_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                        }

                        Match ValveOpenNum_match = Regex.Match(row, @"ValveOpenNum=(\d+)");
                        if (ValveOpenNum_match.Success)
                        {
                            segNoData.ValveOpenNum = Convert.ToDecimal(Regex.Match(ValveOpenNum_match.Groups[0].Value, @"\d+").Groups[0].Value);
                        }

                        Match TopValveOpen_match = Regex.Match(row, @"TopValveOpen =(\d+)");
                        if (TopValveOpen_match.Success)
                        {
                            segNoData.TopValveOpen = Convert.ToDecimal(Regex.Match(TopValveOpen_match.Groups[0].Value, @"\d+").Groups[0].Value);
                        }
                        Match BotValveOpen_match = Regex.Match(row, @"BotValveOpen=(\d+)");
                        if (BotValveOpen_match.Success)
                        {
                            segNoData.BotValveOpen = Convert.ToDecimal(Regex.Match(BotValveOpen_match.Groups[0].Value, @"\d+").Groups[0].Value);
                        }

                        Match fbkConTemp_match = Regex.Match(row, @"fbkConTemp\(af\)=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (fbkConTemp_match.Success)
                        {
                            segNoData.fbkConTemp = Convert.ToDecimal(Regex.Match(fbkConTemp_match.Groups[0].Value, @"-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?").Groups[0].Value);
                        }
                        Match New_pidCalTemp_match = Regex.Match(row, @"New_pidCalTemp=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (New_pidCalTemp_match.Success)
                        {
                            segNoData.small = Convert.ToDecimal(segNoData.segNo);
                            segNoData.New_pidCalTemp = Convert.ToDecimal(Regex.Match(New_pidCalTemp_match.Groups[0].Value, @"-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?").Groups[0].Value);
                        }

                        Match Old_pidCalTemp_match = Regex.Match(row, @"Old_pidCalTemp=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (Old_pidCalTemp_match.Success)
                        {
                            segNoData.Old_pidCalTemp = Convert.ToDecimal(Regex.Match(Old_pidCalTemp_match.Groups[0].Value, @"-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?").Groups[0].Value);
                        }

                        Match speed_match = Regex.Match(row, @"speed=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (speed_match.Success)
                        {
                            segNoData.speed = Convert.ToDecimal(Regex.Match(speed_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                        }

                        Match delaytime_match = Regex.Match(row, @"DelayTime=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (delaytime_match.Success)
                        {
                            segNoData.delaytime = Convert.ToDecimal(Regex.Match(delaytime_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                        }

                        if (row.IndexOf("_calcCmpBuffer.fbkConTemp(af)=") > 0)
                        {

                            listSegNoData.Add(segNoData);
                            stripNo = string.Empty;
                        }

                    }
                }
            }
            sr.Close();

            //string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            //MessageBox.Show("分析完成！","提示");
        }

        public void SaveData()
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                SQLiteTransaction tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    //先删除之前的数据
                    //cmd.CommandText = "delete from striplogs";
                    //cmd.ExecuteNonQuery();
                    string strsql = "";
                    foreach (StripData m in _data)
                    {
                        List<decimal> segnoList = new List<decimal>();
                        List<decimal> cmpCoilTempList = new List<decimal>();
                        List<decimal> epsTempList = new List<decimal>();
                        List<decimal> speedList = new List<decimal>();
                        List<decimal> delaytimeList = new List<decimal>();

                        List<decimal> thisseglist = new List<decimal>();
                        List<decimal> ValveOpenNumlist = new List<decimal>();
                        List<decimal> fbkConTemplist = new List<decimal>();
                        List<decimal> New_pidCalTemplist = new List<decimal>();
                        List<decimal> Old_pidCalTemplist = new List<decimal>();
                        List<decimal> smalllist = new List<decimal>();
                        List<decimal> TopValveOpenlist = new List<decimal>();
                        List<decimal> BotValveOpenlist = new List<decimal>();
                        List<decimal> targetTemp1list = new List<decimal>();
                        List<decimal> targetTemp2list = new List<decimal>();
                        List<decimal> targetTemp3list = new List<decimal>();

                        foreach (SegNoData sd in m.Items)
                        {
                            segnoList.Add(sd.segNo);
                            cmpCoilTempList.Add(sd.cmpCoilTemp);
                            epsTempList.Add(sd.epsTemp);
                            speedList.Add(sd.speed);
                            delaytimeList.Add(sd.delaytime);

                            thisseglist.Add(sd.thisseg);
                            ValveOpenNumlist.Add(sd.ValveOpenNum);
                            fbkConTemplist.Add(sd.fbkConTemp);
                            New_pidCalTemplist.Add(sd.New_pidCalTemp);
                            Old_pidCalTemplist.Add(sd.Old_pidCalTemp);
                            smalllist.Add(sd.small);
                            TopValveOpenlist.Add(sd.TopValveOpen);
                            BotValveOpenlist.Add(sd.BotValveOpen);
                            targetTemp1list.Add(sd.targetTemp1);
                            targetTemp2list.Add(sd.targetTemp2);
                            targetTemp3list.Add(sd.targetTemp3);
                        }


                        float count20 = 0;
                        float count17 = 0;
                        float count30 = 0;

                        for (int i = 0; i < epsTempList.Count; i++)
                        {
                            if (epsTempList[i] <= 17 && epsTempList[i] >= -17) { count17 = count17 + 1; }
                            //if (epsTempList[i] <= 20 && epsTempList[i] >= -20) { count20 = count20 + 1; }
                            //if (epsTempList[i] <= 30 && epsTempList[i] >= -30) { count30 = count30 + 1; }
                        }

                        double tempcount17 = Math.Round((count17 / epsTempList.Count * 100), 2);
                        //double tempcount20 = Math.Round((count20 / epsTempList.Count * 100), 2);
                        //double tempcount30 = Math.Round((count30 / epsTempList.Count * 100), 2);

                        strsql = "insert into striplogs(stripno,nameset,numset1,epsTemp,speed,delaytime1,targettemp1,targettemp2,targettemp3,thisseg,ValveOpenNum,fbkConTemp,New_pidCalTemp,Old_pidCalTemp,small,TopValveOpen,BotValveOpen,Date,Time,eps17) values(@stripno,@nameset,@numset1,@epsTemp,@speed,@delaytime1,@targettemp1,@targettemp2,@targettemp3,@thisseg,@ValveOpenNum,@fbkConTemp,@New_pidCalTemp,@Old_pidCalTemp,@small,@TopValveOpen,@BotValveOpen,@Date,@Time,@eps17)";
                        SQLiteParameter[] ps = new SQLiteParameter[20];
                        ps[0] = new SQLiteParameter("stripno", m.StripNo);
                        ps[1] = new SQLiteParameter("nameset", "[" + string.Join(", ", segnoList) + "]");
                        ps[2] = new SQLiteParameter("numset1", "[" + string.Join(", ", cmpCoilTempList) + "]");
                        ps[3] = new SQLiteParameter("epsTemp", "[" + string.Join(",", epsTempList) + "]");
                        ps[4] = new SQLiteParameter("speed", "[" + string.Join(",", speedList) + "]");
                        ps[5] = new SQLiteParameter("delaytime1", "[" + string.Join(",", delaytimeList) + "]");
                        ps[6] = new SQLiteParameter("targettemp1", targetTemp1list.Max());
                        ps[7] = new SQLiteParameter("targettemp2", targetTemp2list.Max());
                        ps[8] = new SQLiteParameter("targettemp3", targetTemp3list.Max());
                        ps[9] = new SQLiteParameter("thisseg", "[" + string.Join(",", thisseglist) + "]");
                        ps[10] = new SQLiteParameter("ValveOpenNum", "[" + string.Join(",", ValveOpenNumlist) + "]");
                        ps[11] = new SQLiteParameter("fbkConTemp", "[" + string.Join(",", fbkConTemplist) + "]");
                        ps[12] = new SQLiteParameter("New_pidCalTemp", "[" + string.Join(",", New_pidCalTemplist) + "]");
                        ps[13] = new SQLiteParameter("Old_pidCalTemp", "[" + string.Join(",", Old_pidCalTemplist) + "]");
                        ps[14] = new SQLiteParameter("small", "[" + string.Join(",", smalllist) + "]");
                        ps[15] = new SQLiteParameter("TopValveOpen", "[" + string.Join(",", TopValveOpenlist) + "]");
                        ps[16] = new SQLiteParameter("BotValveOpen", "[" + string.Join(",", BotValveOpenlist) + "]");
                        ps[17] = new SQLiteParameter("Date", m.Date);
                        ps[18] = new SQLiteParameter("Time", m.Time);
                        ps[19] = new SQLiteParameter("eps17", tempcount17);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(ps);
                        cmd.CommandText = strsql;
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch (System.Data.SQLite.SQLiteException E)
                {
                    return;
                    tx.Rollback();
                    throw new Exception(E.Message);
                }
            }
        }

        public List<OutScriptCode> GetData(IEnumerable<KeyValuePair<string, object>> parameter = null, bool sortbyaccuracy = false)
        {
            List<OutScriptCode> retval = new List<OutScriptCode>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                string sqltxt = "";
                if (!sortbyaccuracy)
                {
                    sqltxt = @"select * from (select d.stripno,d.nameset,d.numset1,d.epsTemp,d.speed,d.delaytime1,d.targettemp1,d.targettemp2,d.targettemp3,
                                      d.thisseg,d.ValveOpenNum,d.fbkConTemp,d.New_pidCalTemp,d.Old_pidCalTemp,d.small,d.TopValveOpen,
                                      d.BotValveOpen,d.Date,d.Time,d.eps17,m.scf,m.steelGrade,m.targetThickClass,m.targetTempClass,m.fmspeedclass,
                                      m.targettemp,m.segmentno,m.actspeed,m.inheritcoeff,m.lastoutValves,m.tempindistribute 
                                   from striplogs d join stripdats m on d.stripno = m.stripNo) tab where date in (@date) and 1=1 ";
                }
                if (sortbyaccuracy)
                {
                    sqltxt = @"select * from (select d.stripno,d.nameset,d.numset1,d.epsTemp,d.speed,d.delaytime1,d.targettemp1,d.targettemp2,d.targettemp3,
                                      d.thisseg,d.ValveOpenNum,d.fbkConTemp,d.New_pidCalTemp,d.Old_pidCalTemp,d.small,d.TopValveOpen,
                                      d.BotValveOpen,d.Date,d.Time,d.eps17,m.scf,m.steelGrade,m.targetThickClass,m.targetTempClass,m.fmspeedclass,
                                      m.targettemp,m.segmentno,m.actspeed,m.inheritcoeff,m.lastoutValves,m.tempindistribute 
                                   from striplogs d join stripdats m on d.stripno = m.stripNo) tab where date in (@date) and 1=1 order by eps17";
                }

                if (parameter != null && parameter.Count() > 0)
                {
                    string parametterField = "";
                    foreach (var field in parameter)
                    {
                        if (field.Key == "date") //日期参数
                        {
                            sqltxt = sqltxt.Replace("@date", field.Value.ToString());
                        }
                        else {
                            parametterField += " and " + field.Key + "=@" + field.Key;
                        }
                    }

                    if (parametterField != "")
                    {
                        sqltxt = sqltxt.Replace("and 1=1", parametterField);
                    }
                    foreach (var item in parameter)
                    {
                        if (item.Key != "date")
                        {
                            cmd.Parameters.Add(new SQLiteParameter(item.Key, item.Value));
                        }
                    }

                    sqltxt = sqltxt.Replace("date in (@date)", "1=1");
                }
                
                cmd.CommandText = sqltxt;
                SQLiteDataReader rs = cmd.ExecuteReader();
                while (rs.Read())
                {
                    retval.Add(new OutScriptCode()
                    {
                        StripNo = rs["stripno"].ToString(),
                        Date = rs["Date"].ToString(),
                        Time = rs["Time"].ToString(),
                        eps17 = rs["eps17"].ToString(),
                        NameSet = rs["nameset"].ToString(),
                        NumSet1 = rs["numset1"].ToString(),
                        EpsTemp = rs["epsTemp"].ToString(),
                        Delaytime1 = rs["delaytime1"].ToString(),
                        Speed = rs["speed"].ToString(),
                        TargetTemp1 = rs["targettemp1"].ToString(),
                        TargetTemp2 = rs["targettemp2"].ToString(),
                        TargetTemp3 = rs["targettemp3"].ToString(),
                        Thisseg = rs["thisseg"].ToString(),
                        ValveOpenNum = rs["ValveOpenNum"].ToString(),
                        FbkConTemp = rs["fbkConTemp"].ToString(),
                        New_pidCalTemp = rs["New_pidCalTemp"].ToString(),
                        Old_pidCalTemp = rs["Old_pidCalTemp"].ToString(),
                        Small = rs["small"].ToString(),
                        TopValveOpen = rs["TopValveOpen"].ToString(),
                        BotValveOpen = rs["BotValveOpen"].ToString(),

                        Scf = rs["Scf"].ToString(),
                        SteelGrade = rs["SteelGrade"].ToString(),
                        TargetThickClass = rs["TargetThickClass"].ToString(),
                        TargetTempClass = rs["TargetTempClass"].ToString(),
                        FmSpeedclass = rs["FmSpeedclass"].ToString(),
                        TargetTemp = rs["TargetTemp"].ToString(),
                        Segmentno = rs["Segmentno"].ToString(),
                        ActSpeed = rs["ActSpeed"].ToString(),
                        Inheritcoeff = rs["Inheritcoeff"].ToString(),
                        LastoutValves = rs["LastoutValves"].ToString(),
                        Tempindistribute = rs["Tempindistribute"].ToString(),
                    });
                }
                rs.Close();
            }
            return retval;
        }

        public List<string> GetDate()
        {
            List<string> retval = new List<string>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;

                string sqltxt = @"select * from log_date order by logdate desc";

                cmd.CommandText = sqltxt;
                SQLiteDataReader rs = cmd.ExecuteReader();
                while (rs.Read())
                {
                    retval.Add(rs["logdate"].ToString());
                }
                rs.Close();
            }
            return retval;
        }

        public void DeleteData(List<string> date)
        {
            List<string> retval = new List<string>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;

                string dates = "'" + string.Join("','", date.ToArray()) + "'";

                string sqltxt = @"delete from stripdats where stripNo in (select stripno from striplogs where date in (@date))";
                sqltxt = sqltxt.Replace("@date", dates);
                cmd.CommandText = sqltxt;
                cmd.ExecuteNonQuery();

                sqltxt = @"delete from striplogs where date in (@date)";
                sqltxt = sqltxt.Replace("@date", dates);
                cmd.CommandText = sqltxt;
                cmd.ExecuteNonQuery();

                sqltxt = @"delete from log_date where logdate in (@date)";
                sqltxt = sqltxt.Replace("@date", dates);
                cmd.CommandText = sqltxt;
                cmd.ExecuteNonQuery();
            }
        }
        public string Root
        {
            get
            {
                if (root == null)
                {
                    string f = Assembly.GetExecutingAssembly().CodeBase.Substring(8);
                    f = Path.GetDirectoryName(f);
                    f = f.Substring(0, f.LastIndexOf(Path.DirectorySeparatorChar));
                    root = string.Format("{0}{1}", f, Path.DirectorySeparatorChar);
                }
                return root;
            }
        }
        public string ErrorMsg
        {
            get {
                return _errorMsg;
            }
        }
    }


}
