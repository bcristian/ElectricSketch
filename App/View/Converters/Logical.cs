using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ElectricSketch.View.Converters
{
    /// <summary>
    /// A logical not converter
    /// </summary>
    public class LogicalNot : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !System.Convert.ToBoolean(value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !System.Convert.ToBoolean(value);
    }
}
