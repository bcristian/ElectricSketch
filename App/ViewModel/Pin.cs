using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.ComponentModel;

namespace ElectricSketch.ViewModel
{
    public sealed class Pin : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property) ?? throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Pin(Device device, ElectricLib.Pin functional)
        {
            Device = device;
            Functional = functional;
            functional.PropertyChanged += OnFunctionalPropertyChanged;
        }

        private void OnFunctionalPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ElectricLib.Pin.Name))
                RaisePropertyChanged(nameof(Name));
        }

        public Device Device { get; }
        public ElectricLib.Pin Functional { get; }
        public int Index => Device.Pins.IndexOf(this);

        /// <summary>
        /// A human-readable way to identify a pin.
        /// </summary>
        /// <remarks>
        /// Not necessarily unique. Might be null or empty.
        /// </remarks>
        public string Name
        {
            get => Functional.Name;
            set => Functional.Name = value;
        }

        /// <summary>
        /// Offset between the device's position and the pin's position.
        /// </summary>
        public Point Offset
        {
            get => offset;
            set
            {
                if (offset == value)
                    return;

                var oldOffset = offset;
                offset = value;
                OffsetChanged?.Invoke(this, new PinOffsetChangedEventArgs() { oldValue = oldOffset, newValue = offset });
                RaisePropertyChanged();
            }
        }
        Point offset;

        public Point Position => Device.Position + (Size)Offset;

        /// <summary>
        /// Raised when the offset has changed. Arguments are the pin, the old offset, and the new one.
        /// </summary>
        public event EventHandler<PinOffsetChangedEventArgs> OffsetChanged;

        public override string ToString() => Name == null ? Device.ToString() ?? "null" : $"{Device}.{Name}";
    }

    public class PinOffsetChangedEventArgs : EventArgs
    {
        public Point oldValue;
        public Point newValue;
    }
}
