using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using ChatBubble.SharedAPI;

namespace ChatBubbleClientWPF.Utility
{
    [ValueConversion(typeof(Message.MessageStatus), typeof(Style))]
    public class MessageStatusToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Style))
                throw new InvalidOperationException("The target must be a style");

            switch((Message.MessageStatus)value)
            {
                case Message.MessageStatus.ReceivedNotRead:
                    return Application.Current.Resources["ReceivedUnreadMessageBoxStyle"] as Style;
                case Message.MessageStatus.ReceivedRead:
                    return Application.Current.Resources["ReceivedReadMessageBoxStyle"] as Style;
                case Message.MessageStatus.SentRead:
                    return Application.Current.FindResource(typeof(Controls.MessageBox)) as Style;
                case Message.MessageStatus.SendFailed:
                    return Application.Current.Resources["FailedMessageBoxStyle"] as Style;
                case Message.MessageStatus.SentReceived:
                    return Application.Current.Resources["SentUnreadMessageBoxStyle"] as Style;
            }

            return Application.Current.FindResource(typeof(Controls.MessageBox)) as Style;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

