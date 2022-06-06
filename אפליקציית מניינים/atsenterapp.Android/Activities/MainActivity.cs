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
using Android.Views;

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
            Xamarin.FormsMaps.Init(this, savedInstanceState);

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
                    PublishNotification(ea.title, ea.content);
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
                    foregroundService = new MinyanService();
                    Bundle args = new Bundle();
                    args.PutInt("id", minyan.id);
                    args.PutInt("type", (int)minyan.type);
                    args.PutInt("count", minyan.Count);
                    args.PutDouble("latitude", minyan.location.Latitude);
                    args.PutDouble("longitude", minyan.location.Longitude);
                    args.PutString("time", minyan.Time.ToString());
                    
                    string text = "לא יכול לדבר כרגע";
                    bool autoReply = false;
                    if (Global.PropertiesKeyIsNotEmpty("autoreply") && Global.PropertiesKeyIsNotEmpty("autoreplytext"))
                    {
                        text = Xamarin.Forms.Application.Current.Properties["autoreplytext"] as string;
                        autoReply = (Xamarin.Forms.Application.Current.Properties["autoreply"] as string) == "True";
                    }
                    args.PutBoolean("autoreply", autoReply);
                    args.PutString("autoreplytext", text);
                    
                    foregroundService.StartForegroundServiceCompat(args);
                }
            };
            Global.stopForegroundRequest += (s, e) =>
            {
                try
                {
                    if (foregroundService != null)
                        StopService(new Intent(Application.Context, typeof(MinyanService))); // Do not call the ForegroundService.StopForegroundServiceCompat()
                                                                                                 // method - for some reason, it doesn't work!
                    }
                catch { }
            };
            AutoMessageService autoMessageService = new AutoMessageService();
            Global.autoReplyRequest += (s, e) =>
            {
                if (autoMessageService != null)
                {
                    Bundle args = new Bundle();
                    args.PutInt("duration", e);
                    string text = "לא יכול לדבר כרגע";
                    bool autoReply = false;
                    if (Global.PropertiesKeyIsNotEmpty("autoreplytext"))
                    {
                        text = Xamarin.Forms.Application.Current.Properties["autoreplytext"] as string;
                    }
                    args.PutString("message", text);
                    Global.RequestAutoReplyServicePermissions();
                    try
                    {
                        autoMessageService.StartForegroundServiceCompat(args);
                    }
                    catch
                    {
                        Global.PopText("שגיאה בעת הפעלת מענה אוטומטי לשיחות");
                    }
                }
            };
            Global.autoReplyPermissionsRequest += (s, e) =>
            {
                var permissions = new string[]
                {
                    Android.Manifest.Permission.AnswerPhoneCalls,
                    Android.Manifest.Permission.ReadCallLog,
                    Android.Manifest.Permission.SendSms,
                    Android.Manifest.Permission.ReadContacts
                };
                ActivityCompat.RequestPermissions(this, permissions, 123);
            };
            Global.menuRequest += (menu, e) =>
            {
                try
                {
                    TextView anchorView = new TextView(this);
                    anchorView.Layout(e.layL, e.layT, e.layR, e.layB);
                    PopupMenu popupMenu = new PopupMenu(this, anchorView, GravityFlags.Center);
                    int resource = -1;
                    Menu m = e.menu;

                    switch (m)
                    {
                        case Menu.BrowserLinkClick:
                            {
                                resource = Resource.Menu.browser_link_click_menu;
                                break;
                            }
                    }

                    if (resource == -1)
                        return;

                    popupMenu.Inflate(resource);

                    List<IMenuItem> items = new List<IMenuItem>();
                    for (int i = 0; i < popupMenu.Menu.Size(); i++)
                        items.Add(popupMenu.Menu.GetItem(i));

                    popupMenu.MenuItemClick += (s, eItem) =>
                    {
                        e.actions[items.IndexOf(eItem.Item)]();
                    };

                    popupMenu.Show();
                }
                catch (Exception ex)
                {
                    Global.ShowMessage(ex.ToString());
                }
            };

            CreateNotificationChannel();
        }

        private MinyanService foregroundService;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
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

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
        }
    }
}