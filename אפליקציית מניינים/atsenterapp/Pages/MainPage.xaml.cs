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

namespace atsenterapp
{
    public partial class MainPage : ContentPage
    {
        WebClient web = new WebClient();
        private readonly string serverUrl = Global.serverUrl;
        public Location deviceLocation;
        private MinyanEditPage editPage = null;
        private string deviceId = "";
        private string parameter = null;
        public string DeviceId
        {
            get { return deviceId; }
            set
            {
                deviceId = value;
                LoadItems();
            }
        }

        public MainPage()
        {
            InitializeComponent();
            Init();
        }

        public void SetParam(string value)
        {
            parameter = value;
            questionShowed = false;
            if (loaded)
                LoadItems();
        }

        private void Init()
        {
            if (Global.PropertiesKeyIsNotEmpty("range"))
                userFavdist.Text = Application.Current.Properties["range"] as string;
            else
                userFavdist.Text = "8.0";
            if (!Global.PropertiesKeyIsNotEmpty("autoreplytext"))
                Application.Current.Properties["autoreplytext"] = "לא יכול לדבר כרגע (מענה אוטומטי)";
            rangeLbl.Text = rangeLbl.Text.Insert(rangeLbl.Text.Length - 1, " (ק\"מ)");

            if (Global.PropertiesKeyIsNotEmpty("share"))
            {
                try
                {
                    int share = Global.ParseUint(Application.Current.Properties["share"] as string);
                    if (share == 1 || ((share % 8.0) == 0))
                    {
                        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
                        {
                            Global.ShowQuestion("שתף את האפליקציה", "האפליקציה עוזרת לך למצוא מניינים? שתף אותה כדי שנוכל לעזור לעוד אנשים", "אין בעיה", "אולי אחר כך", Global.ShareApp);
                            return false;
                        });
                    }
                    Application.Current.Properties["share"] = (share + 1).ToString();
                    Application.Current.SavePropertiesAsync();
                }
                catch { }
            }
            else
            {
                Application.Current.Properties["share"] = "1";
                Application.Current.SavePropertiesAsync();
                Navigation.PushModalAsync(new HelloPage());
            }
            TapGestureRecognizer tapevent = new TapGestureRecognizer();
            tapevent.Tapped += OpenSettings;
            settingsLbl.GestureRecognizers.Add(tapevent);
        }

