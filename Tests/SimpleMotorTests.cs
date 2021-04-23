using ElectricLib;
using ElectricLib.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Electric_Sketch_Tests
{
    public class SimpleMotorTests
    {
        [Test]
        public void AC()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply();
            sch.AddDevice(ps);

            var m = new SimpleMotor();
            sch.AddDevice(m);

            var sw = new NpstSwitch();
            sch.AddDevice(sw);

            sch.Connect(ps.L, sw.A(0));
            sch.Connect(sw.B(0), m.A1);
            sch.Connect(m.A2, ps.N);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var sm = m.Sim(sim);
            var ssw = sw.Sim(sim);
            Assert.AreEqual(TurnDirection.None, sm.TurnDirection);

            ssw.Closed = true;
            sim.Update(TimeSpan.Zero);
            Assert.AreEqual(TurnDirection.CW, sm.TurnDirection);
        }

        [Test]
        public void DC()
        {
            var sch = new Schematic();

            var ps = new SinglePhaseSupply(new Volt(12), Hz.Zero);
            sch.AddDevice(ps);

            var m = new SimpleMotor();
            sch.AddDevice(m);

            var sw = new RotarySwitch(3, 2)
            {
                Position = 1,
                AllowArbitraryPositionChange = true
            };
            sch.AddDevice(sw);

            sch.Connect(ps.L, sw.CommonPin(0));
            sch.Connect(ps.N, sw.CommonPin(1));
            sch.Connect(sw.PositionPin(0, 0), m.A1);
            sch.Connect(sw.PositionPin(1, 0), m.A2);
            sch.Connect(sw.PositionPin(0, 2), m.A2);
            sch.Connect(sw.PositionPin(1, 2), m.A1);

            var sim = new Simulation(sch, DateTime.UtcNow);
            Assert.IsNull(sim.Error);

            var sm = m.Sim(sim);
            var ssw = sw.Sim(sim);
            Assert.AreEqual(TurnDirection.None, sm.TurnDirection);

            ssw.Position = 0;
            sim.Update(TimeSpan.Zero);
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CW, sm.TurnDirection);

            ssw.Position = 2;
            sim.Update(TimeSpan.Zero);
            Assert.IsNull(sim.Error);
            Assert.AreEqual(TurnDirection.CCW, sm.TurnDirection);
        }
    }
}
