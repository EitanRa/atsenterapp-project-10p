using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atsenterapp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            shareBtn.BackgroundColor = Global.ShareColor;

            foregroundSwitch.IsChecked = (!Global.PropertiesKeyIsNotEmpty("foreground")) || (Application.Current.Properties["foreground"] as string) == "True";
        }

        public void SetContactSubject(string text)
        {
            defSubject = text;
        }

        private string defSubject = "";
        private string defContent = "";
        public void contactBtn_Clicked(object sender, EventArgs e)
        {
            StackLayout contactLayout = new StackLayout { FlowDirection = FlowDirection.RightToLeft };
            Entry fromEntry = new Entry { FlowDirection = FlowDirection.LeftToRight, HorizontalOptions = LayoutOptions.FillAndExpand, Placeholder = "yourmail@example.com", Keyboard = Keyboard.Email };
            Entry subjectEntry = new Entry { Placeholder = "לדוגמה: דיווח על תקלות", HorizontalOptions = LayoutOptions.FillAndExpand, Text = defSubject == "" ? null : defSubject };
            defSubject = "";
            Editor contentEntry = new Editor { Placeholder = "תוכן הפנייה", HorizontalOptions = LayoutOptions.FillAndExpand };
            Button sendBtn = new Button { Text = "שלח" };
            Button cancelBtn = new Button { Text = "חזור" };
            StackLayout fromLay = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = {
                        new Label { Text = "דוא\"ל:", FlowDirection = FlowDirection.RightToLeft, VerticalOptions = LayoutOptions.Center },
                        fromEntry
                    }
            };

            StackLayout subjectLay = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                Children = {
                        new Label { Text = "נושא הפנייה:", FlowDirection = FlowDirection.RightToLeft, VerticalOptions = LayoutOptions.Center },
                        subjectEntry
                    }
            };

            StackLayout ContentLay = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Children = {
                        new Label { Text = "תוכן הפנייה:", FlowDirection = FlowDirection.RightToLeft, HorizontalOptions = LayoutOptions.Start },
                        contentEntry
                    }
            };
            contactLayout.Children.Add(new Label { Text = "יצירת קשר", FontSize = 20, HorizontalOptions = LayoutOptions.Center });
            contactLayout.Children.Add(fromLay);
            contactLayout.Children.Add(subjectLay);
            contactLayout.Children.Add(ContentLay);
            if (defContent != "")
                new Action(async () => // This is not just an effect - this is the solution for a bug - do not delete it!
                {

                    contentEntry.Text = "";
                    foreach (char c in defContent)
                    {
                        await Task.Delay(6);
                        contentEntry.Text += c;
                    }
                }).Invoke();
            contactLayout.Children.Add(sendBtn);
            contactLayout.Children.Add(cancelBtn);
            Content = contactLayout;
            sendBtn.Clicked += async (s, ea) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(fromEntry.Text) || string.IsNullOrWhiteSpace(subjectEntry.Text) || string.IsNullOrWhiteSpace(contentEntry.Text))
                    {
                        Global.PopText("שדה לא מלא");
                        return;
                    }
                    if ((fromEntry.Text + subjectEntry.Text + contentEntry.Text).Contains('|'))
                    {
                        Global.PopText("תו אסור");
                        return;
                    }
                    if (!Global.IsValidEmail(fromEntry.Text))
                    {
                        Global.PopText("יש להזין כתובת אימייל חוקית");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Global.ShowMessage(ex.ToString());
                    return;
                }
                using (WebClient wc = new WebClient())
                {
                    string resp = "";
                    Global.PopText("שולח את הפנייה שלך...");
                    try
                    {
                        resp = await wc.UploadStringTaskAsync(Global.serverUrl + "contact", fromEntry.Text + '|' + subjectEntry.Text + '|' + contentEntry.Text);
                    }
                    catch (Exception ex)
                    {
                        if (ex.ToString().Contains("WebException"))
                            Global.ShowMessage("נראה שאין אינטרנט. בדוק את החיבור ונסה שוב");
                        else
                            Global.PopText("תקלה. הטופס שלך לא נשלח");
                    }
                    if (resp == "done")
                        Global.ShowMessage("הטופס שלך התקבל בהצלחה");
                    else if (resp == "illegal data")
                        Global.ShowMessage("שגיאה", "נראה שמשהו בטופס שלך אינו חוקי", "הבנתי");
                    else if (resp == "error")
                        Global.ShowMessage("שגיאה", "השרת סירב לקבל את פנייתך. נסה שוב מאוחר יותר", "סגור");
                }
                Content = mainLayout;
            };
            cancelBtn.Clicked += (s, ea) => { Content = mainLayout; defContent = ""; };
        }

        /*
        private void addBuisnessBtn_Clicked(object sender, EventArgs e)
        {
            StackLayout backup = new StackLayout { FlowDirection = FlowDirection.RightToLeft };
            foreach (View child in (Content as StackLayout).Children.ToList()) // Must use the ToList() function
            {
                backup.Children.Add(child);
            }

            FormattedString howItWorks = new FormattedString();
            howItWorks.Spans.Add(new Span { Text = "איך זה עובד?" + '\n', FontAttributes = FontAttributes.Bold });
            howItWorks.Spans.Add(new Span { Text = "תמורת דמי הרשמה " });
            howItWorks.Spans.Add(new Span { Text = "חד פעמיים", FontAttributes = FontAttributes.Bold });
            howItWorks.Spans.Add(new Span { Text = " של " });
            howItWorks.Spans.Add(new Span { Text = "99₪ בלבד(!!!)", FontAttributes = FontAttributes.Bold, TextColor = Color.Red, BackgroundColor = Color.Yellow });
            howItWorks.Spans.Add(new Span { Text = ", אצנטר תקבע מניינים בעסק שלך, כך שמתפללים שיגיעו אליו יקנו בו בסבירות גבוהה מאוד.\n\nבמהלך יצירת מניין, אצנטר תציג למשתמשים באיזור העסק שלך הצעה לקיים את המניין שם או בעסקים רשומים אחרים, כך שהמשתמש חייב לבחור באחת ההצעות. ככל שהמרחק בין המשתמש לעסק שלך קטן, כך עדיפות ההצעה שלך תגדל .\n\n" });
            howItWorks.Spans.Add(new Span { Text = "למי זה מתאים?", FontAttributes = FontAttributes.Bold });
            howItWorks.Spans.Add(new Span { Text = '\n' + "לעסקים בעלי מתחם קטן ומוצרי צריכה יום-יומיים, כמו מכולת ותחנת דלק, וגם לדוכני מזון מהיר כמו פיצרייה, פלאפל, שווארמה וכו'" });
            StackLayout buisnessLayout = new StackLayout { FlowDirection = FlowDirection.RightToLeft };
            ScrollView scrollView = new ScrollView
            {
                Content = new StackLayout
                {
                    Children =
                    {
                        new Label { FormattedText = howItWorks, Padding = 2 },
                        new StackLayout { Orientation = StackOrientation.Horizontal, HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.FillAndExpand,
                                          Children = { new Image { Source = ImageSource.FromFile("buisnessBanner.png")},
                                                       new Label { Rotation = 270, VerticalOptions = LayoutOptions.FillAndExpand, HorizontalOptions = LayoutOptions.FillAndExpand, Text = "המחשה (ייתכנו שינויים קלים בעיצוב המסך)"} 
                                                     }
                                        }
                    }
                }
            };
            buisnessLayout.Children.Add(new Label { Text = "רישום עסק באפליקציה", HorizontalOptions = LayoutOptions.CenterAndExpand, FontSize = 18 });
            buisnessLayout.Children.Add(new Label { Text = "מביאים לקוחות עם אצנטר", FontAttributes = FontAttributes.Bold, FontSize = 16, Padding = 2 });
            buisnessLayout.Children.Add(scrollView);
            StyleButton nextBtn = new StyleButton
            {
                Text = "המשך >>",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            nextBtn.Clicked += (s, ea) => AddBuisness_Contact();
            StyleButton cancelBtn = new StyleButton
            {
                Text = "חזור",
                BackgroundColor = Color.WhiteSmoke,
                TextColor = Color.Black,
                HorizontalOptions = LayoutOptions.Start
            };
            cancelBtn.Clicked += (s, ea) => Content = backup;
            buisnessLayout.Children.Add(new StackLayout { Orientation = StackOrientation.Horizontal,
                                                          HorizontalOptions = LayoutOptions.FillAndExpand,
                                                          VerticalOptions = LayoutOptions.End,
                                                          Children = { cancelBtn, nextBtn } });
            Content = buisnessLayout;
        }

        private void AddBuisness_Contact()
        {
            SetContactSubject("רישום עסק באפליקציה");
            defContent = "שלום," + '\n' +
                         "ברצוני להוסיף את העסק שלי למאגר העסקים של אצנטר." + '\n' +
                         "אשמח לקבל פרטים על איך עושים את זה ולמה זה כדאי." + '\n' +
                         "תודה," + '\n' + 
                         "(השם שלך)";
            contactBtn_Clicked(this, EventArgs.Empty);
        }
        */

        private void shareBtn_Clicked(object sender, EventArgs e)
        {
            Global.ShareApp();
        }

        private void zmanimBtn_Clicked(object sender, EventArgs e)
        {
            Content = zmanimLayout;
            zmanimLayout.IsVisible = true;
            LoadZmanim();
        }

        private void LoadZmanim()
        {
            try
            {
                zmanimLbl.FormattedText = null;
                if (!Global.Clock.Success)
                {
                    if (Global.ClockLoadTask != null && !Global.ClockLoadTask.IsCompleted)
                    {
                        zmanimLbl.Text = "טוען...";
                        new Action(async () => { await Global.ClockLoadTask; LoadZmanim(); })();
                        return;
                    }
                    Span fail = new Span { Text = "טעינת הזמנים נכשלה. " };
                    Span tryAgain = new Span { Text = "נסה שוב", TextColor = Color.Blue, TextDecorations = TextDecorations.Underline, FontSize = 15 };
                    TapGestureRecognizer tryAgainGR = new TapGestureRecognizer();
                    tryAgainGR.Tapped += async (sender, e) =>
                    {
                        tryAgain.Text = "";
                        fail.Text = "טוען...";
                        Global.MainPage.ClockTask = Global.Clock.DownloadTimesAsync();
                        await Global.MainPage.ClockTask;
                        zmanimLbl.GestureRecognizers.Remove(tryAgainGR);
                        LoadZmanim();
                    };
                    zmanimLbl.GestureRecognizers.Add(tryAgainGR);
                    zmanimLbl.FormattedText = new FormattedString { Spans = { fail, tryAgain } };
                }
                else
                {
                    string[] values = new string[] { "עלות השחר א", "עלות השחר ב", "טלית ותפילין", "הנץ החמה", "ק״ש גר״א", "תפילה גר״א", "חצות", "מנחה גדולה", "מנחה קטנה", "פלג המנחה", "שקיעה", "צאת הכוכבים" };
                    FormattedString zmanimStr = new FormattedString();
                    try
                    {
                        zmanimStr.Spans.Add(new Span { Text = "בס\"ד" + "\n"});
                        zmanimStr.Spans.Add(new Span { Text = Global.Clock.HebrewDate.ToString() + "\n\n", FontAttributes = FontAttributes.Bold, TextColor = Color.Green });
                    } catch { }
                    foreach (string value in values)
                    {
                        Span name = new Span { Text = value + ": " };
                        string val = "לא ידוע";
                        try
                        {
                            val = Global.Clock.GetValue(value).ToString().Remove(5);
                        } catch { }
                        Span time = new Span { Text = val + "\n\n", FontAttributes = FontAttributes.Bold };
                        zmanimStr.Spans.Add(name);
                        zmanimStr.Spans.Add(time);
                    }
                    string specStr = "הזמנים מוצגים לפי זמני ירושלים, כרגע לא ניתן לשנות זאת.\n".Replace("ירושלים", Global.Clock.location.Replace('_', ' ')) +
                                     "האפליקציה לוקחת בחשבון סטייה אפשרית של מספר דקות, אך בכל זאת יש להתחשב באפשרות של אי דיוק של כמה דקות." + "\n\n" +
                                     "זמני התפילות בהתאם לנ\"ל:" + "\n\n";
                    Span specText = new Span { Text = specStr };
                    zmanimStr.Spans.Add(new Span { Text = "שים לב!\n", FontAttributes = FontAttributes.Bold, TextColor = Color.Red, FontSize = 15 });
                    zmanimStr.Spans.Add(specText);
                    foreach (Tfila tfila in (Tfila[])Enum.GetValues(typeof(Tfila)))
                    {
                        Span name = new Span { Text = Global.TfilaToString(tfila) + ": " };
                        string val = "לא ידוע";
                        try { val = Global.EndTimeOf(tfila).ToString().Remove(5) + " - " + Global.StartTimeOf(tfila).ToString().Remove(5); } catch { }
                        Span time = new Span { Text = val + "\n\n", FontAttributes = FontAttributes.Bold };
                        zmanimStr.Spans.Add(name);
                        zmanimStr.Spans.Add(time);
                    }
                    zmanimLbl.FormattedText = zmanimStr;
                }
            }
            catch
            {
                Global.PopText("שגיאה בטעינת זמני היום");
            }
        }

        private void zmanimBackBtn_Clicked(object sender, EventArgs e)
        {
            zmanimLayout.IsVisible = false;
            Content = mainLayout;
        }

        private void foregroundSwitch_CheckedChanged(object sender, EventArgs e)
        {
            Application.Current.Properties["foreground"] = foregroundSwitch.IsChecked.ToString();
            Application.Current.SavePropertiesAsync();
        }
    }
}