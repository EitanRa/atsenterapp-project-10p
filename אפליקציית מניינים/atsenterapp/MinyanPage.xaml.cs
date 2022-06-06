using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace atsenterapp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MinyanPage : ContentPage
    {
        private StackLayout boardLayout = new StackLayout();
        private Minyan minyan;
        private int id;
        private Tfila type;
        private bool host;
        private int count;
        private int userCount = 1;
        private string address;
        private string locationDescription;
        private double distance;
        private TimeSpan time;
        public int UserCount
        {
            get { return userCount; }
            set
            {
                userCount = (value <= 10) ? value : 10;
                if (userCount > 1)
                {
                    groupCheck.IsChecked = true;
                    countPicker.SelectedIndex = userCount - 2;
                }
                else
                {
                    singleCheck.IsChecked = true;
                }
            }
        }
        public bool Host
        {
            get { return host; }
            set
            {
                host = value;
                HostModeChange();
            }
        }
        public MinyanPage(Minyan minyan_param, int id_param, Tfila type_param, int count_param, string address_param,
            string locationDescription_param, double distance_param, TimeSpan time_param)
        {
            InitializeComponent();
            updatesBoard.Content = boardLayout;
            detailsLbl.Text += "\n" + " 🕑" + " שעת התחלה:" + "\n" + " 📌" + " כתובת (אוטומטי):" + "\n" + " ❓" + " תיאור מיקום:";
            minyan = minyan_param;
            id = id_param;
            type = type_param;
            string typename = Global.TfilaToString(type);
            titleLbl.Text += typename;
            string[] source = new string[9];
            for (int i = 2; i <= 9; i++)
            {
                source[i - 2] = i + " מתפללים";
            }
            source[8] = "10+ מתפללים";
            countPicker.ItemsSource = source;
            UpdateDetails(count_param, address_param, locationDescription_param, distance_param, time_param);
            LoadTimeBox();
            locDescBox.Text = locationDescription;
            SetBordersWidth();
            LoadNavIcons();
            EnableUpdateBtn(false);
            inviteBtn.BackgroundColor = Global.ShareColor;
            inviteBtn.CornerRadius = 3;
        }

        public void UpdateDetails(int count_param, string address_param,
            string locationDescription_param, double distance_param, TimeSpan time_param)
        {
            count = count_param;
            address = address_param;
            locationDescription = locationDescription_param;
            distance = distance_param;
            time = new TimeSpan(time_param.Hours, time_param.Minutes, 0);
            string tomorrow = time <= DateTime.Now.TimeOfDay ? "מחר ב- " : "";
            detailsValues.Text = count.ToString() + '\n' + tomorrow + time.ToString().Remove(5) + '\n' + address + '\n' + locationDescription;
            mstatusLbl.Text = count < 10 ? ("חסרים " + (10 - count) + " מתפללים") : "✅יש מניין";
            mstatusLbl.TextColor = count < 10 ? Color.Green : Color.Chocolate;
        }

        private void HostModeChange()
        {
            timeBox.IsEnabled = host;
            ChangeEnabledState(timeBox);

            locDescLayout.IsEnabled = host;
            ChangeEnabledState(locDescLayout);

            updateLocationBtn.IsEnabled = host;
        }

        public void DisableUpdateBtn()
        {
            EnableUpdateBtn(false);
        }

        public void AddUpdate(string text, bool important)
        {
            if (text == "") return;

            Label lbl = new Label
            {
                Text = text,
                TextColor = Color.White,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalTextAlignment = TextAlignment.Center,
                FontSize = 15
            };

            lbl.BackgroundColor = important ? Color.Red : Color.FromRgb(214, 237, 243);
            if (!important)
                lbl.TextColor = Color.DarkSlateGray;

            Frame frame = new Frame
            {
                CornerRadius = 8,
                BorderColor = Color.Gray,
                BackgroundColor = lbl.BackgroundColor,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.Start,
                Padding = 7
            };

            frame.Content = lbl;
            StackLayout scrollTo_Layout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children =
                {
                    frame,
                    new Label { VerticalOptions = LayoutOptions.Center, Text = DateTime.Now.TimeOfDay.ToString().Remove(5) }
                }
            };
            boardLayout.Children.Add(scrollTo_Layout);
            try
            {
                updatesBoard.ScrollToAsync(scrollTo_Layout, ScrollToPosition.End, true);
            }
            catch { }
        }

        private void groupCheck_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            countPicker.IsEnabled = groupCheck.IsChecked;
            EnableUpdateBtn();
        }

        private void countPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableUpdateBtn();
        }

        private void EnableUpdateBtn(bool state = true)
        {
            updateBtn.IsEnabled = state;
            updateBtn.BorderColor = state ? Global.ShareColor : Color.Default;
        }

        private void LoadTimeBox()
        {
            SimpleTimePicker stp = new SimpleTimePicker(time.Hours, time.Minutes);

            timeBox.Children.Add(new TimePicker { FlowDirection = FlowDirection.RightToLeft, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.EndAndExpand });
            timeBox.Children.Add(new Label { Text = ":ניתן לערוך גם כאן", FlowDirection = FlowDirection.LeftToRight, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.EndAndExpand, HorizontalTextAlignment = TextAlignment.Center });
            timeBox.Children.Add(stp.GetControl());
            timeBox.Children.Add(new Label { Text = " שעת התחלה:", FlowDirection = FlowDirection.RightToLeft, HorizontalOptions = LayoutOptions.EndAndExpand, VerticalOptions = LayoutOptions.Center });

            TimePicker tp = timeBox.Children[0] as TimePicker;

            Func<TimeSpan, Tfila, bool> IsLegallTime = new Func<TimeSpan, Tfila, bool>((t, type) =>
            {
                return Global.TimeBetween(t, Global.StartTimeOf(type), Global.EndTimeOf(type));
            });
            stp.TimeChanged += (s, e) =>
            {
                if (IsLegallTime(stp.Time, type))
                {
                    tp.Time = stp.Time;
                    EnableUpdateBtn();
                }
                else
                {
                    stp.Time = tp.Time;
                    string text = "זמן " + Global.TfilaToString(type) + " הוא בין " + Global.StartTimeOf(type).ToString().Remove(5) + " ל- " + Global.EndTimeOf(type).ToString().Remove(5);
                    if (!Global.Clock.Success)
                        text += " " + '(' + "שגיאה בטעינת זמני היום, מציג זמנים כלליים" + ')';
                    Global.PopText(text);
                }
            };
            tp.Time = stp.Time;
            tp.Unfocused += (s, e) =>
            {
                if (IsLegallTime(tp.Time, type))
                {
                    stp.Time = tp.Time;
                    EnableUpdateBtn();
                }
                else
                {
                    tp.Time = stp.Time;
                    string text = "זמן " + Global.TfilaToString(type) + " הוא בין " + Global.StartTimeOf(type).ToString().Remove(5) + " ל- " + Global.EndTimeOf(type).ToString().Remove(5);
                    if (!Global.Clock.Success)
                        text += " " + '(' + "שגיאה בטעינת זמני היום, מציג זמנים כלליים" + ')';
                    Global.PopText(text);
                }
            };
            ChangeEnabledState(timeBox);
        }

        private static void ChangeEnabledState(StackLayout layout)
        {
            foreach (View view in layout.Children)
            {
                try
                {
                    view.IsEnabled = layout.IsEnabled;
                    if (view.GetType() == typeof(StackLayout))
                        ChangeEnabledState(view as StackLayout);
                }
                catch { }
            }
        }

        private async void Update_Clicked(object sender, EventArgs e)
        {
            try
            {
                string upd = "";


                int upd_usercount;
                if (groupCheck.IsChecked && countPicker.SelectedIndex != -1)
                    upd_usercount = countPicker.SelectedIndex + 2;
                else
                    upd_usercount = 1;
                if (upd_usercount != userCount)
                {
                    string command = "- " + (userCount - upd_usercount).ToString();
                    if (upd_usercount > userCount)
                    {
                        command = "+ " + (upd_usercount - userCount).ToString();
                    }
                    upd += "count = count " + command;
                }


                string upd_locDesc = locDescBox.Text;
                if (upd_locDesc.Length <= 1 || upd_locDesc.Length > 200)
                    throw new Exception("תיאור המיקום חייב להכיל בין 2 ל- 200 תווים");
                else if (!CheckLegall(upd_locDesc, true))
                    throw new Exception("תיאור המיקום יכול להכיל אותיות עבריות / אנגליות, מספרים ופיסוק בסיסי בלבד.");
                if (upd_locDesc != locationDescription)
                {
                    upd += ((upd == "") ? "" : "|") + "locdesc = '" + upd_locDesc + "'";
                }

                TimeSpan upd_time = (timeBox.Children[0] as TimePicker).Time;
                if (upd_time.Hours != time.Hours || upd_time.Minutes != time.Minutes)
                {
                    upd += ((upd == "") ? "" : "|") + "time = '" + upd_time.ToString() + "'";
                }
                EnableUpdateBtn(false);
                if (upd == "")
                {
                    Global.PopText("נראה שלא שינית דבר");
                    return;
                }
                if (await UploadUpdate(upd))
                {
                    Global.PopText("עודכן");
                    if (upd.Contains("count"))
                    {
                        userCount = upd_usercount;
                        Application.Current.Properties["joined"] = id.ToString() + ':' + upd_usercount;
                        Application.Current.SavePropertiesAsync();
                    }
                    if (upd.Contains("locdesc"))
                    {
                        locationDescription = upd_locDesc;
                    }
                    if (upd.Contains("time"))
                    {
                        time = upd_time;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("instance")) // Object reference not set to an instance of an object
                    Global.ShowMessage("בעיית נתונים", "תיאור המיקום חייב להכיל בין 2 ל- 200 תווים", "הבנתי");
                else
                    Global.ShowMessage("בעיית נתונים", ex.Message, "הבנתי");
            }
        }

        private void exitBtn_Clicked(object sender, EventArgs e)
        {
            exitBtn.IsEnabled = false;
            Global.ShowQuestion("יציאה", "האם אתה בטוח שאתה רוצה לצאת מהמניין?", "כן", "ביטול", Exit);
            Device.StartTimer(TimeSpan.FromSeconds(1), () => { exitBtn.IsEnabled = true; return false; });
        }

        private async void Exit()
        {
            bool done = await UploadUpdate((host ? "host = 'none'|" : "") + "count = count - " + userCount.ToString());
            if (done)
            {
                Application.Current.Properties["joined"] = "";
                Application.Current.SavePropertiesAsync();
                try
                {
                    await Navigation.PopModalAsync();
                    minyan.Joined = false;
                    if (host)
                    {
                        minyan.Host = false;
                        Host = false;
                    }
                    Global.MainPage.RemoveJoining();
                }
                catch { }
            }
        }

        WebClient web = new WebClient();
        private async Task<bool> UploadUpdate(string upd)
        {
            try
            {
                string resp = await web.UploadStringTaskAsync(Global.serverUrl + "update?id=" + id.ToString(), upd);
                if (resp == "done")
                    return true;
                else
                {
                    Global.ShowMessage("שגיאה", "תקלה כלשהי גרמה לשרת לסרב לקבל את פנייתך. נסה שוב מאוחר יותר", "הבנתי");
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("WebException"))
                {
                    Global.ShowMessage("שגיאה", "נראה שאין אינטרנט. בדוק את החיבור ונסה שוב", "סגור");
                }
                return false;
            }

        }

        private void locDescBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableUpdateBtn();
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

        private void SetBordersWidth()
        {
            foreach (Xamarin.Forms.Shapes.Line line in PageContent.Children.OfType<Xamarin.Forms.Shapes.Line>())
            {
                line.X2 = PageContent.Width;
            }
        }

        private void updateLocationBtn_Clicked(object sender, EventArgs e)
        {
            Global.ShowQuestion("עדכון מיקום", "לעדכן את המיקום של המניין למיקומך הנוכחי?", "כן", "ביטול", async () =>
            {
                Location upd_loc = null;
                try
                {
                    upd_loc = await Geolocation.GetLocationAsync();
                }
                catch
                {
                    Global.PopText("נראה ששירותי המיקום כבויים או שאין קליטת GPS");
                    return;
                }
                string upd_loc_str = upd_loc.Latitude.ToString() + ',' + upd_loc.Longitude.ToString();
                if (upd_loc.Latitude != minyan.location.Latitude || upd_loc.Longitude != minyan.location.Longitude)
                {
                    if (await UploadUpdate("location = '" + upd_loc_str + "'"))
                    {
                        Global.PopText("מיקום המניין עודכן");
                    }
                }
                else
                    Global.PopText("המיקום שלך זהה למיקום של המניין");
            });
        }

        private async void navigationBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                Global.PopText("פותח");
                string typestr = Global.TfilaToString(type);
                var options = new MapLaunchOptions { Name = "מניין ל" + typestr + " ב" + address };
                await Map.OpenAsync(minyan.location, options);
            }
            catch
            {
                Global.PopText("משהו השתבש");
            }
        }

        private async void LoadNavIcons()
        {
            await Task.Run(() =>
            {
                nav_icon1.Source = ImageSource.FromFile("waze.png");
                nav_icon2.Source = ImageSource.FromFile("gmaps.png");
                nav_icon3.Source = ImageSource.FromFile("moovit1.png");
            });
        }

        private async void Invite_Clicked(object sender, EventArgs e)
        {
            string link = "https://" + Global.LinkHost + "/join?id=" + Global.Encode(id);
            string text = "לחץ כאן להצטרפות למניין שלי באפליקציית \"אצנטר\":\n" + link + "\n\n" + "להורדת האפליקציה בחינם מ- Google Play:" + '\n' + Global.GooglePlayUrl;
            try
            {
                await Share.RequestAsync(text);
            }
            catch
            {
                try
                {
                    string clipboardState = Clipboard.HasText ? "אם העתקת ללוח דברים חשובים, אפשרות זו אינה מומלצת" : "הלוח כרגע ריק";
                    Action copyAction = async () =>
                    {
                        try
                        {
                            await Clipboard.SetTextAsync(text);
                            Global.PopText("הקישור הועתק");
                        }
                        catch
                        {
                            Global.PopText("שגיאה בעת העתקה ללוח");
                        }
                    };
                    Global.ShowQuestion("שגיאה", "לא הצלחנו לפתוח את מסך השיתוף." + '\n' +
                                        "האם ברצונך להעתיק ללוח את קישור ההצטרפות למניין?" + "\n\n" +
                                        clipboardState, "העתק", "לא", copyAction);
                }
                catch
                {
                    Global.PopText("לא ניתן לשתף כרגע");
                }
            }
        }

        private void ReportLabel_Tapped(object sender, EventArgs e)
        {
            Global.ShowQuestion("דיווח על מניין", "האם לדווח על מניין זה?", "דיווח ויציאה מהמניין", "ביטול", () => { Global.ReportMinyan(minyan.id); Exit(); });
        }
    }
}