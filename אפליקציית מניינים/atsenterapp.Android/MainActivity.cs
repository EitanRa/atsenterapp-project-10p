using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Widget;
using Android.Support.V4.App;
using Android.Content;
using System.Collections.Generic;
//using Plugin.LocalNotifications;

namespace atsenterapp.Droid
{
    // To change application name, go to SplashActivity.cs
    [Activity(Label = "אצנטר", Icon = "@drawable/launcher_foreground", LaunchMode = LaunchMode.SingleInstance, Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "atsenter.app", DataPathPrefix = "/join")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static List<MainActivity> instances = new List<MainActivity>();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (Resources.Configuration.Locale.DisplayLanguage == "عربيه")
                System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
            try
            {
                Context context = this;
                PackageManager manager = context.PackageManager;
                PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);
                Global.MainPage.appVersion = info.VersionName;
            }
            catch { }
            try
            {
                var id = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
                (App.Current.MainPage as MainPage).DeviceId = id;
            }
            catch
            {
                (App.Current.MainPage as MainPage).LoadItems();
            }

            Global.IdRequest += (sender, e) =>
            {
                try
                {
                    var id = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
                    (sender as MinyanEditPage).deviceId = id;
                }
                catch { }
            };

            bool questionEnabled = true; // After a question is been appeared, we set a timer of 1 second until a new question can be appeared
            Global.QuestionRequest += (sender, e) =>
            {
                if (questionEnabled)
                {
                    try
                    {
                        questionEnabled = false;
                        Xamarin.Forms.Device.StartTimer(TimeSpan.FromSeconds(1), () => { questionEnabled = true; return false; });
                        TaskCompletionSource<bool> taskCompletionSource;
                        taskCompletionSource = new TaskCompletionSource<bool>();

                        AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                        AlertDialog alert = dialog.Create();
                        alert.SetTitle(e.title);
                        alert.SetMessage(e.question);
                        alert.SetButton(e.ok, (c, ev) =>
                        {
                            e.okAction.Invoke();
                            taskCompletionSource.SetResult(true);
                        });
                        alert.SetButton2(e.cancel, (c, ev) =>
                        {
                            taskCompletionSource.SetResult(false);
                        });
                        alert.Show();
                    }
                    catch { }
                }
            };
            Global.PopTextRequest += (sender, text) =>
            {
                try
                {
                    Toast.MakeText(this, text, ToastLength.Short).Show();
                }
                catch { }
            };
            Global.notificationHandler += (sender, ea) =>
            {
                try
                {
                    if (ea.millis == 0)
                        PublishNotification(ea.title, ea.content);
                    else
                        ScheduleNotification(ea.title, ea.content, ea.millis, ea.checkId);
                }
                catch { }
            };
            Global.PackageNameRequest += (o, e) =>
            {
                if (e != null)
                {
                    try
                    {
                        e.Value = Application.Context.PackageName;
                    }
                    catch
                    {
                        e.Value = "com.atsenter.atsenterapp";
                    }
                }
            };
            Global.closeAppRequest += (s, e) =>
            {
                System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
            };
            Global.foregroundRequest += (s, e) =>
            {
                if (foregroundService == null)
                {
                    Minyan minyan = e.Value;
                    foregroundService = new ForegroundService();
                    Bundle args = new Bundle();
                    args.PutInt("id", minyan.id);
                    args.PutInt("type", (int)minyan.type);
                    args.PutInt("count", minyan.Count);
                    args.PutDouble("latitude", minyan.location.Latitude);
                    args.PutDouble("longitude", minyan.location.Longitude);
                    args.PutString("time", minyan.Time.ToString());
                    foregroundService.StartForegroundServiceCompat(args);
                }
            };
            Global.stopForegroundRequest += (s, e) =>
            {
                try
                {
                    if (foregroundService != null)
                        StopService(new Intent(Application.Context, typeof(ForegroundService))); // No calling the ForegroundService.StopForegroundServiceCompat()
                                                                                                 // method - for some reason, that doesn't work!
                }
                catch { }
            };

