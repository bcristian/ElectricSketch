using ElectricLib;
using ElectricSketch.ViewModel.Devices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ElectricSketch.ViewModel
{
    public sealed class Schematic : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property) ?? throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

#if DEBUG
        // Only used by the designer
        public Schematic() : this(Model.Schematic.Example())
        {
            if (!DependencyObjectExt.InDesignMode)
                throw new InvalidOperationException();

            UndoManager = new Undo.UndoManager("Initial state");
            UndoManager.PropertyChanged += (s, e) => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            SelectedDevices = new ReadOnlySet<Device>(selectedDevices);
        }
#endif

        /// <summary>
        /// Interval used for regular placing of elements.
        /// </summary>
        /// <remarks>
        /// The devices can be placed at arbitrary positions, the grid is a recommended suggestion, not a strict constraint.
        /// </remarks>
        public const int GridSpacing = 10;

        /// <summary>
        /// Returns the grid position closest to the specified value.
        /// </summary>
        public static int SnapToGrid(int pos) => (pos / GridSpacing) * GridSpacing;
        public static int SnapToGrid(double pos) => ((int)Math.Round(pos, MidpointRounding.ToZero) / GridSpacing) * GridSpacing;

        /// <summary>
        /// Returns the grid position closest to the specified value.
        /// </summary>
        public static System.Drawing.Point SnapToGrid(System.Drawing.Point p) => new (SnapToGrid(p.X), SnapToGrid(p.Y));
        public static System.Drawing.Point SnapToGrid(Point p) => new(SnapToGrid(p.X), SnapToGrid(p.Y));
        public static System.Drawing.Point SnapToGrid(double x, double y) => new(SnapToGrid(x), SnapToGrid(y));

        /// <summary>
        /// Creates the VM from the model. Changes will not be reflected into the model, call <see cref="CreateModel"/> to obtain one for the current state.
        /// </summary>
        public Schematic(Model.Schematic sch)
        {
            elements = [];
            Elements = new ReadOnlyObservableCollection<ISchematicElement>(elements);

            foreach (var dm in sch.Devices)
                AddDeviceNoUndo(Device.FromModel(dm));

            foreach (var mConn in sch.Connections)
            {
                var conn = new Connection(
                    ((Device)elements[mConn[0].DeviceIndex]).Pins[mConn[0].PinIndex],
                    ((Device)elements[mConn[1].DeviceIndex]).Pins[mConn[1].PinIndex]);
                ValidateConnection(conn);
                elements.Add(conn);
            }

            // In the UI we want to double-click a history item to go to that state. So we need something that represents "initial state".
            UndoManager = new Undo.UndoManager("Initial state");
            UndoManager.PropertyChanged += (s, e) => System.Windows.Input.CommandManager.InvalidateRequerySuggested();

            SelectedDevices = new ReadOnlySet<Device>(selectedDevices);
        }

        public ReadOnlyObservableCollection<ISchematicElement> Elements { get; }
        readonly ObservableCollection<ISchematicElement> elements;

        public Undo.UndoManager UndoManager { get; }


        /// <summary>
        /// The single selected device. Null if not exactly one device selected.
        /// </summary>
        public Device SelectedDevice
        {
            get => selectedDevices.Count == 1 ? selectedDevices.First() : null;
        }

        /// <summary>
        /// All selected devices.
        /// </summary>
        public ReadOnlySet<Device> SelectedDevices { get; private set; }
        readonly HashSet<Device> selectedDevices = [];

        public bool Select(Device dev)
        {
            var prevSingle = SelectedDevice;
            var result = selectedDevices.Add(dev);
            if (result)
            {
                dev.SelectionChanged(true);
                if (SelectedDevice != prevSingle)
                    RaisePropertyChanged(nameof(SelectedDevice));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            return result;
        }

        public bool Unselect(Device dev)
        {
            var prevSingle = SelectedDevice;
            var result = selectedDevices.Remove(dev);
            if (result)
            {
                dev.SelectionChanged(false);
                if (SelectedDevice != prevSingle)
                    RaisePropertyChanged(nameof(SelectedDevice));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
            return result;
        }

        public void SelectAll()
        {
            var prevSingle = SelectedDevice;
            foreach (var e in elements)
                if (e is Device dev && selectedDevices.Add(dev))
                    dev.SelectionChanged(true);

            if (SelectedDevice != prevSingle)
                RaisePropertyChanged(nameof(SelectedDevice));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        public void UnselectAll()
        {
            var hadSingle = selectedDevices.Count == 1;
            foreach (var e in elements)
                if (e is Device dev && selectedDevices.Remove(dev))
                    dev.SelectionChanged(true);

            if (hadSingle)
                RaisePropertyChanged(nameof(SelectedDevice));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }



        public void AddDevice(Device dev, bool setDefaultName)
        {
            if (setDefaultName)
            {
                // Use the first one not taken.
                var names = new HashSet<string>();
                foreach (var d in Elements.OfType<Device>().Where(d => d.Functional.DefaultNamePrefix == dev.Functional.DefaultNamePrefix))
                    names.Add(d.Name);
                string name;
                for (int i = 1; ; i++)
                {
                    name = $"{dev.Functional.DefaultNamePrefix}{i}";
                    if (!names.Contains(name))
                        break;
                }

                dev.SetNameNoUndo(name);
            }

            UndoManager.Do(new AddDeviceAction(this, dev));
        }

        void AddDeviceNoUndo(Device dev)
        {
            if (dev.Schematic != null)
                throw new DeviceAlreadyInSchematicException();

            elements.Add(dev);
            dev.Schematic = this;
        }

        void RemoveDeviceNoUndo(Device dev)
        {
            if (dev.Schematic != this)
                throw new DeviceNotInSchematicException();

            if (!elements.Remove(dev))
                throw new InvalidOperationException("why was is not there?");

            // Connections should have been removed already.
            System.Diagnostics.Debug.Assert(dev.Pins.All(p => !ConnectionsToPin(p).Any()));

            dev.Schematic = null;
            Unselect(dev);
        }

        class AddDeviceAction : Undo.UndoableAction
        {
            public AddDeviceAction(Schematic sch, Device dev)
            {
                Schematic = sch;
                Device = dev;
                Description = $"Add {Device.GetType().Name} {Device.Name}";
            }

            Schematic Schematic { get; }
            Device Device { get; }

            public override void RedoBeforeChildren()
            {
                Schematic.AddDeviceNoUndo(Device);
            }

            public override void UndoBeforeChildren()
            {
                // We're undoing the addition of a device, there should be no connections, as those could only have been added later
                // and should have been undone already.
                Schematic.RemoveDeviceNoUndo(Device);
            }
        }

        public void RemoveDevice(Device dev, bool removeUnnecessaryJunctions)
        {
            if (dev.Schematic != this)
                throw new DeviceNotInSchematicException();

            // Removing devices implies removing their connections. Because we don't want to have invalid data at any point,
            // we must remove the connections before removing the device, so that there is not a moment where we list connections to
            // devices not in the schematic.
            // Removing connections is a recursive process, to get rid of junctions made obsolete.
            // Junctions are devices.
            // So what happens when the user deletes a junction? Or when the recursive removal reaches back into the same device?
            // We need a method of tracking devices that are going to be removed, so that recursion stops at them.

            // We use a stack to track the devices that are going to be deleted.

            var stackSize = devicesToBeDeleted.Count;
            devicesToBeDeleted.Push(dev);
            UndoManager.Do(new RemoveDeviceAction(this, dev, removeUnnecessaryJunctions));
            if (devicesToBeDeleted.Pop() != dev || devicesToBeDeleted.Count != stackSize)
                throw new InvalidOperationException("devices to be deleted stack corrupted");
        }

        readonly Stack<Device> devicesToBeDeleted = new();

        class RemoveDeviceAction : Undo.UndoableAction
        {
            public RemoveDeviceAction(Schematic sch, Device dev, bool removeUnnecessaryJunctions)
            {
                Schematic = sch;
                Device = dev;
                RemoveUnnecessaryJunctions = removeUnnecessaryJunctions;
                Description = $"Remove {Device.GetType().Name} {Device.Name}";
            }

            Schematic Schematic { get; }
            Device Device { get; }
            bool RemoveUnnecessaryJunctions { get; }

            public override void Do()
            {
                // Remove all connections.
                // We don't need to keep a list of them, they will be contained in the undo actions
                foreach (var pin in Device.Pins)
                    Schematic.RemoveConnections(pin, RemoveUnnecessaryJunctions);

                // Remove the device itself.
                Schematic.RemoveDeviceNoUndo(Device);
            }

            public override void RedoAfterChildren()
            {
                Schematic.RemoveDeviceNoUndo(Device);
            }

            public override void UndoBeforeChildren()
            {
                Schematic.AddDeviceNoUndo(Device);
            }
        }

        public IEnumerable<Connection> ConnectionsToPin(Pin pin) => Elements.OfType<Connection>().Where(c => c.Pins[0] == pin || c.Pins[1] == pin);

        public void AddConnection(Connection conn)
        {
            ValidateConnection(conn);
            UndoManager.Do(new AddConnectionAction(this, conn));
        }

        public void AddConnection(Pin a, Pin b) => AddConnection(new Connection(a, b));

        void ValidateConnection(Connection conn)
        {
            if (conn == null || conn.Pins[0] == null || conn.Pins[1] == null || conn.Pins[0].Device == null || conn.Pins[1].Device == null)
                throw new ArgumentNullException(nameof(conn));
            if (conn.Pins[0].Device.Schematic != this || conn.Pins[1].Device.Schematic != this)
                throw new DeviceNotInSchematicException();
            if (elements.OfType<Connection>().Contains(conn))
                throw new PinsAlreadyConnectedException();
        }

        class AddConnectionAction : Undo.UndoableAction
        {
            public AddConnectionAction(Schematic sch, Connection conn)
            {
                Schematic = sch;
                // Do not keep the connection object. Some devices can change their pins, and we'd be left with bogus references.
                // E.g. reduce the number of poles, then increase it again - the pins will not be the same objects.
                Pins = ConnToRef(conn);
                Description = $"Connect {conn.Pins[0]} and {conn.Pins[1]}";
            }

            Schematic Schematic { get; }
            PinRef[] Pins { get; }

            public override void RedoBeforeChildren()
            {
                Schematic.elements.Add(ConnFromRef(Pins));
            }

            public override void UndoBeforeChildren()
            {
                Schematic.elements.Remove(Schematic.FindConn(Pins));
            }
        }

        public readonly struct PinRef(Device device, int index)
        {
            public Device Device { get; } = device;
            public int Index { get; } = index;
        }

        public static PinRef PinToRef(Pin pin) => new(pin.Device, pin.Index);
        public static PinRef[] ConnToRef(Connection conn) => new PinRef[] { PinToRef(conn.Pins[0]), PinToRef(conn.Pins[1]) };
        public static Pin PinFromRef(PinRef pin) => pin.Device.Pins[pin.Index];
        public static Connection ConnFromRef(PinRef[] pins) => new(PinFromRef(pins[0]), PinFromRef(pins[1]));
        public static bool Equals(Pin pin, PinRef pinRef) => pin.Device == pinRef.Device && pin.Index == pinRef.Index;

        public Connection FindConn(PinRef[] pins)
        {
            foreach (var e in elements)
            {
                if (e is Connection c)
                {
                    if ((Equals(c.Pins[0], pins[0]) && Equals(c.Pins[1], pins[1])) ||
                        (Equals(c.Pins[1], pins[0]) && Equals(c.Pins[0], pins[1])))
                        return c;
                }
            }

            throw new ArgumentException("no connection between the specified pins exists");
        }

        public void RemoveConnection(Connection conn, bool removeUnnecessaryJunctions)
        {
            if (conn == null || conn.Pins[0] == null || conn.Pins[1] == null || conn.Pins[0].Device == null || conn.Pins[1].Device == null)
                throw new ArgumentNullException(nameof(conn));
            if (conn.Pins[0].Device.Schematic != this || conn.Pins[1].Device.Schematic != this)
                throw new DeviceNotInSchematicException();
            if (!elements.OfType<Connection>().Contains(conn))
                throw new PinsNotConnectedException();
            UndoManager.Do(new RemoveConnectionAction(this, conn, removeUnnecessaryJunctions));
        }

        public void RemoveConnections(Pin pin, bool removeUnnecessaryJunctions)
        {
            var connections = ConnectionsToPin(pin).ToList();
            foreach (var conn in connections)
                RemoveConnection(conn, removeUnnecessaryJunctions);
        }

        class RemoveConnectionAction : Undo.UndoableAction
        {
            public RemoveConnectionAction(Schematic sch, Connection conn, bool removeUnnecessaryJunctions)
            {
                Schematic = sch;
                Connection = conn;
                Pins = ConnToRef(conn);
                RemoveUnnecessaryJunctions = removeUnnecessaryJunctions;
                Description = $"Disconnect {Connection.Pins[0]} and {Connection.Pins[1]}";
            }

            Schematic Schematic { get; }
            Connection Connection { get; set; } // just for Do
            PinRef[] Pins { get; }
            bool RemoveUnnecessaryJunctions { get; }

            public override void Do()
            {
                if (!Schematic.elements.Remove(Connection))
                    throw new ArgumentException();

                // Remove automatic junctions that are no longer needed.
                // This will recursively propagate through the circuit.
                if (RemoveUnnecessaryJunctions)
                    foreach (var pin in Connection.Pins)
                        RemoveOrphanJunction(pin);

                Connection = null;
            }

            void RemoveOrphanJunction(Pin pin)
            {
                if (pin.Device is not Junction junction)
                    return;

                // Ignore devices that will be deleted, or we'd go in a loop.
                if (Schematic.devicesToBeDeleted.Contains(junction))
                    return;

                // Do not remove named junctions - if the user put a name on them, they probably serve some purpose beyond routing connections.
                if (!string.IsNullOrEmpty(junction.Name))
                    return;

                // There are two types of orphan junctions:
                // those with only one connection remaining
                // those with 2 connections remaining that are in line (a T where we removed the vertical)

                var connections = Schematic.ConnectionsToPin(pin).ToList();
                if (connections.Count > 2)
                    return;

                if (connections.Count <= 1)
                    Schematic.RemoveDevice(junction, true);
                else
                {
                    var a0 = Math.Abs(connections[0].Angle(pin));
                    var a1 = Math.Abs(connections[1].Angle(pin));
                    var da = Math.Abs(a0 - a1);
                    if (Math.Abs(da - Math.PI) < 0.0087) // .5 deg
                    {
                        Schematic.AddConnection(connections[0].Other(pin), connections[1].Other(pin));
                        Schematic.RemoveDevice(junction, true);
                    }
                }
            }

            public override void RedoBeforeChildren()
            {
                Schematic.elements.Remove(Schematic.FindConn(Pins));
            }

            public override void UndoAfterChildren()
            {
                Schematic.elements.Add(ConnFromRef(Pins));
            }
        }


        /// <summary>
        /// Create the model from the current data.
        /// </summary>
        public Model.Schematic CreateModel()
        {
            var m = new Model.Schematic();

            var devs = elements.OfType<Device>().ToList();
            foreach (var dev in devs)
                    m.Devices.Add(dev.Model());

            Model.PinInfo PinInfo(Pin pin)
            {
                var di = devs.IndexOf(pin.Device);
                if (di < 0)
                    throw new InvalidOperationException();
                var pi = pin.Device.Pins.IndexOf(pin);
                if (pi < 0)
                    throw new InvalidOperationException();
                return new Model.PinInfo() { DeviceIndex = di, PinIndex = pi };
            }

            foreach (var e in elements)
            {
                if (e is Connection conn)
                    m.Connections.Add([PinInfo(conn.Pins[0]), PinInfo(conn.Pins[1])]);
            }

            return m;
        }


        public void Import(Model.Schematic sch, string path)
        {
            using (UndoManager.CreateBatch($"Import schematic {path}"))
            {
                // Import devices and connections.
                // We don't need to keep a list of them, they will be contained in the undo actions.
                // Select the imported devices.

                var devIndexOffset = elements.Count;

                var devPosOffset = new System.Drawing.Size();
                foreach (var dev in elements.OfType<Device>())
                    devPosOffset = new System.Drawing.Size(Math.Max(devPosOffset.Width, dev.Position.X), Math.Max(devPosOffset.Height, dev.Position.Y));
                devPosOffset += new System.Drawing.Size(50, 50);

                UnselectAll();

                foreach (var dm in sch.Devices)
                {
                    dm.Position += devPosOffset;
                    var dev = Device.FromModel(dm);
                    AddDevice(dev, !string.IsNullOrEmpty(dev.Name));
                    Select(dev);
                }

                foreach (var mConn in sch.Connections)
                {
                    AddConnection(new Connection(
                        ((Device)elements[mConn[0].DeviceIndex + devIndexOffset]).Pins[mConn[0].PinIndex],
                        ((Device)elements[mConn[1].DeviceIndex + devIndexOffset]).Pins[mConn[1].PinIndex]));
                }
            }
        }


        /// <summary>
        /// The running simulation. Null if not simulating.
        /// </summary>
        public Simulation Simulation
        {
            get => simulation;
            private set
            {
                if (simulation == value)
                    return;
                simulation = value;
                SimulationChanged?.Invoke();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(InSimulation));
            }
        }
        Simulation simulation;
        DispatcherTimer simUpdater;
        DateTime simStart;

        public bool InSimulation => Simulation != null;

        public event Action SimulationChanged;

        public void StartSimulation()
        {
            var sch = new ElectricLib.Schematic();

            var devs = elements.OfType<Device>().ToList();
            foreach (var dev in devs)
            {
                dev.PrepareForSimulation();
                sch.AddDevice(dev.Functional);
            }

            static ElectricLib.Pin FuncPin(Pin pin) => pin.Device.Functional.Pins[pin.Device.Pins.IndexOf(pin)];

            foreach (var conn in elements.OfType<Connection>())
                sch.Connect(FuncPin(conn.Pins[0]), FuncPin(conn.Pins[1]));

            Simulation = new Simulation(sch, simStart = DateTime.UtcNow);
            UpdatePotentials();
            SimulationError = Simulation.Error;

            if (SimulationError == null)
            {
                simUpdater = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, OnSimUpdate, Dispatcher.CurrentDispatcher);
                simUpdater.Start();
            }
        }

        void OnSimUpdate(object sender, EventArgs e)
        {
            Simulation.Update(DateTime.UtcNow);
            RaisePropertyChanged(nameof(SimulationTime));
            UpdatePotentials();
            SimulationError = Simulation.Error;
            if (SimulationError != null)
                simUpdater.Stop();
        }

        void UpdatePotentials()
        {
            foreach (var conn in elements.OfType<Connection>())
                conn.CheckPotentialChanged();
        }

        public CircuitError SimulationError
        {
            get => simulationError;
            private set
            {
                if (simulationError == value)
                    return;
                simulationError = value;
                RaisePropertyChanged();
            }
        }
        CircuitError simulationError;

        public TimeSpan SimulationTime => Simulation != null ? Simulation.Now - simStart : default;

        public void StopSimulation()
        {
            if (simUpdater != null)
            {
                simUpdater.Stop();
                simUpdater = null;
            }
            Simulation = null;
            SimulationError = null;
        }
    }

    public interface ISchematicElement
    {
    }

    /// <summary>
    /// Thrown when an operation is attempted with a device that should have been part of the schematic but it is not. E.g. removing a device.
    /// </summary>
    public class DeviceNotInSchematicException : Exception
    {
    }

    /// <summary>
    /// Thrown when an operation is attempted with a device that should not have been part of the schematic but it is. E.g. adding a device.
    /// </summary>
    public class DeviceAlreadyInSchematicException : Exception
    {
    }

    /// <summary>
    /// Thrown when attempting to connect already connected pins.
    /// </summary>
    public class PinsAlreadyConnectedException : Exception
    {
    }

    /// <summary>
    /// Thrown when attempting to disconnect pins that are not connected.
    /// </summary>
    public class PinsNotConnectedException : Exception
    {

    }
}
