using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Windows.Controls;
using System.Windows;

namespace ElectricSketch.View
{
    public sealed class Connection : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property);
            if (pi == null)
                throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Connection()
        {
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is ViewModel.Connection conn))
                return;

            ConnVM = conn;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Canvas = this.FindAncestor<SchematicCanvas>();

            System.Diagnostics.Debug.Assert(ConnVM.Pins.Length == 2);
            for (int i = 0; i < 2; i++)
            {
                WeakEventManager<ViewModel.Pin, ViewModel.PinOffsetChangedEventArgs>.AddHandler(ConnVM.Pins[i], nameof(ViewModel.Pin.OffsetChanged), OnPinOffsetChanged);
                WeakEventManager<ViewModel.Device, ViewModel.DevicePositionChangedEventArgs>.AddHandler(ConnVM.Pins[i].Device, nameof(ViewModel.Device.PositionChanged), OnDevicePositionChanged);
            }
        }

        void OnPinOffsetChanged(object sender, ViewModel.PinOffsetChangedEventArgs args)
        {
            // Do not update the layout if the other pin is yet to move.
            if (ConnVM.Other((ViewModel.Pin)sender).Device.PositionWillChange)
                return;
            ComputeLayout();
        }

        void OnDevicePositionChanged(object sender, ViewModel.DevicePositionChangedEventArgs args)
        {
            // Do not update the layout if the other pin is yet to move.
            if (ConnVM.Other((ViewModel.Device)sender).PositionWillChange)
                return;
            ComputeLayout();
        }

        void ComputeLayout()
        {
            RaisePropertyChanged(nameof(LeftA));
            RaisePropertyChanged(nameof(TopA));
            RaisePropertyChanged(nameof(LeftB));
            RaisePropertyChanged(nameof(TopB));
            RaisePropertyChanged(nameof(MidX));
            RaisePropertyChanged(nameof(MidY));
            RaisePropertyChanged(nameof(Mid));
        }

        public SchematicCanvas Canvas { get; private set; }
        public ViewModel.Connection ConnVM { get; private set; }

        /// <summary>
        /// Position of the ends of the connection.
        /// </summary>
        public double LeftA => ConnVM != null && ConnVM.Pins[0] != null ? (ConnVM.Pins[0].Offset.X + ConnVM.Pins[0].Device.Position.X) : 0;
        public double TopA => ConnVM != null && ConnVM.Pins[0] != null ? (ConnVM.Pins[0].Offset.Y + ConnVM.Pins[0].Device.Position.Y) : 0;
        public double LeftB => ConnVM != null && ConnVM.Pins[1] != null ? (ConnVM.Pins[1].Offset.X + ConnVM.Pins[1].Device.Position.X) : 0;
        public double TopB => ConnVM != null && ConnVM.Pins[1] != null ? (ConnVM.Pins[1].Offset.Y + ConnVM.Pins[1].Device.Position.Y) : 0;

        /// <summary>
        /// Position of the middle of the connection.
        /// </summary>
        public double MidX => (LeftA + LeftB) / 2;
        public double MidY => (TopA + TopB) / 2;
        public System.Windows.Point Mid => new System.Windows.Point(MidX, MidY);
    }
}
