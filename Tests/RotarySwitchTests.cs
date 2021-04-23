using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class RotarySwitchTests
    {
        [Test]
        public void TwoLampsSP()
        {
            var sch = new Schematic();

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var sw = new RotarySwitch(2, 1);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);

            sch.Connect(p.N, l1.A2);
            sch.Connect(p.N, l2.A2);

            sch.Connect(p.L, sw.CommonPin(0));

            sch.Connect(sw.PositionPin(0, 0), l1.A1);
            sch.Connect(sw.PositionPin(0, 1), l2.A1);

            sw.Position = 1;

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = (RotarySwitchSim)sim.Devices[sch.Devices.IndexOf(sw)];
            Assert.That(ssw.Position == 1);

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            var sl2 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l2)];
            Assert.That(!sl1.IsOn);
            Assert.That(sl2.IsOn);

            ssw.Position = 0;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            Assert.That(!sl2.IsOn);

            ssw.Position = 1;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(sl2.IsOn);
        }

        [Test]
        public void ThreeLampsSP()
        {
            var sch = new Schematic();

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var sw = new RotarySwitch(3, 1);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);

            sch.Connect(p.N, l1.A2);
            sch.Connect(p.N, l2.A2);
            sch.Connect(p.N, l3.A2);

            sch.Connect(p.L, sw.CommonPin(0));

            sch.Connect(sw.PositionPin(0, 0), l1.A1);
            sch.Connect(sw.PositionPin(0, 1), l2.A1);
            sch.Connect(sw.PositionPin(0, 2), l3.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = (RotarySwitchSim)sim.Devices[sch.Devices.IndexOf(sw)];
            Assert.That(ssw.Position == 0);

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            var sl2 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l2)];
            var sl3 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l3)];
            Assert.That(sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(!sl3.IsOn);

            ssw.Position = 1;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(sl2.IsOn);
            Assert.That(!sl3.IsOn);

            ssw.Position = 2;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(sl3.IsOn);

            ssw.Position = 1; // not a mistake, check that only the latest counts
            ssw.Position = 2;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(sl3.IsOn);

            Assert.Throws<ArgumentException>(() => ssw.Position = 0);
            ssw.Position = 1;
            ssw.Position = 0;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(!sl3.IsOn);
        }


        [Test]
        public void ThreeLampsDP()
        {
            var sch = new Schematic();

            var p = new SinglePhaseSupply();
            sch.AddDevice(p);

            var sw = new RotarySwitch(3, 2);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);
            var l2 = new Lamp();
            sch.AddDevice(l2);
            var l3 = new Lamp();
            sch.AddDevice(l3);

            sch.Connect(p.N, sw.CommonPin(1));

            sch.Connect(sw.PositionPin(1, 0), l1.A2);
            sch.Connect(sw.PositionPin(1, 1), l2.A2);
            sch.Connect(sw.PositionPin(1, 2), l3.A2);

            sch.Connect(p.L, sw.CommonPin(0));

            sch.Connect(sw.PositionPin(0, 0), l1.A1);
            sch.Connect(sw.PositionPin(0, 1), l2.A1);
            sch.Connect(sw.PositionPin(0, 2), l3.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = (RotarySwitchSim)sim.Devices[sch.Devices.IndexOf(sw)];
            Assert.That(ssw.Position == 0);

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            var sl2 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l2)];
            var sl3 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l3)];
            Assert.That(sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(!sl3.IsOn);

            ssw.Position = 1;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(sl2.IsOn);
            Assert.That(!sl3.IsOn);

            ssw.Position = 2;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(sl3.IsOn);

            ssw.Position = 1; // not a mistake, check that only the latest counts
            ssw.Position = 2;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(!sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(sl3.IsOn);

            Assert.Throws<ArgumentException>(() => ssw.Position = 0);
            ssw.Position = 1;
            ssw.Position = 0;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
            Assert.That(!sl2.IsOn);
            Assert.That(!sl3.IsOn);
        }

        [Test]
        public void OneLampsThreePhases()
        {
            var sch = new Schematic();

            var p = new ThreePhaseSupply();
            sch.AddDevice(p);

            var sw = new RotarySwitch(3, 1);
            sch.AddDevice(sw);

            var l1 = new Lamp();
            sch.AddDevice(l1);

            sch.Connect(p.N, l1.A2);

            sch.Connect(l1.A1, sw.CommonPin(0));

            sch.Connect(sw.PositionPin(0, 0), p.R);
            sch.Connect(sw.PositionPin(0, 1), p.S);
            sch.Connect(sw.PositionPin(0, 2), p.T);

            sw.Position = 1;

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.That(sim.Error == null, "Simulation failed on creation");

            var ssw = (RotarySwitchSim)sim.Devices[sch.Devices.IndexOf(sw)];
            Assert.That(ssw.Position == 1);

            var sl1 = (SinglePhaseConsumerSim)sim.Devices[sch.Devices.IndexOf(l1)];
            Assert.That(sl1.IsOn);

            ssw.Position = 0;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);

            ssw.Position = 1;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            ssw.Position = 2;
            sim.Update(TimeSpan.FromMilliseconds(1));
            Assert.That(sim.Error == null, "Simulation failed on update");

            Assert.That(sl1.IsOn);
        }
    }
}
