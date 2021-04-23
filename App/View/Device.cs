using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ElectricSketch.View
{
    /// <summary>
    /// Interface for the controls representing a device schematic.
    /// </summary>
    public interface IDevice
    {
        ViewModel.Device DeviceVM { get; }
    }

    /// <summary>
    /// Use this for devices with fixed pin configurations.
    /// </summary>
    public class SimpleDevice : Control, IDevice
    {
        public SimpleDevice()
        {
        }

        public ViewModel.Device DeviceVM => DataContext as ViewModel.Device;
    }

    public class DeviceTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.GetType().Name ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
