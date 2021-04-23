using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ElectricSketch.View
{
    /// <summary>
    /// Container for handling interaction with a device placed in the schematic view.
    /// </summary>
    public class LibDeviceContainer : ContentControl
    {
        public LibDeviceContainer()
        {
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Device = DataContext as ViewModel.Device;
        }

        ViewModel.Device Device { get; set; }

        // True while the device is being dragged.
        public bool IsHighlighted
        {
            get { return (bool)GetValue(IsHighlightedProperty); }
            set { SetValue(IsHighlightedProperty, value); }
        }

        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(LibDeviceContainer), new PropertyMetadata(false));



        Point? dragStart;

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (MainWindow.Instance.InSimulation)
                return;
            dragStart = e.GetPosition(this);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (MainWindow.Instance.InSimulation)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
                dragStart = null;

            if (dragStart.HasValue)
            {
                var pos = e.GetPosition(this);
                if (Math.Abs(pos.X - dragStart.Value.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(pos.Y - dragStart.Value.Y) >= SystemParameters.MinimumVerticalDragDistance)
                {
                    // Highlight the device.
                    IsHighlighted = true;

                    var dataObj = new DataObject("Electrical Device", Device);
                    DragDrop.DoDragDrop(this, dataObj, DragDropEffects.Copy);

                    // Turn highlight off.
                    IsHighlighted = false;
                }

                e.Handled = true;
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (MainWindow.Instance.InSimulation)
                return;

            IsHighlighted = true;
            MainWindow.Instance.DeviceToPlaceOnClick = Device;
            MainWindow.Instance.DeviceToPlaceOnClickChanged += OnDeviceToPlaceOnClickChanged;
            MainWindow.Instance.StatusText = $"Click into the schematic to place {Device.Name} instances. Esc to exit mode.";
        }

        private void OnDeviceToPlaceOnClickChanged()
        {
            MainWindow.Instance.DeviceToPlaceOnClickChanged -= OnDeviceToPlaceOnClickChanged;
            IsHighlighted = MainWindow.Instance.DeviceToPlaceOnClick == Device;
        }
    }
}
