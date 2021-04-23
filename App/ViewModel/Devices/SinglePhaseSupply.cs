using ElectricLib.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Media;
using Undo;

namespace ElectricSketch.ViewModel.Devices
{
    public sealed class SinglePhaseSupply : TypedDevice<Model.Devices.SinglePhaseSupply, ElectricLib.Devices.SinglePhaseSupply, SinglePhaseSupplySim>
    {
        public SinglePhaseSupply(Model.Devices.SinglePhaseSupply m) : base(m)
        {
            Pins[0].Offset = new Point(30, 20);
            Pins[1].Offset = new Point(30, -20);

            OriginOffset = new Point(25, 40);

            Voltage = new Voltage(this, nameof(Voltage),
                () => functional.Voltage.Value,
                (v) => functional.Voltage = new ElectricLib.Volt(v));
            Frequency = new Frequency(this, nameof(Frequency),
                () => functional.Frequency.Value,
                (v) => functional.Frequency = new ElectricLib.Hz(v));

            Voltage.Value = m.Voltage;
            Frequency.Value = m.Frequency;
        }

        protected override void FillModel(Model.Devices.SinglePhaseSupply m)
        {
            m.Voltage = Voltage.Value;
            m.Frequency = Frequency.Value;
        }

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
