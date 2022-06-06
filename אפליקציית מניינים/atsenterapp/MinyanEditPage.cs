using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace atsenterapp
{
    public class MinyanEditPage : ContentPage
    {
        private Location FirstLocation = null;
        public string deviceId = "";
        private Action SetTimePicker;
        public MinyanEditPage(Location fl, string uid)
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
                        if ((typeBox.Children[0] as RadioButton).IsChecked)
                            (typeBox.Children[1] as RadioButton).IsChecked = true; // To promise that the checked state of shaharit check is changed
                        (typeBox.Children[0] as RadioButton).IsChecked = true;
                        Global.PopText("זמן תפילה לא חוקי, התפילה הוגדרה כשחרית");
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
            StackLayout locDescBox = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                FlowDirection = FlowDirection.RightToLeft,
                Children =
                {
                    new Label { Text = ' ' + " תיאור מיקום:", FlowDirection = FlowDirection.RightToLeft, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center },
                    new Entry { FlowDirection = FlowDirection.RightToLeft, Placeholder = "לדוגמה: מול מגרש הכדורסל", HorizontalOptions = LayoutOptions.FillAndExpand}
                }
            };



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
                        if ((Application.Current.Properties["lastc"] as string) == DateTime.Now.ToShortDateString())
                          throw new Exception("מותר לארגן מניין אחד בכל יום");
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
                    UploadMinyan(type.ToString(), count.ToString(), locDesc, tp.Time.ToString().Remove(5));
                }
                catch (Exception ex)
                {
                    Global.ShowMessage("בעיית נתונים", ex.Message, "הבנתי");
                    create.Enabled = true;
                }
            };
            Content = new StackLayout
            {
                Children =
                {
                    title,
                    typeBox,
                    countBox,
                    timeBox,
                    locDescBox,
                    /*
                    new Label { Text = "הצעות למקומות ציבוריים בהם ניתן לקיים את המניין:\n(ההצעות מבוססות על עסקים שנרשמו לאפליקציה)",
                                HorizontalOptions = LayoutOptions.End, FlowDirection = FlowDirection.RightToLeft, Padding = 4 },
                    new ScrollView { Content = sugsBox, HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.StartAndExpand },
                    */
                    create
                }
            };
        }

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

        private async Task UploadMinyan(string type, string count, string locDesc, string time)
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
                    location = FirstLocation.Latitude + "," + FirstLocation.Longitude;
                }
                catch
                {
                    try
                    {
                        Location loc = await Geolocation.GetLastKnownLocationAsync();
                        location = loc.Latitude.ToString() + "," + loc.Longitude.ToString();
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
                    bool tomorrow = mtime.Hours < now.Hours || (mtime.Hours == now.Hours && mtime.Minutes < now.Minutes);
                    Global.ShowMessage("המניין שלך נוצר בהצלחה.\nאם יצטרפו " +
                                       (10 - Convert.ToInt32(count)) + " או יותר מתפללים, הוא יתקיים " +
                                       (tomorrow ? "מחר" : "היום") + " בשעה " + time);
                    string mid = resp.Substring(2);
                    Application.Current.Properties["joined"] = mid + ':' + count.ToString();
                    Application.Current.Properties["lastc"] = DateTime.Now.ToShortDateString();
                    Application.Current.SavePropertiesAsync();
                    Minyan minyan = new Minyan(Convert.ToInt32(mid), idIsNull ? "none" : deviceId, Convert.ToInt32(type), Convert.ToInt32(count), location, locDesc, mtime);
                    minyan.Joined = true;
                    minyan.GetMiniView(); // To create an instance of the miniView object
                    minyan.Host = true;
                    minyan.miniView.page.UserCount = Convert.ToInt32(count);

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
                Global.ShowMessage("שגיאה", "לא הצלחנו להעלות את המניין שלך לשרת. נסה שוב מאוחר יותר", "סגור");
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

    public class SimpleTimePicker : View
    {
        public event EventHandler<EventArgs> TimeChanged;
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
                try
                {
                    int hour = time.Hours, minute = time.Minutes;
                    ((layout.Children[0] as StackLayout).Children[1] as Label).Text = (hour <= 9) ? '0' + hour.ToString() : hour.ToString();
                    ((layout.Children[2] as StackLayout).Children[1] as Label).Text = (minute <= 9) ? '0' + minute.ToString() : minute.ToString();
                }
                catch (Exception ex) { throw ex; }
                var handler = TimeChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }
        private int hour;
        public int Hour
        {
            get
            {
                return hour;
            }
            set
            {
                hour = value;
                Time = new TimeSpan(hour, Time.Minutes, 0);
            }
        }

        private int minute;
        public int Minute
        {
            get
            {
                return minute;
            }
            set
            {
                minute = value;
                Time = new TimeSpan(Time.Hours, minute, 0);
            }
        }

        private Tfila tfila;
        public Tfila Tfila
        {
            get { return tfila; }
            set
            {
                tfila = value;
            }
        }

        private StackLayout layout = new StackLayout();
        public SimpleTimePicker(int h, int m)
        {
            layout.Orientation = StackOrientation.Horizontal;
            layout.HorizontalOptions = LayoutOptions.Fill;
            StackLayout hourControl = GetBox(0, 23);
            StackLayout minuteControl = GetBox(0, 59);
            layout.Children.Add(hourControl);
            layout.Children.Add(new Label { Text = ":", HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center });
            layout.Children.Add(minuteControl);
            Time = new TimeSpan((h + 1 == 24) ? 0 : h, (m + 1 == 60) ? 0 : m, 0);
        }

        private StackLayout GetBox(int startValue, int max)
        {
            StackLayout lay = new StackLayout();
            lay.HorizontalOptions = LayoutOptions.End;
            Label value = new Label
            {
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.Black
            };
            value.Text = startValue.ToString();
            Button up = new Button
            {
                Text = "˄",
                FontSize = 20,
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = 40,
                VerticalOptions = LayoutOptions.End,
                HeightRequest = 40
            };
            Button down = new Button
            {
                Text = "˅",
                FontSize = up.FontSize,
                HorizontalOptions = LayoutOptions.End,
                WidthRequest = 40,
                VerticalOptions = LayoutOptions.Start,
                HeightRequest = 40
            };
            up.Clicked += (sender, e) =>
            {
                int val = Convert.ToInt32(value.Text) + 1;
                value.Text = (val > max) ? "0" : val.ToString();
                if (max == 23)
                {
                    Hour = Convert.ToInt32(value.Text);
                }
                else if (max == 59)
                {
                    Minute = Convert.ToInt32(value.Text);
                }
            };
            down.Clicked += (sender, e) =>
            {
                int val = Convert.ToInt32(value.Text) - 1;
                value.Text = (val < 0) ? max.ToString() : val.ToString();
                if (max == 23)
                {
                    Hour = Convert.ToInt32(value.Text);
                }
                else if (max == 59)
                {
                    Minute = Convert.ToInt32(value.Text);
                }
            };
            lay.Children.Add(up);
            lay.Children.Add(value);
            lay.Children.Add(down);
            return lay;
        }

        public View GetControl()
        {
            return layout;
        }

    }
}