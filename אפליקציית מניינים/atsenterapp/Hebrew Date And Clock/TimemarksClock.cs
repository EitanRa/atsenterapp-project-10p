using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
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
                    HebrewDate = HebrewDateTime.Now();
                    year = HebrewDate.Year;
                    month = HebrewDate.Month;
                    day = HebrewDate.Day;
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
    }
}