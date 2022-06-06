using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Xamarin.Essentials;
using System.Net;

namespace atsenterapp.Droid
{
    [Service/*(Name = "com.atsenter.atsenterapp.foregroundservice")*/]
    [IntentFilter(new[] { Intent.ActionView })]
    public class ForegroundService : Service
    {
        // This is any integer value unique to the application.
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        public readonly string NOTIFICATION_CHANNEL_ID_STRING = "10000";
        public Tfila type;
        public int count;
        public int id;
        public double latitude, longitude;
        public TimeSpan time;
        private Notification notification;
        public ServiceListener listener;

        public ForegroundService() { }
        
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            try
            {
                id = intent.GetIntExtra("id", 0);
                type = (Tfila)intent.GetIntExtra("type", 0);
                count = intent.GetIntExtra("count", 0);
                latitude = intent.GetDoubleExtra("latitude", 31);
                longitude = intent.GetDoubleExtra("longitude", 31);
                time = TimeSpan.Parse(intent.GetStringExtra("time"));
                listener = new ServiceListener(this);
            } catch { }

            CreateNotificationChannel();
            BuildNotification();

            try
            {
                // Enlist this instance of the service as a foreground service
                StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);

                // Start check-for-update timer
                Xamarin.Forms.Device.StartTimer(TimeSpan.FromSeconds(30), () =>
                {
                    CheckForUpdate();
                    return true;
                });
            }
            catch { }

            return StartCommandResult.Sticky;
        }

        private WebClient web = new WebClient();
        private async void CheckForUpdate()
        {
            try
            {
                string upd = await web.DownloadStringTaskAsync(Global.serverUrl + "mlist/cfu?id=" + id);
                if (upd == "not exists")
                {
                    StopForegroundServiceCompat();
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
                    var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                    BuildNotification();
                    notificationManager.Notify(SERVICE_RUNNING_NOTIFICATION_ID, notification);
                }
            }
            catch { }
        }

        Notification.Builder notBuilder;
        private void BuildNotification()
        {
            if (notBuilder == null)
            {
                notBuilder = new Notification.Builder(this, NOTIFICATION_CHANNEL_ID_STRING)
                .SetSmallIcon(Resource.Drawable.ic_notification5)
                .SetContentIntent(BuildIntentToShowPage())
                .SetOngoing(true)
                .AddAction(BuildNavigateAction())
                .AddAction(BuildStopServiceAction());
            }
            string paramInserter = "{1}";
            string content = count >= 10 ? "✅יש מניין" : string.Format("חסרים {0} מתפללים", 10 - count);

            notBuilder.SetContentTitle(string.Format("המניין שלך ל{0} ב- " + paramInserter, Global.TfilaToString(type), time.ToString().Remove(5)))
                .SetContentText(content);

            notification = notBuilder.Build();
            notification.Flags = NotificationFlags.AutoCancel;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var channelName = "אצנטר - פעילות רקע";
            var channelDescription = "התראות צפות שמציגות מידע מהיר על המניין שאתה מחובר אליו";
            var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID_STRING, channelName, NotificationImportance.Default)
            {
                Description = channelDescription
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        Intent serviceIntent = null;
        public void StartForegroundServiceCompat(Bundle args = null)
        {
            serviceIntent = new Intent(Application.Context, typeof(ForegroundService));
            if (args != null)
            {
                serviceIntent.PutExtras(args);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Application.Context.StartForegroundService(serviceIntent);
            }
            else
            {
                Application.Context.StartService(serviceIntent);
            }
        }

        public void StopForegroundServiceCompat()
        {
            try
            {
                StopService(new Intent(Application.Context, typeof(ForegroundService)));
            }
            catch { }
        }

        public override void OnDestroy()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    StopForeground(StopForegroundFlags.Remove);
                }
                else
                {
                    StopForeground(true);
                }
                StopSelf();
                base.OnDestroy();
            }
            catch { }
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

    public struct ForegroundServiceAction
    {
        public static readonly string Navigate = "navigate";
        public static readonly string NavigateLat = "navigate-lat";
        public static readonly string NavigateLot = "navigate-lot";
        public static readonly string Open = "open";
        public static readonly string Close = "close";
    }
}