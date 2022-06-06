using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Xamarin.Essentials;
using System.Net;
using System.Threading;
using Android.Runtime;

namespace atsenterapp.Droid
{
    [Service(Process = ":minyanProcess")]
    public class MinyanService : ForegroundService
    {
        public Tfila type;
        public int count;
        public int id;
        public double latitude, longitude;
        public TimeSpan time;
        private bool autoReplyFeatureIsOn = false;
        private string autoReplyText = "לא יכול לדבר כרגע";
        private Timer timer;

        public MinyanService() : base(typeof(MinyanService)) { }

        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            try
            {
                id = intent.GetIntExtra("id", 0);
                type = (Tfila)intent.GetIntExtra("type", 0);
                count = intent.GetIntExtra("count", 0);
                latitude = intent.GetDoubleExtra("latitude", 31);
                longitude = intent.GetDoubleExtra("longitude", 31);
                time = TimeSpan.Parse(intent.GetStringExtra("time"));
                autoReplyFeatureIsOn = intent.GetBooleanExtra("autoreply", false);
                autoReplyText = intent.GetStringExtra("autoreplytext");
                listener = new ServiceListener(this);
            }
            catch { }

            SetNotification(notify: false);

            try
            {
                // Start check-for-update timer
                timer = new Timer(new TimerCallback((object obj) => CheckForUpdate()), null, 30000, 30000);
            }
            catch { }
            return base.OnStartCommand(intent, flags, startId);
        }

        private WebClient web = new WebClient();
        private async void CheckForUpdate()
        {
            try
            {
                string upd = await web.DownloadStringTaskAsync(Global.serverUrl + "mlist/cfu?id=" + id);
                if (upd.Equals("not exists"))
                {
                    try
                    {
                        if (autoReplyFeatureIsOn && count >= 10)
                            StartAutoReplyService();
                    }
                    catch { }
                    autoReplyFeatureIsOn = false; // To present restart case
                    StopForegroundServiceCompat();
                    return;
                }
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                int upd_count = Global.ParseUint(upd);
                bool update = false;
                if (upd_count != count)
                {
                    count = upd_count;
                    update = true;
                }
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                string locationStr = upd.Substring(0, upd.IndexOf('|'));
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                upd = upd.Remove(0, upd.IndexOf('|') + 1);
                string timeStr = upd;
                upd = "";
                TimeSpan upd_time = TimeSpan.Parse(timeStr);
                if (time.Hours != upd_time.Hours || time.Minutes != upd_time.Minutes)
                {
                    time = upd_time;
                    update = true;
                }
                if (update)
                {
                    SetNotification();
                }
            }
            catch { }
        }

        private void StartAutoReplyService()
        {
            AutoMessageService autoMessageService = new AutoMessageService();
            Bundle args = new Bundle();
            int duration = 45;
            if (type == Tfila.Mincha)
                duration = 20;
            else if (type == Tfila.Arvit)
                duration = 10;
            args.PutInt("duration", duration);
            args.PutString("message", autoReplyText);
            try
            {
                autoMessageService.StartForegroundServiceCompat(args);
            }
            catch
            {
                PopText("שגיאה בעת הפעלת מענה אוטומטי לשיחות", ToastLength.Long);
            }
        }

        private bool firstBuild = true;
        private void SetNotification(bool notify = true)
        {
            string paramInserter = "{1}";
            string title = string.Format("המניין שלך ל{0} ב- " + paramInserter, Global.TfilaToString(type), time.ToString().Remove(5));
            string content = count >= 10 ? "✅יש מניין" : string.Format("חסרים {0} מתפללים", 10 - count);
            if (firstBuild)
            {
                firstBuild = false;
                BuildNotification(title, content, BuildIntentToShowPage(), new Notification.Action[] { BuildNavigateAction(), BuildStopServiceAction() }, Resource.Drawable.ic_notification5);
            }
            else
            {
                BuildNotification(title, content);
            }
            if (notify)
                NotifyNotification();
        }

        public override void OnDestroy()
        {
            try
            {
                timer.Dispose();
            }
            catch { }
            base.OnDestroy();
        }

        private PendingIntent BuildIntentToShowPage()
        {
            return PendingIntent.GetBroadcast(this, 1, new Intent(ForegroundServiceAction.Open).PutExtra(ForegroundServiceAction.Open, id), PendingIntentFlags.CancelCurrent);
        }

        private Notification.Action BuildNavigateAction()
        {
            var intent = PendingIntent.GetBroadcast(this, 2, new Intent(Intent.ActionCloseSystemDialogs).PutExtra(ForegroundServiceAction.NavigateLat, latitude).PutExtra(ForegroundServiceAction.NavigateLot, longitude), PendingIntentFlags.CancelCurrent);
            return new Notification.Action(0, "נווט למניין", intent);
        }

        private Notification.Action BuildStopServiceAction()
        {
            var intent = PendingIntent.GetBroadcast(this, 3, new Intent(ForegroundServiceAction.Close), PendingIntentFlags.CancelCurrent);
            return new Notification.Action(0, "כיבוי התראה", intent);
        }
    }

    [BroadcastReceiver(Enabled = true)]
    public class ServiceListener : BroadcastReceiver
    {
        public Service service;
        public ServiceListener(ForegroundService service)
        {
            IntentFilter filter = new IntentFilter();
            filter.AddAction(ForegroundServiceAction.Navigate);
            filter.AddAction(Intent.ActionCloseSystemDialogs);
            filter.AddAction(ForegroundServiceAction.Open);
            filter.AddAction(ForegroundServiceAction.Close);
            this.service = service;
            this.service.RegisterReceiver(this, filter);
        }
        public ServiceListener() { }

        public override async void OnReceive(Context context, Intent intent)
        {
            if (intent != null)
            {
                if (intent.Action == Intent.ActionCloseSystemDialogs && intent.HasExtra(ForegroundServiceAction.NavigateLat))
                {
                    double latitude = intent.GetDoubleExtra(ForegroundServiceAction.NavigateLat, 32);
                    double longitude = intent.GetDoubleExtra(ForegroundServiceAction.NavigateLot, 31);
                    try
                    {
                        var options = new MapLaunchOptions { Name = "מניין" };
                        await Map.OpenAsync(new Location(latitude, longitude), options);
                    }
                    catch
                    {
                        ShowMessage("לא ניתן לפתוח את חלון הניווט");
                    }
                }
                else if (intent.Action == ForegroundServiceAction.Open)
                {
                    int id = intent.GetIntExtra(ForegroundServiceAction.Open, 0);
                    try
                    {
                        if (ApplicationIsRunning())
                            Global.OpenMinyanPage(id);
                        else
                            LaunchApplication();
                    }
                    catch { }
                }
                else if (intent.Action == ForegroundServiceAction.Close)
                {
                    (service as ForegroundService).StopForegroundServiceCompat();
                }
            }
        }

        private bool ApplicationIsRunning()
        {
            var info = new ActivityManager.RunningAppProcessInfo();
            ActivityManager.GetMyMemoryState(info);
            return info.Importance != Importance.ForegroundService;
        }

        private void LaunchApplication()
        {
            var pm = Application.Context.PackageManager;
            Intent intent = pm.GetLaunchIntentForPackage(Application.Context.PackageName);
            if (intent != null)
            {
                intent.SetFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
            }
        }

        public void ShowMessage(string text, ToastLength length = ToastLength.Short)
        {
            Toast.MakeText(Application.Context, text, length).Show();
        }
    }
}