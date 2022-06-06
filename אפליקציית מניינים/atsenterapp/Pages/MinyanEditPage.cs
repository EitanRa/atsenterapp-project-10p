using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Essentials;
using Xamarin.Forms.Maps;

namespace atsenterapp
{
    public class MinyanEditPage : ContentPage
    {
        private Location FirstLocation = null;
        public string deviceId = "";
        private Action SetTimePicker;
        private Xamarin.Forms.Maps.Map map;
        public MinyanEditPage(string uid)
        {
            FirstLocation = (Application.Current.MainPage as MainPage).deviceLocation;
            if (uid != "")
                deviceId = uid;
            else
            {
                Global.RequestId(this);
            }
            //DownloadSuggestions();
            Label title = new Label
            {
                FontSize = 20,
                Text = "יצירת מניין",
                HorizontalOptions = LayoutOptions.Center
            };
            StyleButton create = new StyleButton
            {
                BackgroundColor = Color.Orange,
                Padding = 2,
                CornerRadius = 5,
                Text = "צור מניין"
            };
            TimeSpan now = Global.TimeSpanWithoutDays(DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(1)));
            bool arvit = Global.TimeBetween(now, Global.StartTimeOf(Tfila.Arvit), Global.EndTimeOf(Tfila.Arvit));
            StackLayout typeBox = new StackLayout
            {
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    new RadioButton { Content = "שחרית", IsChecked = Global.TimeBetween(now, Global.StartTimeOf(Tfila.Shaharit), Global.EndTimeOf(Tfila.Shaharit)) },
                    new RadioButton { Content = "מנחה", IsChecked = Global.TimeBetween(now, Global.StartTimeOf(Tfila.Mincha), Global.EndTimeOf(Tfila.Mincha)) },
                    new RadioButton { Content = "ערבית", IsChecked = arvit }
                }
            };
            bool selected = false;
            foreach (RadioButton rb in typeBox.Children.OfType<RadioButton>())
            {
                rb.FlowDirection = FlowDirection.RightToLeft;
                rb.HorizontalOptions = LayoutOptions.End;
                if (!selected) selected = rb.IsChecked;
                if (!selected && (rb.Content as string).Contains("ערבית"))
                {
                    (typeBox.Children[0] as RadioButton).IsChecked = true;
                    now = new TimeSpan(8, 0, 0);
                }
                rb.CheckedChanged += (s, ea) => { SetTimePicker?.Invoke(); };
            }
            if ((!arvit) && (typeBox.Children.Last() as RadioButton).IsChecked)
                (typeBox.Children[0] as RadioButton).IsChecked = true;

