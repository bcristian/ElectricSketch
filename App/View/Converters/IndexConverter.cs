using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ElectricSketch.View.Converters
{
    /// <summary>
    /// Converts from object in list to its index and back. The list must be passed in the parameter.
    /// </summary>
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not IList list)
                return -1;
            return list.IndexOf(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not IList list)
                return null;
            return list[System.Convert.ToInt32(value)];
        }
    }
}
