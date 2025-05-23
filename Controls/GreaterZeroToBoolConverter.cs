using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Controls
{
    public class GreaterZeroToBoolConverter : IValueConverter
    {
        // возвращает true, если value – целое > 0
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is int count && count > 0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
