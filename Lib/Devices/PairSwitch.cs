using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A switch with separate NO and NC contacts per pole.
    /// </summary>
    public class PairSwitch : Device<PairSwitchSim>
    {
        public override string DefaultNamePrefix => "SW";

        public PairSwitch() : this(1) { }
        public PairSwitch(int numPoles, string name = null) : base(name)
        {
            NumPoles = numPoles;
        }

        /// <summary>
        /// One pin of the NC part specified pole.
        /// </summary>
        public Pin NCA(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pins[pole * 4];
        }

        /// <summary>
        /// The other pin of the NC part specified pole.
        /// </summary>
        public Pin NCB(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pins[pole * 4 + 1];
        }

        /// <summary>
        /// One pin of the NO part specified pole.
        /// </summary>
        public Pin NOA(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pins[pole * 4 + 2];
        }

        /// <summary>
        /// The other pin of the NO part specified pole.
        /// </summary>
        public Pin NOB(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pins[pole * 4 + 3];
        }

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
                        for (int i = 0; i < 4; i++)
                            pins.Add(new Pin(this, (4 * p + i + 1).ToString()));
                }
                else
                {
                    while (pins.Count > NumPoles * 4)
                        pins.RemoveAt(pins.Count - 1);
                }
            }
        }
        int numPoles;

        /// <summary>
        /// True = button, false = switch.
        /// </summary>
        public bool Momentary { get; set; }

        /// <summary>
        /// <see cref="ErrorCode.DangerousSwitch"/>
        /// </summary>
        public bool AllowIncompatiblePotentials { get; set; } = true;

        internal override PairSwitchSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new PairSwitchSim(this, sim, pins);
    }


    public class PairSwitchSim : DeviceSimulation
    {
        public PairSwitchSim(PairSwitch dev, Simulation sim, ArraySegment<PinSim> pins) : base(dev, sim, pins)
        {
            switches = new Simulation.Switch[dev.NumPoles * 2];
            for (int pole = 0; pole < dev.NumPoles; pole++)
            {
                var sw = switches[2 * pole + 0] = sim.AddSwitch(dev, true, dev.AllowIncompatiblePotentials);
                sim.Connect(pins[4 * pole + 0], sw.A);
                sim.Connect(pins[4 * pole + 1], sw.B);
                sw = switches[2 * pole + 1] = sim.AddSwitch(dev, false, dev.AllowIncompatiblePotentials);
                sim.Connect(pins[4 * pole + 2], sw.A);
                sim.Connect(pins[4 * pole + 3], sw.B);
            }
        }

        readonly Simulation.Switch[] switches;

        public bool Pressed
        {
            get => switches[1].Closed;
            set
            {
                int v = value ? 1 : 0;
                for (int i = 0; i < switches.Length; i++)
                    switches[i].Closed = i % 2 == v;
            }
        }

        internal override void Update() { }
        internal override void Validate() { }
    }
}
