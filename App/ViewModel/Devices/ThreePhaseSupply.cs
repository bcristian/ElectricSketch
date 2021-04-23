using ElectricLib.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Media;

namespace ElectricSketch.ViewModel.Devices
{
    public class ThreePhaseSupply : TypedDevice<Model.Devices.ThreePhaseSupply, ElectricLib.Devices.ThreePhaseSupply, ThreePhaseSupplySim>
    {
        public ThreePhaseSupply(Model.Devices.ThreePhaseSupply m) : base(m)
        {
            Pins[0].Offset = new Point(30, 40);
            Pins[1].Offset = new Point(30, -40);
            Pins[2].Offset = new Point(30, -20);
            Pins[3].Offset = new Point(30, 0);

            OriginOffset = new Point(25, 60);

            Voltage = new Voltage(this, nameof(Voltage),
                () => functional.Voltage.Value,
                (v) => functional.Voltage = new ElectricLib.Volt(v));
            Frequency = new Frequency(this, nameof(Frequency),
                () => functional.Frequency.Value,
                (v) => functional.Frequency = new ElectricLib.Hz(v));

            Voltage.Value = m.Voltage;
            Frequency.Value = m.Frequency;
        }

        protected override void FillModel(Model.Devices.ThreePhaseSupply m)
        {
            m.Voltage = Voltage.Value;
            m.Frequency = Frequency.Value;
        }

        // TODO validate values

        /// <summary>
        /// Nominal supply voltage.
        /// </summary>
        public Voltage Voltage { get; }

        /// <summary>
        /// Nominal supply frequency. Zero means DC.
        /// </summary>
        public Frequency Frequency { get; }
    }
}