        private bool msgShowed = false;
        private bool checkedForUpdate = false;
        private bool searchedBK = false;
        private bool questionShowed = false;
        public string appVersion = "unknown";
        public TimemarksClock clock = null;
        private bool loaded = false;
        public async void LoadItems()
        {
            loaded = true;
            if (clock == null)
                clock = new TimemarksClock(web);
            if (deviceLocation == null)
            {
                try
                {
                    statusLbl.Text = "מאתר את המיקום שלך...";
                    deviceLocation = await Geolocation.GetLocationAsync();

                    Task.Run(() => editPage = new MinyanEditPage(deviceId));
                }
                catch
                {
                    DisplayAlert("בעיה בשירותי המיקום", "נראה ששירותי המיקום כבויים או שאין קליטת GPS", "הבנתי", FlowDirection.RightToLeft);
                    statusLbl.Text = "לא הצלחנו לאתר את המיקום שלך. וודא שהמיקום מופעל ושיש קליטת GPS ונסה שוב";
                    Button tryAgain = new Button();
                    tryAgain.Text = "נסה שוב";
                    tryAgain.Clicked += (sender, e) =>
                    {
                        listView.Content = null;
                        LoadItems();
                    };
                    listView.Content = tryAgain;
                    return;
                }
            }
            try
            {
                if (parameter != null && !questionShowed)
                {
                    if (listView.BackgroundColor != Color.Transparent)
                    {
                        statusLbl.IsVisible = true;
                        listView.BackgroundColor = Color.Transparent;
                    }
                    Button yes = new Button { Text = "כן", BackgroundColor = Color.Green, CornerRadius = 3, TextColor = Color.White, BorderColor = Color.White, Padding = 2 };
                    Button no = new Button { Text = "לא" };
                    yes.Clicked += (s, ea) =>
                    {
                        yes.IsVisible = false;
                        no.IsVisible = false;
                        LoadItems();
                    };
                    no.Clicked += (s, ea) =>
                    {
                        parameter = null;
                        yes.IsVisible = false;
                        no.IsVisible = false;
                        LoadItems();
                    };
                    statusLbl.Text = "האם ברצונך להצטרף למניין ששותף איתך?";
                    if (Global.PropertiesKeyIsNotEmpty("joined"))
                        statusLbl.Text += "\n" + "אתה תנותק מהמניין שאליו אתה מחובר";
                    listView.Content = new StackLayout { Children = { yes, no } };
                    questionShowed = true;
                    return;
                }
                string error_illegal_range = "יש לבחור טווח חיפוש תקין";
                if (userFavdist.Text == "" || (Global.CountOf(userFavdist.Text, '.') >= 2) || userFavdist.Text[0] == '.' || userFavdist.Text[userFavdist.Text.Length - 1] == '.')
                {
                    Global.ShowMessage("שגיאה", error_illegal_range, "הבנתי");
                    refreshBtn.IsVisible = true;
                    statusLbl.IsVisible = false;
                    return;
                }
                foreach (char c in userFavdist.Text)
                {
                    if ((c > '9' || c < '0') && c != '.')
                    {
                        Global.ShowMessage("שגיאה", error_illegal_range, "הבנתי");
                        refreshBtn.IsVisible = true;
                        statusLbl.IsVisible = false;
                        return;
                    }
                }
                try
                {
                    double range = Convert.ToDouble(userFavdist.Text);
                    if (range > 20 || range < 3)
                    {
                        Global.ShowMessage("שגיאה", "יש לבחור טווח חיפוש בין 3 ל- 20 ק\"מ", "הבנתי");
                        refreshBtn.IsVisible = true;
                        statusLbl.IsVisible = false;
                        return;
                    }
                }
                catch
                {
                    Global.ShowMessage("שגיאה", error_illegal_range, "הבנתי");
                    refreshBtn.IsVisible = true;
                    statusLbl.IsVisible = false;
                    return;
                }

                statusLbl.Text = "טוען מניינים...";
                Task<string[]> listTask = GetList();
                string[] list = await listTask;
                if (list.Length > 0)
                {
                    LoadList(list);
                }
                else
                {
                    statusLbl.Text = "לא נמצאו מניינים רלוונטיים";
                    listView.Content = null;
                }
                create.Enabled = CanCreate();
                if (!searchedBK)
                {
                    try
                    {
                        web.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded; charset=UTF-8");
                        string htmlBK = await web.UploadStringTaskAsync("https://www.kipa.co.il/ajax.php", "action=nearby_prayers&lat="
                                                                                           + deviceLocation.Latitude
                                                                                           + "&lon=" + deviceLocation.Longitude
                                                                                           + "&distance=" + Global.ParseUint(userFavdist.Text) + "&start_in_min=" + (int)DateTime.Now.TimeOfDay.TotalMinutes + "&end_in_min=1440");
                        searchedBK = true;
                        if (!string.IsNullOrWhiteSpace(htmlBK))
                        {
                            htmlBK = ("<!DOCTYPE html><html dir=\"rtl\"><body style=\"background-color:#fffafafa;\"><h2>בתי כנסת קרובים אליך</h2><p>מופעל על ידי <a href=\"https://www.kipa.co.il\">אתר כיפה</a> | <a href=\"https://www.kipa.co.il/%D7%91%D7%AA%D7%99-%D7%9B%D7%A0%D7%A1%D7%AA/%D7%A2%D7%93%D7%9B%D7%95%D7%9F/0/\">הוספת בית כנסת/מניין</a><br></br>שים לב - זמני התפילות עשויים להיות בלתי מעודכנים</p>" + htmlBK + "</body></html>").Replace("\t", "").Replace("\t", "").Replace("\r", "");
                            nearbyBK.Source = new HtmlWebViewSource { Html = htmlBK };
                            nearbyBK.Navigating += (s, e) =>
                            {
                                e.Cancel = true;
                                Action open = () =>
                                {
                                    Global.OpenBrowser(e.Url);
                                };
                                Action copy = async () =>
                                {
                                    await Clipboard.SetTextAsync(e.Url);
                                    Global.PopText("כתובת הקישור הועתקה");
                                };
                                Global.OpenMenu(new MenuEventArgs(Menu.BrowserLinkClick, new Action[] { open, copy }, 0, 0, 0, 1070));
                            };
                            if (CanCreate())
                            {
                                minyansTitle.IsVisible = true;
                                nearbyBK.IsVisible = true;
                                nearbyBK_Frame.IsVisible = true;
                                int height = (int)(settingsLbl.Y - listView.Y) / 2;
                                nearbyBK_Frame.HeightRequest = height;
                                if (listView.Height > height)
                                    listView.HeightRequest = height;
                            }
                        }
                    }
                    catch
                    {
                        Global.PopText("שגיאה בעת טעינת בתי כנסת באיזור שלך");
                    }
                }

                if (!clock.Success)
                    await LoadClockAsync();

                if (!msgShowed) // Because this function might run more than 1 time
                {
                    msgShowed = true;
                    Task<string> msgTask;

                    msgTask = web.DownloadStringTaskAsync(new Uri(serverUrl + "msg"));

                    string msg = await msgTask;
                    if (msg != "" && msg != "none")
                    {
                        Device.StartTimer(TimeSpan.FromSeconds(0.1), () =>
                        {
                            FormatMessage(msg);
                            return false;
                        });
                    }
                }

                if (!checkedForUpdate)
                {
                    try
                    {
                        string resp = await web.DownloadStringTaskAsync(serverUrl + "app_version");
                        checkedForUpdate = true;
                        if (!string.IsNullOrWhiteSpace(resp) && resp != "error" && appVersion.Contains('.') && !resp.StartsWith(appVersion))
                        {
                            if (resp[resp.Length - 1] != '!') // Example: 1.3! = must update
                                Global.ShowQuestion("יש עדכון זמין", "גרסה זו של האפליקציה אינה מעודכנת. יש לעדכן את האפליקציה.", "עדכן", "לא עכשיו", () => Global.UpdateApp());
                            else
                            {
                                Global.ShowMessage("האפליקציה לא מעודכנת", "יש לעדכן את האפליקציה", "סגור");
                                Device.StartTimer(TimeSpan.FromSeconds(3), () =>
                                {
                                    Global.UpdateApp();
                                    if (resp.EndsWith("!!")) // No using the app if not updated
                                        Global.CloseApp();
                                    return false;
                                });
                            }
                        }
                    }
                    catch { }
                }

                refreshBtn.IsVisible = true;
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("WebException"))
                {
                    statusLbl.Text = "אין אינטרנט. בדוק את החיבור ונסה שוב";
                    Button tryAgain = new Button();
                    tryAgain.Text = "נסה שוב";
                    tryAgain.Clicked += (sender, e) =>
                    {
                        listView.Content = null;
                        LoadItems();
                    };
                    listView.Content = tryAgain;
                }
                else
                    statusLbl.Text = "שגיאה. נסה לסגור ולפתוח מחדש את האפליקציה";
            }
        }

        public Task<bool> ClockTask;
        private async Task LoadClockAsync()
        {
            if (!clock.Success)
            {
                clock = new TimemarksClock(web);
                ClockTask = clock.DownloadTimesAsync();
                await ClockTask;
            }
        }

        public void SetHoliday()
        {
            try
            {
                HebrewDateTime date = clock.HebrewDate;
                int month = date.Month;
                int day = date.Day;
                DayOfWeek dayOfWeek = DateTime.Now.DayOfWeek;
                string holiday = "";
                if (month == 1 && day >= 15 && day <= 21)
                    holiday = "חג סוכות";
                else if ((month == 3 && day >= 25) || (month == 4 && day <= 2))
                    holiday = "חג חנוכה";
                else if (month == 5 && day == 15)
                    holiday = "ט\"ו בשבט";
                else if (month == 7 && day == 14)
                    holiday = "פורים";
                else if (month == 7 && day == 15)
                    holiday = "שושן פורים";
                else if (month == 8 && day >= 15 && day <= 20)
                    holiday = "חג פסח כשר ו";
                else if ((month == 9 && day == 5 && dayOfWeek != DayOfWeek.Friday) || (month == 9 && day == 4 && dayOfWeek == DayOfWeek.Thursday))
                    holiday = "יום עצמאות";
                else if (month == 9 && day == 18)
                    holiday = "ל\"ג בעומר";
                else if (month == 9 && day == 28)
                    holiday = "יום שחרור ירושלים";
                if (holiday != "")
                {
                    holidaysLbl.Text = holiday + " שמח!";
                    if (holiday.Contains("פסח"))
                        holidaysLbl.Text = holidaysLbl.Text.Replace("ו שמח", "ושמח");
                    holidaysLbl.IsVisible = true;
                }
                else
                {
                    holidaysLbl.Text = "";
                    holidaysLbl.IsVisible = false;
                }
            }
            catch { }
        }

        private StackLayout controls = new StackLayout();
        private void LoadList(string[] list)
        {
            listView.BackgroundColor = Color.FromRgb(204, 229, 255);
            statusLbl.IsVisible = false;
            controls = new StackLayout();
            listView.Content = controls;
            controls.Children.Add(new Line { X1 = 0, X2 = controls.Width, Y1 = 0, Y2 = 0, BackgroundColor = Color.Black });
            foreach (string minyan in list)
            {
                string str = minyan;
                string name = "", location = "", locDesc = "", timeStr = "";
                int type = 0, count = 0, id = 0;
                TimeSpan time;
                try
                {
                    id = Global.ParseUint(str);
                    str = str.Remove(0, str.IndexOf('|') + 1);
                    name = str.Substring(0, str.IndexOf('|'));
                    str = str.Remove(0, str.IndexOf('|') + 1);
                    type = str[0] - '0';
                    str = str.Remove(0, str.IndexOf('|') + 1);
                    count = Global.ParseUint(str);
                    str = str.Remove(0, str.IndexOf('|') + 1);
                    location = str.Substring(0, str.IndexOf('|'));
                    str = str.Remove(0, str.IndexOf('|') + 1);
                    locDesc = str.Substring(0, str.IndexOf('|'));
                    str = str.Remove(0, str.IndexOf('|') + 1);
                    timeStr = str;
                    str = "";

                    time = TimeSpan.Parse(timeStr);
                }
                catch
                {

                }

                Minyan mobj = null;
                bool btnEnabled = true;
                if (PageContent.Children.OfType<MinyanMiniView>().Any())
                {
                    btnEnabled = false;
                    MinyanMiniView check = PageContent.Children[PageContent.Children.Count - 1] as MinyanMiniView;
                    if (check.id == id)
                    {
                        mobj = check.owner;
                    }
                }
                if (mobj == null)
                {
                    mobj = new Minyan(id, name, type, count, location, locDesc, time);
                    if (Application.Current.Properties.ContainsKey("joined"))
                    {
                        string key = Application.Current.Properties["joined"] as string;
                        if (key != "")
                        {
                            try
                            {
                                string keyId = Global.ParseUint(key).ToString();
                                if (keyId == id.ToString())
                                {
                                    Device.StartTimer(TimeSpan.FromSeconds(0.1), () =>
                                    {
                                        joinMinyan(mobj, false);
                                        mobj.Host = mobj.hostname == deviceId;
                                        try
                                        {
                                            mobj.miniView.page.UserCount = Global.ParseUint(key.Substring(key.IndexOf(':')));
                                            mobj.miniView.page.DisableUpdateBtn();
                                            if (openRequestInProgressId == id)
                                                mobj.miniView.Open();
                                        }
                                        catch { }
                                        return false;
                                    });
                                }
                            }
                            catch { }
                        }
                    }
                }

                mobj.JoinRequest += (sender, e) =>
                {
                    if (!mobj.Joined && CanCreate())
                    {
                        ChangeButtonsEnableState(false);
                        DisableCreating();
                        mobj.ButtonText = "...טוען";
                        // It's important to subscribe the UpdateComplete handler before calling the Update() method
                        EventHandler<bool> completeHandler = null;
                        completeHandler = new EventHandler<bool>(
                            (ms, success) =>
                            {
                                if (success)
                                    joinMinyan(mobj, true);
                                else
                                {
                                    ChangeButtonsEnableState(true);
                                    Global.PopText("נכשל: בדוק את החיבור ונסה שוב");
                                    create.Enabled = true;
                                    mobj.ButtonText = "הצטרף";
                                }
                                if (completeHandler != null)
                                    mobj.UpdateComplete -= completeHandler;
                            }
                        );
                        mobj.UpdateComplete += completeHandler;
                        mobj.Update(web, "count = count + 1");
                    }
                    else
                    {
                        mobj.miniView.Open();
                    }
                };
                var item = mobj.GetListItem(mobj.Joined || btnEnabled);
                controls.Children.Add(item);
                controls.Children.Add(new Line { BackgroundColor = Color.Black, X1 = 0, X2 = item.Width, Y1 = item.Y, Y2 = item.Y });
            }
        }

        public void DisableCreating()
        {
            create.Enabled = false;
        }

        public void joinMinyan(Minyan mobj, bool save)
        {
            if (!CanCreate())
                return;
            mobj.Joined = true;
            create.Enabled = false;
            if (save)
            {
                Application.Current.Properties["joined"] = mobj.id.ToString() + ":1";
                Application.Current.SavePropertiesAsync();
            }
            AddMiniView(mobj.GetMiniView());
            foreach (var view in controls.Children)
            {
                try
                {
                    Button btn = (view as StackLayout).Children.OfType<Button>().ElementAt(0);
                    btn.IsEnabled = btn.Text == "פתח";
                }
                catch { }
            }
        }

        private void ChangeButtonsEnableState(bool state)
        {
            try
            {
                foreach (var view in controls.Children)
                {
                    try
                    {
                        var btn = (view as StackLayout).Children[0] as Button;
                        btn.IsEnabled = state;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void RemoveJoining()
        {
            try
            {
                if (!CanCreate())
                {
                    var miniview = (Content as StackLayout).Children.OfType<MinyanMiniView>().ElementAt(0);
                    Minyan m = miniview.owner;
                    m.Joined = false;
                    (Content as StackLayout).Children.Remove(miniview);
                }
                create.Enabled = true;
                foreach (var view in controls.Children)
                {
                    try
                    {
                        var btn = (view as StackLayout).Children[0] as Button;
                        btn.IsEnabled = true;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void AddMiniView(MinyanMiniView view)
        {
            if (PageContent.Children.Contains(settingsLbl))
                PageContent.Children.Remove(settingsLbl);
            nearbyBK_Frame.IsVisible = false;
            nearbyBK.IsVisible = false;
            if (!PageContent.Children.OfType<MinyanMiniView>().Any())
                PageContent.Children.Add(view);
        }

        public bool CanCreate()
        {
            if ((Content as StackLayout).Children.OfType<MinyanMiniView>().Any())
            {
                DisableCreating();
                return false;
            }
            return true;
        }

        public int openRequestInProgressId = -1;
        private async Task<string[]> GetList()
        {
            bool parameterTryAgain = false;
            if (parameter != null)
            {
                // Quit connected minyans if exists and connect to the linked minyan
                try
                {
                    string error_server_returned_error = "השרת סירב לקבל את בקשתך מסיבה כלשהי";
                    bool toConnect = true;
                    if (Global.PropertiesKeyIsNotEmpty("joined"))
                    {
                        string quitFrom = Application.Current.Properties["joined"] as string;
                        int quitId = Global.ParseUint(quitFrom);
                        if (quitId.ToString() == parameter)
                        {
                            Global.PopText("אתה כבר מחובר למניין המקושר");
                            toConnect = false;
                        }
                        else
                        {
                            int quitUc = Global.ParseUint(quitFrom.Substring(quitFrom.IndexOf(':')));
                            string quitSuccess = await web.UploadStringTaskAsync(serverUrl + "update?id=" + quitId, "count = count - " + quitUc);
                            if (quitSuccess != "done")
                            {
                                toConnect = false;
                                if (quitSuccess == "error")
                                    Global.ShowMessage("שגיאה", error_server_returned_error, "סגור");
                            }
                            else
                            {
                                Application.Current.Properties["joined"] = "";
                                Application.Current.SavePropertiesAsync();
                                if (PageContent.Children.OfType<MinyanMiniView>().Any())
                                    RemoveJoining();
                            }
                        }
                    }
                    if (toConnect)
                    {
                        string resp = await web.UploadStringTaskAsync(serverUrl + "update?id=" + parameter, "count = count + 1");
                        if (resp == "done")
                        {
                            Application.Current.Properties["joined"] = parameter + ":1";
                            Application.Current.SavePropertiesAsync();
                        }
                        else if (resp == "error")
                            Global.ShowMessage("שגיאה", error_server_returned_error, "סגור");
                        else
                            Global.ShowMessage("שגיאה", "משהו השתבש. נסה שוב מאוחר יותר", "סגור");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("WebException"))
                        parameterTryAgain = true;
                    else
                        Global.PopText("שגיאה. לא ניתן לצרף אותך למניין המקושר. נסה שוב מאוחר יותר");
                }
            }
            if (!Global.PropertiesKeyIsNotEmpty("range") || (Application.Current.Properties["range"] as string) != userFavdist.Text)
            {
                Application.Current.Properties["range"] = userFavdist.Text;
                Application.Current.SavePropertiesAsync();
            }
            Task<string> str;
            str = web.DownloadStringTaskAsync(serverUrl + "mlist?lat=" + deviceLocation.Latitude.ToString() + "&lot="
                                                                       + deviceLocation.Longitude.ToString() + "&range="
                                                                       + userFavdist.Text);
            string dl = await str;
            List<string> list = new List<string>();
            int jid = 0;
            bool jstate = false;
            bool added = false;

            if (Global.PropertiesKeyIsNotEmpty("joined"))
            {
                if ((Application.Current.Properties["joined"] as string) != "")
                {
                    jid = Global.ParseUint(Application.Current.Properties["joined"] as string);
                    jstate = true;
                }
            }
            int count = Global.CountOf(dl, '\n');
            for (int c = 0; c < count; c++)
            {
                string mstr = dl.Substring(0, dl.IndexOf("\n"));
                list.Add(mstr);
                dl = dl.Remove(0, dl.IndexOf("\n") + 1);
                if (jstate && !added)
                {
                    added = Global.ParseUint(mstr) == jid;
                }
            }
            if (!added && jstate)
            {
                string joinedm = await web.DownloadStringTaskAsync(serverUrl + "mlist/cfu?id=" + jid.ToString());
                if (joinedm != "not exists")
                    list.Insert(0, jid.ToString() + '|' + joinedm);
                else
                {
                    Application.Current.Properties["joined"] = "";
                    Application.Current.SavePropertiesAsync();
                    RemoveJoining();
                    if (parameter != null && jid.ToString() == parameter)
                        Global.PopText("המניין המקושר אינו קיים עוד");
                }
            }
            if (!parameterTryAgain)
                parameter = null;
            return list.ToArray();
        }

        private async void create_Clicked(object sender, EventArgs e)
        {
            if (CanCreate())
            {
                if (editPage == null)
                    editPage = new MinyanEditPage(deviceId);
                try
                {
                    await Navigation.PushModalAsync(editPage);
                    await editPage.LoadMap();
                }
                catch
                {
                    try
                    {
                        editPage.DisableMap();
                        editPage.Parent = null;
                        await Navigation.PushModalAsync(editPage);
                    }
                    catch
                    {
                        Global.ShowQuestion("שגיאה", "מצטערים, לא ניתן לארגן מניין. אם התקלה נמשכת, אנא דווח לנו", "דווח לנו", "סגור", () => Global.OpenContact(settingPage, "שגיאה בעת ארגון מניין"));
                    }
                }
            }
        }

        private void refreshBtn_Clicked(object sender, EventArgs e)
        {
            statusLbl.IsVisible = true;
            refreshBtn.IsVisible = false;
            LoadItems();
        }

        private void userFavdist_Focused(object sender, FocusEventArgs e)
        {
            Dispatcher.BeginInvokeOnMainThread(() =>
            {
                userFavdist.CursorPosition = 0;
                userFavdist.SelectionLength = userFavdist.Text != null ? userFavdist.Text.Length : 0;
            });
        }

        private SettingsPage settingPage;
        private void OpenSettings(object sender, EventArgs e)
        {
            if (settingPage == null)
                settingPage = new SettingsPage();
            try
            {
                Navigation.PushModalAsync(settingPage);
            }
            catch { }
        }

        private void FormatMessage(string data)
        {
            data += '\n';
            try
            {
                int count = Global.CountOf(data, '\n');
                Structs.MessageProp[] properties = new Structs.MessageProp[count];
                for (int i = 0; i < count; i++)
                {
                    int ind = data.IndexOf('=');
                    int endl = data.IndexOf('\n');
                    properties[i] = new Structs.MessageProp(data.Substring(0, ind), data.Substring(ind + 1, endl - (ind + 1)));
                    if (i < count - 1)
                        data = data.Remove(0, endl + 1);
                }
                bool question = false;
                string title = "", content = "", okText = "אוקיי", cancelText = "סגור", contactSubject = "";
                Action okAction = null;
                foreach (Structs.MessageProp prop in properties)
                {
                    switch (prop.Name)
                    {
                        case "type":
                            if (prop.Value == "q")
                                question = true;
                            break;
                        case "title":
                            title = prop.Value;
                            break;
                        case "content":
                            content = prop.Value;
                            break;
                        case "okText":
                            okText = prop.Value;
                            break;
                        case "cancelText":
                            cancelText = prop.Value;
                            break;
                        case "contactSubject":
                            contactSubject = prop.Value;
                            break;
                        case "okAction":
                            if (prop.Value == "share")
                                okAction = Global.ShareApp;
                            else if (prop.Value == "contact")
                                okAction = () => Global.OpenContact(settingPage, contactSubject);
                            else if (prop.Value.StartsWith("browse:"))
                            {
                                string val = prop.Value;
                                try
                                {
                                    int ind = val.IndexOf(':') + 1;
                                    okAction = () => Global.OpenBrowser(val.Substring(ind));
                                }
                                catch { Global.PopText("שגיאה"); }
                            }
                            break;
                    }
                }
                if (question)
                    Global.ShowQuestion(title, content, okText, cancelText, okAction);
                else
                    Global.ShowMessage(title, content, okText);
            }
            catch { }
        }
    }
}