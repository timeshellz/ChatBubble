using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatBubbleClientWPF.Utility
{
    public class MouseOverTransparencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value is double opacity)
            {
                if ((string)parameter == "Hover") return 0.1;
                if ((string)parameter == "Click") return 0.2;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double opacity)
            {
                return 0.0;
            }

            return value;
        }
    }
}
