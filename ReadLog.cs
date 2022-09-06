using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LogAnalysisTools
{
    public class ReadLog
    {
        string _filePath = string.Empty;
        string _fileName = string.Empty;
        string _errorMsg = string.Empty;
        string _fileKey = string.Empty;
        string sql = string.Empty;

        SQLite lite = SQLite.Instance(SQLiteDb.sqlite_db);
        public ReadLog(string filepath,string filename,string filekey)
        {
            _filePath = filepath;
            _fileName = filename;

            if (string.IsNullOrEmpty(filekey))
            {
                _fileKey = Guid.NewGuid().ToString("N");
                sql = @"insert into logfile(file_key,add_time,file_name) values(@file_key,@add_time,@file_name)";
                lite.Execute(sql, new Dictionary<string, object> { { "file_key", _fileKey }, { "add_time", SQLite.Date2 }, { "file_name", filename } });
            }
            else
            {
                _fileKey = filekey;
            }
        }

        public string Analysis()
        {
            string begenTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string patternStripNo = @"curStrip_stripNo=\d+";

            string stripNo = string.Empty;
            Dictionary<string, object> colVals = new Dictionary<string, object>();

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
                        colVals = new Dictionary<string, object>();

                        stripNo = Regex.Match(m.Groups[0].Value, @"\d+").Groups[0].Value;

                        colVals.Add("file_key", _fileKey);
                        colVals.Add("log_time", vals[0]);
                        colVals.Add("stripno", stripNo);

                        Match segno_match = Regex.Match(row, @".segNo=(\d+)");
                        if (segno_match.Success)
                        {
                            colVals.Add("segNo", Regex.Match(segno_match.Groups[0].Value, @"\d+").Groups[0].Value);
                        }
                    }
                    else if (!string.IsNullOrEmpty(stripNo))
                    {
                        Match coiltemp_match = Regex.Match(row, @"  cmpCoilTemp=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (coiltemp_match.Success)
                        {
                            colVals.Add("cmpCoilTemp", Regex.Match(coiltemp_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                            
                        }
                        Match epstemp_match = Regex.Match(row, @", epsTemp=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (epstemp_match.Success)
                        {
                            colVals.Add("epsTemp", Regex.Match(epstemp_match.Groups[0].Value, @"-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?").Groups[0].Value);
                        }

                        Match temptarget_match = Regex.Match(row, @"TempTarget=(\d+)(\,\d+)(\,\d+)");
                        if (temptarget_match.Success)
                        {
                            colVals.Add("TempTarget", Regex.Match(temptarget_match.Groups[0].Value, @"(\d+)(\,\d+)(\,\d+)").Groups[0].Value);
                        }

                        Match speed_match = Regex.Match(row, @"speed=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (speed_match.Success)
                        {
                            colVals.Add("speed", Regex.Match(speed_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                        }

                        Match delaytime_match = Regex.Match(row, @"DelayTime=(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)");
                        if (delaytime_match.Success)
                        {
                            colVals.Add("delaytime", Regex.Match(delaytime_match.Groups[0].Value, @"(-[0-9]+(\.[0-9]+)?|[0-9]+(\.[0-9]+)?)").Groups[0].Value);
                        }

                        if (colVals.Count == 9 && row.IndexOf("_calcCmpBuffer.fbkConTemp(af)=")>0)
                        {
                            sql = @"insert into striplog(file_key,log_time,stripno,segNo,cmpCoilTemp,speed,delaytime,epsTemp,TempTarget) values(@file_key,@log_time,@stripno,@segNo,@cmpCoilTemp,@speed,@delaytime,@epsTemp,@TempTarget)";
                            lite.Execute(sql,colVals);

                            stripNo = string.Empty;
                        }

                    }
                }
            }
            sr.Close();

            string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            MessageBox.Show(string.Format("开始时间：{0}  ， 结束时间：{1}", begenTime, endTime));

            return _fileKey;
        }
        public string ErrorMsg
        {
            get {
                return _errorMsg;
            }
        }
    }
}
