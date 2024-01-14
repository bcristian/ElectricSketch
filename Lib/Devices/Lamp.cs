using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    public class Lamp(string name) : SinglePhaseConsumer<SinglePhaseConsumerSim>((device, simulation, pins) => new SinglePhaseConsumerSim((ISinglePhaseConsumer)device, device, simulation, pins), name)
    {
        public override string DefaultNamePrefix => "L";

        public Lamp() : this(null) { }
    }
}
