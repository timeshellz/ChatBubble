using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ChatBubbleClientWPF.Utility
{
    [ValueConversion(typeof(Models.Notification.NotificationType), typeof(Style))]
    public class NotificationTypeToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Style))
                throw new InvalidOperationException("The target must be a style");

            switch ((Models.Notification.NotificationType)value)
            {
                case Models.Notification.NotificationType.NewMessage:
                    return Application.Current.FindResource(typeof(Controls.NotificationTile)) as Style;
                case Models.Notification.NotificationType.Error:
                    return Application.Current.Resources["ErrorNotificationStyle"] as Style;
            }

            return Application.Current.FindResource(typeof(Controls.NotificationTile)) as Style;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

