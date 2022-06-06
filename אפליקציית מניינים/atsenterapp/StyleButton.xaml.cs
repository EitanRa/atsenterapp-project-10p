using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atsenterapp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StyleButton : ContentView
    {
        private string buttonText;
        public string Text
        {
            get { return buttonText; }
            set
            {
                buttonText = value;
                body.Text = buttonText;
            }
        }

        private Color disabledBc = Color.LightGray;
        private Color bcolor = Color.DodgerBlue;
        public Color BackgroundColor
        {
            get { return bcolor; }
            set
            {
                if (value != disabledBc)
                    bcolor = value;
                frame.BackgroundColor = value;
                body.BackgroundColor = value;
            }
        }

        private Color disabledTc = Color.Gray;
        private Color tcolor = Color.White;
        public Color TextColor
        {
            get { return tcolor; }
            set
            {
                if (value != disabledTc)
                    tcolor = value;
                body.TextColor = value;
                body.BorderColor = value;
            }
        }

        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                IsEnabled = enabled;
                BackgroundColor = enabled ? BackgroundColor : disabledBc;
                TextColor = enabled ? TextColor : disabledTc;
                body.BorderColor = enabled ? TextColor : disabledBc;
            }
        }

        private int corner;
        public int CornerRadius
        {
            get { return corner; }
            set
            {
                corner = value;
                body.CornerRadius = corner;
                frame.CornerRadius = corner;
            }
        }

        public StyleButton()
        {
            InitializeComponent();
        }

        public StyleButton(string text)
        {
            InitializeComponent();
            Text = text;
        }

        public StyleButton(string text, Color backColor)
        {
            InitializeComponent();
            Text = text;
            BackgroundColor = backColor;
        }

        public event EventHandler<EventArgs> Clicked;
        private void body_Clicked(object sender, EventArgs e)
        {
            var handler = Clicked;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}