using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Shapes;
using Xamarin.Essentials;

namespace atsenterapp
{
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
                    {
                        Global.StopForeground();
                    }
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
}