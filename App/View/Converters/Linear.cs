using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ElectricSketch.View.Converters
{
    /// <summary>
    /// Implements a linear conversion f(x) = a0 + a1 * x + a2 * x^2 ...
    /// The arguments are passed in the parameter, as an array or a comma-separated string of numbers.
    /// </summary>
    public class LinearConverter : IValueConverter
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
                return 0d;

            var result = args[0];
            var x = System.Convert.ToDouble(value);
            var xp = x;
            for (int i = 1; i < args.Length; i++)
            {
                result += xp * args[i];
                xp *= x;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    /// <summary>
    /// Implements a linear conversions f(x, y) = a0 + a1 * x + a2 * y + a3 * x * y.
    /// x is the first value, y is the second.
    /// The arguments are passed in the parameter, as an array or a comma-separated string of numbers. Missing values are assumed to be zero.
    /// </summary>
    public class LinearXYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                return 0d;

            var a0 = args[0];
            var a1 = args.Length > 1 ? args[1] : 0;
            var a2 = args.Length > 2 ? args[2] : 0;
            var a3 = args.Length > 3 ? args[3] : 0;

            var x = System.Convert.ToDouble(values[0]);
            var y = System.Convert.ToDouble(values[1]);

            return a0 + a1 * x + a2 * y + a3 * x * y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    /// <summary>
    /// Implements a linear conversions f(x, y, z) = a0 + a1 * x + a2 * y + a3 * z + a4 * x * y + a5 * y * z + a6 * z * x + a7 * x * y * z
    /// x is the first value, y is the second, z is the third.
    /// The arguments are passed in the parameter, as an array or a comma-separated string of numbers. Missing values are assumed to be zero.
    /// </summary>
    public class LinearXYZConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                return 0d;

            var a0 = args[0];
            var a1 = args.Length > 1 ? args[1] : 0;
            var a2 = args.Length > 2 ? args[2] : 0;
            var a3 = args.Length > 3 ? args[3] : 0;
            var a4 = args.Length > 4 ? args[4] : 0;
            var a5 = args.Length > 5 ? args[5] : 0;
            var a6 = args.Length > 6 ? args[6] : 0;
            var a7 = args.Length > 7 ? args[7] : 0;

            var x = System.Convert.ToDouble(values[0]);
            var y = System.Convert.ToDouble(values[1]);
            var z = System.Convert.ToDouble(values[2]);

            return a0 + a1 * x + a2 * y + a3 * z + a4 * x * y + a5 * y * z + a6 * z * x + a7 * x * y * z;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    /// <summary>
    /// Implements a choice of linear conversions f(x) = a0 + a1 * x + a2 * x^2 ... where there are two choices for the values of a[i].
    /// x is the first value, the selector is the second. The selector must be convertible to boolean.
    /// The arguments are passed in the parameter, as an array or a comma-separated string of numbers. The first half are the values used when the
    /// selector is false, the second half are used when the selector is true.
    /// </summary>
    public class LinearChoiceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
                return 0d;

            var selector = System.Convert.ToBoolean(values[1]);
            var firstArg = selector ? args.Length / 2 : 0;

            var result = args[firstArg];
            var x = System.Convert.ToDouble(values[0]);
            var xp = x;
            for (int i = 1; i < args.Length / 2; i++)
            {
                result += xp * args[firstArg + i];
                xp *= x;
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
