using ElectricLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using Undo;

namespace ElectricSketch.ViewModel
{
    public abstract class Device : ISchematicElement, INotifyPropertyChanged, ICloneable<Device>
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property);
            if (pi == null)
                throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        /// <summary>
        /// Construct the view-model from the model.
        /// </summary>
        /// <param name="dev"></param>
        /// <returns></returns>
        public static Device FromModel(Model.Device dev)
        {
            return dev switch
            {
                Model.Devices.Junction m => new Devices.Junction(m),
                Model.Devices.Lamp m => new Devices.Lamp(m),
                Model.Devices.SinglePhaseSupply m => new Devices.SinglePhaseSupply(m),
                Model.Devices.ThreePhaseSupply m => new Devices.ThreePhaseSupply(m),
                Model.Devices.NpstSwitch m => new Devices.NpstSwitch(m),
                Model.Devices.RotarySwitch m => new Devices.RotarySwitch(m),
                Model.Devices.Relay m => new Devices.Relay(m),
                Model.Devices.PairSwitch m => new Devices.PairSwitch(m),
                Model.Devices.CamSwitch m => new Devices.CamSwitch(m),
                Model.Devices.SimpleMotor m => new Devices.SimpleMotor(m),
                Model.Devices.ThreePhaseMotor m => new Devices.ThreePhaseMotor(m),
                Model.Devices.Transformer m => new Devices.Transformer(m),
                Model.Devices.VFD m => new Devices.VFD(m),
                _ => throw new NotImplementedException(),
            };
        }

        public Device(Model.Device m)
        {
            name = m.Name;
            position = m.Position;

            Pins = new ReadOnlyObservableCollectionTransform<ElectricLib.Pin, Pin>(Functional.Pins, (p) => new Pin(this, p), true);
        }

        public override string ToString() => Name ?? $"[{GetType().Name}]";

        /// <summary>
        /// Constructs the model from the current state.
        /// </summary>
        public abstract Model.Device Model();

        /// <summary>
        /// The schematic this device is part of. Null if not part of a schematic, e.g. the devices in the library.
        /// </summary>
        public Schematic Schematic
        {
            get => schematic;
            internal set
            {
                if (schematic == value)
                    return;
                var old = schematic;
                schematic = value;
                RaisePropertyChanged();
                OnSchematicChanged(old);
            }
        }
        Schematic schematic;

        protected virtual void OnSchematicChanged(Schematic old)
        {
            if (old != null)
                old.SimulationChanged -= OnSimulationChanged;
            if (Schematic != null)
                Schematic.SimulationChanged += OnSimulationChanged;
            SchematicChanged?.Invoke(old);
        }

        protected virtual void OnSimulationChanged()
        {
            InSimulationChanged?.Invoke();
            RaisePropertyChanged(nameof(InSimulation));
        }

        /// <summary>
        /// Raised after the schematic has changed. The arguments is the old value.
        /// </summary>
        public Action<Schematic> SchematicChanged;

        /// <summary>
        /// The device's pins. Observable because devices can change their pins.
        /// </summary>
        public ReadOnlyObservableCollectionTransform<ElectricLib.Pin, Pin> Pins { get; }

        /// <summary>
        /// The corresponding functional device. <seealso cref="IDevice"/>
        /// </summary>
        public abstract IDevice Functional { get; }
        protected abstract void RemoveFunctional();

        /// <summary>
        /// Tells the device to update all the properties of the functional, in order to run the simulation.
        /// </summary>
        public virtual void PrepareForSimulation()
        {
            Functional.Name = Name;
        }

        public bool InSimulation => Schematic != null && Schematic.Simulation != null;

        public event Action InSimulationChanged;

        public abstract DeviceSimulation Simulation { get; }

        /// <summary>
        /// Name of the device, a human-readable way to identify a device.
        /// </summary>
        /// <remarks>
        /// Not necessarily unique. Might be null or empty.
        /// </remarks>
        public string Name
        {
            get => name;
            set
            {
                if (value == name)
                    return;

                if (Schematic != null)
                    if (InSimulation)
                        throw new CannotChangeStateInSimulationException();
                    else
                        Schematic.UndoManager.Do(new ChangeNameAction(this, value));
                else
                    SetNameNoUndo(value);
            }
        }
        protected string name;

        internal void SetNameNoUndo(string value)
        {
            name = value;
            RaisePropertyChanged(nameof(Name));
        }

        protected sealed class ChangeNameAction : PropertyChangeAction<Device>
        {
            public ChangeNameAction(Device device, string newName) : base(device)
            {
                name = new OneOrMore<string>(device.Name, newName, (dev, name) => dev.SetNameNoUndo(name));
                UpdateDescription();
            }

            readonly OneOrMore<string> name;

            public override bool Merge(UndoableAction action)
            {
                var merged = base.Merge(action);
                if (merged)
                    UpdateDescription();
                return merged;
            }

            void UpdateDescription()
            {
                if (device != null)
                    Description = $"Rename {device.GetType().Name} from {name.oneOld} to {name.oneNew}";
                else
                    Description = $"Rename {devices.Count} devices";
            }
        }


        /// <summary>
        /// Position of the device on the schematic.
        /// </summary>
        public Point Position
        {
            get => position;
            set
            {
                // If the flag is set, we must raise the event even if the same value is set.
                // Otherwise a connection might see that this device will move, delay its update waiting for the move to happen,
                // and remain in the wrong position because the event is not raised.
                if (PositionWillChange)
                {
                    PositionWillChange = false;
                    if (position == value)
                    {
                        SetPositionNoUndo(position);
                        return;
                    }
                }
                else if (position == value)
                    return;

                if (Schematic != null)
                    if (InSimulation)
                        throw new CannotChangeStateInSimulationException();
                    else
                        Schematic.UndoManager.Do(new ChangePositionAction(this, value));
                else
                    SetPositionNoUndo(value);
            }
        }
        Point position;

        /// <summary>
        /// Offset between the top-left of the canvas containing the device's schematic and the origin of the device.
        /// The position of the device means the position of its origin.
        /// </summary>
        public Point OriginOffset { get; protected set; }

        /// <summary>
        /// Raised when the position has changed. Arguments are the device, the old position, and the new one.
        /// </summary>
        public event EventHandler<DevicePositionChangedEventArgs> PositionChanged;

        protected virtual void OnPositionChanged(Point oldPos)
        {
            PositionChanged?.Invoke(this, new DevicePositionChangedEventArgs() { oldValue = oldPos, newValue = position });
        }

        internal void SetPositionNoUndo(Point value)
        {
            var oldPos = position;
            position = value;
            OnPositionChanged(oldPos);
            RaisePropertyChanged(nameof(Position));
        }

        /// <summary>
        /// Used for optimizing layout computations when multiple elements are moved.
        /// The mover should set this on all the devices that will be re-positioned, before setting the new values.
        /// This way when both ends of a connection are moved it can avoid recomputing its layout on the first event.
        /// The value is reset when the position changes, before the property changed event is raised.
        /// </summary>
        public bool PositionWillChange { get; set; }

        protected sealed class ChangePositionAction : PropertyChangeAction<Device>
        {
            public ChangePositionAction(Device device, Point newPos) : base(device)
            {
                pos = new OneOrMore<Point>(device.position, newPos, (dev, pos) => dev.SetPositionNoUndo(pos));
                Description = $"Move {device}";
            }

#pragma warning disable IDE0052 // Remove unread private members
            readonly OneOrMore<Point> pos;
#pragma warning restore IDE0052

            public override bool Merge(UndoableAction action)
            {
                var numDev = devices?.Count ?? 1;
                var merged = base.Merge(action);
                if (merged)
                {
                    var newNumDev = devices?.Count ?? 1;
                    if (newNumDev != numDev)
                        Description = $"Move {devices.Count} devices";
                }
                return merged;
            }
        }

        /// <summary>
        /// Is the device selected.
        /// </summary>
        public bool IsSelected
        {
            get => Schematic != null && Schematic.SelectedDevices.Contains(this);
            set
            {
                if (Schematic == null)
                    throw new InvalidOperationException("Only devices in a schematic can be selected");
                if (value)
                    Schematic.Select(this);
                else
                    Schematic.Unselect(this);
            }
        }

        internal void SelectionChanged(bool _) => RaisePropertyChanged(nameof(IsSelected));


        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Creates a duplicate of the device, in its free state - i.e. not attached to a schematic (even if the original was).
        /// </summary>
        public Device Clone() => FromModel(Model());


        // Used for interacting with the devices using the keyboard.
        // HACK because I don't feel like writing the whole command scaffolding.
        public virtual void ActionPress() { }
        public virtual void ActionRelease() { }
        public virtual void NextPress() { }
        public virtual void NextRelease() { }
        public virtual void PrevPress() { }
        public virtual void PrevRelease() { }
    }

    public abstract class TypedDevice<TModel, TBase, TSim> : Device
        where TBase : IDevice, new()
        where TModel : Model.TypedDevice<TBase>, new()
        where TSim : DeviceSimulation
    {
        public TypedDevice(TModel m) : base(m) { }

        public TBase TypedFunctional => functional ??= new TBase();
        protected TBase functional;

        public override IDevice Functional => TypedFunctional;

        protected override void RemoveFunctional() => functional = default;

        public override Model.Device Model()
        {
            var m = new TModel
            {
                Name = Name,
                Position = Position,
                Pins = Pins.Select(p => new Model.Pin() { Name = p.Name }).ToList()
            };
            FillModel(m);
            return m;
        }

        protected abstract void FillModel(TModel m);

        public override DeviceSimulation Simulation => simulation;
        public TSim TypedSimulation => simulation;
        protected TSim simulation;

        protected override void OnSimulationChanged()
        {
            // Do this first, because the view will update when the simulation changed event is raised.
            if (InSimulation)
                simulation = Schematic.Simulation.Device<TSim>(functional);
            else
                simulation = null;

            base.OnSimulationChanged();
        }
    }

    public class DevicePositionChangedEventArgs : EventArgs
    {
        public Point oldValue;
        public Point newValue;
    }

    public class CannotChangeStateInSimulationException : Exception { }
}
