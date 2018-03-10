using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimileTimeline
{
    internal class SimileDate
    {
        public int yearnum;
        public int monthnum;
        public int daynum;

        public SimileDate()
        {
            // Empty constructor
        }

        public SimileDate(int YearNum, int MonthNum, int DayNum)
        {
            yearnum = YearNum;
            monthnum = MonthNum;
            daynum = DayNum;
        }

        public SimileDate(DateTime OriginalDate)
        {
            yearnum = OriginalDate.Year;
            monthnum = OriginalDate.Month;
            daynum = OriginalDate.Day;
        }
    }
}