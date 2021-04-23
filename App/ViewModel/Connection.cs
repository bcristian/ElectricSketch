using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Linq;

namespace ElectricSketch.ViewModel
{
    // In this context, a connection is a single line between two pins.

    // We want a clean schematic here, not a rat's nest.
    // That means horizontal and vertical lines that go around the components and that the user can move.
    // So we add anonymous, automatically created junctions where the connections would have corners.

    public sealed class Connection : ISchematicElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property);
            if (pi == null)
                throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Connection(Pin a, Pin b)
        {
            Pins[0] = a;
            Pins[1] = b;
        }

        public Pin[] Pins { get; } = new Pin[2];

        public Pin Other(Pin pin)
        {
            if (pin == Pins[0])
                return Pins[1];
            if (pin == Pins[1])
                return Pins[0];
            throw new ArgumentException("pin not in this connection");
        }

        public Device Other(Device device)
        {
            if (Pins[0].Device == device)
                return Pins[1].Device;
            if (Pins[1].Device == device)
                return Pins[0].Device;
            throw new ArgumentException("device not in this connection");
        }

        public bool IsHorizontal => Pins[0].Position.Y == Pins[1].Position.Y;
        public bool IsVertical => Pins[0].Position.X == Pins[1].Position.X;
        public double Angle(Pin relativeTo) // -pi to pi
        {
            var other = Other(relativeTo);
            var d = other.Position - (Size)relativeTo.Position;
            return Math.Atan2(d.Y, d.X);
        }

        public RelayCommand SplitCommand => splitCmd ??= new RelayCommand(Split, CanSplit);
        RelayCommand splitCmd;

        public Schematic Schematic => Pins[0].Device.Schematic;

        public void Split()
        {
            if (Schematic.Simulation != null)
                throw new CannotChangeStateInSimulationException();

            using var scope = Schematic.UndoManager.CreateBatch($"Split connection {Pins[0]} {Pins[1]}");

            var pos = Pins[0].Position + (Size)Pins[1].Position;
            pos.X /= 2;
            pos.Y /= 2;
            var j = new Devices.Junction(pos);
            Schematic.AddDevice(j, false);

            Schematic.AddConnection(Pins[0], j.Pins[0]);
            Schematic.AddConnection(Pins[1], j.Pins[0]);
            Schematic.RemoveConnection(this, false);
        }

        public bool CanSplit() => Pins.All(p => p?.Device?.Schematic != null);

        public override bool Equals(object obj) => (obj is Connection other) && Equals(other);
        public bool Equals([System.Diagnostics.CodeAnalysis.AllowNull] Connection other) =>
            (Pins[0] == other.Pins[0] && Pins[1] == other.Pins[1]) || (Pins[0] == other.Pins[1] && Pins[1] == other.Pins[0]);
        public override int GetHashCode() => Pins.GetHashCode();
        public static bool operator ==(Connection a, Connection b) => Equals(a, b);
        public static bool operator !=(Connection a, Connection b) => !Equals(a, b);


        public bool IsEnergized => Potential.HasValue;

        public ElectricLib.Potential? Potential => Schematic.InSimulation ? potential : null;
        ElectricLib.Potential? potential;

        internal void CheckPotentialChanged()
        {
            var devSim = Pins[0].Device.Simulation;
            var pinSim = devSim.Pins[Pins[0].Index];
            var newPotential = Schematic.Simulation.GetPotential(pinSim);
            if (newPotential != potential)
            {
                var wasEnergized = IsEnergized;
                potential = newPotential;
                RaisePropertyChanged(nameof(Potential));
                if (IsEnergized != wasEnergized)
                    RaisePropertyChanged(nameof(IsEnergized));
            }
        }
    }
}
