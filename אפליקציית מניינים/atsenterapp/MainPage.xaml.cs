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
using System.IO;
using System.Reflection;
using System.Resources;

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

                    Task.Run(() => editPage = new MinyanEditPage(deviceLocation, deviceId));
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
                            htmlBK = ("<!DOCTYPE html><html dir=\"rtl\"><body style=\"background-color:#fffafafa;\"><h2>בתי כנסת קרובים אליך</h2><p>מופעל על ידי <a href=\"https://www.kipa.co.il\">אתר כיפה</a><br></br>שים לב - זמני התפילות עשויים להיות בלתי מעודכנים</p>" + htmlBK + "</body></html>").Replace("\t", "").Replace("\t", "").Replace("\r", "");
                            nearbyBK.Source = new HtmlWebViewSource { Html = htmlBK };
                            nearbyBK.Navigating += (s, e) =>
                            {
                                e.Cancel = true;
                                Global.OpenBrowser(e.Url);
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
                int month = date.MonthAsInt();
                int day = date.DayAsInt();
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
                    await Task.Run(() => editPage = new MinyanEditPage(deviceLocation, deviceId));
                try
                {
                    await Navigation.PushModalAsync(editPage);
                }
                catch { }
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
                MessageProp[] properties = new MessageProp[count];
                for (int i = 0; i < count; i++)
                {
                    int ind = data.IndexOf('=');
                    int endl = data.IndexOf('\n');
                    properties[i] = new MessageProp(data.Substring(0, ind), data.Substring(ind + 1, endl - (ind + 1)));
                    if (i < count - 1)
                        data = data.Remove(0, endl + 1);
                }
                bool question = false;
                string title = "", content = "", okText = "אוקיי", cancelText = "סגור", contactSubject = "";
                Action okAction = null;
                foreach (MessageProp prop in properties)
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

    public class Minyan
    {
        string deviceId = (Application.Current.MainPage as MainPage).DeviceId;
        private bool joined = false;
        private bool host = false;
        public bool Host
        {
            get { return host; }
            set
            {
                host = value;
                try
                {
                    miniView.host = host;
                    miniView.page.Host = host;
                }
                catch { }
            }
        }
        public bool Joined
        {
            get
            {
                return joined;
            }
            set
            {
                joined = value;
                JoinedStateChanged();
            }
        }
        public int id;
        private int count;
        public int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
                UpdateDetailsText();
            }
        }
        public Tfila type;
        public string hostname;
        public Location location;
        private TimeSpan time;
        public TimeSpan Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
                UpdateDetailsText();
                //SetNotificaion();
            }
        }
        public string locationDescription;
        public Location deviceLoc = (Application.Current.MainPage as MainPage).deviceLocation;
        private double distance;
        public string address;
        public double Distance
        {
            get
            {
                return distance;
            }
            set
            {
                distance = value;
                try
                {
                    if (distLbl != null)
                    {
                        if (distance > -1)
                        {
                            distLbl.Text = Global.GetDistanceDisplayText(distance) + " ק\"מ";
                        }
                        else
                            distLbl.Text = "-";
                    }
                }
                catch { }
            }
        }
        public string ButtonText
        {
            get { return joinBtn.Text; }
            set
            {
                joinBtn.Text = value;
            }
        }
        public int relevant = 0, userMaxDistance = 10;
        public event EventHandler<EventArgs> JoinRequest;
        public MinyanMiniView miniView = null;

        public Minyan(int idp, string name, int typeInt, int countp, string loc, string locDesc, TimeSpan timep)
        {
            id = idp;
            hostname = name;
            if (typeInt == 0)
                type = Tfila.Shaharit;
            else if (typeInt == 1)
                type = Tfila.Mincha;
            else
                type = Tfila.Arvit;
            count = countp;
            relevant += count;
            try
            {
                location = new Location(Convert.ToDouble(loc.Substring(0, loc.IndexOf(','))), Convert.ToDouble(loc.Substring(loc.IndexOf(',') + 1)));
            }
            catch { }
            locationDescription = locDesc;
            time = timep;
        }

        /*bool notificationInProccess = false;
        public void SetNotificaion()
        {
            if (!notificationInProccess)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                long when = Global.millisUntilTime(now.Hours, now.Minutes, time.Hours, time.Minutes) - (15 * 60000);
                if (when <= 0)
                {
                    when = 60000;
                }
                Global.PublishNotification("יש לך מניין בקרוב", "תזכורת: " + Global.TfilaToString(type) + " בשעה " + time.ToString().Remove(5), when, id);
                notificationInProccess = true;
            }
        }*/

        private void UpdateDetailsText()
        {
            if (detailsLbl != null)
            {
                string typeName = Global.TfilaToString(type);
                string tomorrow = time <= DateTime.Now.TimeOfDay ? " (מחר)" : "";
                detailsLbl.Text = "  " + typeName + "  •  " + "👤" + count.ToString() + "  •  " + "🕑" + time.ToString().Remove(5) + tomorrow + "\n  📌" + address;
                detailsLbl.TextColor = (count < 10) ? Color.Default : Color.Green;
                detailsLbl.FontAttributes = (count < 10) ? FontAttributes.None : FontAttributes.Bold;
            }
        }

        public MinyanMiniView GetMiniView()
        {
            if (miniView == null)
                miniView = new MinyanMiniView(this);
            return miniView;
        }

        WebClient webc = new WebClient();
        bool ForegroundServiceActivated = false;
        private void JoinedStateChanged()
        {
            if (Joined)
            {
                if (horizontalLayout != null)
                    horizontalLayout.BackgroundColor = Color.Gold;
                if (joinBtn != null)
                    joinBtn.Text = "פתח";

                Device.StartTimer(TimeSpan.FromSeconds(10), () =>
                {
                    CheckForUpdate();
                    return Joined;
                });
                if (!ForegroundServiceActivated && (!Global.PropertiesKeyIsNotEmpty("foreground") || (Application.Current.Properties["foreground"] as string).Contains("True")))
                {
                    ForegroundServiceActivated = true;
                    Global.StartForeground(this);
                }
            }
            else
            {
                try
                {
                    horizontalLayout.BackgroundColor = Color.Transparent;
                    if (joinBtn != null)
                        joinBtn.Text = "הצטרף";
                    if (ForegroundServiceActivated)
                        Global.StopForeground();
                }
                catch { }
            }
        }

        private async void CheckForUpdate()
        {
            try
            {
                string upd = await webc.DownloadStringTaskAsync(Global.serverUrl + "mlist/cfu?id=" + id);
                if (upd == "not exists")
                {
                    if (Joined)
                    {
                        Global.PopText("המניין הזה לא קיים יותר");
                    }
                    (Application.Current.MainPage as MainPage).RemoveJoining();
                    if (Joined) Joined = false; // 'Joined' should be set to false at the RemoveJoining function, so this is just
                                                // for case it did'nt happen for some reason
                    horizontalLayout.IsVisible = false;

                }
                List<string> updates = new List<string>();
                string upd_name = upd.Substring(0, upd.IndexOf('|'));
                if (upd_name != hostname)
                {
                    Host = upd_name == deviceId;
                    hostname = upd_name;
                }
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                int upd_count = Global.ParseUint(upd);
                if (upd_count != count)
                {
                    updates.Add(Global.Real(upd_count - count).ToString() + " מתפללים " + ((upd_count > count) ? "הצטרפו ל" : "פרשו מה") + "מניין");
                    count = upd_count;
                }
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                string locationStr = upd.Substring(0, upd.IndexOf('|'));
                try
                {
                    Location upd_location = new Location(Convert.ToDouble(locationStr.Substring(0, locationStr.IndexOf(','))), Convert.ToDouble(locationStr.Substring(locationStr.IndexOf(',') + 1)));
                    if (upd_location.Latitude != location.Latitude || upd_location.Longitude != location.Longitude)
                    {
                        updates.Add("שים לב - מיקום המניין שונה");
                        location = upd_location;
                        if (deviceLoc != null)
                        {
                            Distance = Location.CalculateDistance(location.Latitude, location.Longitude, deviceLoc.Latitude, deviceLoc.Longitude, DistanceUnits.Kilometers);
                            if (distance < 0.1)
                                Distance = 0.01;
                        }
                        LoadAddress();
                    }
                }
                catch { }
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                string upd_locDesc = upd.Substring(0, upd.IndexOf('|'));
                if (upd_locDesc != locationDescription)
                {
                    locationDescription = upd_locDesc;
                    updates.Add("תיאור המיקום שונה");
                }
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                string timeStr = upd;
                upd = "";
                TimeSpan upd_time = TimeSpan.Parse(timeStr);
                if (time.Hours != upd_time.Hours || time.Minutes != upd_time.Minutes)
                {
                    time = upd_time;
                    //SetNotificaion();
                    updates.Add("שעת התפילה עודכנה ל- " + time.ToString().Remove(5));
                }
                UpdateDetailsText();
                miniView.UpdateDetails(updates);
            }
            catch { }
        }

        private readonly string error_illegal_data = "השרת הגיב בשגיאת נתונים לא חוקיים. נסה שוב ואם התקלה נמשכת דווח לנו";
        private readonly string error_server_error = "אירעה שגיאה. נסה שוב מאוחר יותר";
        public event EventHandler<bool> UpdateComplete;
        public void Update(string data)
        {
            using (WebClient web = new WebClient())
                Update(web, data);
        }

        public async void Update(WebClient web, string data)
        {
            var handler = UpdateComplete;
            string resp = "";
            try
            {
                resp = await web.UploadStringTaskAsync(Global.serverUrl + "/update?id=" + id.ToString(), data);
            }
            catch
            {
                if (handler != null)
                    handler(this, false);
                return;
            }
            if (resp == "Illegal data")
            {
                Global.ShowMessage("תקלה", error_illegal_data, "סגור");
                if (handler != null)
                    handler(this, false);
            }
            else if (resp == "error")
            {
                Global.ShowMessage("תקלה", error_server_error, "סגור");
                if (handler != null)
                    handler(this, false);
            }
            else if (resp == "done")
            {
                if (handler != null)
                    handler(this, true);
            }
            else
            {
                if (handler != null)
                    handler(this, false);
            }
        }

        private Button joinBtn;
        private Label detailsLbl;
        private StackLayout horizontalLayout;
        private Label distLbl;
        public StackLayout GetListItem(bool btnEnabled)
        {
            joinBtn = new Button
            {
                Text = Joined ? "פתח" : "הצטרף",
                IsEnabled = btnEnabled,
                VerticalOptions = LayoutOptions.End
            };
            joinBtn.Clicked += (sender, e) =>
            {
                var handler = JoinRequest;
                if (handler != null)
                    handler(sender, e);
            };
            detailsLbl = new Label
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalTextAlignment = TextAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                TextColor = (count < 10) ? Color.Default : Color.Green,
                FontAttributes = (count < 10) ? FontAttributes.None : FontAttributes.Bold
            };

            UpdateDetailsText();
            if (location != null)
            {
                LoadAddress();
            }
            else
                detailsLbl.VerticalOptions = LayoutOptions.Center;

            // Calculate distance: -----------------------------------------------------
            StackLayout distLay = new StackLayout();
            Label thanyouLbl = new Label
            {
                Text = "   ממך   ",
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Center,

                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Start
            };
            distLbl = new Label
            {
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Center,

                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.End,
                FlowDirection = FlowDirection.RightToLeft,
                FontAttributes = FontAttributes.Bold
            };
            distLay.Children.Add(distLbl);
            distLay.Children.Add(thanyouLbl);
            string distText = "-";
            if (deviceLoc != null && location != null)
            {
                try
                {
                    Distance = Location.CalculateDistance(location.Latitude, location.Longitude, deviceLoc.Latitude,
                        deviceLoc.Longitude, DistanceUnits.Kilometers);
                    if (distance < 0.1)
                        Distance = 0.01;
                    relevant += userMaxDistance - (int)distance;
                }
                catch
                {
                    Distance = -1;
                }
            }

            horizontalLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start
            };
            if (Joined)
                horizontalLayout.BackgroundColor = Color.Gold;

            horizontalLayout.Children.Add(joinBtn); // The join button must be added first, to change that edit MainPage -> joinMinyan
            horizontalLayout.Children.Add(detailsLbl);
            Line line = new Line
            {
                X1 = detailsLbl.X + detailsLbl.Width + 1,
                X2 = detailsLbl.X + detailsLbl.Width + 1,
                Y1 = 5,
                Y2 = horizontalLayout.Height - 5,
                BackgroundColor = Color.Black
            };
            if (Application.Current.RequestedTheme == OSAppTheme.Dark)
                line.BackgroundColor = Color.White;
            horizontalLayout.Children.Add(line);
            horizontalLayout.Children.Add(distLay);
            return horizontalLayout;
        }

        private async void LoadAddress()
        {
            // Load address: ------------------------------------------------------
            try
            {
                Task<string> resultTask = Global.GetLocationAreaNameAsync(location);
                string result = await resultTask;
                if (result != "")
                {
                    address = result;
                    UpdateDetailsText();
                }
            }
            catch { }
        }
    }

    public enum Tfila
    {
        Shaharit,
        Mincha,
        Arvit
    }
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
                await Share.RequestAsync(text + link);
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

        public static void PublishNotification(string title, string content, long millis)
        {
            var handler = notificationHandler;
            if (handler != null)
                handler(null, new NotificationEventArgs(title, content, millis));
        }

        public static void PublishNotification(string title, string content, long millis, int checkId)
        {
            var handler = notificationHandler;
            if (handler != null)
                handler(null, new NotificationEventArgs(title, content, millis, checkId));
        }

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

    public class QuestionDialogEventArgs : EventArgs
    {
        public string title, question, ok, cancel;
        public Action okAction;
        public QuestionDialogEventArgs(string titlep, string questionp, string okp, string cancelp, Action okActionp)
        {
            title = titlep;
            question = questionp;
            ok = okp;
            cancel = cancelp;
            okAction = okActionp;
        }
    }

    public class NotificationEventArgs : EventArgs
    {
        public string title, content;
        public long millis = 0;
        public int checkId;
        public NotificationEventArgs(string t, string c)
        {
            title = t;
            content = c;
        }
        public NotificationEventArgs(string t, string c, long m)
        {
            title = t;
            content = c;
            millis = m;
        }
        public NotificationEventArgs(string t, string c, long m, int id)
        {
            title = t;
            content = c;
            millis = m;
            checkId = id;
        }
    }

    public struct MessageProp
    {
        public string Name, Value;
        public MessageProp(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class ObjectEventArgs<T> : EventArgs
    {
        public T Value;
        public ObjectEventArgs(T value)
        {
            Value = value;
        }
        public ObjectEventArgs() { }
    }
}
