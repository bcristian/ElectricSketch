using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElectricSketch.View
{
    /// <summary>
    /// A control that, given a FrameworkElement contained in a scroll viewer, shows a (zoomed) view of the entire canvas and where the visible portion of it is.
    /// </summary>
    /// <remarks>
    /// The height of the control will be set based on its width and the aspect ratio of the target canvas
    /// </remarks>
    public partial class Minimap : UserControl
    {
        public Minimap()
        {
            InitializeComponent();

            moveThumb.DragDelta += OnMoveThumbDragDelta;
            zoomSlider.ValueChanged += OnZoomSliderValueChanged;
        }

        /// <summary>
        /// The visual we're displaying the mini map for.
        /// </summary>
        public FrameworkElement Target
        {
            get { return (FrameworkElement)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(FrameworkElement), typeof(Minimap), new FrameworkPropertyMetadata(OnTargetChanged));

        static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Minimap)d).OnTargetCanvasChanged((FrameworkElement)e.OldValue, (FrameworkElement)e.NewValue);
        }

        void OnTargetCanvasChanged(FrameworkElement oldTarget, FrameworkElement targetCanvas)
        {
            if (oldTarget != null)
                oldTarget.LayoutUpdated -= OnTargetCanvasLayoutUpdated;

            if (targetCanvas == null)
                return;

            myCanvas.Background = new VisualBrush(targetCanvas) { Stretch = Stretch.Uniform };

            if (targetCanvas.LayoutTransform is not ScaleTransform targetScaler)
            {
                if (targetCanvas.LayoutTransform != Transform.Identity)
                    throw new ArgumentException("The target canvas already has a different layout transform");
                targetCanvas.LayoutTransform = targetScaler = new ScaleTransform();
            }
            else
                UpdateZoomSliderValue(targetScaler.ScaleX);

            targetCanvas.LayoutUpdated += OnTargetCanvasLayoutUpdated;
        }

        void OnTargetCanvasLayoutUpdated(object sender, EventArgs e)
        {
            var targetCanvas = Target;
            if (targetCanvas.ActualWidth == 0 || targetCanvas.ActualHeight == 0)
            {
                myCanvas.Height = 0;
                return;
            }

            if (targetCanvas.LayoutTransform is not ScaleTransform targetScaler)
                return;

            var scrollViewer = targetCanvas.FindAncestor<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var tgtW = targetCanvas.ActualWidth * targetScaler.ScaleX;
            var tgtH = targetCanvas.ActualHeight * targetScaler.ScaleY;

            var ar = tgtH / tgtW;
            var w = myCanvas.ActualWidth;
            var h = myCanvas.Height = w * ar;
            // TODO limit max height/width
            // This approach only shrinks
            //if (!double.IsPositiveInfinity(MaxHeight) && myCanvas.Height > MaxHeight)
            //{
            //    h = myCanvas.Height = MaxHeight;
            //    w = myCanvas.Width = MaxHeight / ar;
            //}

            var sW = w / tgtW;
            var sH = h / tgtH;

            moveThumb.Width = sW * scrollViewer.ViewportWidth;
            moveThumb.Height = sW * scrollViewer.ViewportHeight;

            Canvas.SetLeft(moveThumb, scrollViewer.HorizontalOffset * sW);
            Canvas.SetTop(moveThumb, scrollViewer.VerticalOffset * sH);

            if (targetScaler.ScaleX != zoomSlider.Value / 100)
                UpdateZoomSliderValue(targetScaler.ScaleX);
        }

        void OnMoveThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            var targetCanvas = Target;
            if (targetCanvas == null)
                return;
            if (targetCanvas.LayoutTransform is not ScaleTransform targetScaler)
                return;
            var scrollViewer = targetCanvas.FindAncestor<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var scale = targetCanvas.ActualWidth * targetScaler.ScaleX / myCanvas.ActualWidth;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + e.HorizontalChange * scale);
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + e.VerticalChange * scale);
        }

        void OnZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ignoreSliderChanges || e.OldValue == 0)
                return;

            var targetCanvas = Target;
            if (targetCanvas == null)
                return;
            if (targetCanvas.LayoutTransform is not ScaleTransform targetScaler)
                return;
            var scrollViewer = targetCanvas.FindAncestor<ScrollViewer>();
            if (scrollViewer == null)
                return;

            var r = e.NewValue / e.OldValue;

            targetScaler.ScaleX *= r;
            targetScaler.ScaleY *= r;

            var halfViewH = scrollViewer.ViewportHeight / 2;
            scrollViewer.ScrollToVerticalOffset((scrollViewer.VerticalOffset + halfViewH) * r - halfViewH);

            var halfViewW = scrollViewer.ViewportWidth / 2;
            scrollViewer.ScrollToHorizontalOffset((scrollViewer.HorizontalOffset + halfViewW) * r - halfViewW);
        }

        void UpdateZoomSliderValue(double scale)
        {
            ignoreSliderChanges = true;
            zoomSlider.Value = scale * 100;
            ignoreSliderChanges = false;
        }

        bool ignoreSliderChanges = false;
    }
}
