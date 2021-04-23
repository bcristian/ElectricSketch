using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ElectricSketch.View.Converters
{
    /// <summary>
    /// Converts between <see cref="System.Drawing.Point"/> and <see cref="System.Windows.Point"/>
    /// </summary>
    public class PointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Convert((System.Drawing.Point)value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Convert((System.Windows.Point)value);

        public static System.Windows.Point Convert(System.Drawing.Point p) => new System.Windows.Point(p.X, p.Y);
        public static System.Drawing.Point Convert(System.Windows.Point p) => new System.Drawing.Point((int)Math.Round(p.X, MidpointRounding.ToZero), (int)Math.Round(p.Y, MidpointRounding.ToZero));
    }

    /// <summary>
    /// Converts between <see cref="System.Drawing.Point"/> and <see cref="System.Windows.Point"/>, applying a linear conversion f(x) = a0 + a1 * x + a2 * x^2 ...
    /// The arguments are passed in the parameter, as an array or a comma-separated string of numbers a0x, a0y, a1x, a1y, ...
    /// </summary>
    public class PointConverterLinear : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double[] args;
            if (parameter is Array arr)
            {
                args = new double[arr.Length];
                for (int i = 0; i < args.Length; i++)
                    args[i] = System.Convert.ToDouble(arr.GetValue(i));
            }
            else
            {
                var s = System.Convert.ToString(parameter);
                var tokens = s.Split(',');
                args = new double[tokens.Length];
                for (int i = 0; i < args.Length; i++)
                    args[i] = System.Convert.ToDouble(tokens[i]);
            }

            if (args.Length == 0)
                return new System.Windows.Point();
            if (args.Length == 1)
                return new System.Windows.Point(args[0], 0);

            var result = new System.Windows.Point(args[0], args[1]);
            var dp = (System.Drawing.Point)value;
            var p = new System.Windows.Point(dp.X, dp.Y);
            var pp = p;
            for (int i = 1, e = args.Length / 2; i < e; i++)
            {
                result = new System.Windows.Point(result.X + pp.X * args[2 * i + 0], result.Y + pp.Y * args[2 * i + 1]);
                pp = new System.Windows.Point(pp.X * p.X, pp.Y * p.Y);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
