using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ElectricSketch.View
{
    // A trick to position pins. Their positions are specified relative to the logical device position, and we need to convert to
    // coordinates relative to the origin of the canvas. That difference is in the OriginOffset property of the device.
    // Multi binding and a converter would be enough, but it would be written twice, once for Top and once for Left.
    public class PinPosition : IMultiValueConverter
    {
        public static readonly DependencyProperty OffsetProperty =
             DependencyProperty.RegisterAttached("Offset", typeof(Point), typeof(PinPosition),
             new PropertyMetadata(new Point(), OnOffsetChanged));

        public static void SetOffset(DependencyObject element, Point value)
        {
            element.SetValue(OffsetProperty, value);
        }

        public static Point GetOffset(DependencyObject element)
        {
            return (Point)element.GetValue(OffsetProperty);
        }

        static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var offset = GetOffset(d);
            var ui = (UIElement)d;
            Canvas.SetLeft(ui, offset.X);
            Canvas.SetTop(ui, offset.Y);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new Point();
            foreach (var v in values)
            {
                if (v is Point p)
                    result += (Vector)p;
                else if (v is System.Drawing.Point dp)
                    result += (Vector)Converters.PointConverter.Convert(dp);
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
