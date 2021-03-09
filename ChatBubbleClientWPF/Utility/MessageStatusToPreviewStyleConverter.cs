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
    public class MessageStatusToPreviewStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Style))
                throw new InvalidOperationException("The target must be a style");

            if((Message.MessageStatus)value == Message.MessageStatus.ReceivedNotRead)
            {
                return Application.Current.Resources["AuxillaryRoundedButtonStyle"] as Style;
            }

            return Application.Current.Resources["SecondaryRoundedButtonStyle"] as Style;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
