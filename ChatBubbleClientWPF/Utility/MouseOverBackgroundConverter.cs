using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatBubbleClientWPF.Utility
{
    public class MouseOverBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value is SolidColorBrush brush)
            {
                if((string)parameter == "Hover") return new SolidColorBrush(Color.FromArgb(255, (byte)(brush.Color.R - 15), (byte)(brush.Color.G - 15), (byte)(brush.Color.B - 15)));
                if((string)parameter == "Click") return new SolidColorBrush(Color.FromArgb(255, (byte)(brush.Color.R - 35), (byte)(brush.Color.G - 35), (byte)(brush.Color.B - 35)));
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return new SolidColorBrush(Color.FromArgb(255, (byte)(brush.Color.R + 10), (byte)(brush.Color.G + 10), (byte)(brush.Color.B + 10)));
            }

            return value;
        }
    }
}
