using System;
using System.Windows;
using System.Windows.Controls;

namespace ElectricSketch.View
{
    public class PositionContainer
    {
        public static readonly DependencyProperty PositionProperty =
             DependencyProperty.RegisterAttached("Position", typeof(Point), typeof(PositionContainer),
             new PropertyMetadata(default(Point), OnPositionChanged));

        public static void SetPosition(FrameworkElement element, Point value)
        {
            element.SetValue(PositionProperty, value);
        }

        public static Point GetPosition(FrameworkElement element)
        {
            return (Point)element.GetValue(PositionProperty);
        }

        public static readonly DependencyProperty OffsetProperty =
             DependencyProperty.RegisterAttached("Offset", typeof(Point), typeof(PositionContainer),
             new PropertyMetadata(default(Point), OnOffsetChanged));

        public static void SetOffset(FrameworkElement element, Point value)
        {
            element.SetValue(OffsetProperty, value);
        }

        public static Point GetOffset(FrameworkElement element)
        {
            return (Point)element.GetValue(OffsetProperty);
        }


        // If not set, we'll use the first ancestor with a canvas above.
        public static readonly DependencyProperty ContainerProperty =
             DependencyProperty.RegisterAttached("Container", typeof(FrameworkElement), typeof(PositionContainer),
             new PropertyMetadata(default(FrameworkElement), OnContainerChanged));

        public static void SetContainer(FrameworkElement element, FrameworkElement value)
        {
            element.SetValue(ContainerProperty, value);
        }

        public static FrameworkElement GetContainer(FrameworkElement element)
        {
            return (FrameworkElement)element.GetValue(ContainerProperty);
        }


        // We need to watch for the size of the container changing, but at that point we need to know the object
        // that caused all this. So we attach this property to the container.
        public static readonly DependencyProperty SourceProperty =
             DependencyProperty.RegisterAttached("Source", typeof(FrameworkElement), typeof(PositionContainer),
             new FrameworkPropertyMetadata(default(FrameworkElement), FrameworkPropertyMetadataOptions.Inherits));

        public static void SetSource(FrameworkElement element, FrameworkElement value)
        {
            element.SetValue(SourceProperty, value);
        }

        public static FrameworkElement GetSource(FrameworkElement element)
        {
            return (FrameworkElement)element.GetValue(SourceProperty);
        }


        static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Apply((FrameworkElement)d);
        }

        static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Apply((FrameworkElement)d);
        }

        static void OnContainerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (FrameworkElement)d;
            if (e.OldValue is FrameworkElement oldContainer)
            {
                oldContainer.SizeChanged -= OnSizeChanged;
                SetSource(oldContainer, null);
            }
            if (e.NewValue is FrameworkElement newContainer)
            {
                newContainer.SizeChanged += OnSizeChanged;
                SetSource(newContainer, source);
                Apply(source);
            }
        }

        static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Apply(GetSource((FrameworkElement)sender));
        }

        static void Apply(FrameworkElement element)
        {
            var container = GetContainer(element);
            if (container == null)
            {
                container = (FrameworkElement)element.FindAncestorWithParent<Canvas>();
                if (container != null)
                    SetContainer(element, container); // This will cause Apply to be called again if one is found.
                return;
            }

            var offset = GetOffset(element);
            if (container != element)
                offset = element.TranslatePoint(offset, container);
            var position = GetPosition(element);
            position -= (Vector)offset;

            container.SetValue(Canvas.LeftProperty, position.X);
            container.SetValue(Canvas.TopProperty, position.Y);

            Applied?.Invoke(element);
        }

        public static event Action<FrameworkElement> Applied;
    }
}
