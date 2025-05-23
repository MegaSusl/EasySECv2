using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Controls
{
    /// <summary>
    /// Converts a bool (batch mode flag) to SelectionMode.Single or SelectionMode.Multiple.
    /// </summary>
    public class BoolToSelectionModeConverter : IValueConverter
    {
        /// <summary>
        /// Converts bool to SelectionMode.
        /// True -> Multiple, False -> Single.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return SelectionMode.Multiple;
            return SelectionMode.Single;
        }

        /// <summary>
        /// Converts SelectionMode back to bool.
        /// Multiple -> True, Single -> False.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SelectionMode mode)
                return mode == SelectionMode.Multiple;
            return false;
        }
    }
}
