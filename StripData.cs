using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalysisTools
{      

    
    public class StripData
    {
        public string StripNo { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public List<SegNoData> Items { get; set; }
    }


    public class SegNoData
    {
        public decimal segNo { get; set; }
        public decimal cmpCoilTemp { get; set; }
        public decimal epsTemp { get; set; }
        //public decimal targetTemp { get; set; }
        public decimal speed { get; set; }
        public decimal delaytime { get; set; }

        public decimal targetTemp1 { get; set; }
        public decimal targetTemp2 { get; set; }
        public decimal targetTemp3 { get; set; }
        public decimal thisseg { get; set; }
        public decimal ValveOpenNum { get; set; }
        public decimal fbkConTemp { get; set; }
        public decimal New_pidCalTemp { get; set; }
        public decimal Old_pidCalTemp { get; set; }
        public decimal small { get; set; }
        public decimal TopValveOpen { get; set; }
        public decimal BotValveOpen { get; set; }
    }

    public class OutScriptCode
    {
        public string StripNo { get; set; }

        public string Date { get; set; }
        public string eps17 { get; set; }
        public string Time { get; set; }
        public string NameSet { get; set; }
        public string NumSet1 { get; set; }
        public string EpsTemp { get; set; }
        public string Speed { get; set; }
        public string Delaytime1 { get; set; }
        public string TargetTemp1 { get; set; }
        public string TargetTemp2 { get; set; }
        public string TargetTemp3 { get; set; }
        public string Thisseg { get; set; }
        public string ValveOpenNum { get; set; }
        public string FbkConTemp { get; set; }
        public string New_pidCalTemp { get; set; }
        public string Old_pidCalTemp { get; set; }
        public string Small { get; set; }
        public string TopValveOpen { get; set; }
        public string BotValveOpen { get; set; }

        //二进制文件内容可以加在后面

        public string Scf { get; set; }
        public string SteelGrade { get; set; }
        public string TargetThickClass { get; set; }
        public string TargetTempClass { get; set; }
        public string FmSpeedclass { get; set; }
        public string TargetTemp { get; set; }
        public string Segmentno { get; set; }
        public string ActSpeed { get; set; }
        public string Inheritcoeff { get; set; }
        public string LastoutValves { get; set; }
        public string Tempindistribute { get; set; }

    }
}
