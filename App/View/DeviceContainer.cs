using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ElectricSketch.View
{
    public static class DeviceContainer
    {
        public static readonly DependencyProperty DeviceProperty =
             DependencyProperty.RegisterAttached("Device", typeof(ViewModel.Device), typeof(DeviceContainer),
             new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetDevice(FrameworkElement element, ViewModel.Device value)
        {
            element.SetValue(DeviceProperty, value);
        }

        public static ViewModel.Device GetDevice(FrameworkElement element)
        {
            return (ViewModel.Device)element.GetValue(DeviceProperty);
        }


        public static readonly DependencyProperty PinProperty =
             DependencyProperty.RegisterAttached("Pin", typeof(ViewModel.Pin), typeof(DeviceContainer),
             new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetPin(FrameworkElement element, ViewModel.Pin value)
        {
            element.SetValue(PinProperty, value);
        }

        public static ViewModel.Pin GetPin(FrameworkElement element)
        {
            return (ViewModel.Pin)element.GetValue(PinProperty);
        }


        public static readonly DependencyProperty ConnectionProperty =
             DependencyProperty.RegisterAttached("Connection", typeof(ViewModel.Connection), typeof(DeviceContainer),
             new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetConnection(FrameworkElement element, ViewModel.Connection value)
        {
            element.SetValue(ConnectionProperty, value);
        }

        public static ViewModel.Connection GetConnection(FrameworkElement element)
        {
            return (ViewModel.Connection)element.GetValue(ConnectionProperty);
        }
    }
}
