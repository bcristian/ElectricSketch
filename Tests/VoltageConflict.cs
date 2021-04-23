using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class VoltageConflict
    {
        [Test]
        public void DirectShort()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);
            sch.Connect(ps.L, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.VoltageConflict);
        }

        [Test]
        public void ShortThroughSwitch()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);
            var sw = new NpstSwitch(1);
            sch.AddDevice(sw);

            sw.AllowIncompatiblePotentials = true;
            sch.Connect(ps.L, sw.A(0));
            sch.Connect(ps.N, sw.B(0));

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null);
            sw.Sim(sim).Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.VoltageConflict);
        }
    }
}
