using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class CamSwitchTests
    {
        [Test]
        public void TwoCircuits()
        {
            var sch = new Schematic();

            var ps1 = new SinglePhaseSupply();
            sch.AddDevice(ps1);

            var ps2 = new SinglePhaseSupply();
            sch.AddDevice(ps2);

            var l1 = new Lamp();
            sch.AddDevice(l1);

            var l2 = new Lamp();
            sch.AddDevice(l2);

            var sw = new CamSwitch(3, 2, new bool[,] { { false, true, false }, { false, false, true } });
            sch.AddDevice(sw);

            sch.Connect(ps1.N, l1.A2);
            sch.Connect(ps1.L, sw.Pins[0]);
            sch.Connect(sw.Pins[1], l1.A1);

            sch.Connect(ps2.N, l2.A2);
            sch.Connect(ps2.L, sw.Pins[2]);
            sch.Connect(sw.Pins[3], l2.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var l1s = l1.Sim(sim);
            var l2s = l2.Sim(sim);
            var sws = sw.Sim(sim);

            Assert.IsFalse(l1s.IsOn);
            Assert.IsFalse(l2s.IsOn);

            sws.Position = 1;
            sim.Update(TimeSpan.FromSeconds(0));

            Assert.IsTrue(l1s.IsOn);
            Assert.IsFalse(l2s.IsOn);

            sws.Position = 2;
            sim.Update(TimeSpan.FromSeconds(0));

            Assert.IsFalse(l1s.IsOn);
            Assert.IsTrue(l2s.IsOn);
        }
    }
}
