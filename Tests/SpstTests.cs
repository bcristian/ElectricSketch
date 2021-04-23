using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class SpstTests
    {
        [Test]
        public void OneLampOnLive()
        {
            OneLampTest(true);
        }

        [Test]
        public void OneLampOnNeutral()
        {
            OneLampTest(false);
        }

        void OneLampTest(bool switchOnLive)
        {
            var sch = new Schematic();

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var s1 = new NpstSwitch(1);
            sch.AddDevice(s1);
            s1.Closed = false;

            if (switchOnLive)
            {
                sch.Connect(p.L, s1.A(0));
                sch.Connect(s1.B(0), l1.A2);
                sch.Connect(p.N, l1.A1);
            }
            else
            {
                sch.Connect(p.L, l1.A2);
                sch.Connect(p.N, s1.A(0));
                sch.Connect(s1.B(0), l1.A1);
            }

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error == null, "Simulation failed on creation");

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(!sl1.IsOn);
            var sl1PA = sim.GetPotential(sl1.Pins[0]);
            var sl1PB = sim.GetPotential(sl1.Pins[1]);
            if (switchOnLive)
            {
                Assert.That(!sl1PB.HasValue);
                Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == Volt.Zero);
            }
            else
            {
                Assert.That(!sl1PA.HasValue);
                Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);
            }

            var ss1 = (NpstSwitchSim)sim.Devices[sch.Devices.IndexOf(s1)];
            ss1.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            sl1PA = sim.GetPotential(sl1.Pins[0]);
            sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == Volt.Zero);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);

            ss1.Closed = false;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");
            Assert.That(!sl1.IsOn);
            sl1PA = sim.GetPotential(sl1.Pins[0]);
            sl1PB = sim.GetPotential(sl1.Pins[1]);
            if (switchOnLive)
            {
                Assert.That(!sl1PB.HasValue);
                Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == Volt.Zero);
            }
            else
            {
                Assert.That(!sl1PA.HasValue);
                Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);
            }

            ss1.Closed = true;
            sim.Update(TimeSpan.FromSeconds(1));
            Assert.IsTrue(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            sl1PA = sim.GetPotential(sl1.Pins[0]);
            sl1PB = sim.GetPotential(sl1.Pins[1]);
            Assert.That(sl1PA.HasValue && sl1PA.Value.Voltage == Volt.Zero);
            Assert.That(sl1PB.HasValue && sl1PB.Value.Voltage == p.Voltage);
        }

        [Test]
        public void ShortsTheSourceOnePhase()
        {
            // TODO add tests for other sources, and for linking sources.
            var sch = new Schematic();

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var s = new NpstSwitch(1);
            sch.AddDevice(s);

            sch.Connect(p.L, s.A(0));
            sch.Connect(p.N, s.B(0));

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error != null && sim.Error.Code == ErrorCode.DangerousSwitch, "Missed the badly placed switch");
        }

        [Test]
        public void ConnectsDifferentSources1()
        {
            ConnectsDifferentSourcesTest((sch, p1, p2, s) =>
            {
                sch.Connect(p1.L, s.A(0));
                sch.Connect(p2.L, s.B(0));
            });
        }

        [Test]
        public void ConnectsDifferentSources2()
        {
            ConnectsDifferentSourcesTest((sch, p1, p2, s) =>
            {
                sch.Connect(p1.L, s.A(0));
                sch.Connect(p2.N, s.B(0));
            });
        }

        [Test]
        public void ConnectsDifferentSources3()
        {
            ConnectsDifferentSourcesTest((sch, p1, p2, s) =>
            {
                sch.Connect(p1.N, s.A(0));
                sch.Connect(p2.N, s.B(0));
            });
        }

        void ConnectsDifferentSourcesTest(Action<Schematic, SinglePhaseSupply, SinglePhaseSupply, NpstSwitch> connect)
        {
            var sch = new Schematic();

            var p1 = new SinglePhaseSupply();
            sch.AddDevice(p1);

            var p2 = new SinglePhaseSupply();
            sch.AddDevice(p2);

            var s = new NpstSwitch(1);
            sch.AddDevice(s);

            connect(sch, p1, p2, s);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsTrue(sim.Error != null && sim.Error.Code == ErrorCode.DangerousSwitch, "Missed the badly placed switch");
        }

        [Test]
        public void RunInDebugger()
        {
            // TODO put an unconnected switch, test that it is removed by the simulation. Test with one pin connected or none.
        }
    }
}
