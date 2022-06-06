using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Shapes;
using Xamarin.Essentials;
using System.Net.Security;

namespace atsenterapp
{
    public class TimemarksClock
    {
        private WebClient web = null;
        private readonly string url = "https://skyview2020.co.il/send-ajax.php?";
        private string settings = "LocName=ירושלים&Region=Israel&Method=חזון_שמים";
        public DateTime date = DateTime.Now;
        public TimeSpan time = DateTime.Now.TimeOfDay;
        private string clock = null;
        public HebrewDateTime HebrewDate = null;
        public string location = "ירושלים";
        private bool locationLoaded = false;
        private bool holidaySetted = false;
        public TimemarksClock(WebClient webClient = null)
        {
            if (webClient != null)
                web = webClient;
            else
                web = new WebClient();
            InitiateSSLTrust();
        }

        public void InitiateSSLTrust()
        {
            try
            {
                //Change SSL checks so that all checks pass
                ServicePointManager.ServerCertificateValidationCallback =
                   new RemoteCertificateValidationCallback(
                        delegate
                        { return true; }
                    );
            }
            catch (Exception ex)
            {
                //ActivityLog.InsertSyncActivity(ex);
            }
        }

        private bool success = false;
        public bool Success
        {
            get { return success; }
        }

        private bool isDownloading = false;
        public bool IsDownloading
        {
            get { return isDownloading; }
        }

        private bool firstLoad = false;
        public bool FirstLoad
        {
            get { return firstLoad; }
        }
        public async Task<bool> DownloadTimesAsync()
        {
            firstLoad = true;
            if (!string.IsNullOrWhiteSpace(clock))
            {
                success = true;
                return true;
            }
            isDownloading = true;
            try
            {
                int year = 5783, month = 1, day = 1;
                bool fullSuccess = false;
                try
                {
                    string dateData = await web.DownloadStringTaskAsync("https://www.shoresh.org.il/dates/go.aspx?what=gth&d=" + date.Day + "&m=" + date.Month + "&y=" + date.Year + "&res=txt");
                    dateData = dateData.Replace("^^", "^");
                    string yearStr = DataValue(dateData, 6);
                    string monthStr = DataValue(dateData, 5);
                    string dayStr = DataValue(dateData, 4);
                    HebrewDate = new HebrewDateTime(yearStr, monthStr, dayStr);
                    year = HebrewDate.YearAsInt();
                    month = HebrewDate.MonthAsInt();
                    day = HebrewDate.DayAsInt();
                    if (!holidaySetted)
                    {
                        holidaySetted = true;
                        Global.MainPage.SetHoliday();
                    }
                    fullSuccess = true;
                }
                catch
                {
                    year = date.Year + 3851;
                    month = Global.Real(date.Month - 8);
                }
                if (!locationLoaded)
                {
                    Location loc = Global.MainPage.deviceLocation;
                    if (loc != null)
                    {
                        try
                        {
                            locationLoaded = true;
                            var addressesTask = Geocoding.GetPlacemarksAsync(loc);
                            var addresses = await addressesTask;
                            List<Placemark> list = addresses.ToList();
                            if (list.Count > 0)
                            {
                                string city = list[0].Locality;
                                if (Global.IsHebrew(location))
                                    location = city.Replace(' ', '_');
                            }
                        }
                        catch { }
                    }
                }
                try
                {
                    string add = url + settings.Replace("LocName=ירושלים", "LocName=" + location) + "&year=" + year + "&month=" + month + "&day=" + day;
                    clock = await web.DownloadStringTaskAsync(add);
                    string jerusalem = "ירושלים";
                    if ((!location.Contains(jerusalem)) && clock.Contains(jerusalem))
                    {
                        location = jerusalem;
                    }
                }
                catch { }
                success = !string.IsNullOrWhiteSpace(clock);
                isDownloading = false;
                return fullSuccess;
            }
            catch
            {
                isDownloading = false;
                success = false;
                return false;
            }
        }

        public TimeSpan GetValue(string of)
        {
            if (clock != null)
            {
                if (of == "עלות השחר")
                    of += " א";
                else if (of == "הנץ החמה")
                    of = "זריחה מישורית";
                else if (of == "צאת הכוכבים")
                    return ClockValue(clock, "שקיעה").Add(TimeSpan.FromMinutes(18));
                return ClockValue(clock, of);
            }
            else
                throw new Exception("שגיאה בעת טעינת זמני היום");
        }

        private string DataValue(string src, int index)
        {
            int startIndex = IndexOfSearchIndex(src, '^', index) + 1;
            string value = "לא ידוע";
            if (startIndex != -1)
            {
                try
                {
                    value = src.Substring(startIndex, IndexOfSearchIndex(src, '^', ++index) - startIndex);
                }
                catch { }
            }

            return value;
        }

