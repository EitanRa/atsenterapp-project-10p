using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.App;

namespace atsenterapp.Droid
{
    [IntentFilter(new[] { Intent.ActionView })]
    public class ForegroundService : Service
    {
        private static int serviceIndex = 1000;
        public int SERVICE_RUNNING_NOTIFICATION_ID = serviceIndex;
        public string NOTIFICATION_CHANNEL_ID_STRING = (serviceIndex++).ToString();
        protected BroadcastReceiver listener;

        public ForegroundService(Type type)
        {
            this.type = type;
        }

        public ForegroundService() { } // Must provide a public default constructor

        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
            return StartCommandResult.Sticky;
        }

        protected void PopText(string text, ToastLength length = ToastLength.Short)
        {
            Toast.MakeText(this, text, length).Show();
        }

        private int notId = 1500;
        protected void PublishNotification(string title, string content)
        {
            // Set up an intent so that tapping the notifications returns to this app:
            Intent intent = new Intent(this, typeof(MainActivity));

            // Create a PendingIntent; we're only using one PendingIntent (ID = 0):
            const int pendingIntentId = 0;
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, pendingIntentId, intent, PendingIntentFlags.OneShot);

            // Instantiate the builder and set notification elements:
            NotificationCompat.Builder builder = new NotificationCompat.Builder(this, "atsenter0")
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

        protected Notification.Builder notBuilder;
        protected Notification notification = new Notification();
        protected void BuildNotification(string title, string content, PendingIntent intent = null, Notification.Action[] actions = null, int icon = Resource.Drawable.icon)
        {
            if (notBuilder == null)
            {
                notBuilder = new Notification.Builder(Application.Context, NOTIFICATION_CHANNEL_ID_STRING)
                .SetSmallIcon(icon)
                .SetOngoing(true);
                if (intent != null)
                    notBuilder.SetContentIntent(intent);
                if (actions != null)
                {
                    foreach (Notification.Action action in actions)
                    {
                        notBuilder.AddAction(action);
                    }
                }
            }
            notBuilder.SetContentTitle(title).SetContentText(content);

            notification = notBuilder.Build();
            notification.Flags = NotificationFlags.AutoCancel;
        }

        protected void NotifyNotification()
        {
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.Notify(SERVICE_RUNNING_NOTIFICATION_ID, notification);
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
        private Type type;
        public void StartForegroundServiceCompat(Bundle args = null, Context context = null)
        {
            if (context == null)
                context = Application.Context;
            serviceIntent = new Intent(context, type);
            if (args != null)
            {
                serviceIntent.PutExtras(args);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(serviceIntent);
            }
            else
            {
                context.StartService(serviceIntent);
            }
        }

        public void StopForegroundServiceCompat()
        {
            try
            {
                StopService(new Intent(Application.Context, type));
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
    }
}