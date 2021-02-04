using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ChatBubbleClientWPF.Models
{
    public class Notification
    {
        public EventHandler<EventArgs> NotificationTimedOut;

        public enum NotificationType { NewMessage, Error}
        public NotificationType Type { get; private set; }
        public string Content { get; private set; }

        Timer timer;

        public Notification(NotificationType type, string content)
        {
            Type = type;
            Content = content;

            timer = new Timer(5000);
            timer.Elapsed += NotificationTimeout;

            timer.Start();
        }

        void NotificationTimeout(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Elapsed -= NotificationTimeout;

            NotificationTimedOut?.Invoke(this, e);
        }
    }
}
