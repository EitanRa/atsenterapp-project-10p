using Android.App;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;
using Xamarin.Android;
using Android.Support.V7.App;

namespace atsenterapp.Droid.Resources.values
{
    [Activity(Label = "אצנטר", Icon = "@drawable/launcher_foreground", Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
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
    }
}