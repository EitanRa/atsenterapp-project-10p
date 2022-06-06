using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.IO;
using System.Reflection;

namespace atsenterapp
{
    public static class Global
    {
        public static string serverUrl = "https://atsenterserver.herokuapp.com/";

        public static event EventHandler<EventArgs> IdRequest;

        public static Color ShareColor = Color.FromRgb(34, 204, 0);

        public static event EventHandler<ObjectEventArgs<string>> PackageNameRequest;

        public static TimemarksClock Clock
        {
            get
            {
                return MainPage.clock;
            }
        }

        public static Task<bool> ClockLoadTask
        {
            get { return MainPage.ClockTask; }
        }

        public static TimeSpan StartTimeOf(Tfila tfila)
        {
            try
            {
                if (tfila == Tfila.Shaharit)
                {
                    return Clock.GetValue("הנץ החמה");
                }
                else if (tfila == Tfila.Mincha)
                {
                    return Clock.GetValue("מנחה גדולה");
                }
                else
                    return Clock.GetValue("צאת הכוכבים").Add(TimeSpan.FromMinutes(2));
            }
            catch
            {
                if (tfila == Tfila.Shaharit)
                {
                    return new TimeSpan(5, 0, 0);
                }
                else if (tfila == Tfila.Mincha)
                {
                    return new TimeSpan(12, 0, 0);
                }
                else
                    return new TimeSpan(17, 30, 0);
            }
        }

        public static TimeSpan EndTimeOf(Tfila tfila)
        {
            try
            {
                if (tfila == Tfila.Shaharit)
                {
                    return Clock.GetValue("תפילה גר״א");
                }
                else if (tfila == Tfila.Mincha)
                {
                    return Clock.GetValue("שקיעה").Subtract(TimeSpan.FromMinutes(5));
                }
                else
                {
                    /*var list = new List<TimeSpan>(); list.AddRange(new TimeSpan[] { Clock.GetValue("שקיעה"), Clock.GetValue("הנץ החמה") });
                    double average = list.Average(ts => ts.Ticks);
                    long averageTicks = Convert.ToInt64(average);*/
                    return Clock.GetValue("חצות").Add(TimeSpan.FromHours(12)).TimeSpanWithoutDays();
                }
            }
            catch
            {
                if (tfila == Tfila.Shaharit)
                {
                    return new TimeSpan(10, 0, 0);
                }
                else if (tfila == Tfila.Mincha)
                {
                    return new TimeSpan(16, 40, 0);
                }
                else
                    return new TimeSpan(23, 50, 0);
            }
        }

        public static void UpdateApp()
        {
            Launcher.OpenAsync(GooglePlayUrl);
        }

        public static event EventHandler<EventArgs> closeAppRequest;
        public static void CloseApp()
        {
            var handler = closeAppRequest;
            if (handler != null)
                handler(null, EventArgs.Empty);
        }

        public static string GooglePlayUrl
        {
            get
            {
                var handler = PackageNameRequest;
                var objEA = new ObjectEventArgs<string>();
                objEA.Value = "com.atsenter.atsenterapp";
                if (handler != null)
                    handler(null, objEA);
                return "https://play.google.com/store/apps/details?id=" + objEA.Value;
            }
        }

        public static void OpenMinyanPage(int id)
        {
            MainPage mainPage = MainPage;
            if (!mainPage.CanCreate())
            {
                MinyanMiniView miniView = (mainPage.Content as StackLayout).Children.OfType<MinyanMiniView>().ElementAt(0);
                if (miniView.owner.id == id)
                {
                    miniView.Open();
                }
            }
            else
            {
                mainPage.openRequestInProgressId = id;
            }
        }

        public static event EventHandler<ObjectEventArgs<Minyan>> foregroundRequest;
        public static event EventHandler<EventArgs> stopForegroundRequest;
        public static void StartForeground(Minyan minyan)
        {
            var handler = foregroundRequest;
            if (handler != null)
            {
                ObjectEventArgs<Minyan> args = new ObjectEventArgs<Minyan>(minyan);
                handler(null, args);
            }
        }

        public static event EventHandler<int> autoReplyRequest;
        public static void StartAutoReplyService(int duration)
        {
            var handler = autoReplyRequest;
            handler?.Invoke(null, duration);
        }

        public static event EventHandler<EventArgs> autoReplyPermissionsRequest;
        public static void RequestAutoReplyServicePermissions()
        {
            var handler = autoReplyPermissionsRequest;
            handler?.Invoke(null, EventArgs.Empty);
        }

