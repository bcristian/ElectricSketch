using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class Ringing
    {
        [Test]
        public void Buzzer()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new SinglePhaseSupply());
            var rl = sch.AddDevice(new Relay(1));

            sch.Connect(ps.N, rl.A2);
            sch.Connect(ps.L, rl.Common(0));
            sch.Connect(rl.NC(0), rl.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.Ringing);
        }
    }
}
