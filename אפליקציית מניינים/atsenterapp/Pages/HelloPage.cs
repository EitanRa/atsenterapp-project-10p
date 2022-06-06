using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace atsenterapp
{
    public class HelloPage : ContentPage
    {
        public HelloPage()
        {
            FormattedString howItWorks = new FormattedString();
            howItWorks.Spans.Add(new Span { Text = "מי לא מכיר את הרגע הזה בטיול שצריך להתחיל לחפש אנשים שעוד לא התפללו מנחה ורוצים להשלים לכם מניין?"
                                                 + "\n" + "מעכשיו, אנחנו נדאג למצוא לכם את המניין מתי ואיפה שתצטרכו." + "\n\n", FontAttributes = FontAttributes.Bold });
            howItWorks.Spans.Add(new Span { Text = "איך זה עובד?" + '\n', FontAttributes = FontAttributes.Bold, FontSize = 15 });
            string mainText = "אופן השימוש באפליקציה דומה קצת לווצאפ, עם רשימת מניינים במקום רשימת צ'אטים." + '\n'
                            + "בכל פעם שהאפליקציה תיפתח, היא תבצע חיפוש מהיר של מניינים באיזור שלך. אם היא תמצא, היא תציג את התוצאות. אם לא, "
                            + "תוכל ללחוץ על 'ארגן מניין', לבחור את התפילה שאתה מארגן (שחרית, מנחה או ערבית) ואת השעה הרצויה וללחוץ על 'אישור',"
                            + " ואז אנשים באיזור שלך יוכלו להצטרף. "
                            + "אם תרצה לשנות בהמשך את שעת התפילה או את מיקומה, תוכל לעשות זאת בקלות במסך ההגדרות של המניין. "
                            + "פשוט תלחץ על כפתור הפתיחה שיופיע בתחתית המסך"
                            + ", ואז יפתח מסך ההגדרות של המניין שבו תוכל:" + "\n\n";
            howItWorks.Spans.Add(new Span { Text = mainText, FontAttributes = FontAttributes.Bold });
            string rules = ("# לראות את פרטי המניין ולשנות אותם (אם אתה המנהל של המניין)|" 
                         +  "# ללחוץ על כפתור הניווט כדי לנווט למניין עם האפליקציה המועדפת עליך, כגון Waze|"
                         +  "# ללחוץ על 'שיתוף' כדי לשתף קישור להצטרפות למניין עם חברים או אנשים באיזור שעשויים להצטרף|"
                         +  "# אם אתה נמצא עם חברים / משפחה, לעדכן את מספר המתפללים שיגיעו ביחד איתך להתפלל|"
                         +  "# במידה וקרה משהו המחייב זאת, לצאת מהמניין כדי לבטל את הגעתך|")
                         .Replace('#', '•').Replace("|", ".\n\n");
            howItWorks.Spans.Add(new Span { Text = rules, FontAttributes = FontAttributes.Bold });
            StackLayout helloLayout = new StackLayout { FlowDirection = FlowDirection.RightToLeft, VerticalOptions = LayoutOptions.FillAndExpand };
            ScrollView scrollView = new ScrollView
            {
                Content = new Label { FormattedText = howItWorks, Padding = 2 },
                VerticalOptions = LayoutOptions.StartAndExpand
            };
            helloLayout.Children.Add(new Label { Text = "ברוכים הבאים לאצנטר!", HorizontalOptions = LayoutOptions.CenterAndExpand, FontSize = 25 });
            helloLayout.Children.Add(new Label { Text = "לפני שמתחילים, הנה הסבר קצר על האפליקציה", FontAttributes = FontAttributes.Bold, FontSize = 16, Padding = 2 });
            helloLayout.Children.Add(scrollView);
            StyleButton nextBtn = new StyleButton
            {
                Text = "המשך >>",
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            nextBtn.Clicked += (s, ea) => Navigation.PopModalAsync();
            StyleButton cancelBtn = new StyleButton
            {
                Text = "סגור",
                BackgroundColor = Color.WhiteSmoke,
                TextColor = Color.Black,
                HorizontalOptions = LayoutOptions.Start
            };
            cancelBtn.Clicked += (s, ea) => Global.CloseApp();
            helloLayout.Children.Add(new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End,
                Children = { cancelBtn, nextBtn }
            });
            Content = helloLayout;
        }
    }
}