            StackLayout groupBox = new StackLayout { Orientation = StackOrientation.Horizontal };
            CheckBox groupCheck = new CheckBox();
            groupBox.HorizontalOptions = LayoutOptions.End;
            Label groupLbl = new Label { Text = "אני חלק מקבוצה של מתפללים (▼)", VerticalTextAlignment = TextAlignment.Center, FlowDirection = FlowDirection.RightToLeft, TextColor = Color.Black };
            TapGestureRecognizer lbltap = new TapGestureRecognizer();
            lbltap.Tapped += (s, se) => groupCheck.IsChecked = !groupCheck.IsChecked;
            groupLbl.GestureRecognizers.Add(lbltap);
            groupBox.Children.Add(groupLbl);
            groupBox.Children.Add(groupCheck);
            Picker countPicker = new Picker
            {
                ItemsSource = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10+" },
                Title = "מספר מתפללים בקבוצה",
                IsVisible = false,
                HorizontalOptions = LayoutOptions.End,
                FlowDirection = FlowDirection.RightToLeft
            };
            groupCheck.CheckedChanged += (sender, e) =>
            {
                countPicker.IsVisible = groupCheck.IsChecked;
                try
                {
                    var lbl = groupBox.Children[0] as Label;
                    if (groupCheck.IsChecked)
                        lbl.Text = lbl.Text.Replace('▼', '▲');
                    else
                        lbl.Text = lbl.Text.Replace('▲', '▼');
                }
                catch { }
            };
            StackLayout countBox = new StackLayout
            {
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    groupBox,
                    countPicker
                }
            };
            
            SimpleTimePicker stp = new SimpleTimePicker(now.Hours, now.Minutes);
            now = DateTime.Now.TimeOfDay;
            if (stp.Time <= now)
                create.Text = "ארגן מניין למחר ב- " + stp.Time.ToString().Remove(5);
            else
                create.Text = "ארגן מניין ל- " + stp.Time.ToString().Remove(5);
            StackLayout timeBox = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Fill,
                Children =
                {
                    new TimePicker { FlowDirection = FlowDirection.RightToLeft, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.EndAndExpand },
                    new Label { Text = ":ניתן לערוך גם כאן", FlowDirection = FlowDirection.LeftToRight, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.EndAndExpand, HorizontalTextAlignment = TextAlignment.Center },
                    stp.GetControl(),
                    new Label { Text = " שעת התחלה:", FlowDirection = FlowDirection.RightToLeft, HorizontalOptions = LayoutOptions.EndAndExpand, VerticalOptions = LayoutOptions.Center }
                }
            };
            TimePicker tp = timeBox.Children[0] as TimePicker;
            Func<Tfila> GetTfila = new Func<Tfila>(() =>
            {
                Tfila type = Tfila.Shaharit;
                if ((typeBox.Children[1] as RadioButton).IsChecked)
                    type = Tfila.Mincha;
                else if ((typeBox.Children[2] as RadioButton).IsChecked)
                    type = Tfila.Arvit;
                return type;
            });
            Func<TimeSpan, Tfila, bool> IsLegallTime = new Func<TimeSpan, Tfila, bool>((t, type) =>
            {
                return Global.TimeBetween(t, Global.StartTimeOf(type), Global.EndTimeOf(type));
            });
            stp.TimeChanged += (s, e) =>
            {
                Tfila tfila = GetTfila();
                if (IsLegallTime(stp.Time, tfila))
                {
                    tp.Time = stp.Time;
                    if (stp.Time <= now)
                        create.Text = "ארגן מניין למחר ב- " + stp.Time.ToString().Remove(5);
                    else
                        create.Text = "ארגן מניין ל- " + stp.Time.ToString().Remove(5);
                }
                else
                {
                    if (!IsLegallTime(tp.Time, GetTfila()))
                    {
                        stp.Time = Global.EndTimeOf(GetTfila());
                        Global.PopText("זמן תפילה לא חוקי");
                    }
                    else
                    {
                        stp.Time = tp.Time;
                        string text = "זמן " + Global.TfilaToString(tfila) + " הוא בין " + Global.StartTimeOf(tfila).ToString().Remove(5) + " ל- " + Global.EndTimeOf(tfila).ToString().Remove(5);
                        if (!Global.Clock.Success)
                            text += " " + '(' + "שגיאה בטעינת זמני היום, מציג זמנים כלליים" + ')';
                        Global.PopText(text);
                    }
                }
            };
            tp.Time = stp.Time;
            tp.Unfocused += (s, e) =>
            {
                Tfila tfila = GetTfila();
                if (IsLegallTime(tp.Time, tfila))
                    stp.Time = tp.Time;
                else
                {
                    tp.Time = stp.Time;
                    string text = "זמן " + Global.TfilaToString(tfila) + " הוא בין " + Global.StartTimeOf(tfila).ToString().Remove(5) + " ל- " + Global.EndTimeOf(tfila).ToString().Remove(5);
                    if (!Global.Clock.Success)
                        text += " " + '(' + "שגיאה בטעינת זמני היום, מציג זמנים כלליים" + ')';
                    Global.PopText(text);
                }
            };
            SetTimePicker = new Action(() =>
            {
                Tfila tfila = GetTfila();
                if (!IsLegallTime(stp.Time, tfila))
                {
                    if (IsLegallTime(now.Add(TimeSpan.FromHours(1)), tfila))
                        stp.Time = Global.TimeSpanWithoutDays(now.Add(TimeSpan.FromHours(1)));
                    else
                        stp.Time = Global.TimeSpanWithoutDays(Global.EndTimeOf(tfila).Subtract(TimeSpan.FromMinutes(30)));
                }
            });
            if (!IsLegallTime(stp.Time, GetTfila()))
            {
                (typeBox.Children[0] as RadioButton).IsChecked = true;
            }

            string locInfoText = "מיקום המניין נקבע על פי המיקום הנוכחי שלך, או על פי המיקום החלופי הנבחר במפה. בנוסף, יש להזין תיאור שטח על מנת להכווין את המצטרפים באופן מדויק לעמדת התפילה";
            Label locInfo = new Label { Text = locInfoText, FlowDirection = FlowDirection.RightToLeft, Padding = 2 };
            StackLayout locDescBox = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                FlowDirection = FlowDirection.RightToLeft,
                Children =
                {
                    new Label { Text = ' ' + " תיאור מיקום:", FlowDirection = FlowDirection.RightToLeft, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center },
                    new Entry { FlowDirection = FlowDirection.RightToLeft, Placeholder = "לדוגמה: ליד החניון", HorizontalOptions = LayoutOptions.FillAndExpand}
                }
            };

            string mapInfoText = "ניתן לשנות את מיקום המניין על ידי לחיצה על נקודה במפה. אם המפה אינה מוצגת, מיקום המניין יהיה במיקומך הנוכחי";
            Label mapInfo = new Label { Text = mapInfoText, FlowDirection = FlowDirection.RightToLeft, Padding = 2 };

            Content = new StackLayout
            {
                Children =
                {
                    title,
                    typeBox,
                    countBox,
                    timeBox,
                    locInfo,
                    locDescBox,
                    mapInfo,
                    /*
                    new Label { Text = "הצעות למקומות ציבוריים בהם ניתן לקיים את המניין:\n(ההצעות מבוססות על עסקים שנרשמו לאפליקציה)",
                                HorizontalOptions = LayoutOptions.End, FlowDirection = FlowDirection.RightToLeft, Padding = 4 },
                    new ScrollView { Content = sugsBox, HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.StartAndExpand },
                    */
                    create
                }
            };

            DisableMap = new Action(() =>
            {
                StackLayout content = Content as StackLayout;
                if (content.Children.Contains(map))
                    content.Children.Remove(map);
                if (content.Children.Contains(mapInfo))
                    content.Children.Remove(mapInfo);
                locInfo.Text = locInfoText.Insert(locInfoText.IndexOf("במפה") + 4, " (שגיאה בעת טעינת המפה)");
            });
            create.Clicked += (sender, e) =>
            {
                try
                {
                    create.Enabled = false;
                    int type = 0;
                    if ((typeBox.Children[1] as RadioButton).IsChecked)
                        type = 1;
                    else if ((typeBox.Children[2] as RadioButton).IsChecked)
                        type = 2;
                    int count = 1;
                    if (groupCheck.IsChecked)
                    {
                        if (countPicker.SelectedItem != null)
                            count = countPicker.SelectedIndex + 1;
                        else
                            throw new Exception("לא נבחר מספר מתפללים בקבוצה.");
                    }

                    StackLayout content = Content as StackLayout;
                    string timestr = tp.Time.ToString().Remove(5);

                    string locDesc = (locDescBox.Children[1] as Entry).Text;
                    if (Global.PropertiesKeyIsNotEmpty("lastc"))
                    {
                        if (GetCreationNumber() >= 3)
                            throw new Exception("מותר לארגן עד 3 מניינים בכל יום");
                    }
                    if (stp.Time <= DateTime.Now.TimeOfDay && DateTime.Now.DayOfWeek == DayOfWeek.Friday)
                        throw new Exception("לא ניתן לארגן מניין לשבת");
                    if (string.IsNullOrWhiteSpace(locDesc) || locDesc.Length <= 1 || locDesc.Length > 200)
                        throw new Exception("תיאור המיקום חייב להכיל בין 2 ל- 200 תווים");
                    else if (!CheckLegall(locDesc, true))
                        throw new Exception("תיאור המיקום יכול להכיל אותיות עבריות / אנגליות, מספרים ופיסוק בסיסי בלבד.");
                    if (Global.TimeBetween(tp.Time, now, now.Add(TimeSpan.FromMinutes(20))))
                        throw new Exception("יש לקבוע מניינים לזמן של החל מ- 20 דקות");
                    else if (!IsLegallTime(tp.Time, GetTfila()))
                        throw new Exception("הזמן שנבחר אינו מתאים לזמני התפילה שנבחרה");
                    Location loc = FirstLocation;
                    if ((Content as StackLayout).Children.Contains(map) && map.Pins.Count == 1)
                        loc = new Location(map.Pins[0].Position.Latitude, map.Pins[0].Position.Longitude);
                    UploadMinyan(type.ToString(), count.ToString(), loc, locDesc, tp.Time.ToString().Remove(5));
                }
                catch (Exception ex)
                {
                    Global.ShowMessage("בעיית נתונים", ex.Message, "הבנתי");
                    create.Enabled = true;
                }
            };
        }

        private int GetCreationNumber()
        {
            if (Global.PropertiesKeyIsNotEmpty("lastc"))
            {
                string lastc = Application.Current.Properties["lastc"] as string;
                if (lastc.Contains(':') && !lastc.EndsWith(":") && !lastc.StartsWith(":"))
                {
                    try
                    {
                        if (lastc.Substring(0, lastc.IndexOf(':')) != DateTime.Now.ToShortDateString())
                            return 0;
                        int num = Global.ParseUint(lastc.Substring(lastc.IndexOf(':')));
                        return num;
                    }
                    catch
                    {
                        return 3;
                    }
                }
                else
                    return 0;
            }
            return 0;
        }

        public async Task LoadMap()
        {
            StackLayout content = Content as StackLayout;
            if (content.Children.OfType<Xamarin.Forms.Maps.Map>().Any())
                return;
            Position initMapPosition = new Position(FirstLocation.Latitude, FirstLocation.Longitude);
            await Task.Run(() =>
            {
                map = new Xamarin.Forms.Maps.Map(new MapSpan(initMapPosition, 0.01, 0.01));
                Device.BeginInvokeOnMainThread(() =>
                {
                    Pin mapPin = new Pin
                    {
                        Label = "מיקום המניין",
                        Position = initMapPosition,
                        Type = PinType.Generic
                    };
                    map.Pins.Add(mapPin);
                    map.MapClicked += (s, e) =>
                    {
                        mapPin.Position = e.Position;
                    };
                    content.Children.Insert(content.Children.Count - 1, map);
                });
            });
        }

        public Action DisableMap;

        private void EnableCreateButton(bool state = true)
        {
            (Content as StackLayout).Children.OfType<StyleButton>().ElementAt(0).Enabled = state;
        }


        private bool CheckLegall(string text, bool basicP)
        {
            if (text.Length == 0)
                return false;
            foreach (char c in text)
            {
                if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < 'א' || c > 'ת') && (c < '0' || c > '9') && c != ' ')
                {
                    if (basicP)
                    {
                        if (c != ',' && c != '.' && c != ')' && c != '(' && c != ';' && c != ':' && c != '?' && c != '!')
                            return false;
                    }
                    else
                        return false;
                }
            }
            return true;
        }

        private async Task UploadMinyan(string type, string count, Location loc, string locDesc, string time)
        {
            if (!Global.MainPage.CanCreate())
            {
                Global.PopText("אתה כבר מצורף למניין");
                return;
            }
            string location = "";
            //if (placeSelection == null)
            //{
                try
                {
                    location = loc.Latitude + "," + loc.Longitude;
                }
                catch
                {
                    try
                    {
                        Location getloc = await Geolocation.GetLastKnownLocationAsync();
                        location = getloc.Latitude.ToString() + "," + loc.Longitude.ToString();
                    }
                    catch { }
                }
            //}
            //else
                //location = placeSelection.Location.Latitude + "," + placeSelection.Location.Longitude;
            try
            {
                bool idIsNull = deviceId == "";
                string resp = "";
                using (WebClient web = new WebClient())
                {
                    try
                    {
                        resp = await web.UploadStringTaskAsync("https://atsenterserver.herokuapp.com/new", (idIsNull ? "none" : deviceId) + "|" + type + "|" + count + "|" + location + "|" + locDesc + "|" + time);
                    }
                    catch (Exception ex)
                    {
                        if (ex.ToString().Contains("WebException"))
                        {
                            Global.ShowMessage("אין חיבור לאינטרנט");
                            EnableCreateButton();
                            return;
                        }
                    }
                }
                if (!resp.Contains("ID"))
                    Global.ShowMessage("שגיאה", "תקלה כלשהי גרמה לשרת לסרב לקבל את הפנייה שלך.\nיתכן שסגירת האפליקציה ויצירת המניין מחדש יפתרו את הבעיה", "סגור");
                else
                {
                    TimeSpan mtime = TimeSpan.Parse(time);
                    TimeSpan now = DateTime.Now.TimeOfDay;
                    Minyan minyan = null;
                    bool tomorrow = mtime.Hours < now.Hours || (mtime.Hours == now.Hours && mtime.Minutes < now.Minutes);
                    int countNum = 10 - Convert.ToInt32(count);
                    bool shareLink = false;
                    string msg = "";
                    if (countNum > 0)
                        msg += "אם יצטרפו " + countNum + " או יותר מתפללים, ";
                    msg += "המניין יתקיים " + (tomorrow ? "מחר" : "היום") + " בשעה " + time + ".\n";
                    msg += "ניתן לשתף קישור להצטרפות למניין";
                    Global.ShowQuestion("המניין שלך נוצר בהצלחה", msg, "הזמן חברים להצטרף", "סגור", () => { if (minyan == null) shareLink = true; else minyan.miniView?.page?.ShareMinyan(); });
                    string mid = resp.Substring(2);
                    Application.Current.Properties["joined"] = mid + ':' + count.ToString();
                    Application.Current.Properties["lastc"] = DateTime.Now.ToShortDateString() + ":" + (GetCreationNumber() + 1);
                    Application.Current.SavePropertiesAsync();
                    minyan = new Minyan(Convert.ToInt32(mid), idIsNull ? "none" : deviceId, Convert.ToInt32(type), Convert.ToInt32(count), location, locDesc, mtime);
                    minyan.Joined = true;
                    minyan.GetMiniView(); // To create an instance of the miniView object
                    minyan.Host = true;
                    minyan.miniView.page.UserCount = Convert.ToInt32(count);
                    if (shareLink)
                        minyan.miniView.page.ShareMinyan();

                    var mp = Application.Current.MainPage as MainPage;
                    mp.DisableCreating();
                    mp.AddMiniView(minyan.GetMiniView());
                    mp.LoadItems();
                    if (idIsNull)
                        Global.ShowMessage("עקב תקלה, סגירת האפליקציה תגרום לכך שלא תהיה יותר המנהל של המניין שיצרת כעת");
                }
            }
            catch
            {
                Global.ShowMessage("שגיאה", "זוהתה שגיאה. ייתכן שהמניין שלך לא התקבל", "סגור");
            }
            EnableCreateButton();
            try
            {
                await Navigation.PopModalAsync();
            }
            catch { }
        }

        /*
        private async void DownloadSuggestions()
        {
            try
            {
                Location curLoc = FirstLocation;
                string resp = "";
                using (WebClient web = new WebClient())
                    resp = await web.DownloadStringTaskAsync(Global.serverUrl + "buisness/suggestions?lat=" + curLoc.Latitude + "&lot=" + curLoc.Longitude + "&range=500");
                if (resp == "error")
                    throw new Exception();
                int count = Global.CountOf(resp, '\n') + 1;
                count = (count == 1) ? 0 : count;
                List<PlaceSuggestion> sugs = new List<PlaceSuggestion>();
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        bool last = !resp.Contains('\n');

                        string data = last ? resp : resp.Substring(0, resp.IndexOf('\n'));
                        string name = data.Substring(0, data.IndexOf('|'));
                        data = data.Remove(0, data.IndexOf('|') + 1);
                        string desc = data.Substring(0, data.IndexOf('|'));
                        data = data.Remove(0, data.IndexOf('|') + 1);
                        double lat = Convert.ToDouble(data.Substring(0, data.IndexOf('|')));
                        data = data.Remove(0, data.IndexOf('|') + 1);
                        double lot = Convert.ToDouble(data);
                        if (!last)
                            resp = resp.Remove(0, resp.IndexOf('\n') + 1);
                        sugs.Add(new PlaceSuggestion(name, desc, curLoc.CalculateDistance(lat, lot, DistanceUnits.Kilometers), new Location(lat, lot)));
                    }
                    catch { }
                }
                LoadSuggestions(sugs);
            }
            catch
            {
                LoadSuggestions(null);
            }
        }

        private StackLayout sugsBox = new StackLayout
        {
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.StartAndExpand
        };
        private Label sugStatusLbl = new Label { FlowDirection = FlowDirection.RightToLeft, HorizontalOptions = LayoutOptions.CenterAndExpand };
        private Button sugTryAgain = new Button { Text = "נסה שוב" };
        private PlaceSuggestion placeSelection = null;
        private void LoadSuggestions(List<PlaceSuggestion> sugs)
        {
            sugTryAgain.Clicked += (s, e) => { DownloadSuggestions(); sugStatusLbl.Text = "טוען הצעות..."; };
            try
            {
                sugsBox.Children.Clear();
                if (sugs == null)
                {
                    sugStatusLbl.Text = "שגיאה בטעינת ההצעות";
                    sugsBox.Children.Add(sugStatusLbl);
                    sugsBox.Children.Add(sugTryAgain);
                    return;
                }
                if (sugs.Count == 0)
                {
                    sugStatusLbl.Text = "אין הצעות רלוונטיות";
                    sugsBox.Children.Add(sugStatusLbl);
                    sugsBox.Children.Add(sugTryAgain);
                    return;
                }

                sugsBox.Children.Add(new Xamarin.Forms.Shapes.Line { X2 = Content.Width, BackgroundColor = Color.Black });
                int rightX = (int)Content.Width - 1;
                var rightLine = new Xamarin.Forms.Shapes.Line { X1 = rightX, X2 = rightX };
                var leftLine = new Xamarin.Forms.Shapes.Line();
                sugsBox.SizeChanged += (s, ea) =>
                {
                    rightLine.Y2 = sugsBox.Height;
                    leftLine.Y2 = sugsBox.Height;
                };
                // Global.AddRange(sugsBox.Children, new View[] { rightLine, leftLine });
                foreach (PlaceSuggestion sug in sugs)
                {
                    sug.Clicked += (sender, ea) =>
                    {
                        if (sug.ButtonText == "בחר")
                        {
                            placeSelection = sug;
                            foreach (PlaceSuggestion subsug in sugs)
                                subsug.Enabled = subsug == sug;
                            sug.ButtonText = "בטל";
                        }
                        else
                        {
                            placeSelection = null;
                            foreach (PlaceSuggestion subsug in sugs)
                                subsug.Enabled = true;
                            sug.ButtonText = "בחר";
                        }
                    };
                    sugsBox.Children.Add(sug.View);
                    sugsBox.Children.Add(new Xamarin.Forms.Shapes.Line { X2 = Content.Width, BackgroundColor = Color.Black });
                }
            }
            catch { }
        }
        */
    }

    public class PlaceSuggestion
    {
        public string name, desc;
        public double distance;
        public Location Location;

        public event EventHandler<EventArgs> Clicked;

        private View view = null;
        private StyleButton btn = new StyleButton { Text = "בחר", HorizontalOptions = LayoutOptions.End };
        public View View
        {
            get
            {
                if (view == null)
                {
                    try
                    {
                        string dist = distance.ToString();
                        int disDec = dist[dist.IndexOf('.') + 1] - '0';
                        if (distance < 0.01)
                            disDec = 0;
                        StackLayout layout = new StackLayout
                        {
                            Orientation = StackOrientation.Horizontal,
                            FlowDirection = FlowDirection.RightToLeft,
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            Children =
                            {
                                new Label { Text = (int)distance +  ((disDec == 0) ? "" : "." + disDec) + " ק\"מ" + '\n' + "ממך", HorizontalTextAlignment = TextAlignment.Center,
                                            TextColor = Color.Blue, VerticalOptions = LayoutOptions.Center },
                                new Label { Text = "  " + name, FontSize = 17, FlowDirection = FlowDirection.RightToLeft, VerticalOptions = LayoutOptions.Center,
                                            TextColor = Color.Black },
                                btn
                            }
                        };
                        btn.Clicked += (s, ea) =>
                        {
                            bool state = btn.Text == "בחר";
                            btn.BackgroundColor = state ? Color.WhiteSmoke : Color.DodgerBlue;
                            btn.TextColor = state ? Color.Black : Color.White;

                            var handler = Clicked;
                            if (handler != null)
                                handler(this, ea);
                        };
                        view = layout;
                    }
                    catch { }
                }
                return view;
            }
        }

        private bool enabled = false;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                btn.Enabled = enabled;
            }
        }
        public string ButtonText
        {
            get { return btn.Text; }
            set
            {
                btn.Text = value;
            }
        }

        public PlaceSuggestion(string n, string d, double dis, Location loc)
        {
            name = n;
            desc = d;
            distance = dis;
            Location = loc;
        }
    }
}