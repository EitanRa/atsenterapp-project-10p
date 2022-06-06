using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace atsenterapp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new MainPage();
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            if (uri.Host.EndsWith(Global.LinkHost, StringComparison.OrdinalIgnoreCase))
            {
                string parameter = null;
                try
                {
                    if (uri != null)
                    {
                        parameter = uri.Query.Replace("?", "");
                        string name, value;
                        name = parameter.Substring(0, parameter.IndexOf('='));
                        value = Global.Decode(parameter.Substring(parameter.IndexOf('=') + 1)).ToString();
                        if (name == "id")
                            parameter = value;
                        Convert.ToInt32(value); // If the parameter is not a legall integer, this will throw an exception
                    }
                }
                catch
                {
                    parameter = null;
                }
                (MainPage as MainPage).SetParam(parameter);
            }
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
