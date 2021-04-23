using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class UnconnectedElements
    {
        [Test]
        public void WithoutPower()
        {
            var sch = new Schematic();
            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(!sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.IsTrue(sl1PA == null);
            Assert.IsTrue(sl1PB == null);

            var sl2 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l2)];
            Assert.That(!sl2.IsOn);
            var sl2PA = sim.GetPotential(sl2.Pins[0]);
            var sl2PB = sim.GetPotential(sl2.Pins[1]);
            Assert.IsTrue(sl2PA == null);
            Assert.IsTrue(sl2PB == null);
        }

        [Test]
        public void WithPower()
        {
            var sch = new Schematic();
            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(!sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.IsTrue(sl1PA == null);
            Assert.IsTrue(sl1PB == null);

            var sl2 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l2)];
            Assert.That(!sl2.IsOn);
            var sl2PA = sim.GetPotential(sl2.Pins[0]);
            var sl2PB = sim.GetPotential(sl2.Pins[1]);
            Assert.IsTrue(sl2PA == null);
            Assert.IsTrue(sl2PB == null);
        }
    }
}
