using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class JunctionTests
    {
        [Test]
        public void OneLamp()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var j1 = new Junction();
            sch.AddDevice(j1);
            var j2 = new Junction();
            sch.AddDevice(j2);

            sch.Connect(p.L, j1.Pin);
            sch.Connect(j1.Pin, l1.A2);
            sch.Connect(p.N, j2.Pin);
            sch.Connect(j2.Pin, l1.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == Volt.Zero);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            sl1PA = sim.GetPotential(sl1.Pins[0]);
            sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == Volt.Zero);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);
        }
    }
}