        public static void StopForeground()
        {
            var handler = stopForegroundRequest;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        public static async void ShareApp()
        {
            string text = "";
            try
            {
                var assembly = IntrospectionExtensions.GetTypeInfo(typeof(MainPage)).Assembly;
                Stream stream = assembly.GetManifestResourceStream("atsenterapp.ShareMessage.txt");
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }
            catch
            {
                text = "*אצנטר - אפליקציה חדשה שמשלימה מניינים!!*"
                            + "\n"
                            + "מכירים את הרגע הזה בטיול שצריך להתחיל לחפש אנשים שרוצים להתפלל מנחה❓"
                            + "\n"
                            + "אפליקציה חדשה מקשרת אותך לאנשים באזור שלך שמחפשים מניין, וכך מארגנת קבוצות של 🔟 מתפללים או יותר וקובעת את מקום ושעת התפילה"
                            + "\n\n"
                            + "להתקנת האפליקציה בחינם מ- "
                            + "Google Play:"
                            + " 🙂\n";
            }
            try
            {
                string link = GooglePlayUrl;
                await Share.RequestAsync(text + link, "שתף באמצעות");
            }
            catch
            {
                PopText("שגיאה");
            }
        }

        public static async void OpenBrowser(string url)
        {
            try
            {
                await Browser.OpenAsync(url, BrowserLaunchMode.External);
            }
            catch
            {
                PopText("שגיאה בעת פתיחת הדפדפן");
            }
        }

        public static void OpenContact(SettingsPage page, string subject = null)
        {
            if (page == null)
                page = new SettingsPage();
            try
            {
                if (subject != null)
                    page.SetContactSubject(subject);
                (Application.Current.MainPage as MainPage).Navigation.PushModalAsync(page);
                page.contactBtn_Clicked(null, EventArgs.Empty);
            }
            catch { }
        }

        public static event EventHandler<MenuEventArgs> menuRequest;
        public static void OpenMenu(MenuEventArgs eventArgs)
        {
            var handler = menuRequest;
            handler?.Invoke(null, eventArgs);
        }

        public static async void ReportMinyan(int id, WebClient wc = null)
        {
            bool dispose = false;
            if (wc == null)
            {
                wc = new WebClient();
                dispose = true;
            }
            string resp = "";
            try
            {
                resp = await wc.UploadStringTaskAsync(serverUrl + "contact", "מערכת|דיווח על מניין|משתמש עם מזהה " + MainPage.DeviceId + " דיווח על מניין " + id);
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("WebException"))
                    ShowMessage("נראה שאין אינטרנט. בדוק את החיבור ונסה שוב");
                else
                    PopText("תקלה. הדיווח שלך לא נשלח");
            }
            if (resp == "done")
                ShowMessage("תודה על הדיווח", "הדיווח שלך התקבל בהצלחה", "סגור");
            else if (resp == "illegal data")
                PopText("שגיאה");
            else if (resp == "error")
                ShowMessage("שגיאה", "השרת סירב לקבל את פנייתך. נסה שוב מאוחר יותר", "סגור");
            if (dispose)
                wc.Dispose();
        }

        public static void RequestId(MinyanEditPage editPage)
        {
            var handler = IdRequest;
            if (handler != null)
                handler(editPage, EventArgs.Empty);
        }

        public static event EventHandler<ObjectEventArgs<string>> basePathRequest;
        public static string GetBasePath()
        {
            ObjectEventArgs<string> e = new ObjectEventArgs<string>();
            var handler = basePathRequest;
            handler?.Invoke(null, e);
            return e.Value;
        }

        public static int Real(int num)
        {
            if (num < 0)
                return -1 * num;
            return num;
        }

        public static event EventHandler<string> PopTextRequest;
        public static void PopText(string text)
        {
            var handler = PopTextRequest;
            if (handler != null)
                handler(null, text);
        }

        public static void ShowMessage(string message)
        {
            try
            {
                if (message != "")
                    ShowMessage("ⓘ", message, "סגור");
            }
            catch { }
        }

        public static void ShowMessage(string title, string message, string cancelText)
        {
            try
            {
                if (message != "" && title != "" && cancelText != "")
                    (Application.Current.MainPage as MainPage).DisplayAlert(title, message, cancelText, FlowDirection.RightToLeft);
            }
            catch { }
        }

        public static MainPage MainPage
        {
            get
            {
                return Application.Current.MainPage as MainPage;
            }
        }
        public static string LinkHost = "atsenter.app";
        public static async Task<string> GetLocationAreaNameAsync(Location loc)
        {
            try
            {
                var addressesTask = Geocoding.GetPlacemarksAsync(loc);
                var addresses = await addressesTask;
                List<Placemark> list = addresses.ToList();
                string city = list[0].Locality;
                string street = list[0].Thoroughfare;
                string addr = "";
                if (!string.IsNullOrWhiteSpace(street))
                    addr = street;
                addr += (!string.IsNullOrWhiteSpace(city)) ? ((addr != "" ? ", " : "") + city) : "";
                return addr;
            }
            catch
            {
                return "";
            }
        }
        public static int CountOf(string source, char c)
        {
            int count = 0;
            foreach (char ch in source)
            {
                if (ch == c) count++;
            }
            return count;
        }

