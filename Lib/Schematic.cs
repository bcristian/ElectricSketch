using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;

namespace ElectricLib
{
    /// <summary>
    /// Model of an electrical circuit. It consists of connected devices (e.g. power sources, switches, relays, motors, etc.).
    /// The devices have pins that can be connected together.
    /// Use the <see cref="Simulation"/> to simulate the functionality of the circuit.
    /// </summary>
    public class Schematic
    {
        /// <summary>
        /// Devices in the circuit.
        /// </summary>
        public ReadOnlyCollection<IDevice> Devices { get; }
        protected List<IDevice> devices;

        /// <summary>
        /// Connections between pins.
        /// </summary>
        public ReadOnlyCollection<Connection> Connections { get; }
        protected List<Connection> connections;

        public bool IsConnected(Pin pin) => Connections.Any(c => c.Pins.Contains(pin));

        public Connection ConnectedTo(Pin pin) => Connections.FirstOrDefault(c => c.pins.Contains(pin));


        public Schematic()
        {
            devices = [];
            Devices = devices.AsReadOnly();

            connections = [];
            Connections = connections.AsReadOnly();
        }

        /// <summary>
        /// Adds a device to the circuit. If useDefaultName is true and the device does not have a name, one is created from its type and number of already existing 
        /// </summary>
        public TDevice AddDevice<TDevice>(TDevice device, bool useDefaultName = true) where TDevice : IDevice
        {
            if (devices.Contains(device))
                throw new ArgumentException("Device already present in the schematic");

            devices.Add(device);

            if (useDefaultName && device.Name == null)
                device.Name = $"{device.DefaultNamePrefix}{devices.Count(d => d.DefaultNamePrefix == device.DefaultNamePrefix)}";

            return device;
        }

        /// <summary>
        /// Removes a device from the circuit.
        /// </summary>
        /// <param name="device"></param>
        public void RemoveDevice(IDevice device)
        {
            if (!devices.Remove(device))
                throw new ArgumentException("Device not in the circuit");

            // Disconnect all pins.
            foreach (var pin in device.Pins)
                Disconnect(pin);
        }

        /// <summary>
        /// Connects the specified pins together.
        /// This will merge all connections the pins are currently part of together.
        /// Say we have pins A and B connected, and pins C and D connected. If we connect pins A and D, we'll now have a single connection, containing all 4 pins.
        /// </summary>
        public void Connect(params Pin[] pins)
        {
            if (pins.Length < 2)
                throw new ArgumentException("At least 2 pins are needed to form a connection");

            List<Connection> connectionsToMerge = [];
            foreach (var pin in pins)
            {
                if (pin.Device == null)
                    throw new ArgumentException("pins must belong to a device");
                if (!Devices.Contains(pin.Device))
                    throw new ArgumentException("the devices must be part of the schematic");

                var c = ConnectedTo(pin);
                if (c != null)
                    connectionsToMerge.Add(c);
            }

            if (connectionsToMerge.Count == 1)
            {
                // If there is exactly one existing connection involved, just add to it.
                connectionsToMerge[0].pins.UnionWith(pins);
            }
            else
            {
                foreach (var c in connectionsToMerge)
                    connections.Remove(c);

                var newConnection = new HashSet<Pin>();
                foreach (var c in connectionsToMerge)
                    newConnection.UnionWith(c.Pins);
                newConnection.UnionWith(pins);
                connections.Add(new Connection(newConnection));
            }
        }

        /// <summary>
        /// Disconnects the specified pin.
        /// </summary>
        public void Disconnect(Pin pin)
        {
            var conn = ConnectedTo(pin);
            if (conn == null)
                return;
            if (conn.pins.Count == 1)
                connections.Remove(conn);
            else
                conn.pins.Remove(pin);
        }
    }

    /// <summary>
    /// A connection in the circuit. It lists (in no particular order) the pins that are connected together.
    /// </summary>
    public class Connection
    {
        public Connection()
        {
            pins = [];
            Pins = pins.AsReadOnly();
        }

        internal Connection(HashSet<Pin> pins)
        {
            this.pins = pins;
            Pins = pins.AsReadOnly();
        }

        internal HashSet<Pin> pins;

        public ReadOnlySet<Pin> Pins { get; }
    }
}
