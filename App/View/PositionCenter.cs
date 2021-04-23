using System.Windows;
using System.Windows.Controls;

namespace ElectricSketch.View
{
    public class PositionCenter
    {
        public static readonly DependencyProperty PositionProperty =
             DependencyProperty.RegisterAttached("Position", typeof(Point), typeof(PositionCenter),
             new PropertyMetadata(default(Point), OnPositionChanged));

        public static void SetPosition(FrameworkElement element, Point value)
        {
            element.SetValue(PositionProperty, value);
        }

        public static Point GetPosition(FrameworkElement element)
        {
            return (Point)element.GetValue(PositionProperty);
        }

        static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fe = (FrameworkElement)d;
            fe.SizeChanged -= OnSizeChanged;
            fe.SizeChanged += OnSizeChanged;
            Apply(fe);
        }

        static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Apply((FrameworkElement)sender);
        }

        static void Apply(FrameworkElement element)
        {
            var position = GetPosition(element);
            position.X -= element.ActualWidth / 2;
            position.Y -= element.ActualHeight / 2;

            element.SetValue(Canvas.LeftProperty, position.X);
            element.SetValue(Canvas.TopProperty, position.Y);
        }
    }
}
