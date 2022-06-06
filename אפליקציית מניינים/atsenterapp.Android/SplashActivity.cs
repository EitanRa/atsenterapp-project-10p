using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Support.V4;
using Android.Support.V7.App;

namespace atsenterapp.Droid.Resources.values
{
    
    [Activity(Label = "אצנטר", Icon = "@drawable/launcher_foreground", Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    //[IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "atsenter.app", DataPathPrefix = "/join")]
    public class SplashActivity : AppCompatActivity
    {
        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
        }

        // Launches the startup task
        protected override void OnResume()
        {
            base.OnResume();
            Task startupWork = new Task(() => { Startup(); });
            startupWork.Start();
        }
        
        async void Startup()
        {
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }

        /*protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
        }
        */
    }
}