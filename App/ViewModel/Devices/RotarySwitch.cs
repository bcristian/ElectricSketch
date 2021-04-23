using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class RotarySwitch : TypedDevice<Model.Devices.RotarySwitch, ElectricLib.Devices.RotarySwitch, ElectricLib.Devices.RotarySwitchSim>
    {
        public RotarySwitch(Model.Devices.RotarySwitch m) : base(m)
        {
            OriginOffset = new Point(25, 15);

            NumPositions = new NumPositions(this, "number of positions", () => functional.NumPositions, SetNumPositions); // Setting the value later, it builds related state
            NumPoles = new NumPoles(this, "number of poles", () => functional.NumPoles, SetNumPoles); // Setting the value later, it builds related state
            AllowArbitraryPositionChange = new DesignOnlyBoolean(this, "Can jump to any position",
                () => functional.AllowArbitraryPositionChange,
                (v) => functional.AllowArbitraryPositionChange = v);
            CurrentPosition = new CurrentPosition(this, "Position", GetCurrentPosition, SetCurrentPosition);
            AllowIncompatiblePotentials = new DesignOnlyBoolean(this, "Allow incompatible potentials",
                () => functional.AllowIncompatiblePotentials,
                (v) => functional.AllowIncompatiblePotentials = v);
            MomentaryFirstPosition = new DesignOnlyBoolean(this, "Momentary first position",
                () => momentaryFirstPosition,
                (v) => momentaryFirstPosition = v);
            MomentaryLastPosition = new DesignOnlyBoolean(this, "Momentary last position",
                () => momentaryLastPosition,
                (v) => momentaryLastPosition = v);

            poles = new ObservableCollection<RotaryPole>();
            Poles = new ReadOnlyObservableCollection<RotaryPole>(poles);

            positions = new ObservableCollection<RotaryPosition>();
            Positions = new ReadOnlyObservableCollection<RotaryPosition>(positions);

            // We need to force the creation of the associated state, which will not happen if the value happens to be the one the functional was created with.
            SetNumPositions(m.NumPositions);
            SetNumPoles(m.NumPoles);
            AllowArbitraryPositionChange.Value = m.AllowArbitraryPositionChange;
            SetCurrentPosition(m.CurrentPosition);
            AllowIncompatiblePotentials.Value = m.AllowIncompatiblePotentials;
            MomentaryFirstPosition.Value = m.MomentaryFirstPosition;
            MomentaryLastPosition.Value = m.MomentaryLastPosition;
        }

        protected override void FillModel(Model.Devices.RotarySwitch m)
        {
            m.NumPositions = NumPositions.Value;
            m.NumPoles = NumPoles.Value;
            m.AllowArbitraryPositionChange = AllowArbitraryPositionChange.Value;
            m.CurrentPosition = CurrentPosition.Value;
            m.AllowIncompatiblePotentials = AllowIncompatiblePotentials.Value;
            m.MomentaryFirstPosition = MomentaryFirstPosition.Value;
            m.MomentaryLastPosition = MomentaryLastPosition.Value;
        }

        public ReadOnlyObservableCollection<RotaryPole> Poles { get; }
        readonly ObservableCollection<RotaryPole> poles;

        // We also need to enumerate positions to draw.
        public ReadOnlyObservableCollection<RotaryPosition> Positions { get; }
        readonly ObservableCollection<RotaryPosition> positions;


        public int CanvasWidth => 50;
        public int PositionHeight => 20;
        public int PoleHeight => NumPositions.Value * PositionHeight;
        public int CanvasHeight => NumPoles.Value * PoleHeight + PositionHeight / 2;
        public int PinHOffset = 30;

        void SetPinOffsets(int start, int num)
        {
            for (int i = start, end = start + num; i < end; i++)
            {
                var pole = Math.DivRem(i, NumPositions.Value + 1, out int pos);
                if (pos == 0)
                    Pins[i].Offset = new Point(-PinHOffset, pole * PoleHeight + (NumPositions.Value - 1) * PositionHeight / 2);
                else // pos is switch pos + 1
                    Pins[i].Offset = new Point(PinHOffset, pole * PoleHeight + PositionHeight * (pos - 1));
            }
        }


        void SetNumPositions(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.
                for (int p = 0; p < functional.NumPoles; p++)
                    for (int i = value; i < functional.NumPositions; i++)
                        Schematic.RemoveConnections(Pins[functional.PositionPinIndex(p, i)], true);
            }
            CurrentPosition.Value = Math.Min(CurrentPosition.Value, MaxPosition);
            functional.NumPositions = value;

            RaisePropertyChanged(nameof(MaxPosition));
            RaisePropertyChanged(nameof(CanvasHeight));
            RaisePropertyChanged(nameof(PoleHeight));
            SetPinOffsets(0, Pins.Count);

            if (positions.Count < value)
            {
                for (int i = positions.Count; i < value; i++)
                    positions.Add(new RotaryPosition(this, i));
            }
            else
            {
                while (positions.Count > value)
                    positions.RemoveAt(positions.Count - 1);
            }
        }

        void SetNumPoles(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.
                for (int p = value; p < functional.NumPoles; p++)
                {
                    Schematic.RemoveConnections(Pins[functional.CommonPinIndex(p)], true);
                    for (int i = 0; i < functional.NumPositions; i++)
                        Schematic.RemoveConnections(Pins[functional.PositionPinIndex(p, i)], true);
                }
            }

            functional.NumPoles = value;
            RaisePropertyChanged(nameof(CanvasHeight));

            if (poles.Count < value)
            {
                var fp = functional.CommonPinIndex(poles.Count);
                SetPinOffsets(fp, Pins.Count - fp);

                for (int i = poles.Count; i < value; i++)
                    poles.Add(new RotaryPole(this, i));
            }
            else
            {
                // Works because the functional removes poles from the end.
                while (poles.Count > value)
                    poles.RemoveAt(poles.Count - 1);
            }
        }

        /// <summary>
        /// Number of switch positions.
        /// </summary>
        public NumPositions NumPositions { get; }

        public int MaxPosition { get => NumPositions.Value - 1; }

        /// <summary>
        /// Connected position, 0 to <see cref="NumPositions"/> - 1.
        /// </summary>
        public CurrentPosition CurrentPosition { get; }

        int GetCurrentPosition()
        {
            if (InSimulation)
                return simulation.Position;
            else
                return functional.Position;
        }

        void SetCurrentPosition(int value)
        {
            if (InSimulation)
            {
                // HACK so that I don't have to implement in the view a method of preventing dragging the slider too fast
                if (!AllowArbitraryPositionChange.Value)
                    value = Math.Clamp(value, Math.Max(0, GetCurrentPosition() - 1), Math.Min(GetCurrentPosition() + 1, MaxPosition));
                simulation.Position = value;
            }
            else
                functional.Position = value;
        }

        /// <summary>
        /// Number of poles.
        /// </summary>
        public NumPoles NumPoles { get; }

        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public DesignOnlyBoolean AllowIncompatiblePotentials { get; }

        /// <summary>
        /// If true, the switch can be set to any position at any time, not just the previous and next ones.
        /// </summary>
        public DesignOnlyBoolean AllowArbitraryPositionChange { get; }

        /// <summary>
        /// If true, the switch will not stay in the first position.
        /// </summary>
        public DesignOnlyBoolean MomentaryFirstPosition { get; }
        bool momentaryFirstPosition;

        /// <summary>
        /// If true, the switch will not stay in the last position.
        /// </summary>
        public DesignOnlyBoolean MomentaryLastPosition { get; }
        bool momentaryLastPosition;


        public override void ActionPress()
        {
            if (NumPositions.Value != 2)
                return;
            if (InSimulation)
                if (momentaryLastPosition)
                    CurrentPosition.Value = 1;
                else if (momentaryFirstPosition)
                    CurrentPosition.Value = 0;
                else
                    CurrentPosition.Value = 1 - CurrentPosition.Value;
            else
                CurrentPosition.Value = 1 - CurrentPosition.Value;
        }

        public override void ActionRelease()
        {
            if (NumPositions.Value != 2 || !InSimulation)
                return;
            if (momentaryLastPosition)
                CurrentPosition.Value = 0;
            else if (momentaryFirstPosition)
                CurrentPosition.Value = 1;
        }

        public override void NextPress()
        {
            CurrentPosition.Value = Math.Min(CurrentPosition.Value + 1, NumPositions.Value - 1);
        }

        public override void NextRelease()
        {
            if (InSimulation && momentaryLastPosition && CurrentPosition.Value == NumPositions.Value - 1)
                CurrentPosition.Value = NumPositions.Value - 2;
        }

        public override void PrevPress()
        {
            CurrentPosition.Value = Math.Max(CurrentPosition.Value - 1, 0);
        }

        public override void PrevRelease()
        {
            if (InSimulation && momentaryFirstPosition && CurrentPosition.Value == 0)
                CurrentPosition.Value = 1;
        }
    }

    /// <summary>
    /// An abstract device pole. Used so that the view can enumerate them in order to create visuals for each.
    /// </summary>
    public class RotaryPole : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property);
            if (pi == null)
                throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public RotaryPole(RotarySwitch sw, int index)
        {
            Switch = sw;
            Index = index;

            sw.NumPositions.ValueChanged += () =>
            {
                RaisePropertyChanged(nameof(Center));
                RaisePropertyChanged(nameof(Offset));
            };

            sw.CurrentPosition.ValueChanged += () => RaisePropertyChanged(nameof(CurrentPosition));
        }

        public RotarySwitch Switch { get; }
        public int Index { get; }

        public ReadOnlyObservableCollection<RotaryPosition> Positions => Switch.Positions;

        public int CurrentPosition => Switch.CurrentPosition.Value;

        // Relative to the pole's canvas.
        public double Center => Switch.Pins[Switch.TypedFunctional.CommonPinIndex(Index)].Offset.Y - Offset + Switch.OriginOffset.Y;

        // Relative to the canvas.
        public double Offset => Index * Switch.PoleHeight + Switch.PositionHeight / 4 - Switch.OriginOffset.Y;
    }


    /// <summary>
    /// An abstract device position. Used so that the view can enumerate them in order to create visuals for each.
    /// </summary>
    public class RotaryPosition : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
#if DEBUG
            var pi = GetType().GetProperty(property);
            if (pi == null)
                throw new ArgumentException($"Property {property} not found on {this}");
#endif
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public RotaryPosition(RotarySwitch sw, int index)
        {
            Switch = sw;
            Index = index;
        }

        public RotarySwitch Switch { get; }
        public int Index { get; }


        // Relative to the pole's canvas.
        public double Offset => Switch.Pins[Switch.TypedFunctional.PositionPinIndex(0, Index)].Offset.Y + Switch.OriginOffset.Y - Switch.Poles[0].Offset;
    }
}
