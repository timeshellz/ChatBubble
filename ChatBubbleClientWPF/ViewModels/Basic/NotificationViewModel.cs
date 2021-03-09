using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ChatBubble;

namespace ChatBubbleClientWPF.ViewModels.Basic
{
    class NotificationViewModel : BaseViewModel
    {
        string header;
        string content;
        Models.Notification.NotificationType notificationType;

        public Models.Notification NotificationModel { get; private set; }

        public string Header
        {
            get { return header; }
            set
            {
                header = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get { return content; }
            set
            {
                content = value;
                OnPropertyChanged();
            }
        }

        public Models.Notification.NotificationType NotificationType
        {
            get { return notificationType; }
            set
            {
                notificationType = value;
                OnPropertyChanged();
            }
        }

        public NotificationViewModel(Models.Notification notification)
        {
            Content = notification.Content;
            NotificationType = notification.Type;

            switch(notification.Type)
            {
                case Models.Notification.NotificationType.Error:
                    Header = "Error";
                    break;
                case Models.Notification.NotificationType.NewMessage:
                    Header = "New Message";
                    break;
            }

            NotificationModel = notification;
        }
    }
}