        public static bool IsHebrew(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;
            bool hebrewFound = false;
            foreach (char c in str)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    return false;
                hebrewFound = (c >= 'א' && c <= 'ת') || hebrewFound;
            }
            return hebrewFound;
        }

        public static string GetDistanceDisplayText(double distance)
        {
            try
            {
                string distStr = distance.ToString();
                char afterDot = distStr[distStr.IndexOf('.') + 1];
                if (afterDot != '0')
                    return distStr.Substring(0, distStr.IndexOf('.') + 2);
                return ((int)distance).ToString();
            }
            catch { return "-"; }
        }

        public static event EventHandler<QuestionDialogEventArgs> QuestionRequest;
        public static void ShowQuestion(string title, string question, string ok, string cancel, Action okAction)
        {
            if (title == "" || question == "" || ok == "" || cancel == "" || okAction == null)
                return;
            var handler = QuestionRequest;
            var ea = new QuestionDialogEventArgs(title, question, ok, cancel, okAction);
            if (handler != null)
                handler(null, ea);
        }

        public static event EventHandler<NotificationEventArgs> notificationHandler;
        public static void PublishNotification(string title, string content)
        {
            var handler = notificationHandler;
            if (handler != null)
                handler(null, new NotificationEventArgs(title, content));
        }

        /*public static void PublishNotification(string title, string content, long millis, int checkId)
        {
            var handler = notificationHandler;
            if (handler != null)
                handler(null, new NotificationEventArgs(title, content, millis, checkId));
        }*/

        public static int ParseUint(string str)
        {
            try
            {

                string n = "";
                foreach (char c in str)
                {
                    if (c >= '0' && c <= '9')
                        n += c;
                    else
                    {
                        if (n.Length > 0)
                        {
                            return Convert.ToInt32(n);
                        }
                    }
                }
                return Convert.ToInt32(n);
            }
            catch { return -1; }
        }

        public static string TfilaToString(Tfila tfila)
        {
            if (tfila == Tfila.Mincha)
                return "מנחה";
            else if (tfila == Tfila.Arvit)
                return "ערבית";
            return "שחרית";
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                if (email[email.Length - 1] == '.')
                    return false;

                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static void AddRange<T>(IList<T> list, T[] arr)
        {
            foreach (T obj in arr)
            {
                list.Add(obj);
            }
        }

        public static bool PropertiesKeyIsNotEmpty(string key)
        {
            if (Application.Current.Properties.ContainsKey(key))
            {
                return !string.IsNullOrEmpty(Application.Current.Properties[key] as string);
            }
            return false;
        }

        public static long millisUntilTime(int h, int m, int th, int tm)
        {
            long millis;
            millis = (th - h) * (60 * 60000);
            if (m < tm)
                millis += (tm - m) * 60000;
            else
                millis -= (m - tm) * 60000;
            if (th < h || (th == h && tm < m))
            {
                millis = millisUntilTime(h, m, 23, 59) + millisUntilTime(0, 0, th, tm) + 60000;
            }
            return millis;
        }

        public static string Encode(int num)
        {
            string str = num.ToString();
            Random random = new Random();
            int key = random.Next(14);
            string code = "";
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                code += (char)('a' + ((c - '0') + key));
            }
            return key.ToString() + code;
        }

        public static int Decode(string code)
        {
            try
            {
                int key = ParseUint(code);
                code = code.Replace(key.ToString(), "");
                string number = "";
                for (int i = 0; i < code.Length; i++)
                {
                    char c = code[i];
                    number += (char)(((c - key) - 'a' + '0'));
                }
                return Convert.ToInt32(number);
            }
            catch
            {
                return -1;
            }
        }
        public static bool TimeBetween(TimeSpan now, TimeSpan start, TimeSpan end, bool removeDays = true)
        {
            if (removeDays)
            {
                now.TimeSpanWithoutDays();
                start.TimeSpanWithoutDays();
                end.TimeSpanWithoutDays();
            }

            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }

        public static bool IsBefore(this TimeSpan time, TimeSpan target)
        {
            return time.Hours < target.Hours || (time.Hours == target.Hours && time.Minutes < target.Minutes);
        }

        public static bool IsAfter(this TimeSpan time, TimeSpan target)
        {
            return time.Hours > target.Hours || (time.Hours == target.Hours && time.Minutes > target.Minutes);
        }

        public static bool IsSameAs(this TimeSpan time, TimeSpan target)
        {
            return time.Hours == target.Hours && time.Minutes == target.Minutes;
        }

        public static TimeSpan TimeSpanWithoutDays(this TimeSpan time)
        {
            if (time.Days > 0)
                time = time.Subtract(TimeSpan.FromDays(time.Days));
            return time;
        }
    }
}