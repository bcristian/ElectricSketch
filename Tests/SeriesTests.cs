using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class SeriesTests
    {
        [Test]
        public void TwoLamps()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            sch.Connect(ps.L, l1.A1);
            sch.Connect(l1.A2, l2.A1);
            sch.Connect(l2.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void TwoLampsAndSwitch()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var sw = new NpstSwitch(1);
            sch.AddDevice(sw);

            sch.Connect(ps.L, l1.A1);
            sch.Connect(l1.A2, sw.A(0));
            sch.Connect(sw.B(0), l2.A1);
            sch.Connect(l2.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null);
            sw.Sim(sim).Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void ThreeLamps()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);

            sch.Connect(ps.L, l1.A1);
            sch.Connect(l1.A2, l2.A1);
            sch.Connect(l2.A2, l3.A1);
            sch.Connect(l3.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void FourLamps()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);
            var l4 = new Lamp();
            sch.AddDevice(l4);

            sch.Connect(ps.L, l1.A1);
            sch.Connect(l1.A2, l2.A1);
            sch.Connect(l2.A2, l3.A1);
            sch.Connect(l3.A2, l4.A1);
            sch.Connect(l4.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void FourLamps2P2S()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);
            var l4 = new Lamp();
            sch.AddDevice(l4);

            sch.Connect(ps.L, l1.A1);
            sch.Connect(ps.L, l2.A1);
            sch.Connect(l1.A2, l2.A2);
            sch.Connect(l1.A2, l3.A1);
            sch.Connect(l1.A2, l4.A1);
            sch.Connect(l3.A2, ps.N);
            sch.Connect(l4.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }

        [Test]
        public void TwoLampsInParallelWithSwitch()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            var sw = new NpstSwitch(1);
            sch.AddDevice(sw);

            sch.Connect(ps.N, l1.A1);
            sch.Connect(ps.N, l2.A1);
            sch.Connect(l1.A2, sw.A(0));
            sch.Connect(l2.A2, sw.A(0));
            sch.Connect(sw.B(0), ps.L);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null);

            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);
            var ssw = sw.Sim(sim);

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);

            ssw.Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);

            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);

            ssw.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);
        }

        [Test]
        public void ThreePhaseTwoLamps()
        {
            var sch = new Schematic();

            var ps = sch.AddDevice(new ThreePhaseSupply());
            var l1 = sch.AddDevice(new Lamp());
            var l2 = sch.AddDevice(new Lamp());
            var sw = sch.AddDevice(new NpstSwitch(1));
            sw.Closed = true;

            sch.Connect(ps.R, l1.A1);
            sch.Connect(ps.S, l2.A1);
            sch.Connect(l1.A2, l2.A2);
            sch.Connect(l1.A2, sw.A(0));
            sch.Connect(sw.B(0), ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null);

            sw.Sim(sim).Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.That(sim.Error != null && sim.Error.Code == ErrorCode.SeriesConnection);
        }
    }
}
