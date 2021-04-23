using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// N pole single throw switch.
    /// </summary>
    public class NpstSwitch : Device<NpstSwitchSim>
    {
        public override string DefaultNamePrefix => "SW";

        public NpstSwitch() : this(1) { }
        public NpstSwitch(int numPoles, string name = null) : base(name)
        {
            NumPoles = numPoles;
        }

        /// <summary>
        /// One pin of the specified pole.
        /// </summary>
        public Pin A(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pins[pole * 2];
        }

        /// <summary>
        /// The other pin of the specified pole.
        /// </summary>
        public Pin B(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return pins[pole * 2 + 1];
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
                    {
                        pins.Add(new Pin(this, (2 * p + 1).ToString()));
                        pins.Add(new Pin(this, (2 * p + 2).ToString()));
                    }
                }
                else
                {
                    while (pins.Count > NumPoles * 2)
                        pins.RemoveAt(pins.Count - 1);
                }
            }
        }
        int numPoles;

        /// <summary>
        /// Default state, i.e. as drawn in the schematic.
        /// </summary>
        public bool Closed { get; set; }

        /// <summary>
        /// True = button, false = switch.
        /// </summary>
        public bool Momentary { get; set; }

        /// <summary>
        /// <see cref="ErrorCode.DangerousSwitch"/>
        /// </summary>
        public bool AllowIncompatiblePotentials { get; set; }

        internal override NpstSwitchSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new NpstSwitchSim(this, sim, pins);
    }


    public class NpstSwitchSim : DeviceSimulation
    {
        public NpstSwitchSim(NpstSwitch dev, Simulation sim, ArraySegment<PinSim> pins) : base(dev, sim, pins)
        {
            switches = new Simulation.Switch[dev.NumPoles];
            for (int pole = 0; pole < dev.NumPoles; pole++)
            {
                var sw = switches[pole] = sim.AddSwitch(dev, dev.Closed, dev.AllowIncompatiblePotentials);
                sim.Connect(pins[2 * pole + 0], sw.A);
                sim.Connect(pins[2 * pole + 1], sw.B);
            }
        }

        readonly Simulation.Switch[] switches;

        public bool Closed
        {
            get => switches[0].Closed;
            set
            {
                foreach (var sw in switches)
                    sw.Closed = value;
            }
        }

        internal override void Update() { }
        internal override void Validate() { }
    }
}
