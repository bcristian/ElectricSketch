using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ElectricSketch.View
{
    public class SelectionAdorner : Adorner
    {
        public SelectionAdorner(SchematicCanvas canvas, Point dragStart) : base(canvas)
        {
            start = dragStart;

            adornerCanvas = new Canvas();
            adornerCanvas.Background = Brushes.Transparent;

            visualChildren = new VisualCollection(this);
            visualChildren.Add(adornerCanvas);

            selShape = new Rectangle();
            selShape.Stroke = Brushes.Navy;
            selShape.StrokeThickness = 1;
            selShape.StrokeDashArray = new DoubleCollection(new double[] { 2 });

            adornerCanvas.Children.Add(selShape);

            wasSelected = new Dictionary<ViewModel.Device, bool>();
            foreach (var dev in canvas.Schematic.Elements.OfType<ViewModel.Device>())
                wasSelected[dev] = dev.IsSelected;
        }

        readonly Point start;
        readonly Rectangle selShape;
        readonly VisualCollection visualChildren;
        readonly Canvas adornerCanvas;
        readonly Dictionary<ViewModel.Device, bool> wasSelected;

        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index) => visualChildren[index];

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (!IsMouseCaptured)
                CaptureMouse();

            e.Handled = true;

            var selRect = new Rect(start, e.GetPosition(this));

            selShape.Width = selRect.Width;
            selShape.Height = selRect.Height;
            Canvas.SetLeft(selShape, selRect.X);
            Canvas.SetTop(selShape, selRect.Y);

            foreach (var kvp in wasSelected)
            {
                var dev = kvp.Key;
                var wasSelected = kvp.Value;

                if (selRect.Contains(dev.Position.X, dev.Position.Y))
                {
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        dev.IsSelected = !wasSelected;
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                        dev.IsSelected = false;
                    else
                        dev.IsSelected = true;
                }
                else
                    dev.IsSelected = wasSelected;
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
                ReleaseMouseCapture();

            if (Parent is AdornerLayer adornerLayer)
                adornerLayer.Remove(this);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            adornerCanvas.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}
