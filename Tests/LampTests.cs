using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class LampTests
    {
        [Test]
        public void OneLamp()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            sch.Connect(p.L, l1.A2);
            sch.Connect(p.N, l1.A1);

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

        [Test]
        public void BothPinsToPower()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            sch.Connect(p.L, l1.A2);
            sch.Connect(p.L, l1.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(!sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == p.Voltage);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            sl1PA = sim.GetPotential(sl1.Pins[0]);
            sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == p.Voltage);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);
        }

        [Test]
        public void TwoLampsOneNotConnected()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            sch.Connect(p.L, l1.A1);
            sch.Connect(p.L, l2.A2);
            sch.Connect(p.N, l1.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == p.Voltage);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == Volt.Zero);

            var sl2 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l2)];
            var sl2PA = sim.GetPotential(sl2.Pins[0]);
            var sl2PB = sim.GetPotential(sl2.Pins[1]);
            Assert.IsTrue(sl2PA == null);
            Assert.That(sl2PB.HasValue && sl2PB.Value.Voltage == p.Voltage);
        }

        [Test]
        public void TwoLampsInParallel()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            sch.Connect(p.L, l1.A1);
            sch.Connect(p.L, l2.A2);
            sch.Connect(p.N, l1.A2);
            sch.Connect(p.N, l2.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = l1.Sim(sim);
            Assert.That(sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == p.Voltage);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == Volt.Zero);

            var sl2 = l2.Sim(sim);
            Assert.That(sl2.IsOn);
            var sl2PA = sim.GetPotential(sl2.Pins[0]);
            var sl2PB = sim.GetPotential(sl2.Pins[1]);
            Assert.That(sl2PA.HasValue && sl2PA.Value.Voltage == Volt.Zero);
            Assert.That(sl2PB.HasValue && sl2PB.Value.Voltage == p.Voltage);
        }
    }
}
