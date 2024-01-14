using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// Supplies three-phase power. Voltage means line voltage, phase to phase.
    /// </summary>
    public class ThreePhaseSupply : Device<ThreePhaseSupplySim>
    {
        public override string DefaultNamePrefix => "PWR";

        public ThreePhaseSupply() : this(null) { }
        public ThreePhaseSupply(string name = null) : this(new Volt(380), new Hz(50), name) { }

        public ThreePhaseSupply(Volt voltage, Hz frequency, string name) : base(name)
        {
            Voltage = voltage;
            Frequency = frequency;

            L = new Pin[3];
            pins.Add(N = new Pin(this, "N"));
            pins.Add(L[0] = new Pin(this, "R"));
            pins.Add(L[1] = new Pin(this, "S"));
            pins.Add(L[2] = new Pin(this, "T"));
        }

        public Volt Voltage { get; set; }
        public Hz Frequency { get; set; }

        public Pin N { get; }
        public Pin[] L { get; }
        public Pin R => L[0];
        public Pin S => L[1];
        public Pin T => L[2];

        internal override ThreePhaseSupplySim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new(this, sim, pins);
    }

    public class ThreePhaseSupplySim : DeviceSimulation
    {
        internal ThreePhaseSupplySim(ThreePhaseSupply device, Simulation simulation, ArraySegment<PinSim> pins) : base(device, simulation, pins)
        {
            var ps = simulation.AddPowerSource(device,
                new Potential(Volt.Zero, device.Frequency, Phase.Zero, device),
                new Potential(device.Voltage / 1.732f, device.Frequency, Phase.R, device),
                new Potential(device.Voltage / 1.732f, device.Frequency, Phase.S, device),
                new Potential(device.Voltage / 1.732f, device.Frequency, Phase.T, device));
            for (int i = 0; i < pins.Count; i++)
                simulation.Connect(pins[i], ps.Pins[i]);
        }

        internal override void Update() { }
        internal override void Validate() { }
    }
}
