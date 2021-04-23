using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class NpstTests
    {
        [Test]
        public void OneLamp()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var sw = new NpstSwitch(2);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);

            sch.Connect(ps.N, sw.A(0));
            sch.Connect(ps.L, sw.A(1));

            sch.Connect(sw.B(0), l1.A1);
            sch.Connect(sw.B(1), l1.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = sw.Sim(sim);
            var sl1 = l1.Sim(sim);

            Assert.That(!sl1.IsOn);

            ssw.Closed = true;

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);

            ssw.Closed = false;

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
        }

        [Test]
        public void TwoLamps()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var sw = new NpstSwitch(2);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            sch.Connect(ps.N, l1.A2);
            sch.Connect(ps.N, l2.A2);

            sch.Connect(ps.L, sw.A(0));
            sch.Connect(ps.L, sw.A(1));

            sch.Connect(sw.B(0), l1.A1);
            sch.Connect(sw.B(1), l2.A1);

            sw.Closed = true;

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = sw.Sim(sim);
            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);

            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);

            ssw.Closed = false;

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);

            ssw.Closed = true;

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);
        }

        [Test]
        public void OneLampDoublePass()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var sw = new NpstSwitch(2);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);

            sch.Connect(ps.N, l1.A2);
            sch.Connect(ps.L, sw.A(0));
            sch.Connect(sw.B(0), sw.A(1));
            sch.Connect(sw.B(1), l1.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = sw.Sim(sim);
            var sl1 = l1.Sim(sim);

            Assert.That(!sl1.IsOn);

            ssw.Closed = true;

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);

            ssw.Closed = false;

            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
        }
    }
}
