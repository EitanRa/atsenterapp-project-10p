using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System;
using Android.Telephony;
using Android.Telecom;

namespace atsenterapp.Droid
{
    public class PhoneCallDetector : PhoneStateListener
    {
        public event EventHandler<string> OnIncomingCall;
        public override void OnCallStateChanged([GeneratedEnum] Android.Telephony.CallState state, string phoneNumber)
        {
            if (state == Android.Telephony.CallState.Ringing)
            {
                var handler = OnIncomingCall;
                handler?.Invoke(this, phoneNumber);
            }
            base.OnCallStateChanged(state, phoneNumber);
        }

        public bool EndCall()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                TelecomManager tm = (TelecomManager)Application.Context.GetSystemService(Context.TelecomService);
                if (tm != null)
                {
                    bool success = tm.EndCall();
                    return success;
                }
            }
            return false;
        }
    }
}