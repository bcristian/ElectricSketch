using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A switch that uses cams to open and close contacts according to a pattern that corresponds to the selector position.
    /// </summary>
    public class CamSwitch : Device<CamSwitchSim>
    {
        public override string DefaultNamePrefix => "SW";

        public CamSwitch() : this(2, 1, new bool[,] { { false }, { true } }) { }
        // pattern[contact, pos] = is the contact closed in that selector position
        public CamSwitch(int numPositions, int numContacts, bool[,] pattern, string name = null) : base(name)
        {
            NumPositions = numPositions;
            NumContacts = numContacts;
            Pattern = pattern;
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

                numPositions = value;
            }
        }
        int numPositions;

        /// <summary>
        /// Number of contacts.
        /// </summary>
        public int NumContacts
        {
            get => numContacts;
            set
            {
                if (numContacts == value)
                    return;

                if (value < 1)
                    throw new ArgumentException("At least 1 contact must exist for this to be a switch");

                var prevNumContacts = numContacts;
                numContacts = value;

                if (prevNumContacts < numContacts)
                {
                    for (int c = prevNumContacts; c < numContacts; c++)
                    {
                        pins.Add(new Pin(this, (2 * c + 1).ToString()));
                        pins.Add(new Pin(this, (2 * c + 2).ToString()));
                    }
                }
                else
                {
                    var numPins = numContacts * 2;
                    while (pins.Count > numPins)
                        pins.RemoveAt(pins.Count - 1);
                }
            }
        }
        int numContacts;

        /// <summary>
        /// The contact pattern.
        /// pattern[contact, pos] = is the contact closed in that selector position
        /// </summary>
        public bool[,] Pattern { get; set; }

        public bool GetContactState(int contact, int position)
        {
            if (Pattern == null)
                return false;

            if (position < 0 || position >= NumPositions)
                throw new ArgumentOutOfRangeException(nameof(position));
            if (contact < 0 || contact >= numContacts)
                throw new ArgumentOutOfRangeException(nameof(contact));
            if (contact >= Pattern.GetLength(0))
                return false;
            if (position >= Pattern.GetLength(1))
                return false;
            return Pattern[contact, position];
        }

        /// <summary>
        /// <see cref="ErrorCode.DangerousSwitch"/>
        /// </summary>
        /// <remarks>
        /// There are many ways to use such switches safely with apparent conflicting potentials, e.g. reversing, start/delta, or Dahlander switches.
        /// </remarks>
        public bool AllowIncompatiblePotentials { get; set; } = true;


        /// <summary>
        /// Selector position, 0 to <see cref="NumPositions"/> - 1.
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

        internal override CamSwitchSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new(this, sim, pins);
    }

    public sealed class CamSwitchSim : DeviceSimulation
    {
        internal CamSwitchSim(CamSwitch device, Simulation sim, ArraySegment<PinSim> pins) : base(device, sim, pins)
        {
            position = device.Position;

            switches = new Simulation.Switch[device.NumContacts];
            for (int i = 0; i < switches.Length; i++)
            {
                var sw = switches[i] = sim.AddSwitch(device, device.GetContactState(i, position), device.AllowIncompatiblePotentials);
                sim.Connect(pins[2 * i + 0], sw.A);
                sim.Connect(pins[2 * i + 1], sw.B);
            }
        }

        readonly Simulation.Switch[] switches;

        public int Position
        {
            get => position;
            set
            {
                var cs = (CamSwitch)Device;
                if (position < 0 || position >= cs.NumPositions)
                    throw new ArgumentOutOfRangeException("Invalid position");

                if (position == value)
                    return;

                for (int i = 0; i < switches.Length; i++)
                    switches[i].Closed = false;

                position = value;

                for (int i = 0; i < switches.Length; i++)
                    switches[i].Closed = cs.GetContactState(i, position);
            }
        }
        int position;

        internal override void Update() { }
        internal override void Validate() { }
    }
}
