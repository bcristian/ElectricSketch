using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// Supplies single-phase power. Voltage means phase voltage, line to neutral.
    /// </summary>
    public class SinglePhaseSupply : Device<SinglePhaseSupplySim>
    {
        public override string DefaultNamePrefix => "PWR";

        public SinglePhaseSupply() : this(null) { }
        public SinglePhaseSupply(string name = null) : this(new Volt(220), new Hz(50), name) { }

        public SinglePhaseSupply(Volt voltage, Hz frequency, string name = null) : base(name)
        {
            pins.Add(N = new Pin(this));
            pins.Add(L = new Pin(this));
            Voltage = voltage;
            this.frequency = frequency;

            SetPinNames();
        }

        public Volt Voltage { get; set; }
        public Hz Frequency
        {
            get => frequency;
            set
            {
                if (frequency == value)
                    return;
                frequency = value;
                SetPinNames();
            }
        }
        Hz frequency;

        public Pin N { get; }
        public Pin L { get; }

        void SetPinNames()
        {
            if (frequency.Value == 0)
            {
                pins[0].Name = "-";
                pins[1].Name = "+";
            }
            else
            {
                pins[0].Name = "N";
                pins[1].Name = "L";
            }
        }

        internal override SinglePhaseSupplySim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new SinglePhaseSupplySim(this, sim, pins);
    }

    public class SinglePhaseSupplySim : DeviceSimulation
    {
        internal SinglePhaseSupplySim(SinglePhaseSupply device, Simulation simulation, ArraySegment<PinSim> pins) : base(device, simulation, pins)
        {
            var ps = simulation.AddPowerSource(device,
                new Potential(Volt.Zero, device.Frequency, Phase.Zero, device),
                new Potential(device.Voltage, device.Frequency, Phase.Zero, device));
            simulation.Connect(pins[0], ps.Pins[0]);
            simulation.Connect(pins[1], ps.Pins[1]);
        }

        internal override void Update() { }
        internal override void Validate() { }
    }
}
