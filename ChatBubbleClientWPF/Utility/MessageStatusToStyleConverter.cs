using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ChatBubbleClientWPF.Utility
{
    [ValueConversion(typeof(Models.Message.MessageStatus), typeof(Style))]
    public class MessageStatusToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Style))
                throw new InvalidOperationException("The target must be a style");

            switch((Models.Message.MessageStatus)value)
            {
                case Models.Message.MessageStatus.ReceivedNotRead:
                case Models.Message.MessageStatus.ReceivedRead:
                    return Application.Current.Resources["ReceivedMessageBoxStyle"] as Style;
                case Models.Message.MessageStatus.SentRead:
                    return Application.Current.FindResource(typeof(Controls.MessageBox)) as Style;
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

