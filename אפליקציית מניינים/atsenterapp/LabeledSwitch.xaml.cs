using Xamarin.Forms;
using System;

namespace atsenterapp
{
    public partial class LabeledSwitch : ContentView
    {
        public string Title
        {
            get
            {
                return titleLbl.Text;
            }
            set
            {
                titleLbl.Text = value;
            }
        }

        public string Detail
        {
            get
            {
                return detailLbl.Text;
            }
            set
            {
                detailLbl.Text = value;
            }
        }

        public event EventHandler<EventArgs> CheckedChanged;

        public bool IsChecked
        {
            get
            {
                return sw.IsToggled;
            }
            set
            {
                sw.IsToggled = value;
            }
        }

        public LabeledSwitch()
        {
            InitializeComponent();
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            IsChecked = !IsChecked;
            var handler = CheckedChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}