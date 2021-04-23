using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A point to connect wires to.
    /// </summary>
    public class Junction : Device<JunctionSim>
    {
        public override string DefaultNamePrefix => "J";

        public Junction() : this(null) { }
        public Junction(string name) : base(name)
        {
            pins.Add(Pin = new Pin(this));
        }

        public Pin Pin { get; }

        internal override JunctionSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new JunctionSim(this, sim, pins);
    }

    public class JunctionSim : DeviceSimulation
    {
        internal JunctionSim(Junction device, Simulation simulation, ArraySegment<PinSim> pins) : base(device, simulation, pins) { }

        internal override void Update() { }
        internal override void Validate() { }
    }
}
