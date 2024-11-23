using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RequestBuilder.Converters
{
    public class DoubleAdjustmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dValue = (double)value;
            var adjustment = 0d;
            if (parameter != null && parameter.GetType() == typeof(string))
            {
                adjustment = ((string)parameter).AsDouble(0);
            }
            else if (parameter != null && parameter.GetType() == typeof(double))
            {
                adjustment = (double)parameter;
            }
            return dValue - adjustment;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
