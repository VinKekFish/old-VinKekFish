using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// TODO: что у нас насчёт проверки целостности программы?
namespace vinkekfish
{
    public static class HelperClass
    {
        public static string DateToDateString(DateTime now)
        {
            return
                now.Year.ToString("D4") + "." + now.Month.ToString("D2") + "." + now.Day.ToString("D2") + " " +
                now.Hour.ToString("D2") + ":" + now.Minute.ToString("D2") + ":" + now.Second.ToString("D2") + "." + now.Millisecond.ToString("D3");
        }

        public static string DateToDateFileString(DateTime now)
        {
            return
                now.Year.ToString("D4") + "-" + now.Month.ToString("D2") + now.Day.ToString("D2") + "-" +
                now.Hour.ToString("D2") + now.Minute.ToString("D2") + "-" + now.Second.ToString("D2") + "" + now.Millisecond.ToString("D3");
        }

        public static string TimeStampTo_HHMMSS_String(TimeSpan span)
        {
            return span.ToString(@"hh\'mm\:ss");
        }

        public static string TimeStampTo_HHMMSSfff_String(TimeSpan span)
        {
            return span.ToString(@"hh\'mm\:ss\.fff");
        }
    }
}
