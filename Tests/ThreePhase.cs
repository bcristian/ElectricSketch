using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class ThreePhase
    {
        [Test]
        public void OneLamp220()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var ps = new ThreePhaseSupply();
            sch.AddDevice(ps);

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.N, l1.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue);
            Assert.AreEqual(ps.Voltage.Value / 1.732f, sl1PA.Value.Voltage.Value, .1);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == Volt.Zero);
        }

        [Test]
        public void OneLamp380()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var ps = new ThreePhaseSupply();
            sch.AddDevice(ps);

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.T, l1.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue);
            Assert.AreEqual(ps.Voltage.Value / 1.732f, sl1PA.Value.Voltage.Value, .1);
            Assert.That(sl1PB.HasValue);
            Assert.AreEqual(ps.Voltage.Value / 1.732f, sl1PB.Value.Voltage.Value, .1);
            Assert.That(Validators.VoltageDifference(sl1PA, sl1PB, out Volt? deltaV) && deltaV.HasValue);
            Assert.AreEqual(ps.Voltage.Value, deltaV.Value.Value, .1);
        }


        [Test]
        public void OneLampOoops()
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);
            l1.Voltage = new Volt(220);

            var ps = new ThreePhaseSupply();
            sch.AddDevice(ps);

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.T, l1.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error != null && sim.Error.Code == ErrorCode.InvalidSupplyVoltage);
        }

        [Test]
        public void TwoLampsStarWithNeutral()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new ThreePhaseSupply());
            var l1 = sch.AddDevice(new Lamp());
            var l2 = sch.AddDevice(new Lamp());

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.S, l2.A1);

            sch.Connect(l1.A2, l2.A2);
            sch.Connect(l1.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.Null(sim.Error);
            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);
            Assert.AreEqual(ps.Voltage.Value / 1.732f, sl1.DetectedVoltage.Value.Value, .1);
            Assert.AreEqual(sl1.DetectedVoltage, sl2.DetectedVoltage);
        }

        [Test]
        public void TwoLampsStarWithoutNeutral()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new ThreePhaseSupply());
            var l1 = sch.AddDevice(new Lamp());
            var l2 = sch.AddDevice(new Lamp());

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.S, l2.A1);

            sch.Connect(l1.A2, l2.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void ThreeLampsStarWithNeutral()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new ThreePhaseSupply());
            var l1 = sch.AddDevice(new Lamp());
            var l2 = sch.AddDevice(new Lamp());
            var l3 = sch.AddDevice(new Lamp());

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.S, l2.A1);
            sch.Connect(ps.T, l3.A1);

            sch.Connect(l1.A2, l2.A2);
            sch.Connect(l1.A2, l3.A2);
            sch.Connect(l1.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.Null(sim.Error);
            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);
            var sl3 = l3.Sim(sim);
            Assert.AreEqual(ps.Voltage.Value / 1.732f, sl1.DetectedVoltage.Value.Value, .1);
            Assert.AreEqual(sl1.DetectedVoltage, sl2.DetectedVoltage);
            Assert.AreEqual(sl1.DetectedVoltage, sl3.DetectedVoltage);
        }

        [Test]
        public void ThreeLampsStarWithoutNeutral()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new ThreePhaseSupply());
            var l1 = sch.AddDevice(new Lamp());
            var l2 = sch.AddDevice(new Lamp());
            var l3 = sch.AddDevice(new Lamp());

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.S, l2.A1);
            sch.Connect(ps.T, l3.A1);

            sch.Connect(l1.A2, l2.A2);
            sch.Connect(l1.A2, l3.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void ThreeLampsDelta()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new ThreePhaseSupply());
            var l1 = sch.AddDevice(new Lamp());
            var l2 = sch.AddDevice(new Lamp());
            var l3 = sch.AddDevice(new Lamp());

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.S, l1.A2);
            sch.Connect(ps.S, l2.A1);
            sch.Connect(ps.T, l2.A2);
            sch.Connect(ps.T, l3.A1);
            sch.Connect(ps.R, l3.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.Null(sim.Error);
            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);
            var sl3 = l3.Sim(sim);
            Assert.AreEqual(ps.Voltage.Value, sl1.DetectedVoltage.Value.Value, .1);
            Assert.AreEqual(sl1.DetectedVoltage, sl2.DetectedVoltage);
            Assert.AreEqual(sl1.DetectedVoltage, sl3.DetectedVoltage);
        }
    }
}
