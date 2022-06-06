using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Telephony;
using Android.Provider;
using Android.Database;
using Xamarin.Essentials;
using System.Threading;

namespace atsenterapp.Droid
{
    [Service(Process = ":arService")]
    [IntentFilter(new[] { Intent.ActionView })]
    public class AutoMessageService : ForegroundService
    {
        public readonly string OFF_ACTION = "off";

        public int duration = 45;
        public string messageText = "Can't talk right now";
        private bool DetectorIsConnected = false;
        public Timer timer = null;

        public AutoMessageService() : base(typeof(AutoMessageService))
        {
            
        }

        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (intent != null)
            {
                duration = intent.GetIntExtra("duration", 45);
                messageText = intent.GetStringExtra("message");
            }

            IntentFilter filter = new IntentFilter();
            filter.AddAction(OFF_ACTION);
            listener = new AutoReplyServiceListener(this);
            RegisterReceiver(listener, filter);

            BuildNotification("מענה אוטומטי לשיחות מופעל", "הודעת SMS אוטומטית תישלח לאנשים שמתקשרים אליך ", null, new Notification.Action[] { BuildOffAction() }, Resource.Drawable.ic_autoreply);
            if (!DetectorIsConnected)
            {
                DetectorIsConnected = true;
                PhoneCallDetector phoneCallDetector = new PhoneCallDetector();

                var tm = (TelephonyManager)base.GetSystemService(TelephonyService);
                tm.Listen(phoneCallDetector, PhoneStateListenerFlags.CallState);

                phoneCallDetector.OnIncomingCall += (sender, e) =>
                {
                    if (SendSms(messageText, e))
                    {
                        Action error = () => PublishNotification("שגיאה", "לא ניתן לנתק את השיחה הנכנסת");
                        try
                        {
                            if (!phoneCallDetector.EndCall())
                                error();
                            else
                            {
                                try
                                {
                                    string callerName = FindCallerName(this, e);
                                    if (callerName != null)
                                        e = callerName;
                                }
                                catch { }
                                PublishNotification(e + " חיפש/ה אותך", "המערכת שלחה הודעת מענה אוטומטית");
                            }
                        }
                        catch
                        {
                            error();
                        }
                    }
                };
            }

            timer = new Timer(new TimerCallback((object obj) => { try { timer.Dispose(); } catch { } StopForegroundServiceCompat(); }), null, duration * 60000, 10000);
            return base.OnStartCommand(intent, flags, startId);
        }

        SmsManager smsManager = SmsManager.Default;
        public bool SendSms(string msgText, string recipient)
        {
            try
            {
                smsManager.SendTextMessage(recipient, null, msgText, null, null);
                return true;
            }
            catch (FeatureNotSupportedException ex)
            {
                PublishNotification("לא ניתן היה לשלוח מענה אוטומטי", "מכשיר זה אינו תומך בשירותי SMS");
                return false;
            }
            catch
            {
                PublishNotification("שגיאה", "לא ניתן היה לשלוח מענה אוטומטי");
                return false;
            }
        }

        private string FindCallerName(Context context, string number)
        {
            var uri = ContactsContract.CommonDataKinds.Phone.ContentUri;

            string[] projection = {
                ContactsContract.Contacts.InterfaceConsts.DisplayName,
                ContactsContract.CommonDataKinds.Phone.Number,
            };

            var loader = new CursorLoader(context, uri, projection, null, null, null);
            var cursor = (ICursor)loader.LoadInBackground();

            if (cursor.MoveToFirst())
            {
                do
                {
                    var contactNumber = cursor.GetString(cursor.GetColumnIndex(projection[1]));
                    if (PhoneNumberUtils.Compare(contactNumber, number))
                    {
                        var callerName = cursor.GetString(cursor.GetColumnIndex(projection[0]));
                        return callerName;
                    }
                } while (cursor.MoveToNext());
            }
            return null;
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

        private Notification.Action BuildOffAction()
        {
            var intent = PendingIntent.GetBroadcast(this, 3, new Intent(OFF_ACTION), PendingIntentFlags.CancelCurrent);
            return new Notification.Action(0, "כיבוי", intent);
        }
    }

    [BroadcastReceiver(Enabled = true)]
    public class AutoReplyServiceListener : BroadcastReceiver
    {
        public AutoMessageService service;
        public AutoReplyServiceListener(AutoMessageService service)
        {
            this.service = service;
        }

        public AutoReplyServiceListener() { }
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent != null)
            {
                if (intent.Action == service.OFF_ACTION)
                {
                    try
                    {
                        service.timer.Dispose();
                    }
                    catch { }
                    service.StopForegroundServiceCompat();
                }
            }
        }
    }
}