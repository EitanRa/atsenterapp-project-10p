using System;

namespace atsenterapp
{
    public class NotificationEventArgs : EventArgs
    {
        public string title, content;
        public int checkId;
        public NotificationEventArgs(string t, string c)
        {
            title = t;
            content = c;
        }
        public NotificationEventArgs(string t, string c, long m, int id)
        {
            title = t;
            content = c;
            checkId = id;
        }
    }
}
