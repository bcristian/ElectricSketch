using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace ElectricLib
{
    public interface IDevice
    {
        string Name { get; set; }
        string DefaultNamePrefix { get; }
        ReadOnlyObservableCollection<Pin> Pins { get; }
        internal DeviceSimulation CreateSimulation(Simulation sim, ArraySegment<PinSim> pins);
    }

    public abstract class Device<TSim> : IDevice where TSim : DeviceSimulation
    {
        public Device(string name = null)
        {
            Name = name;
            pins = [];
            Pins = new ReadOnlyObservableCollection<Pin>(pins);
        }

        /// <summary>
        /// Human-readable identifier. Not used by the library.
        /// </summary>
        public virtual string Name { get; set; }

        public override string ToString() => Name;

        /// <summary>
        /// The prefix to use when creating default names for devices of this type.
        /// </summary>
        /// <remarks>
        /// This way works much faster than using an attribute. And you cannot forget to place it :)
        /// </remarks>
        public abstract string DefaultNamePrefix { get; }

        /// <summary>
        /// Pins, connection points.
        /// </summary>
        public ReadOnlyObservableCollection<Pin> Pins { get; }
        protected ObservableCollection<Pin> pins;

        /// <summary>
        /// Called when a simulation is created. The device should return its simulation representation.
        /// The pin simulations are already created, in the same order as the pins on the device.
        /// </summary>
        internal abstract TSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins);

        DeviceSimulation IDevice.CreateSimulation(Simulation sim, ArraySegment<PinSim> pins)
        {
            return CreateSimulation(sim, pins);
        }

        public TSim Sim(Simulation sim) => sim.Device<TSim>(this);
    }


    /// <summary>
    /// A connection point on a device.
    /// </summary>
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

        public Pin(IDevice device, string name = "")
        {
            Device = device;
            Name = name;
        }

        /// <summary>
        /// The device on which the pin belongs.
        /// </summary>
        public IDevice Device { get; }

        /// <summary>
        /// Human-readable identifier. Not used by the library.
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;
                name = value;
                RaisePropertyChanged();
            }
        }
        private string name;


        public override string ToString() => $"{Device}.{Name}";
    }
}
