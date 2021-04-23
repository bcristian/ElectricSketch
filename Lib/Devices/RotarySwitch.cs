using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A switch that connects a common pin to exactly one of the others. The same happens on every pole.
    /// </summary>
    public class RotarySwitch : Device<RotarySwitchSim>, IRotarySwitch
    {
        public override string DefaultNamePrefix => "SW";

        public RotarySwitch() : this(2, 1) { }
        public RotarySwitch(int numPositions, int numPoles, string name = null) : base(name)
        {
            // set in this order, to append pins instead of inserting
            NumPositions = numPositions;
            NumPoles = numPoles;
        }

        /// <summary>
        /// Central pin.
        /// </summary>
        public Pin CommonPin(int pole) => pins[CommonPinIndex(pole)];

        public int CommonPinIndex(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pole * (NumPositions + 1);
        }

        /// <summary>
        /// Pin for he specified position.
        /// </summary>
        public Pin PositionPin(int pole, int position) => pins[PositionPinIndex(pole, position)];

        public int PositionPinIndex(int pole, int position)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException(nameof(pole));
            if (position < 0 || position >= NumPositions)
                throw new IndexOutOfRangeException(nameof(position));
            return pole * (NumPositions + 1) + position + 1;
        }

        /// <summary>
        /// Number of switch positions.
        /// </summary>
        public int NumPositions
        {
            get => numPositions;
            set
            {
                if (numPositions == value)
                    return;

                if (value < 2)
                    throw new ArgumentException("At least 2 positions must exist for this to be a switch");

                var prevNumPos = numPositions;
                numPositions = value;

                if (numPoles == 0) // valid during construction
                    return;

                if (prevNumPos < numPositions)
                {
                    for (int p = 0; p < NumPoles; p++)
                    {
                        for (int i = prevNumPos, ip = p * (numPositions + 1) + i + 1; i < numPositions; i++, ip++)
                            pins.Insert(ip, new Pin(this, $"P{p + 1}{i + 1}"));
                    }
                }
                else
                {
                    for (int p = NumPoles - 1; p >= 0; p--)
                    {
                        for (int i = prevNumPos - 1, ip = p * (prevNumPos + 1) + i + 1; i >= numPositions; i--, ip--)
                            pins.RemoveAt(ip);
                    }
                }
            }
        }
        int numPositions;

        /// <summary>
        /// Number of poles.
        /// </summary>
        public int NumPoles
        {
            get => numPoles;
            set
            {
                if (numPoles == value)
                    return;

                if (value < 1)
                    throw new ArgumentException("At least 1 pole must exist for this to be a switch");

                var prevNumPoles = numPoles;
                numPoles = value;

                if (prevNumPoles < numPoles)
                {
                    for (int p = prevNumPoles; p < NumPoles; p++)
                    {
                        pins.Add(new Pin(this, $"C{p + 1}"));
                        for (int i = 0; i < numPositions; i++)
                            pins.Add(new Pin(this, $"P{p + 1}{i + 1}"));
                    }
                }
                else
                {
                    var numPins = numPoles * (numPositions + 1);
                    while (pins.Count > numPins)
                        pins.RemoveAt(pins.Count - 1);
                }
            }
        }
        int numPoles;

        /// <summary>
        /// If true, the switch can be set to any position at any time, not just the previous and next ones.
        /// </summary>
        public bool AllowArbitraryPositionChange { get; set; }

        /// <summary>
        /// <see cref="ErrorCode.DangerousSwitch"/>
        /// </summary>
        /// <remarks>
        /// There are many ways to use such switches safely with apparent conflicting potentials, e.g. reversing, start/delta, or Dahlander switches.
        /// </remarks>
        public bool AllowIncompatiblePotentials { get; set; } = true;


        /// <summary>
        /// Connected position, 0 to <see cref="NumPositions"/> - 1.
        /// </summary>
        public int Position
        {
            get => position;
            set
            {
                if (value < 0 || value >= NumPositions)
                    throw new IndexOutOfRangeException("Position must be 0 to N-1");
                position = value;
            }
        }
        int position;

        internal override RotarySwitchSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new RotarySwitchSim(this, this, sim, pins);
    }

    internal interface IRotarySwitch
    {
        int NumPositions { get; }
        int NumPoles { get; }
        int Position { get; }
        bool AllowIncompatiblePotentials { get; }
        bool AllowArbitraryPositionChange { get; }
    }

    public sealed class RotarySwitchSim : DeviceSimulation
    {
        internal RotarySwitchSim(IRotarySwitch parameters, IDevice device, Simulation sim, ArraySegment<PinSim> pins) : base(device, sim, pins)
        {
            this.parameters = parameters;
            position = parameters.Position;

            switches = new Simulation.Switch[parameters.NumPositions * parameters.NumPoles];
            for (int pole = 0, i = 0; pole < parameters.NumPoles; pole++)
            {
                var cp = pins[pole * (parameters.NumPositions + 1)];
                for (int pos = 0; pos < parameters.NumPositions; pos++, i++)
                {
                    var sw = switches[i] = sim.AddSwitch(device, pos == position, parameters.AllowIncompatiblePotentials);
                    sim.Connect(cp, sw.A);
                    sim.Connect(pins[i + pole + 1], sw.B);
                }
            }
        }

        internal IRotarySwitch parameters;
        readonly Simulation.Switch[] switches;

        public int Position
        {
            get => position;
            set
            {
                if (position < 0 || position >= parameters.NumPositions)
                    throw new IndexOutOfRangeException("Invalid position");

                if (position == value)
                {
                    if (NoConnection)
                        CloseSwitches();
                    return;
                }

                if (!parameters.AllowArbitraryPositionChange && Math.Abs(position - value) > 1)
                    throw new ArgumentException("The device only allows incremental changes");

                if (!NoConnection)
                {
                    OpenSwitches();
                    // TODO add simulation point
                }

                position = value;

                CloseSwitches();
            }
        }
        int position;

        /// <summary>
        /// True while the switch is in the intermediary position, when no contacts are made.
        /// </summary>
        /// <remarks>
        /// Multiple-throw switches change state by opening the current connection before closing the other one.
        /// Otherwise there would be a point where the common, current and next position pins would all be connected together.
        /// </remarks>
        public bool NoConnection { get; private set; }

        /// <summary>
        /// Opens the switches, without moving to a new position. Set the new position (or the current one) to close them.
        /// </summary>
        public void OpenSwitches()
        {
            if (NoConnection)
                return;
            for (int pole = 0; pole < parameters.NumPoles; pole++)
                switches[pole * parameters.NumPositions + position].Closed = false;
        }

        void CloseSwitches()
        {
            for (int pole = 0; pole < parameters.NumPoles; pole++)
                switches[pole * parameters.NumPositions + position].Closed = true;
        }

        internal override void Update() { }
        internal override void Validate() { }
    }
}
