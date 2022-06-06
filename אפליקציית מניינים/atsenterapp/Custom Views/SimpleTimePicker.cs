using System;
using Xamarin.Forms;

namespace atsenterapp
{
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
