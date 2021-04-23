using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class RelayTests_Simple
    {
        [Test]
        public void Contactor1()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var rl = new Relay(3);
            sch.AddDevice(rl);
            var on = new NpstSwitch(1) { Closed = false, Momentary = true };
            sch.AddDevice(on);
            var off = new NpstSwitch(1) { Closed = true, Momentary = true };
            sch.AddDevice(off);

            var l = new Lamp();
            sch.AddDevice(l);

            sch.Connect(ps.L, rl.Common(0));
            sch.Connect(ps.N, rl.Common(1));
            sch.Connect(rl.NO(0), l.A1);
            sch.Connect(rl.NO(1), l.A2);

            sch.Connect(ps.N, rl.A2);
            sch.Connect(ps.L, rl.Common(2));
            sch.Connect(rl.NO(2), off.A(0));
            sch.Connect(off.B(0), rl.A1);
            sch.Connect(rl.Common(2), on.A(0));
            sch.Connect(rl.NO(2), on.B(0));

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null);

            var sOn = on.Sim(sim);
            var sOff = off.Sim(sim);
            var sl = l.Sim(sim);
            //var srl = rl.Sim(sim);

            Assert.That(!sl.IsOn);

            sOn.Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl.IsOn);

            sOn.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl.IsOn);

            sOn.Closed = true;
            sOff.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(!sl.IsOn);

            sOn.Closed = false;
            sOff.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(!sl.IsOn);

            sOn.Closed = true;
            sOff.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(!sl.IsOn);

            sOn.Closed = true;
            sOff.Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl.IsOn);

            sOn.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl.IsOn);
        }

        [Test]
        public void Contactor2()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var rl = new Relay(2);
            sch.AddDevice(rl);
            var on = new NpstSwitch(1) { Closed = false, Momentary = true };
            sch.AddDevice(on);
            var off = new NpstSwitch(1) { Closed = true, Momentary = true };
            sch.AddDevice(off);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            var jN = new Junction();
            sch.AddDevice(jN);

            var j1 = new Junction();
            sch.AddDevice(j1);

            var j2 = new Junction();
            sch.AddDevice(j2);

            sch.Connect(jN.Pin, ps.N);
            sch.Connect(jN.Pin, l1.A2);
            sch.Connect(jN.Pin, rl.A2);

            sch.Connect(ps.L, rl.Common(0));
            sch.Connect(rl.NO(0), l1.A1);

            sch.Connect(ps.L, rl.Common(1));
            sch.Connect(on.A(0), rl.Common(1));
            sch.Connect(on.B(0), rl.NO(1));
            sch.Connect(j1.Pin, rl.NO(1));

            sch.Connect(j1.Pin, off.A(0));
            sch.Connect(off.B(0), j2.Pin);

            sch.Connect(j2.Pin, rl.A1);

            sch.Connect(rl.A1, l2.A1);
            sch.Connect(rl.A2, l2.A2);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null);

            var sOn = on.Sim(sim);
            var sOff = off.Sim(sim);
            var sl1 = l1.Sim(sim);
            var sl2 = l2.Sim(sim);
            //var srl = rl.Sim(sim);

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);

            sOn.Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);

            sOn.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);

            sOn.Closed = true;
            sOff.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);

            sOn.Closed = false;
            sOff.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);

            sOn.Closed = true;
            sOff.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);

            sOn.Closed = true;
            sOff.Closed = true;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);

            sOn.Closed = false;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null);
            Assert.That(sl1.IsOn);
            Assert.That(sl2.IsOn);
        }
    }
}
