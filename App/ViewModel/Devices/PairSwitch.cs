using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class PairSwitch : TypedDevice<Model.Devices.PairSwitch, ElectricLib.Devices.PairSwitch, ElectricLib.Devices.PairSwitchSim>
    {
        public PairSwitch(Model.Devices.PairSwitch m) : base(m)
        {
            OriginOffset = new Point(25, 25);

            NumPoles = new NumPoles(this, "Number of poles", () => functional.NumPoles, SetNumPoles); // Setting the value later, it builds related state
            Momentary = new DesignOnlyBoolean(this, nameof(Momentary),
                () => functional.Momentary,
                (v) => functional.Momentary = v);
            AllowIncompatiblePotentials = new DesignOnlyBoolean(this, "Allow incompatible potentials",
                () => functional.AllowIncompatiblePotentials,
                (v) => functional.AllowIncompatiblePotentials = v);
            Pressed = new Boolean(this, nameof(Pressed), GetPressed, SetPressed, false);

            poles = [];
            Poles = new ReadOnlyObservableCollection<PairPole>(poles);

            SetPinOffsets(0, Pins.Count);

            // We need to force the creation of the associated state, which will not happen if the value happens to be the one the functional was created with.
            SetNumPoles(m.NumPoles);
            Momentary.Value = m.Momentary;
            AllowIncompatiblePotentials.Value = m.AllowIncompatiblePotentials;
        }

        protected override void FillModel(Model.Devices.PairSwitch m)
        {
            m.NumPoles = NumPoles.Value;
            m.Momentary = Momentary.Value;
            m.AllowIncompatiblePotentials = AllowIncompatiblePotentials.Value;
        }

        public ReadOnlyObservableCollection<PairPole> Poles { get; }
        readonly ObservableCollection<PairPole> poles;


        public int CanvasWidth => 50;
        public int PoleHeight => 40;
        public int CanvasHeight => NumPoles.Value * PoleHeight + 10;
        public int PinHOffset => 30;

        void SetPinOffsets(int start, int num)
        {
            for (int i = start, end = start + num; i < end; i++)
            {
                var pole = Math.DivRem(i, 4, out int pos);
                var left = pos % 2 == 0;
                Pins[i].Offset = new Point(left ? -PinHOffset : PinHOffset, pole * PoleHeight + pos / 2 * PoleHeight / 2 - PoleHeight / 4);
            }
        }


        void SetNumPoles(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.
                for (int p = value; p < functional.NumPoles; p++)
                    for (int i = 0; i < 4; i++)
                        Schematic.RemoveConnections(Pins[4 * p + i], true);
            }

            functional.NumPoles = value;
            RaisePropertyChanged(nameof(CanvasHeight));

            if (poles.Count < value)
            {
                var fp = 4 * poles.Count;
                SetPinOffsets(fp, Pins.Count - fp);

                for (int i = poles.Count; i < value; i++)
                    poles.Add(new PairPole(i));
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
        /// Switch pressed. Only works in the simulation.
        /// </summary>
        public Boolean Pressed { get; }

        bool GetPressed()
        {
            if (InSimulation)
                return simulation.Pressed;
            else
                return false;
        }

        void SetPressed(bool value)
        {
            if (InSimulation)
                simulation.Pressed = value;
        }


        /// <summary>
        /// True = button, false = switch.
        /// </summary>
        public DesignOnlyBoolean Momentary { get; }


        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public DesignOnlyBoolean AllowIncompatiblePotentials { get; }


        public override void ActionPress()
        {
            if (InSimulation)
                if (Momentary.Value)
                    Pressed.Value = true;
                else
                    Pressed.Value = !Pressed.Value;
        }

        public override void ActionRelease()
        {
            if (InSimulation && Momentary.Value)
                Pressed.Value = false;
        }
    }

    /// <summary>
    /// An abstract device pole. Used so that the view can enumerate them in order to create visuals for each.
    /// </summary>
    public class PairPole(int index) : INotifyPropertyChanged
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
    }
}
