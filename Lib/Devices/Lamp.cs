using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    public class Lamp : SinglePhaseConsumer<SinglePhaseConsumerSim>
    {
        public override string DefaultNamePrefix => "L";

        public Lamp() : this(null) { }
        public Lamp(string name) : base((device, simulation, pins) => new SinglePhaseConsumerSim((ISinglePhaseConsumer)device, device, simulation, pins), name) { }
    }
}