        private TimeSpan ClockValue(string src, string name)
        {
            //try
            //{
                int index = src.IndexOf(name) + name.Length + 8;
                string unformatted = src.Substring(index - 8, 8);
                int hour = Global.ParseUint(unformatted);
                int minute = Global.ParseUint(unformatted.Substring(unformatted.IndexOf(':')));
                return new TimeSpan(hour, minute, 0);
            /*}
            catch
            {
                return TimeSpan.Zero;
            }*/
        }

        private int IndexOfSearchIndex(string src, char c, int i)
        {
            int current = 0;
            for (int cursor = 0; cursor < src.Length; cursor++)
            {
                if (src[cursor] == c)
                {
                    if (current == i)
                        return cursor;
                    else
                        current++;
                }
            }
            return -1;
        }
    }

    public class HebrewDateTime
    {
        private string year, month, day;
        public HebrewDateTime(string year = null, string month = null, string day = null)
        {
            this.year = year;
            this.month = month;
            this.day = day;

            for (int i = 'א'; i <= 'ת'; i++)
            {
                if (i != 'ך' && i != 'ם' && i != 'ן' && i != 'ף' && i != 'ץ')
                    abc.Add((char)i);
            }
        }
        public string Year
        {
            get { return year; }
            set { year = value; }
        }
        public string Month
        {
            get { return month; }
            set { month = value; }
        }
        public string Day
        {
            get { return day; }
            set { day = value; }
        }
        public TimeSpan TimeOfDay
        {
            get { return DateTime.Now.TimeOfDay; }
        }
        public string ToString(bool withYear = true)
        {
            try
            {
                string strDay = string.IsNullOrWhiteSpace(day) ? "" : day;
                if (strDay != "" && !strDay.Contains("\"") && !strDay.Contains("'"))
                {
                    if (day.Length == 2)
                        strDay = strDay.Insert(1, "\"");
                    else
                        strDay += "'";
                }
                string strYear = string.IsNullOrWhiteSpace(year) ? "" : year;
                if (strYear != "" && !strYear.Contains("\""))
                    strYear = strYear.Insert(strYear.Length - 1, "\"");
                return (strDay != "" ? strDay + " ב" : "") + month + " " + (withYear ? strYear : "");
            }
            catch
            {
                return "תאריך לא ידוע";
            }
        }

        public static List<char> abc = new List<char>();
        public int YearAsInt()
        {
            if (year == null) return -1;
            return YearAsInt(year);
        }
        public int MonthAsInt()
        {
            if (month == null) return -1;
            return MonthAsInt(month);
        }
        public int DayAsInt()
        {
            if (day == null) return -1;
            return DayAsInt(day);
        }
        public static int YearAsInt(string year)
        {
            try
            {
                int z = 18;
                int value = 0;
                foreach (char c in year)
                {
                    int abcIndex = abc.IndexOf(c) + 1;
                    if (abcIndex >= 11 && abcIndex <= z)
                    {
                        value += (abcIndex - 9) * 10;
                    }
                    else if (abcIndex >= z)
                    {
                        value += (abcIndex - z) * 100;
                    }
                    else
                        value += abcIndex;
                }
                return value + 5000;
            }
            catch { return -1; }
        }

        public static int MonthAsInt(string month)
        {
            try
            {
                int value = 1;
                switch (month)
                {
                    case "תשרי":
                        value = 1;
                        break;
                    case "חשון":
                    case "חשוון":
                        value = 2;
                        break;
                    case "כסלו":
                    case "כסליו":
                        value = 3;
                        break;
                    case "טבת":
                        value = 4;
                        break;
                    case "שבט":
                        value = 5;
                        break;
                    case "אדר א": case "אדר הראשון":
                        value = 6;
                        break;
                     case "אדר": case "אדר ב": case "אדר השני":
                        value = 7;
                        break;
                    case "ניסן":
                        value = 8;
                        break;
                    case "אייר":
                        value = 9;
                        break;
                    case "סיון":
                    case "סיוון":
                        value = 10;
                        break;
                    case "תמוז":
                        value = 11;
                        break;
                    case "אב":
                        value = 12;
                        break;
                    case "אלול":
                        value = 13;
                        break;
                }
                return value;
            }
            catch
            {
                return -1;
            }
        }

        public static int DayAsInt(string day)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(day))
                    return -1;
                int value = 0;
                if (day.Length == 2)
                {
                    if (day[0] == 'י')
                        value += 10;
                    else if (day[0] == 'כ')
                        value += 20;
                    else if (day[0] == 'ט')
                        value += 9;
                    value += abc.IndexOf(day[1]) + 1;
                    return value;
                }
                else if (day == "ל")
                    return 30;
                else if (day == "כ")
                    return 20;
                else
                {
                    return abc.IndexOf(day[0]) + 1;
                }
            }
            catch
            {
                return -1;
            }
        }
    }
}
