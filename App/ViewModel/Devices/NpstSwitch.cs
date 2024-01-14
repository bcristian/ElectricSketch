using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class NpstSwitch : TypedDevice<Model.Devices.NpstSwitch, ElectricLib.Devices.NpstSwitch, ElectricLib.Devices.NpstSwitchSim>
    {
        public NpstSwitch(Model.Devices.NpstSwitch m) : base(m)
        {
            OriginOffset = new Point(25, 15);

            NumPoles = new NumPoles(this, "Number of poles", () => functional.NumPoles, SetNumPoles); // Setting the value later, it builds related state
            Closed = new Boolean(this, nameof(Closed), GetClosed, SetClosed);
            Closed.ValueChanged += OnClosedChanged;
            Momentary = new DesignOnlyBoolean(this, nameof(Momentary), () => functional.Momentary, SetMomentary);
            AllowIncompatiblePotentials = new DesignOnlyBoolean(this, "Allow incompatible potentials",
                () => functional.AllowIncompatiblePotentials,
                (v) => functional.AllowIncompatiblePotentials = v);

            poles = [];
            Poles = new ReadOnlyObservableCollection<NpstPole>(poles);

            Pins.CollectionChanged += OnPinsCollectionChanged;
            SetPinOffsets(0, Pins.Count);

            // We need to force the creation of the associated state, which will not happen if the value happens to be the one the functional was created with.
            SetNumPoles(m.NumPoles);
            SetClosed(m.Closed);
            SetMomentary(m.Momentary);
            AllowIncompatiblePotentials.Value = m.AllowIncompatiblePotentials;
        }

        protected override void FillModel(Model.Devices.NpstSwitch m)
        {
            m.NumPoles = NumPoles.Value;
            m.Closed = Closed.Value;
            m.Momentary = Momentary.Value;
            m.AllowIncompatiblePotentials = AllowIncompatiblePotentials.Value;
        }

        public ReadOnlyObservableCollection<NpstPole> Poles { get; }
        readonly ObservableCollection<NpstPole> poles;

        void OnPinsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var startIndex = e.NewStartingIndex;
                        if (startIndex < 0)
                            startIndex = Pins.Count - e.NewItems.Count;
                        SetPinOffsets(startIndex, e.NewItems.Count);

                        return;
                    }

                case NotifyCollectionChangedAction.Remove:
                    if (Pins.Count - e.OldStartingIndex > 0)
                        SetPinOffsets(e.OldStartingIndex, Pins.Count - e.OldStartingIndex);

                    return;

                case NotifyCollectionChangedAction.Replace:
                    SetPinOffsets(e.NewStartingIndex, e.NewItems.Count);
                    return;

                case NotifyCollectionChangedAction.Move:
                    {
                        var startIndex = Math.Min(e.NewStartingIndex, e.OldStartingIndex);
                        SetPinOffsets(startIndex, Pins.Count - startIndex);
                        return;
                    }

                case NotifyCollectionChangedAction.Reset:
                    SetPinOffsets(0, e.NewItems.Count);

                    return;

            }
            throw new InvalidOperationException();
        }


        public int CanvasWidth => 50;
        public int PoleHeight => 20;
        public int CanvasHeight => NumPoles.Value * PoleHeight + PoleHeight / 2;
        public int PinHOffset => 30;

        void SetPinOffsets(int start, int num)
        {
            for (int i = start, end = start + num; i < end; i++)
            {
                var left = i % 2 == 0;
                var pole = i / 2;
                Pins[i].Offset = new Point(left ? -PinHOffset : PinHOffset, pole * PoleHeight);
            }
        }


        void SetNumPoles(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.
                for (int i = value; i < functional.NumPoles; i++)
                {
                    Schematic.RemoveConnections(Pins[2 * i + 0], true);
                    Schematic.RemoveConnections(Pins[2 * i + 1], true);
                }
            }

            functional.NumPoles = value;
            RaisePropertyChanged(nameof(CanvasHeight));

            if (poles.Count < value)
            {
                for (int i = poles.Count; i < value; i++)
                    poles.Add(new NpstPole(i, PoleHeight * i - 5));
            }
            else
            {
                // Works because the functional removes poles from the end.
                while (poles.Count > value)
                    poles.RemoveAt(poles.Count - 1);
            }
        }

        /// <summary>
        /// Number of poles.
        /// </summary>
        public NumPoles NumPoles { get; }


        /// <summary>
        /// Default state, i.e. as drawn in the schematic.
        /// </summary>
        public Boolean Closed { get; }

        bool GetClosed()
        {
            if (InSimulation)
                return simulation.Closed;
            else
                return functional.Closed;
        }

        void SetClosed(bool value)
        {
            if (InSimulation)
                simulation.Closed = value;
            else
                functional.Closed = value;
        }

        void OnClosedChanged()
        {
            // Not in SetClosed, because we must also raise these when entering/exiting the simulation.
            if (Momentary.Value)
            {
                RaisePropertyChanged(nameof(IsNoButtonOpen));
                RaisePropertyChanged(nameof(IsNcButtonClosed));
                RaisePropertyChanged(nameof(IsNoButtonClosed));
                RaisePropertyChanged(nameof(IsNcButtonOpen));
            }
            else
            {
                RaisePropertyChanged(nameof(IsSwitchOpen));
                RaisePropertyChanged(nameof(IsSwitchClosed));
            }
        }


        /// <summary>
        /// True = button, false = switch.
        /// </summary>
        public DesignOnlyBoolean Momentary { get; }

        void SetMomentary(bool value)
        {
            functional.Momentary = value;
            if (Closed.Value)
            {
                RaisePropertyChanged(nameof(IsSwitchClosed));
                RaisePropertyChanged(nameof(IsNcButtonClosed));
            }
            else
            {
                RaisePropertyChanged(nameof(IsSwitchOpen));
                RaisePropertyChanged(nameof(IsNoButtonOpen));
            }
        }

        public bool IsSwitchOpen => !Momentary.Value && !Closed.Value;
        public bool IsSwitchClosed => !Momentary.Value && Closed.Value;
        public bool IsNoButton => Momentary.Value && !functional.Closed;
        public bool IsNcButton => Momentary.Value && functional.Closed;
        public bool IsNoButtonOpen => IsNoButton && !Closed.Value;
        public bool IsNoButtonClosed => IsNoButton && Closed.Value;
        public bool IsNcButtonOpen => IsNcButton && !Closed.Value;
        public bool IsNcButtonClosed => IsNcButton && Closed.Value;


        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public DesignOnlyBoolean AllowIncompatiblePotentials { get; }


        public override void ActionPress()
        {
            Closed.Value = !Closed.Value;
        }

        public override void ActionRelease()
        {
            if (InSimulation && Momentary.Value)
                Closed.Value = functional.Closed;
        }
    }

    /// <summary>
    /// An abstract device pole. Used so that the view can enumerate them in order to create visuals for each.
    /// </summary>
    public class NpstPole(int index, double offset) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property) ?? throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public int Index { get; } = index;
        public double Offset { get; } = offset;
    }
}
