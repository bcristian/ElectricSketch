using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// Covers transformers and PSU's. Something that supplies power if itself powered.
    /// </summary>
    public class Transformer : Device<TransformerSim>, ISinglePhaseConsumer
    {
        public override string DefaultNamePrefix => "TR";

        public Transformer() : this(null) { }
        public Transformer(string name = null) : this(new Volt(12), new Hz(50), name) { }

        public Transformer(Volt outVoltage, Hz outFrequency, string name = null) : base(name)
        {
            pins.Add(A1 = new Pin(this, "A1"));
            pins.Add(A2 = new Pin(this, "A2"));

            pins.Add(B1 = new Pin(this, "B1"));
            pins.Add(B2 = new Pin(this, "B2"));

            OutVoltage = outVoltage;
            OutFrequency = outFrequency;
        }

        /// <summary>
        /// Input pins.
        /// </summary>
        public Pin A1 { get; }
        public Pin A2 { get; }

        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public Volt? InVoltage { get; set; }
        Volt? ISinglePhaseConsumer.Voltage => InVoltage;

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public Hz? InFrequency { get; set; }
        Hz? ISinglePhaseConsumer.Frequency => InFrequency;

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public Hz FrequencyTolerance { get; set; } = new Hz(15);

        /// <summary>
        /// If true and this is a DC device (<see cref="Frequency"/> == 0), the potential of pin <see cref="A1"/> must be greater than that of pin <see cref="A2"/>.
        /// </summary>
        public bool PolarityMatters { get; set; }



        /// <summary>
        /// Output pins.
        /// </summary>
        public Pin B1 { get; }
        public Pin B2 { get; }

        /// <summary>
        /// Output voltage.
        /// </summary>
        public Volt OutVoltage { get; set; } = new Volt(12);

        /// <summary>
        /// Output frequency.
        /// </summary>
        public Hz OutFrequency { get; set; } = new Hz(50);


        internal override TransformerSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new TransformerSim(this, sim, pins);
    }

    public class TransformerSim : DeviceSimulation
    {
        internal TransformerSim(Transformer device, Simulation simulation, ArraySegment<PinSim> pins) : base(device, simulation, pins)
        {
            ps = simulation.AddPowerSource(device,
                new Potential(Volt.Zero, device.OutFrequency, Phase.Zero, device),
                new Potential(device.OutVoltage, device.OutFrequency, Phase.Zero, device));

            sw = new Simulation.Switch[2];
            sw[0] = simulation.AddSwitch(device, false, true);
            sw[1] = simulation.AddSwitch(device, false, true);

            cons = new SinglePhaseConsumerSim(device, device, simulation, pins.Slice(0, 2));

            simulation.Connect(ps.Pins[0], sw[0].A);
            simulation.Connect(ps.Pins[1], sw[1].A);
            simulation.Connect(sw[0].B, pins[2]);
            simulation.Connect(sw[1].B, pins[3]);
        }

        readonly Simulation.PowerSource ps;
        readonly Simulation.Switch[] sw;
        readonly SinglePhaseConsumerSim cons;

        internal override void Update()
        {
            cons.Update();
            sw[0].Closed = sw[1].Closed = cons.IsOn;
            sw[0].Update();
            sw[1].Update();
            ps.Update();
        }

        internal override void Validate()
        {
            cons.Validate();
            sw[0].Validate();
            sw[1].Validate();
            ps.Validate();
        }

        /// <summary>
        /// True if the device has adequate supply power.
        /// </summary>
        public bool IsOn => cons.IsOn;

        /// <summary>
        /// Detected supply voltage, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. 220V at some point and 380V at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Volt? DetectedVoltage => cons.DetectedVoltage;

        /// <summary>
        /// Detected supply frequency, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. AC at some point and DC at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Hz? DetectedFrequency => cons.DetectedFrequency;
    }
}
