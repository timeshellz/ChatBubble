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
    [ValueConversion(typeof(Message.MessageStatus), typeof(Visibility))]
    public class MessageStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be visibility");

            if ((Message.MessageStatus)value == Message.MessageStatus.SentReceived)
                return Visibility.Visible;

            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