            CreateNotificationChannel();
        }

        private ForegroundService foregroundService;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private readonly string NOTIFICATION_CHANNEL_ID = "atsenter0";
        private void CreateNotificationChannel()
        {
            try
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                {
                    // Notification channels are new in API 26 (and not a part of the
                    // support library). There is no need to create a notification
                    // channel on older versions of Android.
                    return;
                }

                var channelName = "אצנטר - ראשי";
                var channelDescription = "התראות כלליות";
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, channelName, NotificationImportance.Default)
                {
                    Description = channelDescription
                };
                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
            catch { }
        }

        private int notId = 1338;
        private void PublishNotification(string title, string content)
        {
            //CrossLocalNotifications.Current.Show(title, content, notId++);

            // Set up an intent so that tapping the notifications returns to this app:
            Intent intent = new Intent(this, typeof(MainActivity));

            // Create a PendingIntent; we're only using one PendingIntent (ID = 0):
            const int pendingIntentId = 0;
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, pendingIntentId, intent, PendingIntentFlags.OneShot);
            // Instantiate the builder and set notification elements:
            NotificationCompat.Builder builder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentIntent(pendingIntent)
                .SetContentTitle(title)
                .SetContentText(content)
                .SetSmallIcon(Resource.Drawable.icon);

            // Build the notification:
            Notification notification = builder.Build();
            notification.Flags = NotificationFlags.AutoCancel;

            // Get the notification manager:
            NotificationManager notificationManager = GetSystemService(NotificationService) as NotificationManager;

            // Publish the notification:
            notificationManager.Notify(notId++, notification);

        }
        private void ScheduleNotification(string title, string content, long millis, int checkId)
        {
            //CrossLocalNotifications.Current.Show(title, content, notId++, DateTime.Now.AddSeconds(10));
            /*
            var alarmIntent = new Intent(this, typeof(AlarmReceiver));
            alarmIntent.PutExtra("title", title);
            alarmIntent.PutExtra("message", content);
            alarmIntent.PutExtra("channel", NOTIFICATION_CHANNEL_ID);
            alarmIntent.PutExtra("notId", notId++);
            alarmIntent.PutExtra("checkId", checkId);

            PendingIntent pendingIntent = PendingIntent.GetBroadcast(Application.Context, 0, alarmIntent, PendingIntentFlags.UpdateCurrent);
            AlarmManager alarmManager = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
            alarmManager.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + millis, pendingIntent);*/
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
        }
    }


    /*[BroadcastReceiver]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                using (System.Net.WebClient web = new System.Net.WebClient())
                {
                    string checkId = intent.GetIntExtra("checkId", -1).ToString();
                    if (checkId != null && checkId != "" && checkId != "-1")
                    {
                        string resp = web.DownloadString(Global.serverUrl + "mlist/cfu?id=" + checkId);
                        if (resp == "not exists")
                            return;
                        else
                        {
                            int parsePoint = resp.IndexOf(':') - 2;
                            if (parsePoint != -1)
                            {
                                try
                                {
                                    TimeSpan time = TimeSpan.Parse(resp.Substring(parsePoint, 5));
                                    TimeSpan now = DateTime.Now.TimeOfDay;
                                    if (time.Hours != now.Hours || time.Minutes != now.Minutes)
                                    {
                                        string day = "";
                                        if (time.Hours < now.Hours || (time.Hours == now.Hours && time.Minutes < now.Minutes))
                                            day = "מחר ב";
                                        long millis = Global.millisUntilTime(now.Hours, now.Minutes, time.Hours, time.Minutes) - (15 * 60000);
                                        if (millis <= 0)
                                        {
                                            millis = 60000;
                                        }
                                        Global.PublishNotification("יש לך מניין בקרוב", "תזכורת: יש לך מניין בשעה " + time.ToString().Remove(5), millis, Convert.ToInt32(checkId));
                                        Global.PublishNotification("המניין שלך נדחה", "המניין שלך נדחה ל" + day + "שעה " + time.ToString().Remove(5));
                                        return;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch { }
            string message = intent.GetStringExtra("message");
            string title = intent.GetStringExtra("title");
            int id = intent.GetIntExtra("notId", defaultValue: 1337);

            Intent resultIntent = new Intent(context, typeof(MainActivity));
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            PendingIntent pending = PendingIntent.GetActivity(context, 0,
                resultIntent,
                PendingIntentFlags.CancelCurrent);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(context, intent.GetStringExtra("channel"))
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetSmallIcon(Resource.Drawable.icon)
                    .SetAutoCancel(true);

            builder.SetContentIntent(pending);

            Notification notification = builder.Build();

            NotificationManager manager = NotificationManager.FromContext(context);
            manager.Notify(id, notification);
        }

    }*/
}